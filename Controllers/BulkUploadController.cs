using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ebook_backend_api.Data;
using ebook_backend_api.Models;
using Microsoft.EntityFrameworkCore;

namespace ebook_backend_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BulkUploadController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IServiceProvider _serviceProvider;

        private static readonly ConcurrentDictionary<string, BulkSession> _sessions = new();
        private static readonly string[] AllowedExts = new[] { ".pdf", ".epub", ".txt" };
        private const string VendorUploadsRoot = "/mnt/books/VendorUploads";

        public BulkUploadController(AppDbContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }

        // ============================================
        // 1) CREATE SESSION
        //    Called once when vendor picks a folder
        //    Returns a sessionId used for all subsequent batches
        // ============================================
        [HttpPost("session/start")]
        public async Task<IActionResult> StartSession([FromBody] StartSessionRequest req)
        {
            if (req == null || req.VendorId <= 0)
                return BadRequest(new { success = false, message = "Invalid request" });

            if (req.CategoryId > 0)
            {
                var cat = await _context.Categories.FindAsync(req.CategoryId);
                if (cat == null)
                    return BadRequest(new { success = false, message = "Category not found" });
            }

            var session = new BulkSession
            {
                SessionId = Guid.NewGuid().ToString("N"),
                VendorId = req.VendorId,
                CategoryId = req.CategoryId,
                Price = req.Price,
                Status = "running",
                StartedAt = DateTime.UtcNow
            };

            _sessions[session.SessionId] = session;

            return Ok(new { success = true, sessionId = session.SessionId });
        }

        // ============================================
        // 2) UPLOAD BATCH
        //    Called for each batch of files (10 per batch)
        // ============================================
        [RequestSizeLimit(500_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 500_000_000)]
        [HttpPost("session/{sessionId}/upload-batch")]
        public async Task<IActionResult> UploadBatch(
            string sessionId,
            [FromForm] List<IFormFile> files,
            [FromForm] string relativePathsJson) // JSON array matching files order
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return NotFound(new { success = false, message = "Session not found" });

            if (session.CancelRequested)
                return Ok(new { success = true, cancelled = true, succeeded = 0, failed = 0 });

            // Wait if paused
            while (session.PauseRequested && !session.CancelRequested)
            {
                session.Status = "paused";
                session.PauseSignal.WaitOne(500);
            }
            if (session.CancelRequested)
                return Ok(new { success = true, cancelled = true, succeeded = 0, failed = 0 });
            session.Status = "running";
            session.PauseSignal.Reset();

            if (files == null || files.Count == 0)
                return Ok(new { success = true, succeeded = 0, failed = 0 });

            // Parse relative paths from client
            List<string> relativePaths = new();
            try
            {
                relativePaths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(relativePathsJson) ?? new List<string>();
            }
            catch { }

            var vendorFolder = Path.Combine(VendorUploadsRoot, $"Vendor_{session.VendorId}");
            Directory.CreateDirectory(vendorFolder);

            int succeeded = 0, failed = 0;
            var errors = new List<string>();

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            for (int i = 0; i < files.Count; i++)
            {
                if (session.CancelRequested) break;

                var f = files[i];
                var relPath = i < relativePaths.Count ? relativePaths[i] : f.FileName;

                // Update current file for UI
                session.CurrentFile = f.FileName;

                try
                {
                    var ext = Path.GetExtension(f.FileName).ToLower();
                    if (!AllowedExts.Contains(ext))
                    {
                        failed++;
                        errors.Add($"{f.FileName}: unsupported extension");
                        session.FailedFiles++;
                        session.ProcessedFiles++;
                        continue;
                    }

                    // Clean filename
                    var origName = Path.GetFileName(f.FileName);
                    var safeName = System.Text.RegularExpressions.Regex
                        .Replace(origName, @"[^a-zA-Z0-9.\s\-_]", "").Replace(" ", "_");

                    var finalName = safeName;
                    var counter = 1;
                    var nameExt = Path.GetExtension(safeName);
                    var noExt = Path.GetFileNameWithoutExtension(safeName);
                    while (System.IO.File.Exists(Path.Combine(vendorFolder, finalName)))
                        finalName = $"{noExt}_{counter++}{nameExt}";

                    var destPath = Path.Combine(vendorFolder, finalName);

                    // Save file
                    using (var stream = new FileStream(destPath, FileMode.Create))
                        await f.CopyToAsync(stream);

                    // Determine category from folder names if auto-category mode
                    int? finalCategoryId = session.CategoryId > 0 ? session.CategoryId : (int?)null;

                    if (session.CategoryId <= 0 && !string.IsNullOrEmpty(relPath))
                    {
                        // Auto-category: use folder names from relPath
                        var relDir = Path.GetDirectoryName(relPath)?.Replace('\\', '/') ?? "";
                        var parts = relDir.Split('/', StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length >= 1)
                        {
                            var catName = parts.Length >= 2 ? parts[parts.Length - 2] : parts[parts.Length - 1];
                            var subName = parts.Length >= 2 ? parts[parts.Length - 1] : "";

                            var rootCat = await db.Categories
                                .FirstOrDefaultAsync(c => c.CategoryName == catName && c.ParentId == null);
                            if (rootCat == null)
                            {
                                rootCat = new Category { CategoryName = catName, ParentId = null, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                                db.Categories.Add(rootCat);
                                await db.SaveChangesAsync();
                            }

                            finalCategoryId = rootCat.Id;

                            if (!string.IsNullOrEmpty(subName) && subName != catName)
                            {
                                var subCat = await db.Categories
                                    .FirstOrDefaultAsync(c => c.CategoryName == subName && c.ParentId == rootCat.Id);
                                if (subCat == null)
                                {
                                    subCat = new Category { CategoryName = subName, ParentId = rootCat.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                                    db.Categories.Add(subCat);
                                    await db.SaveChangesAsync();
                                }
                                finalCategoryId = subCat.Id;
                            }
                        }
                    }

                    // Insert book row
                    var book = new Book
                    {
                        VendorId = session.VendorId,
                        Title = Path.GetFileNameWithoutExtension(f.FileName),
                        Price = session.Price,
                        CategoryId = finalCategoryId,
                        BookPath = finalName,
                        BookCoverPath = "",
                        CreatedAt = DateTime.UtcNow
                    };
                    db.Books.Add(book);
                    await db.SaveChangesAsync();

                    succeeded++;
                    session.SucceededFiles++;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"{f.FileName}: {ex.Message}");
                    session.FailedFiles++;
                }

                session.ProcessedFiles++;
            }

            return Ok(new { success = true, succeeded, failed, errors = errors.Take(10).ToList() });
        }

        // ============================================
        // 3) FINISH SESSION
        // ============================================
        [HttpPost("session/{sessionId}/finish")]
        public IActionResult FinishSession(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return NotFound();
            session.Status = "completed";
            session.CompletedAt = DateTime.UtcNow;
            return Ok(new { success = true });
        }

        // ============================================
        // 4) PAUSE / RESUME / CANCEL
        // ============================================
        [HttpPost("session/{sessionId}/pause")]
        public IActionResult Pause(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var s)) return NotFound();
            s.PauseRequested = true;
            return Ok(new { success = true });
        }

        [HttpPost("session/{sessionId}/resume")]
        public IActionResult Resume(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var s)) return NotFound();
            s.PauseRequested = false;
            s.PauseSignal.Set();
            return Ok(new { success = true });
        }

        [HttpPost("session/{sessionId}/cancel")]
        public IActionResult Cancel(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var s)) return NotFound();
            s.CancelRequested = true;
            s.PauseSignal.Set();
            s.Status = "cancelled";
            return Ok(new { success = true });
        }

        // ============================================
        // 5) STATUS
        // ============================================
        [HttpGet("session/{sessionId}/status")]
        public IActionResult Status(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var s)) return NotFound();
            return Ok(new
            {
                success = true,
                sessionId = s.SessionId,
                status = s.Status,
                processed = s.ProcessedFiles,
                succeeded = s.SucceededFiles,
                failed = s.FailedFiles,
                currentFile = s.CurrentFile,
                totalFiles = s.TotalFiles,
                startedAt = s.StartedAt,
                completedAt = s.CompletedAt
            });
        }

        // ============================================
        // 6) UPDATE TOTAL (called once after folder selection)
        // ============================================
        [HttpPost("session/{sessionId}/set-total")]
        public IActionResult SetTotal(string sessionId, [FromBody] SetTotalRequest req)
        {
            if (!_sessions.TryGetValue(sessionId, out var s)) return NotFound();
            s.TotalFiles = req.TotalFiles;
            return Ok(new { success = true });
        }

        // ============================================
        // DTOs + Session model
        // ============================================
        public class StartSessionRequest
        {
            public int VendorId { get; set; }
            public int CategoryId { get; set; }   // 0 or missing = auto-create from folders
            public decimal Price { get; set; }
        }

        public class SetTotalRequest
        {
            public int TotalFiles { get; set; }
        }

        private class BulkSession
        {
            public string SessionId { get; set; } = "";
            public int VendorId { get; set; }
            public int CategoryId { get; set; }
            public decimal Price { get; set; }
            public int TotalFiles { get; set; }
            public int ProcessedFiles { get; set; }
            public int SucceededFiles { get; set; }
            public int FailedFiles { get; set; }
            public string CurrentFile { get; set; } = "";
            public string Status { get; set; } = "pending";
            public DateTime StartedAt { get; set; }
            public DateTime? CompletedAt { get; set; }

            public volatile bool PauseRequested;
            public volatile bool CancelRequested;
            public ManualResetEvent PauseSignal { get; } = new ManualResetEvent(false);
        }
    }
}

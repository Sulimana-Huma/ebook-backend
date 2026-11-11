using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using System;
using ebook_backend_api.Data;
using ebook_backend_api.Models;
using Microsoft.AspNetCore.Http;
using ebook_backend_api.Dtos;
using System.Linq;
using MySqlConnector;
using Microsoft.EntityFrameworkCore;


// Add this DTO class
public class UpdateHardDriveBookDto
{
    public int Id { get; set; }
    public string? BookName { get; set; }
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
}

namespace ebook_backend_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly AppDbContext _context;

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "BooksController is working", timestamp = DateTime.Now });
        }

        public BooksController(AppDbContext context)
        {
            _context = context;
        }



// ============================================
// NEW: Get all books with category/subcategory NAMES resolved
// ============================================
/*[HttpGet("all-with-names")]
public async Task<IActionResult> GetAllBooksWithNames()
{
    try
    {
        var books = await _context.Books
            .Include(b => b.Category)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                bookId = b.BookId,
                vendorId = b.VendorId,
                title = b.Title,
                price = b.Price,
                category = b.CategoryId,
                categoryName = b.Category != null ? b.Category.CategoryName : null,
                parentCategoryId = b.Category != null ? b.Category.ParentId : null,
                filePath = b.FilePath,
                bookCover = b.BookCover,
                bookPath = b.BookPath,
                bookCoverPath = b.BookCoverPath,
                createdAt = b.CreatedAt,
                description = b.Description
            })
            .ToListAsync();

        return Ok(books);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in GetAllBooksWithNames: {ex.Message}");
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}*/

// ============================================
// COVER LOOKUP for hard-drive books
// ============================================
private const string CoversBasePath = "/data/apps/ocr/bulk/IslamicSites-f301541a/covers";
private const string BooksBasePath = "/mnt/books/BooksCollection";

/// <summary>
/// Given a PDF's full disk path, find its JPG cover under CoversBasePath.
/// Tries several strategies:
///   1. Match relative path exactly (e.g. "IslamicSites/dorar.net/الموسوعة العقدية/book.jpg")
///   2. Match by filename only (walk the tree)
/// </summary>

// Static cache — built once, reused for all lookups
private static readonly object _coverCacheLock = new object();
private static Dictionary<string, string>? _coverCache = null;

private static Dictionary<string, string> BuildCoverCache()
{
    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    if (!Directory.Exists(CoversBasePath))
        return map;

    foreach (var file in Directory.EnumerateFiles(CoversBasePath, "*.jpg", SearchOption.AllDirectories))
    {
        var key = Path.GetFileNameWithoutExtension(file); // e.g. "18_110988"
        if (!string.IsNullOrEmpty(key) && !map.ContainsKey(key))
            map[key] = file;
    }
    // Also scan .JPG (uppercase)
    foreach (var file in Directory.EnumerateFiles(CoversBasePath, "*.JPG", SearchOption.AllDirectories))
    {
        var key = Path.GetFileNameWithoutExtension(file);
        if (!string.IsNullOrEmpty(key) && !map.ContainsKey(key))
            map[key] = file;
    }
    return map;
}

[HttpGet("hard-drive-cover")]
public IActionResult GetHardDriveCover([FromQuery] string filePath)
{
    try
    {
        if (string.IsNullOrEmpty(filePath))
            return BadRequest(new { message = "filePath required" });

        var decoded = Uri.UnescapeDataString(filePath);
        if (!decoded.StartsWith(BooksBasePath))
            return NotFound(new { message = "Not a hard-drive book path" });

        // Build (or reuse) the file-name → cover-path cache
        if (_coverCache == null)
        {
            lock (_coverCacheLock)
            {
                if (_coverCache == null)
                    _coverCache = BuildCoverCache();
            }
        }

        var fileNameNoExt = Path.GetFileNameWithoutExtension(decoded);
        if (!string.IsNullOrEmpty(fileNameNoExt) && _coverCache.TryGetValue(fileNameNoExt, out var coverPath))
        {
            if (System.IO.File.Exists(coverPath))
                return File(System.IO.File.ReadAllBytes(coverPath), "image/jpeg");
        }

        return NotFound(new { message = "Cover not found", fileName = fileNameNoExt });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = ex.Message });
    }
}


[HttpGet("debug-cover-paths")]
public IActionResult DebugCoverPaths([FromQuery] string filePath)
{
    var decoded = Uri.UnescapeDataString(filePath);
    var relPath = decoded.Substring(BooksBasePath.Length).TrimStart('/');
    var relNoExt = Path.ChangeExtension(relPath, null);
    var fileName = Path.GetFileName(relNoExt);
    var relDir = Path.GetDirectoryName(relNoExt) ?? "";

    var segments = relDir.Replace('\\', '/').Trim('/').Split('/');
    var candidates = new List<object>();
    for (int i = 0; i < segments.Length; i++)
    {
        var candidateRelDir = string.Join("/", segments.Skip(i));
        var folderOnDisk = Path.Combine(CoversBasePath, candidateRelDir);
        var fullPath = Path.Combine(folderOnDisk, fileName + ".jpg");
        candidates.Add(new
        {
            candidateRelDir,
            fullPath,
            exists = System.IO.File.Exists(fullPath)
        });
    }

    return Ok(new
    {
        inputPath = decoded,
        booksBasePath = BooksBasePath,
        coversBasePath = CoversBasePath,
        relPath,
        relNoExt,
        fileName,
        relDir,
        segments,
        candidates
    });
}
[HttpGet("all-with-names")]
public async Task<IActionResult> GetAllBooksWithNames()
{
    try
    {
        var books = await _context.Books
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                bookId = b.BookId,
                vendorId = b.VendorId,
                title = b.Title,
                price = b.Price,
                categoryId = b.CategoryId,
                bookPath = b.BookPath,
                bookCoverPath = b.BookCoverPath,
                createdAt = b.CreatedAt,
                description = b.Description
            })
            .ToListAsync();

        // Load categories once for name lookup
        var allCategories = await _context.Categories
            .Select(c => new { c.Id, c.CategoryName, c.ParentId })
            .ToListAsync();
        var catById = allCategories.ToDictionary(c => c.Id, c => c);

        var result = books.Select(b =>
        {
            string categoryName = "Uncategorized";
            string parentCategoryName = "Uncategorized";
            string subCategoryName = "";
            int? parentCategoryId = null;

            if (b.categoryId.HasValue && catById.TryGetValue(b.categoryId.Value, out var cat))
            {
                if (cat.ParentId.HasValue && catById.TryGetValue(cat.ParentId.Value, out var parent))
                {
                    categoryName = parent.CategoryName;
                    parentCategoryName = parent.CategoryName;
                    subCategoryName = cat.CategoryName;
                    parentCategoryId = parent.Id;
                }
                else
                {
                    categoryName = cat.CategoryName;
                    parentCategoryName = cat.CategoryName;
                    subCategoryName = "";
                    parentCategoryId = null;
                }
            }

            return new
            {
                bookId = b.bookId,
                vendorId = b.vendorId,
                title = b.title,
                price = b.price,
                category = b.categoryId,
                categoryId = b.categoryId,
                categoryName = categoryName,
                parentCategory = parentCategoryName,
                parentCategoryId = parentCategoryId,
                subCategory = subCategoryName,

                // Full URLs built here
                filePath = BuildBookUrl(b.vendorId, b.bookPath),
                bookCover = BuildBookUrl(b.vendorId, b.bookCoverPath),

                // Raw filenames (optional)
                bookPath = b.bookPath,
                bookCoverPath = b.bookCoverPath,

                createdAt = b.createdAt,
                description = b.description
            };
        }).ToList();

        return Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in GetAllBooksWithNames: {ex.Message}");
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}

private string BuildBookUrl(int vendorId, string? bookPath)
{
    if (string.IsNullOrEmpty(bookPath)) return "";

    // Hard-drive books store a full disk path starting with "/mnt/" or "/home/"
    if (bookPath.StartsWith("/mnt/") || bookPath.StartsWith("/home/"))
    {
        return $"/api/books/serve-hard-drive-book?filePath={Uri.EscapeDataString(bookPath)}";
    }

    // Vendor-uploaded books store just a filename
    var encoded = Uri.EscapeDataString(bookPath);
    return $"/api/books/serve-file/{vendorId}/{encoded}";
}

// ============================================
// NEW: Update book title + category + subcategory + price
// ============================================
[HttpPut("{id}/update-details")]
public async Task<IActionResult> UpdateBookDetails(int id, [FromBody] UpdateBookDto dto)
{
    try
    {
        Console.WriteLine($"=== UpdateBookDetails for book {id} ===");

        var book = await _context.Books.FindAsync(id);
        if (book == null)
            return NotFound(new { success = false, message = $"Book {id} not found" });

        if (!string.IsNullOrWhiteSpace(dto.Title))
            book.Title = dto.Title.Trim();

        if (dto.CategoryId.HasValue)
        {
            var category = await _context.Categories.FindAsync(dto.CategoryId.Value);
            if (category == null)
                return BadRequest(new { success = false, message = $"Category {dto.CategoryId} not found" });
            book.CategoryId = dto.CategoryId.Value;
        }

        if (dto.Price > 0)
            book.Price = dto.Price;

        await _context.SaveChangesAsync();

        // Reload with category name + build URLs
        var reloaded = await _context.Books
            .Where(b => b.BookId == id)
            .Select(b => new
            {
                b.BookId,
                b.VendorId,
                b.Title,
                b.Price,
                b.CategoryId,
                b.BookPath,
                b.BookCoverPath,
                b.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (reloaded == null)
            return NotFound(new { success = false, message = $"Book {id} not found after update" });

        // Resolve category name
        string categoryName = null;
        int? parentCategoryId = null;
        if (reloaded.CategoryId.HasValue)
        {
            var cat = await _context.Categories.FindAsync(reloaded.CategoryId.Value);
            if (cat != null)
            {
                categoryName = cat.CategoryName;
                parentCategoryId = cat.ParentId;
            }
        }

        var updated = new
        {
            bookId = reloaded.BookId,
            vendorId = reloaded.VendorId,
            title = reloaded.Title,
            price = reloaded.Price,
            category = reloaded.CategoryId,
            categoryName = categoryName,
            parentCategoryId = parentCategoryId,
            filePath = BuildBookUrl(reloaded.VendorId, reloaded.BookPath),
            bookCover = BuildBookUrl(reloaded.VendorId, reloaded.BookCoverPath),
            createdAt = reloaded.CreatedAt
        };

        return Ok(new { success = true, message = "Book updated successfully", data = updated });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}
// GET: api/books/by-category-id/{categoryId}?page=1&pageSize=8
/*[HttpGet("by-category-id/{categoryId}")]
public async Task<IActionResult> GetBooksByCategoryId(
    int categoryId, 
    int page = 1, 
    int pageSize = 8)
{
    try
    {
        var query = _context.Books.Where(b => b.CategoryId == categoryId);
        var total = await query.CountAsync();

        var books = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                bookId = b.BookId,
                vendorId = b.VendorId,
                title = b.Title,
                price = b.Price,
                categoryId = b.CategoryId,
                filePath = b.FilePath,
                bookCover = b.BookCover,
                createdAt = b.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = books,
            pagination = new
            {
                currentPage = page,
                pageSize = pageSize,
                totalCount = total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}*/

[HttpGet("by-category-id/{categoryId}")]
public async Task<IActionResult> GetBooksByCategoryId(
    int categoryId, int page = 1, int pageSize = 8)
{
    try
    {
        var subIds = await _context.Categories
            .Where(c => c.ParentId == categoryId)
            .Select(c => c.Id)
            .ToListAsync();

        var allIds = new List<int> { categoryId };
        allIds.AddRange(subIds);

        var query = _context.Books
            .Where(b => b.CategoryId != null && allIds.Contains(b.CategoryId.Value));

        var total = await query.CountAsync();

        var books = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                b.BookId,
                b.VendorId,
                b.Title,
                b.Price,
                b.CategoryId,
                b.BookPath,
                b.BookCoverPath,
                b.CreatedAt
            })
            .ToListAsync();

        // Build URLs after fetch
        var result = books.Select(b => new
        {
            bookId = b.BookId,
            vendorId = b.VendorId,
            title = b.Title,
            price = b.Price,
            categoryId = b.CategoryId,
            filePath = BuildBookUrl(b.VendorId, b.BookPath),
            bookCover = BuildBookUrl(b.VendorId, b.BookCoverPath),
            bookPath = b.BookPath,
            bookCoverPath = b.BookCoverPath,
            createdAt = b.CreatedAt
        }).ToList();

        return Ok(new
        {
            success = true,
            data = result,
            pagination = new
            {
                currentPage = page,
                pageSize = pageSize,
                totalCount = total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}

// GET: api/books/count-by-category-tree/{rootId}
[HttpGet("count-by-category-tree/{rootId}")]
public async Task<IActionResult> GetCountByCategoryTree(int rootId)
{
    try
    {
        // Root + all direct children
        var subIds = await _context.Categories
            .Where(c => c.ParentId == rootId)
            .Select(c => c.Id)
            .ToListAsync();

        var allIds = new List<int> { rootId };
        allIds.AddRange(subIds);

        var total = await _context.Books
            .Where(b => b.CategoryId != null && allIds.Contains(b.CategoryId.Value))
            .CountAsync();

        return Ok(new { success = true, categoryId = rootId, totalBooks = total });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}
        [HttpGet("debug-book-files/{vendorId}")]
public IActionResult DebugBookFiles(int vendorId)
{
    try
    {
        var books = _context.Books.Where(b => b.VendorId == vendorId).ToList();
        var hardDrivePath = "/mnt/books/VendorUploads";
        var vendorFolder = Path.Combine(hardDrivePath, $"Vendor_{vendorId}");

        var results = new List<object>();

        foreach (var book in books)
        {
            var fileName = Path.GetFileName(book.BookPath ?? "");
            var filePath = Path.Combine(vendorFolder, fileName);

            var coverFileName = string.IsNullOrEmpty(book.BookCoverPath)
                ? null
                : Path.GetFileName(book.BookCoverPath);

            var coverPath = string.IsNullOrEmpty(coverFileName)
                ? null
                : Path.Combine(vendorFolder, coverFileName);

            results.Add(new
            {
                book.BookId,
                book.Title,
                book.BookPath,
                book.BookCoverPath,
                fileExists = System.IO.File.Exists(filePath),
                actualFilePath = filePath,
                coverFileName = coverFileName,
                coverExists = coverPath != null && System.IO.File.Exists(coverPath),
                coverPath = coverPath
            });
        }

        return Ok(new
        {
            vendorId = vendorId,
            vendorFolder = vendorFolder,
            vendorFolderExists = Directory.Exists(vendorFolder),
            books = results
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = ex.Message });
    }
}

        [HttpGet("debug-folder-settings")]
        public IActionResult DebugFolderSettings()
        {
            try
            {
                var settings = _context.BooksFolderSettings.ToList();
                var result = new
                {
                    count = settings.Count,
                    settings = settings.Select(s => new
                    {
                        s.Id,
                        s.Path,
                        s.UpdatedAt,
                        directoryExists = System.IO.Directory.Exists(s.Path)
                    }),
                    message = settings.Count == 0 ? "No folders configured. Please add a folder path to books_folder_settings table." : null
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

[HttpGet("vendor/{vendorId}")]
public IActionResult GetBooksByVendor(int vendorId)
{
    var raw = _context.Books
        .Where(b => b.VendorId == vendorId)
        .OrderByDescending(b => b.CreatedAt)
        .Select(b => new
        {
            b.BookId,
            b.Title,
            b.Price,
            b.CategoryId,
            b.VendorId,
            b.BookPath,
            b.BookCoverPath,
            b.CreatedAt
        })
        .ToList();

    var books = raw.Select(b => new
    {
        b.BookId,
        b.Title,
        b.Price,
        b.CategoryId,
        b.CreatedAt,
        filePath = BuildBookUrl(b.VendorId, b.BookPath),
        bookCover = BuildBookUrl(b.VendorId, b.BookCoverPath)
    }).ToList();

    return Ok(books);
}      

     [HttpGet("count/{vendorId}")]
        public IActionResult GetBookCountByVendor(int vendorId)
        {
            Console.WriteLine($"Fetching book count for vendor ID: {vendorId}");
            var count = _context.Books.Count(b => b.VendorId == vendorId);
            Console.WriteLine($"Found {count} books for vendor ID: {vendorId}");
            return Ok(new { totalBooks = count });
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadBook(
            [FromForm] IFormFile file,
            [FromForm] IFormFile bookCover,
            [FromForm] string title,
            [FromForm] decimal price,
            [FromForm] int subCategoryId,
            [FromForm] int vendorId)
        {
            try
            {
                Console.WriteLine($"=== Upload Started ===");
                Console.WriteLine($"Title: {title}, Price: {price}, VendorId: {vendorId}, SubCategoryId: {subCategoryId}");
                
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "File is empty" });

                var subCategory = await _context.Categories.FindAsync(subCategoryId);
                if (subCategory == null)
                    return BadRequest(new { message = $"Subcategory with ID {subCategoryId} not found" });

                var hardDrivePath = "/mnt/books/VendorUploads";
                var vendorFolder = Path.Combine(hardDrivePath, $"Vendor_{vendorId}");
                
                if (!Directory.Exists(vendorFolder))
                {
                    Directory.CreateDirectory(vendorFolder);
                }
                
                string originalFileName = Path.GetFileName(file.FileName);
                string cleanFileName = System.Text.RegularExpressions.Regex.Replace(originalFileName, @"[^a-zA-Z0-9.\s\-_]", "");
                cleanFileName = cleanFileName.Replace(" ", "_");
                
                string finalFileName = cleanFileName;
                int counter = 1;
                string nameWithoutExt = Path.GetFileNameWithoutExtension(cleanFileName);
                string extension = Path.GetExtension(cleanFileName);
                
                while (System.IO.File.Exists(Path.Combine(vendorFolder, finalFileName)))
                {
                    finalFileName = $"{nameWithoutExt}_{counter}{extension}";
                    counter++;
                }
                
                var filePath = Path.Combine(vendorFolder, finalFileName);
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                
                Console.WriteLine($"File saved: {finalFileName}");
                
                string coverFileName = string.Empty;
                if (bookCover != null && bookCover.Length > 0)
                {
                    string originalCoverName = Path.GetFileName(bookCover.FileName);
                    string cleanCoverName = System.Text.RegularExpressions.Regex.Replace(originalCoverName, @"[^a-zA-Z0-9.\s\-_]", "");
                    cleanCoverName = cleanCoverName.Replace(" ", "_");
                    
                    string finalCoverName = cleanCoverName;
                    int coverCounter = 1;
                    string coverNameWithoutExt = Path.GetFileNameWithoutExtension(cleanCoverName);
                    string coverExtension = Path.GetExtension(cleanCoverName);
                    
                    while (System.IO.File.Exists(Path.Combine(vendorFolder, finalCoverName)))
                    {
                        finalCoverName = $"{coverNameWithoutExt}_{coverCounter}{coverExtension}";
                        coverCounter++;
                    }
                    coverFileName = finalCoverName;
                    
                    var coverPath = Path.Combine(vendorFolder, coverFileName);
                    
                    using (var stream = new FileStream(coverPath, FileMode.Create))
                    {
                        await bookCover.CopyToAsync(stream);
                    }
                    Console.WriteLine($"Cover saved: {coverFileName}");
                }
                
                var book = new Book
{
    VendorId = vendorId,
    Title = title,
    Price = price,
    CategoryId = subCategoryId,
    BookPath = finalFileName,
    BookCoverPath = coverFileName,
    CreatedAt = DateTime.UtcNow
};
                
                _context.Books.Add(book);
                await _context.SaveChangesAsync();
                
                return Ok(new
                {
                    success = true,
                    message = "Book uploaded successfully",
                    book = new
{
    book.BookId,
    book.Title,
    book.Price,
    book.CategoryId,
    book.BookPath,
    book.BookCoverPath,
    book.CreatedAt
}
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                return StatusCode(500, new { message = $"Server error: {ex.Message}" });
            }
        }

// Add this to your BooksController.cs
[HttpGet("test-hard-drive")]
public IActionResult TestHardDrive()
{
    try
    {
        return Ok(new { 
            success = true, 
            message = "Hard drive endpoint is working!",
            timestamp = DateTime.Now
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = ex.Message });
    }
}

// ADD this new method
[HttpGet("hard-drive-books/by-subcategory")]
public async Task<IActionResult> GetHardDriveBooksBySubCategory(
    [FromQuery] string subCategory,
    [FromQuery] int page = 1, 
    [FromQuery] int pageSize = 100)
{
    try
    {
        Console.WriteLine($"=== Getting Hard Drive Books for subcategory: {subCategory} ===");
        
        var query = _context.HardDriveBooks.Where(b => b.SubCategory == subCategory);
        var totalCount = await query.CountAsync();
        
        var books = await query
            .OrderBy(b => b.BookName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                id = b.Id,
                bookName = b.BookName,
                category = b.Category,
                subCategory = b.SubCategory,
                bookPath = b.BookPath,
                createdAt = b.CreatedAt
            })
            .ToListAsync();
        
        return Ok(new
        {
            success = true,
            data = books,
            pagination = new
            {
                currentPage = page,
                pageSize = pageSize,
                totalCount = totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return StatusCode(500, new { success = false, error = ex.Message });
    }
}


// REPLACE the existing GetHardDriveBooks method with this one
// Changed route to avoid conflicts
[HttpGet("hard-drive-books-paginated")]
public async Task<IActionResult> GetHardDriveBooksPaginated(
    [FromQuery] int page = 1, 
    [FromQuery] int pageSize = 100)
{
    try
    {
        Console.WriteLine($"=== Getting Hard Drive Books - Page {page}, Size {pageSize} ===");
        
        // Get total count
        var totalCount = await _context.HardDriveBooks.CountAsync();
        
        // Get paginated results
        var books = await _context.HardDriveBooks
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                id = b.Id,
                bookName = b.BookName,
                category = b.Category,
                subCategory = b.SubCategory,
                bookPath = b.BookPath,
                createdAt = b.CreatedAt
            })
            .ToListAsync();
        
        var result = new
        {
            success = true,
            data = books,
            pagination = new
            {
                currentPage = page,
                pageSize = pageSize,
                totalCount = totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                hasNextPage = page * pageSize < totalCount,
                hasPreviousPage = page > 1
            }
        };
        
        Console.WriteLine($"Returning {books.Count} books out of {totalCount} total");
        return Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in GetHardDriveBooksPaginated: {ex.Message}");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        return StatusCode(500, new { 
            success = false, 
            message = "Error fetching hard drive books",
            error = ex.Message
        });
    }
}

        [HttpGet("hard-drive-books")]
        public async Task<IActionResult> GetHardDriveBooks([FromQuery] string? subCategory = null)
        {
            try
            {
                IQueryable<HardDriveBook> query = _context.HardDriveBooks;
                
                if (!string.IsNullOrEmpty(subCategory))
                {
                    query = query.Where(b => b.SubCategory == subCategory);
                }
                
                var books = await query
                    .OrderBy(b => b.SubCategory)
                    .ThenBy(b => b.BookName)
                    .Select(b => new
                    {
                        b.Id,
                        Title = b.BookName,
                        b.Category,
                        b.SubCategory,
                        FilePath = b.BookPath,
                        b.CreatedAt,
                        BookCover = (string?)null
                    })
                    .ToListAsync();
                
                return Ok(books);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching hard drive books: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("hard-drive-categories")]
        public async Task<IActionResult> GetHardDriveCategories()
        {
            try
            {
                var categories = await _context.HardDriveBooks
                    .GroupBy(b => new { b.Category, b.SubCategory })
                    .Select(g => new { g.Key.Category, g.Key.SubCategory })
                    .OrderBy(g => g.Category)
                    .ThenBy(g => g.SubCategory)
                    .ToListAsync();
                
                var result = categories
                    .GroupBy(c => c.Category)
                    .Select(g => new
                    {
                        CategoryName = g.Key,
                        SubCategories = g.Select(sc => new { SubCategoryName = sc.SubCategory }).ToList()
                    })
                    .ToList();
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching hard drive categories: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

// REPLACE the existing GetHardDriveBooks method (around line 220) with this:
[HttpGet("books-by-subcategory")]
public async Task<IActionResult> GetBooksBySubCategory(
    [FromQuery] string? subCategory = null)
{
    try
    {
        Console.WriteLine($"=== Getting Books - SubCategory: {subCategory ?? "All"} ===");
        
        IQueryable<HardDriveBook> query = _context.HardDriveBooks;
        
        if (!string.IsNullOrEmpty(subCategory))
        {
            query = query.Where(b => b.SubCategory == subCategory);
        }
        
        var books = await query
            .OrderBy(b => b.SubCategory)
            .ThenBy(b => b.BookName)
            .Select(b => new
            {
                id = b.Id,
                bookName = b.BookName,
                category = b.Category,
                subCategory = b.SubCategory,
                bookPath = b.BookPath,
                createdAt = b.CreatedAt
            })
            .ToListAsync();
        
        Console.WriteLine($"Returning {books.Count} books for subcategory: {subCategory ?? "All"}");
        return Ok(books);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching books: {ex.Message}");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        return StatusCode(500, new { 
            success = false, 
            message = ex.Message,
            stackTrace = ex.StackTrace
        });
    }
}

        /*[HttpGet("serve-file/{vendorId}/{fileName}")]
        public IActionResult ServeFile(int vendorId, string fileName)
        {
            try
            {
                var hardDrivePath = "/mnt/books/VendorUploads";
                var filePath = Path.Combine(hardDrivePath, $"Vendor_{vendorId}", fileName);
                
                Console.WriteLine($"Serving file: {filePath}");
                
                if (!System.IO.File.Exists(filePath))
                    return NotFound(new { message = "File not found" });
                
                var ext = Path.GetExtension(filePath).ToLower();
                var contentType = ext switch
                {
                    ".pdf" => "application/pdf",
                    ".txt" => "text/plain",
                    ".epub" => "application/epub+zip",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    _ => "application/octet-stream"
                };
                
                var bytes = System.IO.File.ReadAllBytes(filePath);
                return File(bytes, contentType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serving file: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }
*/



[HttpGet("serve-file/{vendorId}/{fileName}")]
public IActionResult ServeFile(int vendorId, string fileName)
{
    try
    {
        // Decode URL-encoded filename
        var decodedFileName = Uri.UnescapeDataString(fileName);

        var hardDrivePath = "/mnt/books/VendorUploads";
        var vendorFolder = Path.Combine(hardDrivePath, $"Vendor_{vendorId}");
        var filePath = Path.Combine(vendorFolder, decodedFileName);

        Console.WriteLine($"=== Serving file ===");
        Console.WriteLine($"Vendor: {vendorId}");
        Console.WriteLine($"Decoded filename: {decodedFileName}");
        Console.WriteLine($"Full path: {filePath}");

        if (!System.IO.File.Exists(filePath))
        {
            Console.WriteLine($"File NOT found at {filePath}");

            // List directory to help debug
            if (Directory.Exists(vendorFolder))
            {
                Console.WriteLine($"Folder contents:");
                foreach (var f in Directory.GetFiles(vendorFolder).Take(10))
                    Console.WriteLine($"  - {Path.GetFileName(f)}");
            }

            return NotFound(new { message = "File not found", path = filePath });
        }

        var ext = Path.GetExtension(filePath).ToLower();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".epub" => "application/epub+zip",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

        // Stream the file with range support for large PDFs
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        Response.Headers.Append("Accept-Ranges", "bytes");
        Response.Headers.Append("Access-Control-Allow-Origin", "*");

        return File(stream, contentType, enableRangeProcessing: true);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error serving file: {ex.Message}");
        return StatusCode(500, new { message = ex.Message });
    }
}

        [HttpGet("category/{categoryId}")]
public IActionResult GetBooksByCategory(int categoryId, int page = 1, int pageSize = 8)
{
    var raw = _context.Books
        .Where(b => b.CategoryId == categoryId)
        .OrderByDescending(b => b.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(b => new
        {
            b.BookId,
            b.Title,
            b.Price,
            b.CategoryId,
            b.VendorId,
            b.BookPath,
            b.BookCoverPath,
            b.CreatedAt
        })
        .ToList();

    var books = raw.Select(b => new
    {
        b.BookId,
        b.Title,
        b.Price,
        b.CategoryId,
        b.CreatedAt,
        filePath = BuildBookUrl(b.VendorId, b.BookPath),
        bookCover = BuildBookUrl(b.VendorId, b.BookCoverPath)
    }).ToList();

    return Ok(books);
}
        

    
        [HttpDelete("{id}")]
        public IActionResult DeleteBook(int id)
        {
            var book = _context.Books.FirstOrDefault(b => b.BookId == id);
            if (book == null) return NotFound(new { message = "Book not found" });

            _context.Books.Remove(book);
            _context.SaveChanges();

            return Ok(new { message = "Book deleted successfully" });
        }

        [HttpPost("bulk-upload")]
        public async Task<IActionResult> BulkUploadBooks(
            [FromForm] List<IFormFile> files,
            [FromForm] int vendorId,
            [FromForm] decimal price)
        {
            if (files == null || files.Count == 0)
                return BadRequest(new { message = "No files received" });

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uploadedBooks = new List<object>();
            var categories = await _context.Categories.ToListAsync();

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var safeFileName = Path.GetFileName(file.FileName);
                    var filePath = Path.Combine(uploadsFolder, safeFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var title = Path.GetFileNameWithoutExtension(safeFileName);
                    var subCategory = categories
                        .FirstOrDefault(c => title.Contains(c.CategoryName, StringComparison.OrdinalIgnoreCase) && c.ParentId != null);

                    var book = new Book
{
    VendorId = vendorId,
    Title = title,
    Price = price,
    CategoryId = subCategory?.Id,
    BookPath = "/uploads/" + safeFileName,
    BookCoverPath = "/uploads/" + safeFileName,
    CreatedAt = DateTime.UtcNow
};

                    _context.Books.Add(book);
                    uploadedBooks.Add(book);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = $"{uploadedBooks.Count} books uploaded successfully",
                uploadedBooks
            });
        }

        [HttpGet]
public IActionResult GetAllBooks()
{
    var raw = _context.Books
        .OrderByDescending(b => b.CreatedAt)
        .Select(b => new
        {
            b.BookId,
            b.Title,
            b.Price,
            b.CategoryId,
            b.VendorId,
            b.BookPath,
            b.BookCoverPath,
            b.CreatedAt
        })
        .ToList();

    var books = raw.Select(b => new
    {
        b.BookId,
        b.Title,
        b.Price,
        b.CategoryId,
        b.CreatedAt,
        filePath = BuildBookUrl(b.VendorId, b.BookPath),
        bookCover = BuildBookUrl(b.VendorId, b.BookCoverPath)
    }).ToList();

    return Ok(books);
}
        [HttpGet("browse-files")]
        public IActionResult BrowseFiles([FromQuery] string? path = null)
        {
            try
            {
                Console.WriteLine($"=== BrowseFiles called with path: {(path == null ? "null" : path)} ===");
                
                var folderSettings = _context.BooksFolderSettings.ToList();
                
                Console.WriteLine($"Found {folderSettings.Count} folder settings in database");
                
                if (string.IsNullOrEmpty(path))
                {
                    var rootFolders = new List<object>();

                    foreach (var fs in folderSettings)
                    {
                        var fullPath = fs.Path;
                        Console.WriteLine($"Checking folder path: {fullPath}");
                        
                        if (string.IsNullOrEmpty(fullPath))
                        {
                            Console.WriteLine($"  - Path is null or empty, skipping");
                            continue;
                        }
                        
                        if (!Directory.Exists(fullPath))
                        {
                            Console.WriteLine($"  - Directory does NOT exist: {fullPath}");
                            continue;
                        }
                        
                        Console.WriteLine($"  - Directory exists, adding to list");
                        rootFolders.Add(new
                        {
                            name = Path.GetFileName(fullPath),
                            fullPath = fullPath,
                            type = "folder",
                            isRoot = true
                        });
                    }
                    
                    Console.WriteLine($"Returning {rootFolders.Count} root folders");
                    
                    return Ok(new
                    {
                        items = rootFolders,
                        currentPath = "",
                        parentPath = "",
                        totalFolders = rootFolders.Count,
                        message = rootFolders.Count == 0 ? "No valid folders found. Please check your folder paths in the database." : null
                    });
                }

                var decodedPath = Uri.UnescapeDataString(path);
                Console.WriteLine($"Loading contents of: {decodedPath}");
                
                if (!Directory.Exists(decodedPath))
                {
                    Console.WriteLine($"Directory not found: {decodedPath}");
                    return NotFound(new { message = "Directory not found" });
                }

                var items = new List<object>();
                var allowedExt = new[] { ".pdf", ".txt", ".epub", ".doc", ".docx" };

                foreach (var dir in Directory.GetDirectories(decodedPath))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    items.Add(new
                    {
                        name = dirInfo.Name,
                        fullPath = dir,
                        type = "folder",
                        size = (long?)null,
                        previewImage = (string?)null
                    });
                }

                foreach (var file in Directory.GetFiles(decodedPath))
                {
                    var ext = Path.GetExtension(file).ToLower();

                    if (allowedExt.Contains(ext))
                    {
                        var fileInfo = new FileInfo(file);
                        items.Add(new
                        {
                            name = Path.GetFileNameWithoutExtension(file),
                            fullPath = file,
                            type = "file",
                            size = fileInfo.Length,
                            webPath = $"/api/books/files/{Uri.EscapeDataString(file)}",
                            previewImage = (ext == ".pdf") ? "/images/pdf-placeholder.png" : null
                        });
                    }
                }

                var sortedItems = items
                    .OrderByDescending(x => 
                    {
                        var type = x.GetType().GetProperty("type")?.GetValue(x)?.ToString();
                        return type == "folder";
                    })
                    .ThenBy(x => x.GetType().GetProperty("name")?.GetValue(x)?.ToString())
                    .ToList();

                Console.WriteLine($"Returning {sortedItems.Count} items from {decodedPath}");

                return Ok(new
                {
                    items = sortedItems,
                    currentPath = decodedPath,
                    parentPath = Directory.GetParent(decodedPath)?.FullName ?? "",
                    totalItems = sortedItems.Count
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CRITICAL ERROR in BrowseFiles: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("files/{*filePath}")]
        public IActionResult GetFile(string filePath)
        {
            try
            {
                var decoded = Uri.UnescapeDataString(filePath);

                if (!System.IO.File.Exists(decoded))
                    return NotFound();

                var ext = Path.GetExtension(decoded).ToLower();
                var contentType = ext switch
                {
                    ".pdf" => "application/pdf",
                    ".txt" => "text/plain",
                    ".epub" => "application/epub+zip",
                    ".doc" => "application/msword",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    _ => "application/octet-stream"
                };

                var bytes = System.IO.File.ReadAllBytes(decoded);
                return File(bytes, contentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        private string ConvertToWebPath(string physicalPath)
        {
            var encoded = Uri.EscapeDataString(physicalPath);
            return $"/api/books/files/{encoded}";
        }

        [HttpGet("pdf-info")]
        public IActionResult GetPdfInfo([FromQuery] string filePath)
        {
            try
            {
                var decodedPath = Uri.UnescapeDataString(filePath);
                
                if (!System.IO.File.Exists(decodedPath))
                    return NotFound(new { message = "File not found" });
                
                var fileInfo = new FileInfo(decodedPath);
                
                return Ok(new
                {
                    exists = true,
                    fileName = Path.GetFileName(decodedPath),
                    size = fileInfo.Length,
                    created = fileInfo.CreationTime,
                    modified = fileInfo.LastWriteTime,
                    isPdf = decodedPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase),
                    path = $"/api/books/serve-hard-drive-file?path={Uri.EscapeDataString(decodedPath)}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("serve-hard-drive-file")]
        public IActionResult ServeHardDriveFile([FromQuery] string path)
        {
            try
            {
                var decodedPath = Uri.UnescapeDataString(path);
                
                var allowedPaths = _context.BooksFolderSettings.Select(fs => fs.Path).ToArray();
                if (!allowedPaths.Any(p => decodedPath.StartsWith(p)))
                {
                    return Forbid();
                }
                
                if (!System.IO.File.Exists(decodedPath))
                    return NotFound();
                
                var contentType = Path.GetExtension(decodedPath).ToLower() switch
                {
                    ".pdf" => "application/pdf",
                    ".txt" => "text/plain",
                    ".epub" => "application/epub+zip",
                    _ => "application/octet-stream"
                };
                
                var bytes = System.IO.File.ReadAllBytes(decodedPath);
                return File(bytes, contentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

       /* [HttpGet("serve-hard-drive-book")]
        public IActionResult ServeHardDriveBook([FromQuery] string filePath)
        {
            try
            {
                string physicalPath = filePath;
                
                Console.WriteLine($"=== SERVE HARD DRIVE BOOK ===");
                Console.WriteLine($"Original path: {filePath}");
                Console.WriteLine($"Physical path: {physicalPath}");
                
                if (!System.IO.File.Exists(physicalPath))
                {
                    Console.WriteLine($"File does NOT exist!");
                    return NotFound(new { message = "File not found", path = physicalPath });
                }
                
                var fileInfo = new System.IO.FileInfo(physicalPath);
                Console.WriteLine($"File exists! Size: {fileInfo.Length} bytes");
                
                string contentType = Path.GetExtension(physicalPath).ToLower() == ".pdf" 
                    ? "application/pdf" 
                    : "text/plain; charset=utf-8";
                
                byte[] fileBytes = System.IO.File.ReadAllBytes(physicalPath);
                Console.WriteLine($"Read {fileBytes.Length} bytes successfully");
                
                Response.Headers.Append("Access-Control-Allow-Origin", "*");
                
                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = ex.Message, path = filePath });
            }
        }*/

[HttpGet("serve-hard-drive-book")]
public IActionResult ServeHardDriveBook([FromQuery] string filePath)
{
    try
    {
        if (string.IsNullOrEmpty(filePath))
            return BadRequest(new { message = "filePath required" });

        var physicalPath = filePath;

        // Handle URL-encoded paths gracefully
        if (!System.IO.File.Exists(physicalPath))
        {
            var decoded = Uri.UnescapeDataString(physicalPath);
            if (System.IO.File.Exists(decoded))
                physicalPath = decoded;
            else
                return NotFound(new { message = "File not found", path = physicalPath });
        }

        var contentType = Path.GetExtension(physicalPath).ToLower() switch
        {
            ".pdf"  => "application/pdf",
            ".txt"  => "text/plain; charset=utf-8",
            ".epub" => "application/epub+zip",
            _       => "application/octet-stream"
        };

        // ---- ENABLE RANGE REQUESTS ----
        var fileStream = new FileStream(
            physicalPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true
        );

        // Enable range processing: ASP.NET Core will honor Range headers automatically
        Response.Headers.Append("Accept-Ranges", "bytes");
        Response.Headers.Append("Access-Control-Allow-Origin", "*");
        Response.Headers.Append("Access-Control-Expose-Headers", "Content-Length, Content-Range, Accept-Ranges");

        // EnableRangeProcessing = true is the KEY
        return File(fileStream, contentType, enableRangeProcessing: true);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR serving book: {ex.Message}");
        return StatusCode(500, new { message = ex.Message, path = filePath });
    }
}

        [HttpGet("generate-pdf-preview-alt")]
        public IActionResult GeneratePdfPreviewAlt([FromQuery] string filePath)
        {
            try
            {
                var decodedPath = Uri.UnescapeDataString(filePath);
                
                if (!System.IO.File.Exists(decodedPath))
                    return NotFound(new { message = "File not found" });
                
                var cacheKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(decodedPath));
                var cachePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "previews", $"{cacheKey}.png");
                
                if (System.IO.File.Exists(cachePath))
                {
                    var cachedImage = System.IO.File.ReadAllBytes(cachePath);
                    return File(cachedImage, "image/png");
                }
                
                var previewDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "previews");
                if (!Directory.Exists(previewDir))
                    Directory.CreateDirectory(previewDir);
                
                using (var document = PdfiumViewer.PdfDocument.Load(decodedPath))
                {
                    var image = document.Render(0, 300, 300, true);
                    image.Save(cachePath, System.Drawing.Imaging.ImageFormat.Png);
                    
                    var imageBytes = System.IO.File.ReadAllBytes(cachePath);
                    return File(imageBytes, "image/png");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating PDF preview: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("generate-pdf-preview")]
        public async Task<IActionResult> GeneratePdfPreview([FromQuery] string filePath)
        {
            try
            {
                var decodedPath = Uri.UnescapeDataString(filePath);
                
                if (!System.IO.File.Exists(decodedPath))
                    return NotFound(new { message = "File not found" });
                
                var cacheKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(decodedPath));
                var cachePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "previews", $"{cacheKey}.jpg");
                
                if (System.IO.File.Exists(cachePath) && (DateTime.Now - System.IO.File.GetLastWriteTime(cachePath)).TotalHours < 24)
                {
                    var cachedImage = System.IO.File.ReadAllBytes(cachePath);
                    return File(cachedImage, "image/jpeg");
                }
                
                var previewDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "previews");
                if (!Directory.Exists(previewDir))
                    Directory.CreateDirectory(previewDir);
                
                var outputPath = Path.Combine(previewDir, $"{cacheKey}.jpg");
                
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "gs",
                    Arguments = $"-dNOPAUSE -dBATCH -sDEVICE=jpeg -r72 -dFirstPage=1 -dLastPage=1 -sOutputFile=\"{outputPath}\" \"{decodedPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = System.Diagnostics.Process.Start(psi);
                await process.WaitForExitAsync();
                
                if (process.ExitCode == 0 && System.IO.File.Exists(outputPath))
                {
                    var imageBytes = System.IO.File.ReadAllBytes(outputPath);
                    return File(imageBytes, "image/jpeg");
                }
                
                return NotFound(new { message = "Could not generate preview" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating PDF preview: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("pdf-preview")]
        public IActionResult GetPdfPreview([FromQuery] string filePath)
        {
            try
            {
                var decodedPath = Uri.UnescapeDataString(filePath);

                if (!System.IO.File.Exists(decodedPath))
                    return NotFound();

                var previewPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "images",
                    "pdf-placeholder.png"
                );

                if (!System.IO.File.Exists(previewPath))
                {
                    return File(new byte[] { }, "image/png");
                }

                var bytes = System.IO.File.ReadAllBytes(previewPath);
                return File(bytes, "image/png");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // ============================================
        // ADMIN ENDPOINTS
        // ============================================

        [HttpGet("admin-test")]
        public IActionResult AdminTest()
        {
            return Ok(new { success = true, message = "Admin endpoints are working!", timestamp = DateTime.UtcNow });
        }

        [HttpGet("admin-categories")]
        public async Task<IActionResult> AdminGetCategories()
        {
            try
            {
                Console.WriteLine("AdminGetCategories called");
                var categories = await _context.Categories
                    .Select(c => new { Id = c.Id, Name = c.CategoryName })
                    .ToListAsync();
                
                return Ok(new { success = true, categories = categories });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("admin-books")]
        public async Task<IActionResult> AdminGetAllBooks()
        {
            try
            {
                Console.WriteLine("AdminGetAllBooks called");
                var allBooks = new List<object>();
                
                var mainBooksRaw = await _context.Books
    .Select(b => new
    {
        book_id = b.BookId,
        title = b.Title,
        price = b.Price,
        category_id = b.CategoryId,
        vendor_id = b.VendorId,
        book_path = b.BookPath,
        book_cover_path = b.BookCoverPath,
        created_at = b.CreatedAt
    })
    .ToListAsync();

var mainBooks = mainBooksRaw.Select(b => new
{
    book_id = b.book_id,
    id = (int?)null,
    title = b.title,
    price = b.price,
    sub_category = b.category_id,
    table_source = "books",
    created_at = b.created_at,
    file_path = BuildBookUrl(b.vendor_id, b.book_path),
    book_cover = BuildBookUrl(b.vendor_id, b.book_cover_path)
}).ToList();
                
                allBooks.AddRange(mainBooks);
                Console.WriteLine($"Found {mainBooks.Count} main books");
                
                
                
                                
                return Ok(new { success = true, books = allBooks });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("check-hard-drive-book/{id}")]
        public async Task<IActionResult> CheckHardDriveBook(int id)
        {
            try
            {
                var book = await _context.HardDriveBooks.FindAsync(id);
                if (book == null)
                {
                    return NotFound(new { message = $"Book with ID {id} not found" });
                }
                
                return Ok(new
                {
                    id = book.Id,
                    bookName = book.BookName,
                    category = book.Category,
                    subCategory = book.SubCategory,
                    bookPath = book.BookPath,
                    createdAt = book.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("check-book/{id}")]
public async Task<IActionResult> CheckBook(int id)
{
    try
    {
        Console.WriteLine($"Checking book with ID: {id}");

        var mainBook = await _context.Books.FindAsync(id);
        if (mainBook != null)
        {
            return Ok(new
            {
                source = "books",
                id = mainBook.BookId,
                title = mainBook.Title,
                categoryId = mainBook.CategoryId
            });
        }

        return NotFound(new { message = $"Book with ID {id} not found" });
    }
    catch (Exception ex)
    {
        return BadRequest(new { error = ex.Message });
    }
}

        /*[HttpGet("vendor-with-names/{vendorId}")]
public async Task<IActionResult> GetBooksByVendorWithNames(int vendorId)
{
    var books = await _context.Books
        .Where(b => b.VendorId == vendorId)
        .Include(b => b.Category)
        .ThenInclude(c => c.ParentCategory)
        .Select(b => new
        {
            b.BookId,
            b.Title,
            b.Price,
            CategoryId = b.CategoryId,
            CategoryName = b.Category != null ? b.Category.CategoryName : "",
            MainCategoryName = b.Category != null && b.Category.ParentCategory != null
                ? b.Category.ParentCategory.CategoryName : "",
            FullCategoryPath = b.Category != null
                ? (b.Category.ParentCategory != null
                    ? $"{b.Category.ParentCategory.CategoryName} → {b.Category.CategoryName}"
                    : b.Category.CategoryName) : "",
            b.FilePath,
            b.BookCover,
            b.CreatedAt
        })
        .OrderByDescending(b => b.CreatedAt)
        .ToListAsync();

    return Ok(books);
}
*/


[HttpGet("vendor-with-names/{vendorId}")]
public async Task<IActionResult> GetBooksByVendorWithNames(int vendorId)
{
    try
    {
        var books = await _context.Books
            .Where(b => b.VendorId == vendorId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.BookId,
                b.Title,
                b.Price,
                b.CategoryId,
                b.VendorId, 
                b.BookPath,
                b.BookCoverPath,
                b.CreatedAt
            })
            .ToListAsync();

        var allCategories = await _context.Categories
            .Select(c => new { c.Id, c.CategoryName, c.ParentId })
            .ToListAsync();

        var catById = allCategories.ToDictionary(c => c.Id, c => c);

        var result = books.Select(b =>
        {
            string categoryName = "Uncategorized";
            string parentCategoryName = "Uncategorized";
            string subCategoryName = "";

            if (b.CategoryId.HasValue && catById.TryGetValue(b.CategoryId.Value, out var cat))
            {
                if (cat.ParentId.HasValue && catById.TryGetValue(cat.ParentId.Value, out var parent))
                {
                    categoryName = parent.CategoryName;
                    parentCategoryName = parent.CategoryName;
                    subCategoryName = cat.CategoryName;
                }
                else
                {
                    categoryName = cat.CategoryName;
                    parentCategoryName = cat.CategoryName;
                    subCategoryName = "";
                }
            }

            return new
            {
                bookId = b.BookId,
                title = b.Title,
                price = b.Price,
                categoryId = b.CategoryId,
                category = categoryName,
                parentCategory = parentCategoryName,
                subCategory = subCategoryName,

                // Full URLs built here — reader can fetch these directly
                filePath = BuildBookUrl(b.VendorId, b.BookPath),
                bookCover = BuildBookUrl(b.VendorId, b.BookCoverPath),

                // Also return raw filenames if the frontend needs them
                bookPath = b.BookPath,
                bookCoverPath = b.BookCoverPath,

                createdAt = b.CreatedAt
            };
        }).ToList();

        return Ok(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in GetBooksByVendorWithNames: {ex.Message}");
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}


        [HttpPost("admin-update")]
        public async Task<IActionResult> AdminUpdateBook([FromBody] UpdateBookCategoryDto updateData)
        {
            try
            {
                Console.WriteLine($"=== AdminUpdateBook called ===");
                
                if (updateData == null)
                {
                    return BadRequest(new { success = false, message = "No data received" });
                }
                
                Console.WriteLine($"TableSource: {updateData.TableSource}");
                Console.WriteLine($"Id: {updateData.Id}");
                Console.WriteLine($"BookId: {updateData.BookId}");
                Console.WriteLine($"Title: {updateData.Title}");
                Console.WriteLine($"SubCategory: {updateData.SubCategory}");
                
                if (updateData.TableSource == "hard_drive_books")
                {
                    if (!updateData.Id.HasValue)
                    {
                        return BadRequest(new { success = false, message = "Id is required for hard drive books" });
                    }
                    
                    string sql = @"UPDATE `Hard Drive Books` 
                                  SET book_name = @title, 
                                      category = @category, 
                                      sub_category = @subCategory 
                                  WHERE id = @id";
                    
                    using (var connection = new MySqlConnection(_context.Database.GetConnectionString()))
                    {
                        await connection.OpenAsync();
                        using (var command = new MySqlCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@id", updateData.Id.Value);
                            command.Parameters.AddWithValue("@title", updateData.Title ?? "");
                            command.Parameters.AddWithValue("@category", updateData.Category ?? "");
                            command.Parameters.AddWithValue("@subCategory", updateData.SubCategory ?? "");
                            
                            int rowsAffected = await command.ExecuteNonQueryAsync();
                            
                            if (rowsAffected > 0)
                            {
                                Console.WriteLine($"✅ Successfully updated book ID {updateData.Id}");
                                return Ok(new { success = true, message = "Book updated successfully" });
                            }
                            else
                            {
                                return NotFound(new { success = false, message = $"Book with ID {updateData.Id} not found" });
                            }
                        }
                    }
                }
                else if (updateData.TableSource == "books")
                {
                    if (!updateData.BookId.HasValue)
                    {
                        return BadRequest(new { success = false, message = "BookId is required for main books" });
                    }
                    
                    var book = await _context.Books.FindAsync(updateData.BookId.Value);
                    if (book == null)
                    {
                        return NotFound(new { success = false, message = $"Book with ID {updateData.BookId} not found" });
                    }
                    
                    if (!string.IsNullOrEmpty(updateData.Title))
                        book.Title = updateData.Title;
                    if (!string.IsNullOrEmpty(updateData.SubCategory))
                    {
                        // Try to find category ID by name
                        var category = await _context.Categories
                            .FirstOrDefaultAsync(c => c.CategoryName == updateData.SubCategory);
                        if (category != null)
                            book.CategoryId = category.Id;
                    }
                    if (updateData.Price.HasValue)
                        book.Price = updateData.Price.Value;
                    
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, message = "Book updated successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = $"Invalid table source: {updateData.TableSource}" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("debug-update")]
        public async Task<IActionResult> DebugUpdate([FromBody] dynamic rawData)
        {
            try
            {
                Console.WriteLine("=== DEBUG UPDATE ===");
                string json = rawData.ToString();
                Console.WriteLine($"Raw JSON received: {json}");
                
                var updateData = System.Text.Json.JsonSerializer.Deserialize<UpdateBookCategoryDto>(json, new System.Text.Json.JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                if (updateData == null)
                {
                    return BadRequest(new { success = false, message = "Failed to deserialize data" });
                }
                
                Console.WriteLine($"Id: {updateData.Id}, Title: {updateData.Title}, Category: {updateData.Category}");
                
                if (updateData.TableSource != "hard_drive_books" || !updateData.Id.HasValue)
                {
                    return BadRequest(new { success = false, message = "Invalid data" });
                }
                
                string connectionString = _context.Database.GetConnectionString();
                string sql = @"UPDATE `Hard Drive Books` 
                               SET book_name = @title, 
                                   category = @category, 
                                   sub_category = @subCategory 
                               WHERE id = @id";
                
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@id", updateData.Id.Value);
                        command.Parameters.AddWithValue("@title", updateData.Title ?? "");
                        command.Parameters.AddWithValue("@category", updateData.Category ?? "");
                        command.Parameters.AddWithValue("@subCategory", updateData.SubCategory ?? "");
                        
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        
                        if (rowsAffected > 0)
                        {
                            return Ok(new { success = true, message = "Book updated successfully" });
                        }
                        else
                        {
                            return NotFound(new { success = false, message = $"Book ID {updateData.Id} not found" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("debug-pdf")]
        public IActionResult DebugPdf([FromQuery] string filePath)
        {
            try
            {
                var decodedPath = Uri.UnescapeDataString(filePath);
                
                var result = new
                {
                    originalPath = filePath,
                    decodedPath = decodedPath,
                    fileExists = System.IO.File.Exists(decodedPath),
                    fileSize = System.IO.File.Exists(decodedPath) ? new System.IO.FileInfo(decodedPath).Length : 0,
                    isReadable = false,
                    errorMessage = ""
                };
                
                if (result.fileExists)
                {
                    try
                    {
                        using (var stream = System.IO.File.OpenRead(decodedPath))
                        {
                            byte[] buffer = new byte[8];
                            int bytesRead = stream.Read(buffer, 0, 8);
                            result = result with { isReadable = true };
                        }
                    }
                    catch (Exception ex)
                    {
                        result = result with { errorMessage = ex.Message };
                    }
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("test-path")]
        public IActionResult TestPath([FromQuery] string filePath)
        {
            try
            {
                var result = new
                {
                    receivedPath = filePath,
                    decodedPath = Uri.UnescapeDataString(filePath),
                    fileExists = System.IO.File.Exists(Uri.UnescapeDataString(filePath)),
                    currentDirectory = Directory.GetCurrentDirectory(),
                    testFile = "/mnt/books/BooksCollection/Books/test.txt"
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(new { error = ex.Message });
            }
        }

        [HttpGet("serve-pdf-secure")]
        public IActionResult ServePdfSecure([FromQuery] string filePath)
        {
            try
            {
                string physicalPath = filePath;
                
                if (!System.IO.File.Exists(physicalPath))
                {
                    return NotFound(new { message = "File not found" });
                }
                
                var fileBytes = System.IO.File.ReadAllBytes(physicalPath);
                
                Response.Headers.Append("Content-Disposition", "inline");
                Response.Headers.Append("X-Content-Type-Options", "nosniff");
                Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                Response.Headers.Append("Pragma", "no-cache");
                Response.Headers.Append("Expires", "0");
                Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
                
                return File(fileBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }




// In your BooksController.cs

// Get all hard drive books
/*[HttpGet("hard-drive-books")]
public async Task<IActionResult> GetHardDriveBooks()
{
    try
    {
        var books = await _context.HardDriveBooks
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        return Ok(books);
    }
    catch (Exception ex)
    {
        return StatusCode(500, $"Error fetching hard drive books: {ex.Message}");
    }
}*/

// Update hard drive book
/*[HttpPut("hard-drive-books/{id}")]
public async Task<IActionResult> UpdateHardDriveBook(int id, [FromBody] HardDriveBook updatedBook)
{
    try
    {
        var book = await _context.HardDriveBooks.FindAsync(id);
        if (book == null)
            return NotFound("Book not found");

        book.BookName = updatedBook.BookName;
        book.Category = updatedBook.Category;
        book.SubCategory = updatedBook.SubCategory;
        // Don't update BookPath as it shouldn't change

        await _context.SaveChangesAsync();
        return Ok(book);
    }
    catch (Exception ex)
    {
        return StatusCode(500, $"Error updating hard drive book: {ex.Message}");
    }
}
*/



// Update hard drive book
[HttpPut("hard-drive-books/{id}")]
public async Task<IActionResult> UpdateHardDriveBook(int id, [FromBody] UpdateHardDriveBookDto updatedBook)
{
    try
    {
        Console.WriteLine($"=== Updating Hard Drive Book ID: {id} ===");
        Console.WriteLine($"Received: BookName={updatedBook.BookName}, Category={updatedBook.Category}, SubCategory={updatedBook.SubCategory}");
        
        var book = await _context.HardDriveBooks.FindAsync(id);
        if (book == null)
        {
            Console.WriteLine($"Book with ID {id} not found");
            return NotFound(new { success = false, message = "Book not found" });
        }

        // Update only the fields that are provided
        if (!string.IsNullOrEmpty(updatedBook.BookName))
            book.BookName = updatedBook.BookName;
        
        if (!string.IsNullOrEmpty(updatedBook.Category))
            book.Category = updatedBook.Category;
        
        if (!string.IsNullOrEmpty(updatedBook.SubCategory))
            book.SubCategory = updatedBook.SubCategory;

        await _context.SaveChangesAsync();
        
        Console.WriteLine($"✅ Successfully updated book ID {id}");
        
        return Ok(new { 
            success = true, 
            message = "Book updated successfully",
            data = new
            {
                id = book.Id,
                bookName = book.BookName,
                category = book.Category,
                subCategory = book.SubCategory
            }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error updating hard drive book: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return StatusCode(500, new { success = false, error = ex.Message });
    }
}

// GET: api/books/debug-hard-drive
[HttpGet("debug-hard-drive")]
public async Task<IActionResult> DebugHardDrive()
{
    try
    {
        var result = new
        {
            hasHardDriveBooks = _context.HardDriveBooks != null,
            count = 0,
            sampleData = new List<object>(),
            error = (string?)null
        };
        
        if (_context.HardDriveBooks != null)
        {
            try
            {
                var count = await _context.HardDriveBooks.CountAsync();
                var sample = await _context.HardDriveBooks.Take(5).ToListAsync();
                
                result = result with { 
                    count = count,
                    sampleData = sample.Select(b => new {
                        b.Id,
                        b.BookName,
                        b.Category,
                        b.SubCategory,
                        b.BookPath
                    }).ToList<object>()
                };
            }
            catch (Exception ex)
            {
                result = result with { error = ex.Message };
            }
        }
        
        return Ok(result);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
    }
}


[HttpGet("total-book-count")]
public async Task<IActionResult> GetTotalBookCount()
{
    try
    {
        Console.WriteLine("=== Getting Total Book Count ===");
        
        // Count vendor books
        int vendorBookCount = await _context.Books.CountAsync();
        
        // Count hard drive books
        int hardDriveBookCount = await _context.HardDriveBooks.CountAsync();
        
        int totalBooks = vendorBookCount + hardDriveBookCount;
        
        Console.WriteLine($"Total Books: {totalBooks} (Vendor: {vendorBookCount}, Hard Drive: {hardDriveBookCount})");
        
        return Ok(new
        {
            success = true,
            totalBooks = totalBooks
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error getting total book count: {ex.Message}");
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}


// Delete hard drive book
[HttpDelete("hard-drive-books/{id}")]
public async Task<IActionResult> DeleteHardDriveBook(int id)
{
    try
    {
        var book = await _context.HardDriveBooks.FindAsync(id);
        if (book == null)
            return NotFound("Book not found");

        // Optional: Delete the actual file from hard drive
        if (!string.IsNullOrEmpty(book.BookPath) && System.IO.File.Exists(book.BookPath))
        {
            System.IO.File.Delete(book.BookPath);
        }

        _context.HardDriveBooks.Remove(book);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Book deleted successfully" });
    }
    catch (Exception ex)
    {
        return StatusCode(500, $"Error deleting hard drive book: {ex.Message}");
    }
}



    }
}
























using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ebook_backend_api.Data;
using ebook_backend_api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ebook_backend_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PurchaseRequestsController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================
        // DTOs
        // ============================================
        public class CreateRequestDto
        {
            public int CustomerId { get; set; }
            public List<int> BookIds { get; set; } = new();
        }

        // ============================================
        // 1) CREATE PURCHASE REQUEST (customer checkout)
        // ============================================
        [HttpPost("create")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRequestDto dto)
        {
            try
            {
                if (dto == null || dto.CustomerId <= 0 || dto.BookIds == null || dto.BookIds.Count == 0)
                    return BadRequest(new { success = false, message = "Invalid request" });

                // Fetch books
                var books = await _context.Books
                    .Where(b => dto.BookIds.Contains(b.BookId))
                    .ToListAsync();

                if (books.Count != dto.BookIds.Count)
                    return BadRequest(new { success = false, message = "Some books not found" });

                // Group by vendor — each vendor gets their own request
                var byVendor = books.GroupBy(b => b.VendorId);
                var createdRequests = new List<object>();

                foreach (var group in byVendor)
                {
                    var vendorId = group.Key;
                    var vendorBooks = group.ToList();
                    var total = vendorBooks.Sum(b => b.Price);

                    var request = new PurchaseRequest
                    {
                        CustomerId = dto.CustomerId,
                        VendorId = vendorId,
                        TotalAmount = total,
                        Status = "pending",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.PurchaseRequests.Add(request);
                    await _context.SaveChangesAsync();

                    // Add items
                    foreach (var book in vendorBooks)
                    {
                        _context.PurchaseRequestItems.Add(new PurchaseRequestItem
                        {
                            RequestId = request.RequestId,
                            BookId = book.BookId,
                            Price = book.Price
                        });
                    }

                    // Remove from cart (in case it's still there)
                    var cartItems = await _context.Carts
                        .Where(c => c.CustomerId == dto.CustomerId && vendorBooks.Select(b => b.BookId).Contains(c.BookId))
                        .ToListAsync();
                    _context.Carts.RemoveRange(cartItems);

                    await _context.SaveChangesAsync();

                    createdRequests.Add(new
                    {
                        requestId = request.RequestId,
                        vendorId = request.VendorId,
                        totalAmount = request.TotalAmount,
                        status = request.Status
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = $"{createdRequests.Count} request(s) created",
                    requests = createdRequests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // 2) CUSTOMER — Get their requests
        // ============================================
        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetCustomerRequests(int customerId)
        {
            try
            {
                var requests = await _context.PurchaseRequests
                    .Where(r => r.CustomerId == customerId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                var vendorIds = requests.Select(r => r.VendorId).Distinct().ToList();
                var vendors = await _context.Vendors
                    .Where(v => vendorIds.Contains(v.VendorId))
                    .ToDictionaryAsync(v => v.VendorId);

                var result = new List<object>();
                foreach (var req in requests)
                {
                    var items = await _context.PurchaseRequestItems
                        .Where(i => i.RequestId == req.RequestId)
                        .ToListAsync();

                    var bookIds = items.Select(i => i.BookId).ToList();
                    var books = await _context.Books
                        .Where(b => bookIds.Contains(b.BookId))
                        .ToDictionaryAsync(b => b.BookId);

                    vendors.TryGetValue(req.VendorId, out var vendor);

                    result.Add(new
                    {
                        requestId = req.RequestId,
                        vendorId = req.VendorId,
                        vendorName = vendor?.FullName ?? "Unknown",
                        totalAmount = req.TotalAmount,
                        status = req.Status,
                        createdAt = req.CreatedAt,
                        approvedAt = req.ApprovedAt,
                        vendorNote = req.VendorNote,
                        items = items.Select(i => new
                        {
                            bookId = i.BookId,
                            title = books.TryGetValue(i.BookId, out var b) ? b.Title : "Unknown",
                            price = i.Price
                        })
                    });
                }

                return Ok(new { success = true, requests = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // 3) VENDOR — Get their pending requests
        // ============================================
        [HttpGet("vendor/{vendorId}")]
        public async Task<IActionResult> GetVendorRequests(int vendorId, [FromQuery] string? status = null)
        {
            try
            {
                var query = _context.PurchaseRequests
                    .Where(r => r.VendorId == vendorId);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(r => r.Status == status);

                var requests = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                var customerIds = requests.Select(r => r.CustomerId).Distinct().ToList();
                var customers = await _context.Customers
                    .Where(c => customerIds.Contains(c.CustomerId))
                    .ToDictionaryAsync(c => c.CustomerId);

                var result = new List<object>();
                foreach (var req in requests)
                {
                    var items = await _context.PurchaseRequestItems
                        .Where(i => i.RequestId == req.RequestId)
                        .ToListAsync();

                    var bookIds = items.Select(i => i.BookId).ToList();
                    var books = await _context.Books
                        .Where(b => bookIds.Contains(b.BookId))
                        .ToDictionaryAsync(b => b.BookId);

                    customers.TryGetValue(req.CustomerId, out var customer);

                    result.Add(new
                    {
                        requestId = req.RequestId,
                        customerId = req.CustomerId,
                        customerName = customer?.FullName ?? "Unknown",
                        customerEmail = customer?.Email ?? "",
                        totalAmount = req.TotalAmount,
                        status = req.Status,
                        createdAt = req.CreatedAt,
                        approvedAt = req.ApprovedAt,
                        vendorNote = req.VendorNote,
                        items = items.Select(i => new
                        {
                            bookId = i.BookId,
                            title = books.TryGetValue(i.BookId, out var b) ? b.Title : "Unknown",
                            price = i.Price
                        })
                    });
                }

                return Ok(new { success = true, requests = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // 4) VENDOR — Approve request
        // ============================================
        [HttpPost("{requestId}/approve")]
        public async Task<IActionResult> ApproveRequest(int requestId, [FromBody] ApproveDto dto)
        {
            try
            {
                var request = await _context.PurchaseRequests.FindAsync(requestId);
                if (request == null)
                    return NotFound(new { success = false, message = "Request not found" });

                if (request.Status != "pending")
                    return BadRequest(new { success = false, message = $"Request is already {request.Status}" });

                request.Status = "approved";
                request.ApprovedAt = DateTime.UtcNow;
                request.VendorNote = dto?.VendorNote;

                // Add books to purchased_books
                var items = await _context.PurchaseRequestItems
                    .Where(i => i.RequestId == requestId)
                    .ToListAsync();

                foreach (var item in items)
                {
                    // Prevent duplicates
                    var exists = await _context.PurchasedBooks
                        .AnyAsync(pb => pb.CustomerId == request.CustomerId && pb.BookId == item.BookId);

                    if (!exists)
                    {
                        _context.PurchasedBooks.Add(new PurchasedBook
                        {
                            CustomerId = request.CustomerId,
                            BookId = item.BookId,
                            VendorId = request.VendorId,
                            RequestId = request.RequestId,
                            PurchasedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Request approved. Books added to customer library." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public class ApproveDto
        {
            public string? VendorNote { get; set; }
        }

        // ============================================
        // 5) VENDOR — Reject request
        // ============================================
        [HttpPost("{requestId}/reject")]
        public async Task<IActionResult> RejectRequest(int requestId, [FromBody] ApproveDto dto)
        {
            try
            {
                var request = await _context.PurchaseRequests.FindAsync(requestId);
                if (request == null)
                    return NotFound(new { success = false, message = "Request not found" });

                if (request.Status != "pending")
                    return BadRequest(new { success = false, message = $"Request is already {request.Status}" });

                request.Status = "rejected";
                request.ApprovedAt = DateTime.UtcNow;
                request.VendorNote = dto?.VendorNote;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Request rejected" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // 6) CUSTOMER — Get purchased books (My Books)
        // ============================================
        [HttpGet("my-books/{customerId}")]
        public async Task<IActionResult> GetMyBooks(int customerId)
        {
            try
            {
                var purchases = await _context.PurchasedBooks
                    .Where(pb => pb.CustomerId == customerId)
                    .OrderByDescending(pb => pb.PurchasedAt)
                    .ToListAsync();

                var bookIds = purchases.Select(p => p.BookId).ToList();
                var books = await _context.Books
                    .Where(b => bookIds.Contains(b.BookId))
                    .ToDictionaryAsync(b => b.BookId);

                var vendorIds = purchases.Select(p => p.VendorId).Distinct().ToList();
                var vendors = await _context.Vendors
                    .Where(v => vendorIds.Contains(v.VendorId))
                    .ToDictionaryAsync(v => v.VendorId);

                var result = purchases.Select(pb =>
                {
                    books.TryGetValue(pb.BookId, out var book);
                    vendors.TryGetValue(pb.VendorId, out var vendor);

                    string fileUrl = "";
                    if (book != null && !string.IsNullOrEmpty(book.BookPath))
                    {
                        if (book.BookPath.StartsWith("/mnt/") || book.BookPath.StartsWith("/home/"))
                            fileUrl = $"/api/books/serve-hard-drive-book?filePath={Uri.EscapeDataString(book.BookPath)}";
                        else
                            fileUrl = $"/api/books/serve-file/{book.VendorId}/{Uri.EscapeDataString(book.BookPath)}";
                    }

                    return new
                    {
                        bookId = pb.BookId,
                        title = book?.Title ?? "Unknown",
                        vendorName = vendor?.FullName ?? "Unknown",
                        price = book?.Price ?? 0,
                        filePath = fileUrl,
                        bookCover = book != null && !string.IsNullOrEmpty(book.BookCoverPath)
                            ? $"/api/books/serve-file/{book.VendorId}/{Uri.EscapeDataString(book.BookCoverPath)}"
                            : "",
                        purchasedAt = pb.PurchasedAt
                    };
                }).ToList();

                return Ok(new { success = true, books = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // 7) CHECK ACCESS — Can customer open this book?
        // ============================================
        [HttpGet("can-access")]
        public async Task<IActionResult> CanAccess([FromQuery] int customerId, [FromQuery] int bookId)
        {
            try
            {
                var book = await _context.Books.FindAsync(bookId);
                if (book == null)
                    return NotFound(new { success = false, message = "Book not found" });

                // Free books → always accessible
                if (book.Price <= 0)
                    return Ok(new { canAccess = true, reason = "free" });

                // Purchased → accessible
                var purchased = await _context.PurchasedBooks
                    .AnyAsync(pb => pb.CustomerId == customerId && pb.BookId == bookId);

                if (purchased)
                    return Ok(new { canAccess = true, reason = "purchased" });

                return Ok(new { canAccess = false, reason = "not_purchased", price = book.Price });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
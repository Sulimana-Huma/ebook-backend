using ebook_backend_api.Data;
using ebook_backend_api.Models;
using ebook_backend_api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ebook_backend_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomersController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================
        // GET: api/customers — List all customers (admin)
        // ============================================
        [HttpGet]
        public async Task<IActionResult> GetAllCustomers()
        {
            try
            {
                var customers = await _context.Customers
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        customerId = c.CustomerId,
                        fullName = c.FullName,
                        email = c.Email,
                        accountType = c.AccountType,
                        gender = c.Gender,
                        dateOfBirth = c.DateOfBirth,
                        country = c.Country,
                        createdAt = c.CreatedAt
                    })
                    .ToListAsync();

                return Ok(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // GET: api/customers/{id}
        // ============================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound(new { message = "Customer not found" });

            return Ok(new
            {
                customerId = customer.CustomerId,
                fullName = customer.FullName,
                email = customer.Email,
                accountType = customer.AccountType,
                gender = customer.Gender,
                dateOfBirth = customer.DateOfBirth,
                country = customer.Country,
                createdAt = customer.CreatedAt
            });
        }

        // ============================================
        // POST: api/customers — Register
        // ============================================
        [HttpPost]
        public IActionResult RegisterCustomer([FromBody] Customer customer)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Customers.Add(customer);
            _context.SaveChanges();

            return Ok(new { message = "Customer registered successfully" });
        }

        // ============================================
        // PUT: api/customers/{id} — Admin updates customer details
        // ============================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { success = false, message = "No data received" });

                var customer = await _context.Customers.FindAsync(id);
                if (customer == null)
                    return NotFound(new { success = false, message = "Customer not found" });

                // Update only provided fields
                if (!string.IsNullOrWhiteSpace(dto.FullName))
                    customer.FullName = dto.FullName.Trim();

                if (!string.IsNullOrWhiteSpace(dto.Email))
                    customer.Email = dto.Email.Trim();

                if (!string.IsNullOrWhiteSpace(dto.AccountType))
                    customer.AccountType = dto.AccountType.Trim();

                if (!string.IsNullOrWhiteSpace(dto.Gender))
                    customer.Gender = dto.Gender.Trim();

                if (dto.DateOfBirth.HasValue)
                    customer.DateOfBirth = dto.DateOfBirth.Value;

                if (!string.IsNullOrWhiteSpace(dto.Country))
                    customer.Country = dto.Country.Trim();

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Customer updated successfully",
                    data = new
                    {
                        customerId = customer.CustomerId,
                        fullName = customer.FullName,
                        email = customer.Email,
                        accountType = customer.AccountType,
                        gender = customer.Gender,
                        dateOfBirth = customer.DateOfBirth,
                        country = customer.Country,
                        createdAt = customer.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // DELETE: api/customers/{id} — Admin deletes customer
        // ============================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null)
                    return NotFound(new { success = false, message = "Customer not found" });

                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Customer deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to delete customer. It may have related sales.",
                    error = ex.Message
                });
            }
        }























// ============================================
// CART ENDPOINTS
// ============================================

// GET: api/customers/{customerId}/cart
[HttpGet("{customerId}/cart")]
public async Task<IActionResult> GetCart(int customerId)
{
    try
    {
        var items = await _context.Carts
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.DateAdded)
            .ToListAsync();

        var bookIds = items.Select(i => i.BookId).ToList();
        var books = await _context.Books
            .Where(b => bookIds.Contains(b.BookId))
            .Select(b => new
            {
                b.BookId,
                b.Title,
                b.Price,
                b.VendorId,
                b.BookPath,
                b.BookCoverPath,
                b.CategoryId
            })
            .ToListAsync();

        var bookById = books.ToDictionary(b => b.BookId, b => b);

        var result = items.Select(i =>
        {
            var book = bookById.GetValueOrDefault(i.BookId);
            return new
            {
                cartId = i.CartId,
                customerId = i.CustomerId,
                bookId = i.BookId,
                vendorId = i.VendorId,
                dateAdded = i.DateAdded,
                title = book?.Title ?? "Unknown Book",
                price = book?.Price ?? 0,
                bookCover = BuildBookUrlStatic(i.VendorId, book?.BookCoverPath)
            };
        }).ToList();

        return Ok(new { success = true, items = result });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}

// POST: api/customers/{customerId}/cart
[HttpPost("{customerId}/cart")]
public async Task<IActionResult> AddToCart(int customerId, [FromBody] AddToCartDto dto)
{
    try
    {
        if (dto == null || dto.BookId <= 0)
            return BadRequest(new { success = false, message = "Invalid book ID" });

        var book = await _context.Books.FindAsync(dto.BookId);
        if (book == null)
            return NotFound(new { success = false, message = "Book not found" });

        // Only allow paid books in cart
        if (book.Price <= 0)
            return BadRequest(new { success = false, message = "This book is free — no need to add to cart" });

        // Prevent duplicates
        var existing = await _context.Carts
            .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.BookId == dto.BookId);
        if (existing != null)
            return Ok(new { success = true, message = "Book already in cart", cartId = existing.CartId });

        var cart = new Cart
        {
            CustomerId = customerId,
            BookId = dto.BookId,
            VendorId = book.VendorId,
            DateAdded = DateTime.UtcNow
        };

        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Book added to cart", cartId = cart.CartId });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}

// DELETE: api/customers/{customerId}/cart/{cartId}
[HttpDelete("{customerId}/cart/{cartId}")]
public async Task<IActionResult> RemoveFromCart(int customerId, int cartId)
{
    try
    {
        var item = await _context.Carts
            .FirstOrDefaultAsync(c => c.CartId == cartId && c.CustomerId == customerId);
        if (item == null)
            return NotFound(new { success = false, message = "Cart item not found" });

        _context.Carts.Remove(item);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Item removed from cart" });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}

// DELETE: api/customers/{customerId}/cart (clear all)
[HttpDelete("{customerId}/cart")]
public async Task<IActionResult> ClearCart(int customerId)
{
    try
    {
        var items = await _context.Carts.Where(c => c.CustomerId == customerId).ToListAsync();
        _context.Carts.RemoveRange(items);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Cart cleared" });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}

// Helper for cart URL building
private static string BuildBookUrlStatic(int vendorId, string? bookPath)
{
    if (string.IsNullOrEmpty(bookPath)) return "";
    if (bookPath.StartsWith("/mnt/") || bookPath.StartsWith("/home/"))
        return $"/api/books/serve-hard-drive-book?filePath={Uri.EscapeDataString(bookPath)}";
    return $"/api/books/serve-file/{vendorId}/{Uri.EscapeDataString(bookPath)}";
}
    }
}

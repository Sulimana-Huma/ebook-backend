using ebook_backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ebook_backend_api.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ebook_backend_api.Models;

namespace ebook_backend_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new
            {
                success = true,
                message = "AdminController is working!",
                timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Select(c => new { Id = c.Id, Name = c.CategoryName, ParentId = c.ParentId })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    categories = categories,
                    count = categories.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("books")]
        public async Task<IActionResult> GetAllBooks()
        {
            try
            {
                var allBooks = new List<object>();

                // ===== Main books table =====
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
    category_id = b.category_id,
    table_source = "books",
    created_at = b.created_at,
    file_path = string.IsNullOrEmpty(b.book_path) ? "" : $"/api/books/serve-file/{b.vendor_id}/{Uri.EscapeDataString(b.book_path)}",
    book_cover = string.IsNullOrEmpty(b.book_cover_path) ? "" : $"/api/books/serve-file/{b.vendor_id}/{Uri.EscapeDataString(b.book_cover_path)}"
}).ToList();

                allBooks.AddRange(mainBooks);

                // ===== Hard Drive Books table =====
                var hardDriveBooks = await _context.HardDriveBooks
                    .Select(h => new
                    {
                        book_id = (int?)null,
                        id = h.Id,
                        title = h.BookName,
                        price = (decimal?)null,
                        category = h.Category,
                        sub_category = h.SubCategory,
                        table_source = "hard_drive_books",
                        created_at = h.CreatedAt
                    })
                    .ToListAsync();

                allBooks.AddRange(hardDriveBooks);

                return Ok(new
                {
                    success = true,
                    books = allBooks,
                    total_count = allBooks.Count,
                    main_books_count = mainBooks.Count,
                    hard_drive_books_count = hardDriveBooks.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("subcategories")]
        public async Task<IActionResult> GetSubCategories([FromQuery] string category)
        {
            try
            {
                if (string.IsNullOrEmpty(category))
                {
                    return Ok(new
                    {
                        success = true,
                        subCategories = new List<object>(),
                        message = "No category provided"
                    });
                }

                // Find the parent category by name
                var categoryEntity = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryName == category);

                if (categoryEntity == null)
                {
                    return Ok(new
                    {
                        success = true,
                        subCategories = new List<object>(),
                        message = $"Category '{category}' not found"
                    });
                }

                // Get children via ParentId
                var subCategories = await _context.Categories
                    .Where(c => c.ParentId == categoryEntity.Id)
                    .Select(c => new { Id = c.Id, Name = c.CategoryName })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    subCategories = subCategories,
                    count = subCategories.Count,
                    category = category,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateBook([FromBody] UpdateBookCategoryDto updateData)
        {
            try
            {
                if (updateData.TableSource == "books")
                {
                    var book = await _context.Books.FindAsync(updateData.BookId);
                    if (book == null)
                    {
                        return Ok(new
                        {
                            success = false,
                            message = "Book not found in main database"
                        });
                    }

                    book.Title = updateData.Title;

                    // Resolve a category by name (can be top-level OR child)
                    var targetCategoryName = !string.IsNullOrEmpty(updateData.SubCategory)
                        ? updateData.SubCategory
                        : updateData.Category;

                    if (!string.IsNullOrEmpty(targetCategoryName))
                    {
                        var category = await _context.Categories
                            .FirstOrDefaultAsync(c => c.CategoryName == targetCategoryName);
                        if (category != null)
                        {
                            book.CategoryId = category.Id;
                        }
                    }

                    if (updateData.Price.HasValue)
                    {
                        book.Price = updateData.Price.Value;
                    }

                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        success = true,
                        message = "Book updated successfully in main database",
                        book_id = updateData.BookId
                    });
                }
                else if (updateData.TableSource == "hard_drive_books")
                {
                    var hardDriveBook = await _context.HardDriveBooks.FindAsync(updateData.Id);
                    if (hardDriveBook == null)
                    {
                        return Ok(new
                        {
                            success = false,
                            message = "Book not found in hard drive database"
                        });
                    }

                    hardDriveBook.BookName = updateData.Title;
                    hardDriveBook.Category = updateData.Category;
                    hardDriveBook.SubCategory = updateData.SubCategory;

                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        success = true,
                        message = "Book updated successfully in hard drive database",
                        id = updateData.Id
                    });
                }
                else
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Invalid table source. Must be 'books' or 'hard_drive_books'"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // ============== CATEGORY MANAGEMENT ENDPOINTS ==============

        // Get hierarchical category tree (infinite levels)
        [HttpGet("categories-tree")]
        public async Task<IActionResult> GetCategoriesTree()
        {
            try
            {
                var allCategories = await _context.Categories.ToListAsync();

                var rootCategories = allCategories
                    .Where(c => c.ParentId == null)
                    .OrderBy(c => c.CategoryName)
                    .Select(c => MapToTree(c, allCategories))
                    .ToList();

                return Ok(new { success = true, categories = rootCategories });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Get all categories as flat list for dropdown
        [HttpGet("all-categories-flat")]
        public async Task<IActionResult> GetAllCategoriesFlat()
        {
            try
            {
                var allCategories = await _context.Categories.ToListAsync();

                var categoryDict = allCategories.ToDictionary(c => c.Id, c => c);
                var flatCategories = new List<object>();

                foreach (var cat in allCategories.OrderBy(c => c.ParentId ?? 0).ThenBy(c => c.CategoryName))
                {
                    string displayName = GetCategoryPath(cat, categoryDict);
                    flatCategories.Add(new
                    {
                        cat.Id,
                        cat.CategoryName,
                        cat.ParentId,
                        DisplayName = displayName
                    });
                }

                return Ok(new { success = true, categories = flatCategories });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Create or update category
        [HttpPost("save-category")]
        public async Task<IActionResult> SaveCategory([FromBody] CategoryDto categoryDto)
        {
            try
            {
                if (categoryDto.Id == 0)
                {
                    var category = new Category
                    {
                        CategoryName = categoryDto.CategoryName,
                        ParentId = categoryDto.ParentId == 0 ? null : categoryDto.ParentId
                    };
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, message = "Category created successfully", id = category.Id });
                }
                else
                {
                    var category = await _context.Categories.FindAsync(categoryDto.Id);
                    if (category == null)
                        return Ok(new { success = false, message = "Category not found" });

                    if (categoryDto.ParentId.HasValue && categoryDto.ParentId.Value == category.Id)
                        return Ok(new { success = false, message = "Cannot set category as its own parent" });

                    category.CategoryName = categoryDto.CategoryName;
                    category.ParentId = categoryDto.ParentId == 0 ? null : categoryDto.ParentId;
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, message = "Category updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Delete category
        [HttpDelete("delete-category/{id}")]
        public async Task<IActionResult> DeleteCategory(int id, [FromQuery] bool cascadeDelete = false)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.SubCategories)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                    return Ok(new { success = false, message = "Category not found" });

                if (cascadeDelete)
                {
                    await DeleteCategoryRecursive(id);
                }
                else
                {
                    foreach (var child in category.SubCategories ?? new List<Category>())
                    {
                        child.ParentId = category.ParentId;
                    }
                    _context.Categories.Remove(category);
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Category deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Get categories for dropdown (hierarchical)
        [HttpGet("categories-dropdown")]
        public async Task<IActionResult> GetCategoriesDropdown()
        {
            try
            {
                var allCategories = await _context.Categories.ToListAsync();
                var dropdownItems = new List<object>();

                var rootCategories = allCategories
                    .Where(c => c.ParentId == null)
                    .OrderBy(c => c.CategoryName);

                foreach (var root in rootCategories)
                {
                    dropdownItems.Add(new { id = root.Id, name = root.CategoryName, level = 0 });
                    AddChildCategories(dropdownItems, root.Id, allCategories, 1);
                }

                return Ok(new { success = true, categories = dropdownItems });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============== Helpers ==============

        private async Task DeleteCategoryRecursive(int categoryId)
        {
            var children = await _context.Categories
                .Where(c => c.ParentId == categoryId)
                .ToListAsync();

            foreach (var child in children)
            {
                await DeleteCategoryRecursive(child.Id);
            }

            var category = await _context.Categories.FindAsync(categoryId);
            if (category != null)
                _context.Categories.Remove(category);
        }

        private object MapToTree(Category category, List<Category> allCategories)
        {
            return new
            {
                id = category.Id,
                name = category.CategoryName,
                parent_id = category.ParentId,
                children = allCategories
                    .Where(c => c.ParentId == category.Id)
                    .OrderBy(c => c.CategoryName)
                    .Select(c => MapToTree(c, allCategories))
                    .ToList()
            };
        }

        private string GetCategoryPath(Category category, Dictionary<int, Category> dict)
        {
            var path = new List<string>();
            var current = category;

            while (current != null)
            {
                path.Insert(0, current.CategoryName);
                if (current.ParentId.HasValue && dict.ContainsKey(current.ParentId.Value))
                    current = dict[current.ParentId.Value];
                else
                    current = null;
            }

            return string.Join(" → ", path);
        }

        private void AddChildCategories(List<object> items, int parentId, List<Category> allCategories, int level)
        {
            var children = allCategories
                .Where(c => c.ParentId == parentId)
                .OrderBy(c => c.CategoryName);

            string indent = new string('-', level * 2);

            foreach (var child in children)
            {
                items.Add(new { id = child.Id, name = $"{indent}> {child.CategoryName}", level = level });
                AddChildCategories(items, child.Id, allCategories, level + 1);
            }
        }
    }

    // DTO class
    public class CategoryDto
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int? ParentId { get; set; }
    }
}







































/*using ebook_backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ebook_backend_api.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ebook_backend_api.Models;  // Add this for Category model

namespace ebook_backend_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { 
                success = true, 
                message = "AdminController is working!", 
                timestamp = DateTime.UtcNow 
            });
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Select(c => new { Id = c.Id, Name = c.CategoryName })
                    .ToListAsync();
                
                return Ok(new { 
                    success = true, 
                    categories = categories,
                    count = categories.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }


// In your BooksController.cs

// Get all hard drive books
[HttpGet("hard-drive-books")]
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
}

// Update hard drive book
[HttpPut("hard-drive-books/{id}")]
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


        [HttpGet("books")]
        public async Task<IActionResult> GetAllBooks()
        {
            try
            {
                var allBooks = new List<object>();
                
                // Get books from main books table
                var mainBooks = await _context.Books
                    .Select(b => new
                    {
                        book_id = b.BookId,
                        id = (int?)null,
                        title = b.Title,
                        price = b.Price,
                        category = b.Category,
                        sub_category = b.SubCategory,
                        table_source = "books",
                        created_at = b.CreatedAt
                    })
                    .ToListAsync();
                
                allBooks.AddRange(mainBooks);
                
                // Get books from Hard Drive Books table
                
                
                return Ok(new { 
                    success = true, 
                    books = allBooks,
                    total_count = allBooks.Count,
                    main_books_count = mainBooks.Count,
                    hard_drive_books_count = hardDriveBooks.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("subcategories")]
        public async Task<IActionResult> GetSubCategories([FromQuery] string category)
        {
            try
            {
                if (string.IsNullOrEmpty(category))
                {
                    return Ok(new { 
                        success = true, 
                        subCategories = new List<object>(),
                        message = "No category provided"
                    });
                }
                
                var categoryEntity = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryName == category);
                
                if (categoryEntity == null)
                {
                    return Ok(new { 
                        success = true, 
                        subCategories = new List<object>(),
                        message = $"Category '{category}' not found"
                    });
                }
                
                var subCategories = await _context.SubCategories
                    .Where(sc => sc.CategoryId == categoryEntity.Id)
                    .Select(sc => new { Id = sc.Id, Name = sc.SubCategoryName })
                    .ToListAsync();
                
                return Ok(new { 
                    success = true, 
                    subCategories = subCategories,
                    count = subCategories.Count,
                    category = category,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateBook([FromBody] UpdateBookCategoryDto updateData)
        {
            try
            {
                if (updateData.TableSource == "books")
                {
                    var book = await _context.Books.FindAsync(updateData.BookId);
                    if (book == null)
                    {
                        return Ok(new { 
                            success = false, 
                            message = "Book not found in main database" 
                        });
                    }
                    
                    book.Title = updateData.Title;
                    book.Category = updateData.Category;
                    book.SubCategory = updateData.SubCategory;
                    if (updateData.Price.HasValue)
                    {
                        book.Price = updateData.Price.Value;
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    return Ok(new { 
                        success = true, 
                        message = "Book updated successfully in main database",
                        book_id = updateData.BookId
                    });
                }
                else if (updateData.TableSource == "hard_drive_books")
                {
                    var hardDriveBook = await _context.HardDriveBooks.FindAsync(updateData.Id);
                    if (hardDriveBook == null)
                    {
                        return Ok(new { 
                            success = false, 
                            message = "Book not found in hard drive database" 
                        });
                    }
                    
                    hardDriveBook.BookName = updateData.Title;
                    hardDriveBook.Category = updateData.Category;
                    hardDriveBook.SubCategory = updateData.SubCategory;
                    
                    await _context.SaveChangesAsync();
                    
                    return Ok(new { 
                        success = true, 
                        message = "Book updated successfully in hard drive database",
                        id = updateData.Id
                    });
                }
                else
                {
                    return Ok(new { 
                        success = false, 
                        message = "Invalid table source. Must be 'books' or 'hard_drive_books'" 
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
// ============== ADD THESE NEW ENDPOINTS TO AdminController.cs ==============

// Get hierarchical category tree (infinite levels)
[HttpGet("categories-tree")]
public async Task<IActionResult> GetCategoriesTree()
{
    try
    {
        var allCategories = await _context.Categories
            .Include(c => c.SubCategories)
            .ToListAsync();
        
        var rootCategories = allCategories
            .Where(c => c.ParentId == null)
            .Select(c => MapToTree(c, allCategories))
            .ToList();
        
        return Ok(new { success = true, categories = rootCategories });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}

// Get all categories as flat list for dropdown (with indented names)
[HttpGet("all-categories-flat")]
public async Task<IActionResult> GetAllCategoriesFlat()
{
    try
    {
        var allCategories = await _context.Categories.ToListAsync();
        
        // Build hierarchical display names
        var categoryDict = allCategories.ToDictionary(c => c.Id, c => c);
        var flatCategories = new List<object>();
        
        foreach (var cat in allCategories.OrderBy(c => c.ParentId ?? 0).ThenBy(c => c.CategoryName))
        {
            string displayName = GetCategoryPath(cat, categoryDict);
            flatCategories.Add(new { 
                cat.Id, 
                cat.CategoryName, 
                cat.ParentId,
                DisplayName = displayName
            });
        }
        
        return Ok(new { success = true, categories = flatCategories });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}

// Create or update category
[HttpPost("save-category")]
public async Task<IActionResult> SaveCategory([FromBody] CategoryDto categoryDto)
{
    try
    {
        if (categoryDto.Id == 0)
        {
            // Create new category
            var category = new Category
            {
                CategoryName = categoryDto.CategoryName,
                ParentId = categoryDto.ParentId == 0 ? null : categoryDto.ParentId
            };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Category created successfully", id = category.Id });
        }
        else
        {
            // Update existing category
            var category = await _context.Categories.FindAsync(categoryDto.Id);
            if (category == null)
                return Ok(new { success = false, message = "Category not found" });
            
            // Prevent circular reference
            if (categoryDto.ParentId.HasValue && categoryDto.ParentId.Value == category.Id)
                return Ok(new { success = false, message = "Cannot set category as its own parent" });
            
            category.CategoryName = categoryDto.CategoryName;
            category.ParentId = categoryDto.ParentId == 0 ? null : categoryDto.ParentId;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Category updated successfully" });
        }
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}

// Delete category
[HttpDelete("delete-category/{id}")]
public async Task<IActionResult> DeleteCategory(int id, [FromQuery] bool cascadeDelete = false)
{
    try
    {
        var category = await _context.Categories
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id);
        
        if (category == null)
            return Ok(new { success = false, message = "Category not found" });
        
        if (cascadeDelete)
        {
            // Recursively delete all subcategories
            await DeleteCategoryRecursive(id);
        }
        else
        {
            // Reassign children to parent
            foreach (var child in category.SubCategories)
            {
                child.ParentId = category.ParentId;
            }
            _context.Categories.Remove(category);
        }
        
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Category deleted successfully" });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}

// Helper methods
private async Task DeleteCategoryRecursive(int categoryId)
{
    var children = await _context.Categories
        .Where(c => c.ParentId == categoryId)
        .ToListAsync();
    
    foreach (var child in children)
    {
        await DeleteCategoryRecursive(child.Id);
    }
    
    var category = await _context.Categories.FindAsync(categoryId);
    if (category != null)
        _context.Categories.Remove(category);
}

private object MapToTree(Category category, List<Category> allCategories)
{
    return new
    {
        id = category.Id,
        name = category.CategoryName,
        parent_id = category.ParentId,
        children = allCategories
            .Where(c => c.ParentId == category.Id)
            .OrderBy(c => c.CategoryName)
            .Select(c => MapToTree(c, allCategories))
            .ToList()
    };
}

private string GetCategoryPath(Category category, Dictionary<int, Category> dict)
{
    var path = new List<string>();
    var current = category;
    
    while (current != null)
    {
        path.Insert(0, current.CategoryName);
        if (current.ParentId.HasValue && dict.ContainsKey(current.ParentId.Value))
            current = dict[current.ParentId.Value];
        else
            current = null;
    }
    
    return string.Join(" → ", path);
}

// DTO class (add inside AdminController.cs)
public class CategoryDto
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? ParentId { get; set; }
}

    }
}*/

using Microsoft.AspNetCore.Mvc;
using ebook_backend_api.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ebook_backend_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                var raw = await _context.Categories
                    .Select(c => new
                    {
                        id = c.Id,
                        categoryName = c.CategoryName,
                        parentId = c.ParentId
                    })
                    .ToListAsync();

                var result = raw
                    .OrderBy(c => c.parentId)
                    .ThenBy(c => c.categoryName)
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/categories/main
        [HttpGet("main")]
        public async Task<IActionResult> GetMainCategories()
        {
            try
            {
                var raw = await _context.Categories
                    .Where(c => c.ParentId == null)
                    .Select(c => new { id = c.Id, categoryName = c.CategoryName })
                    .ToListAsync();

                return Ok(raw.OrderBy(c => c.categoryName).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/categories/{id}/subcategories
        [HttpGet("{id}/subcategories")]
        public async Task<IActionResult> GetSubCategories(int id)
        {
            try
            {
                var raw = await _context.Categories
                    .Where(c => c.ParentId == id)
                    .Select(c => new
                    {
                        id = c.Id,
                        categoryName = c.CategoryName,
                        parentId = c.ParentId
                    })
                    .ToListAsync();

                return Ok(raw.OrderBy(c => c.categoryName).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/categories/with-books
        // Returns root categories that have at least one book directly
        // assigned to them OR to any of their subcategories.
        [HttpGet("with-books")]
        public async Task<IActionResult> GetCategoriesWithBooks()
        {
            try
            {
                // Load everything into memory — small table, simplest approach
                var allCategories = await _context.Categories.ToListAsync();
                var bookCategoryIds = await _context.Books
                    .Where(b => b.CategoryId != null)
                    .Select(b => b.CategoryId!.Value)
                    .Distinct()
                    .ToListAsync();

                var rootCategories = allCategories
                    .Where(c => c.ParentId == null)
                    .Select(c => new
                    {
                        id = c.Id,
                        category_name = c.CategoryName,
                        subCategories = allCategories
                            .Where(sc => sc.ParentId == c.Id
                                         && bookCategoryIds.Contains(sc.Id))
                            .OrderBy(sc => sc.CategoryName)
                            .Select(sc => new { id = sc.Id, sub_category_name = sc.CategoryName })
                            .ToList()
                    })
                    .Where(c => c.subCategories.Any())
                    .ToList();

                return Ok(rootCategories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}

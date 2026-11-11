using ebook_backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace ebook_backend_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetDashboardStats()
        {
            var stats = new
            {
                totalVendors = _context.Vendors.Count(),
                totalCustomers = _context.Customers.Count(),
                totalBooks = _context.Books.Count(),
                totalSales = _context.Sales.Count(),
                totalCategories = _context.Categories.Count(),
                totalSubCategories = _context.Categories.Count(c => c.ParentId != null)
            };

            return Ok(stats);
        }





    }
}

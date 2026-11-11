using ebook_backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ebook_backend_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TestController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("database")]
        public async Task<IActionResult> TestDatabaseConnection()
        {
            try
            {
                // Test if we can connect to the database
                var canConnect = await _context.Database.CanConnectAsync();

                if (canConnect)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "✅ Database connection successful!",
                        database = _context.Database.GetDbConnection().Database,
                        server = _context.Database.GetDbConnection().DataSource,
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "❌ Database connection failed",
                        timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "❌ Database connection error",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("tables")]
        public async Task<IActionResult> CheckTables()
        {
            try
            {
                // Check if customers table exists and has data
                var customerCount = await _context.Customers.CountAsync();
                var vendorCount = await _context.Vendors.CountAsync();

                return Ok(new
                {
                    success = true,
                    message = "✅ Tables checked successfully!",
                    customers_count = customerCount,
                    vendors_count = vendorCount,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "❌ Error checking tables",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
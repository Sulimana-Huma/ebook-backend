using ebook_backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using ebook_backend_api.Models;

namespace ebook_backend_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var customers = _context.Customers.ToList();
            return Ok(customers);
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                return BadRequest(new { message = "Email and Password are required" });

            var admin = _context.Admins
       .FirstOrDefault(a => a.Email == request.Email && a.Password == request.Password);

            if (admin != null)
            {
                return Ok(new
                {
                    message = "Login successful",
                    userType = "Admin",
                    user = admin
                });
            }
            // Search in Customers
            var customer = _context.Customers
                .FirstOrDefault(c => c.Email == request.Email && c.PasswordHash == request.Password);

            if (customer != null)
            {
                return Ok(new
                {
                    message = "Login successful",
                    userType = "Customer",
                    user = customer
                });
            }

            // Search in Vendors
            var vendor = _context.Vendors
                .FirstOrDefault(v => v.Email == request.Email && v.PasswordHash == request.Password);

            if (vendor != null)
            {
                return Ok(new
                {
                    message = "Login successful",
                    userType = "Vendor",
                    user = vendor
                });
            }

            return Unauthorized(new { message = "Invalid email or password" });
        }
    }

    // DTO for login request
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}


using Microsoft.AspNetCore.Mvc;
using ebook_backend_api.Models;
using System.Linq;
using ebook_backend_api.Data;

namespace ebook_backend_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CountriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CountriesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetCountries()
        {
            var countries = _context.Countries
                                    .OrderBy(c => c.CountryName)
                                    .ToList();
            return Ok(countries);
        }
    }
}

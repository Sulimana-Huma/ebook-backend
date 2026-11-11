using ebook_backend_api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class PaymentMethodsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PaymentMethodsController(AppDbContext context)
    {
        _context = context;
    }

    // ✅ Get all methods
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var methods = await _context.PaymentMethods.ToListAsync();
        return Ok(methods);
    }

    // ✅ Toggle enable/disable
    [HttpPut("{id}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var method = await _context.PaymentMethods.FindAsync(id);
        if (method == null) return NotFound();

        method.IsEnabled = !method.IsEnabled;
        await _context.SaveChangesAsync();

        return Ok(method);
    }

    // ✅ Get only enabled methods
    [HttpGet("enabled")]
    public async Task<IActionResult> GetEnabled()
    {
        var methods = await _context.PaymentMethods
            .Where(m => m.IsEnabled) // only enabled = true
            .ToListAsync();

        return Ok(methods);
    }

}

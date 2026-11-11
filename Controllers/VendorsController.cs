using ebook_backend_api.Data;
using ebook_backend_api.Dtos;
using ebook_backend_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class VendorsController : ControllerBase
{
    private readonly AppDbContext _context;

    public VendorsController(AppDbContext context)
    {
        _context = context;
    }

    // ============================================
    // GET: api/vendors — List all vendors (admin)
    // ============================================
    [HttpGet]
    public async Task<IActionResult> GetAllVendors()
    {
        try
        {
            var vendors = await _context.Vendors
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new
                {
                    vendorId = v.VendorId,
                    fullName = v.FullName,
                    email = v.Email,
                    accountType = v.AccountType,
                    gender = v.Gender,
                    dateOfBirth = v.DateOfBirth,
                    country = v.Country,
                    createdAt = v.CreatedAt
                })
                .ToListAsync();

            return Ok(vendors);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ============================================
    // GET: api/vendors/{id}
    // ============================================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetVendorById(int id)
    {
        var vendor = await _context.Vendors.FindAsync(id);
        if (vendor == null)
            return NotFound(new { message = "Vendor not found" });

        return Ok(new
        {
            vendorId = vendor.VendorId,
            fullName = vendor.FullName,
            email = vendor.Email,
            accountType = vendor.AccountType,
            gender = vendor.Gender,
            dateOfBirth = vendor.DateOfBirth,
            country = vendor.Country,
            createdAt = vendor.CreatedAt
        });
    }

    // ============================================
    // PUT: api/vendors/{id} — Admin updates vendor details
    // ============================================
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVendor(int id, [FromBody] UpdateVendorDto dto)
    {
        try
        {
            if (dto == null)
                return BadRequest(new { success = false, message = "No data received" });

            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null)
                return NotFound(new { success = false, message = "Vendor not found" });

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(dto.FullName))
                vendor.FullName = dto.FullName.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Email))
                vendor.Email = dto.Email.Trim();

            if (!string.IsNullOrWhiteSpace(dto.AccountType))
                vendor.AccountType = dto.AccountType.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Gender))
                vendor.Gender = dto.Gender.Trim();

            if (dto.DateOfBirth.HasValue)
                vendor.DateOfBirth = dto.DateOfBirth.Value;

            if (!string.IsNullOrWhiteSpace(dto.Country))
                vendor.Country = dto.Country.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Vendor updated successfully",
                data = new
                {
                    vendorId = vendor.VendorId,
                    fullName = vendor.FullName,
                    email = vendor.Email,
                    accountType = vendor.AccountType,
                    gender = vendor.Gender,
                    dateOfBirth = vendor.DateOfBirth,
                    country = vendor.Country,
                    createdAt = vendor.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ============================================
    // DELETE: api/vendors/{id} — Admin deletes vendor
    // ============================================
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVendor(int id)
    {
        try
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null)
                return NotFound(new { success = false, message = "Vendor not found" });

            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Vendor deleted successfully" });
        }
        catch (Exception ex)
        {
            // Common case: FK constraint (vendor has books, sales, etc.)
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to delete vendor. It may have related books/sales.",
                error = ex.Message
            });
        }
    }

    // ============================================
    // POST: api/vendors — Register a new vendor
    // ============================================
    [HttpPost]
    public IActionResult RegisterVendor([FromBody] Vendor vendor)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _context.Vendors.Add(vendor);
        _context.SaveChanges();

        return Ok(new { message = "Vendor registered successfully" });
    }

    // ============================================
    // Payment methods
    // ============================================
    [HttpGet("{vendorId}/payment-methods")]
    public async Task<ActionResult<IEnumerable<VendorPaymentMethod>>> GetPaymentMethods(int vendorId)
    {
        return await _context.VendorPaymentMethods
            .Where(pm => pm.VendorId == vendorId)
            .ToListAsync();
    }

   /* [HttpPost("{vendorId}/payment-methods")]
    public async Task<IActionResult> SavePaymentMethods(
        int vendorId,
        [FromBody] Dictionary<string, PaymentMethodDto> methods)
    {
        var existing = _context.VendorPaymentMethods.Where(pm => pm.VendorId == vendorId);
        _context.VendorPaymentMethods.RemoveRange(existing);

        foreach (var kv in methods)
        {
            var method = kv.Key;
            var details = kv.Value;

            if (details != null && details.Enabled)
            {
                var payment = new VendorPaymentMethod
                {
                    VendorId = vendorId,
                    Method = method,
                    AccountNumber = details.AccountNumber,
                    AccountTitle = details.AccountTitle,
                    Email = details.Email,
                    Enabled = details.Enabled
                };
                _context.VendorPaymentMethods.Add(payment);
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Payment methods saved successfully" });
    }*/

[HttpPost("{vendorId}/payment-methods")]
public async Task<IActionResult> SavePaymentMethods(
    int vendorId,
    [FromBody] Dictionary<string, PaymentMethodDto> methods)
{
    var existing = _context.VendorPaymentMethods.Where(pm => pm.VendorId == vendorId);
    _context.VendorPaymentMethods.RemoveRange(existing);

    foreach (var kv in methods)
    {
        var method = kv.Key;
        var details = kv.Value;

        if (details != null && details.Enabled)
        {
            var payment = new VendorPaymentMethod
            {
                VendorId = vendorId,
                Method = method,
                AccountNumber = details.AccountNumber,
                AccountTitle = details.AccountTitle,
                Email = details.Email,
                Enabled = details.Enabled
            };
            _context.VendorPaymentMethods.Add(payment);
        }
    }

    await _context.SaveChangesAsync();
    return Ok(new { message = "Payment methods saved successfully" });
}


}
































/*﻿using ebook_backend_api.Data;
using ebook_backend_api.Dtos;
using ebook_backend_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // for ToListAsync, Where, etc.
using System.Collections.Generic;   // for Dictionary<>
using System.Linq;                   // for LINQ Where()
using System.Threading.Tasks;        // for async/await


[Route("api/[controller]")]
[ApiController]
public class VendorsController : ControllerBase
{
    private readonly AppDbContext _context;

    public VendorsController(AppDbContext context)
    {
        _context = context;
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetVendorById(int id)
    {
        var vendor = await _context.Vendors.FindAsync(id);
        if (vendor == null) return NotFound(new { message = "Vendor not found" });
        return Ok(vendor);
    }

   


    // ✅ Update Vendor by ID
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVendor(int id, [FromBody] Vendor updatedVendor)
    {
        var vendor = await _context.Vendors.FindAsync(id);
        if (vendor == null) return NotFound(new { message = "Vendor not found" });

        vendor.FullName = updatedVendor.FullName;
        vendor.Email = updatedVendor.Email;
        vendor.Gender = updatedVendor.Gender;
        vendor.DateOfBirth = updatedVendor.DateOfBirth;
        vendor.Country = updatedVendor.Country;
        // vendor.PasswordHash = updatedVendor.PasswordHash; // only if you handle passwords here

        await _context.SaveChangesAsync();

        return Ok(new { message = "Vendor updated successfully", vendor });
    }

    [HttpGet("{vendorId}/payment-methods")]
    public async Task<ActionResult<IEnumerable<VendorPaymentMethod>>> GetPaymentMethods(int vendorId)
    {
        return await _context.VendorPaymentMethods
            .Where(pm => pm.VendorId == vendorId)
            .ToListAsync();
    }

[HttpPost]
public IActionResult RegisterVendor([FromBody] Vendor vendor)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    _context.Vendors.Add(vendor);
    _context.SaveChanges();

    return Ok(new { message = "Vendor registered successfully" });
}
    [HttpPost("{vendorId}/payment-methods")]
    public async Task<IActionResult> SavePaymentMethods(int vendorId, [FromBody] Dictionary<string, PaymentMethodDto> methods)
    {
        // Remove existing methods
        var existing = _context.VendorPaymentMethods.Where(pm => pm.VendorId == vendorId);
        _context.VendorPaymentMethods.RemoveRange(existing);

        foreach (var kv in methods)
        {
            var method = kv.Key; // jazzcash, paypal, etc.
            var details = kv.Value;

            if (details != null && details.Enabled)
            {
                var payment = new VendorPaymentMethod
                {
                    VendorId = vendorId,
                    Method = method,
                    AccountNumber = details.AccountNumber,
                    AccountTitle = details.AccountTitle,
                    Email = details.Email,
                    Enabled = details.Enabled
                };
                _context.VendorPaymentMethods.Add(payment);
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Payment methods saved successfully" });
    }

}*/

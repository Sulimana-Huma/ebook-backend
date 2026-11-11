namespace ebook_backend_api.Dtos
{
    public class UpdateVendorDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? AccountType { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Country { get; set; }
}
}

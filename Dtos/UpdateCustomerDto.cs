using Microsoft.AspNetCore.Http.HttpResults;

namespace ebook_backend_api.Dtos
{
    public class UpdateCustomerDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? AccountType { get; set; }   // ← ADD THIS
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Country { get; set; }
    }
}

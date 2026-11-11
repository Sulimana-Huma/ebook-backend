using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("vendor_payment_methods")]
    public class VendorPaymentMethod
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("vendor_id")]
        public int VendorId { get; set; }

        [Column("method")]
        public string Method { get; set; } = string.Empty;

        [Column("account_number")]
        public string? AccountNumber { get; set; }

        [Column("account_title")]
        public string? AccountTitle { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("enabled")]
        public bool Enabled { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

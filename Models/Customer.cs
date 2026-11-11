using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("customers")]  // map to existing MySQL table
    public class Customer
    {
        [Key]
        [Column("customer_id")]  // match DB column
        public int CustomerId { get; set; }

        [Column("full_name")]
        public string FullName { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("account_type")]
        public string AccountType { get; set; }

        [Column("password_hash")]
        public string PasswordHash { get; set; }

        [Column("gender")]
        public string Gender { get; set; }

        [Column("date_of_birth")]
        public DateTime? DateOfBirth { get; set; }

        [Column("country")]
        public string Country { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

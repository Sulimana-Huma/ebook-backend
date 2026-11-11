using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("admins")]
    public class Admin
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("password")]
        public string Password { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("cart")]
    public class Cart
    {
        [Key]
        [Column("cart_id")]
        public int CartId { get; set; }

        [Column("customer_id")]
        public int CustomerId { get; set; }

        [Column("book_id")]
        public int BookId { get; set; }

        [Column("vendor_id")]
        public int VendorId { get; set; }

        [Column("date_added")]
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    }
}
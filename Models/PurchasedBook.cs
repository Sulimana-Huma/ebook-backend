using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("purchased_books")]
    public class PurchasedBook
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("customer_id")]
        public int CustomerId { get; set; }

        [Column("book_id")]
        public int BookId { get; set; }

        [Column("vendor_id")]
        public int VendorId { get; set; }

        [Column("request_id")]
        public int RequestId { get; set; }

        [Column("purchased_at")]
        public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    }
}
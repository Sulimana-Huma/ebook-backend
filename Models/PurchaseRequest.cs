using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("purchase_requests")]
    public class PurchaseRequest
    {
        [Key]
        [Column("request_id")]
        public int RequestId { get; set; }

        [Column("customer_id")]
        public int CustomerId { get; set; }

        [Column("vendor_id")]
        public int VendorId { get; set; }

        [Column("total_amount")]
        public decimal TotalAmount { get; set; }

        [Column("status")]
        public string Status { get; set; } = "pending";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        [Column("vendor_note")]
        public string? VendorNote { get; set; }
    }
}
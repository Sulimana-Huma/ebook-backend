using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("purchase_request_items")]
    public class PurchaseRequestItem
    {
        [Key]
        [Column("item_id")]
        public int ItemId { get; set; }

        [Column("request_id")]
        public int RequestId { get; set; }

        [Column("book_id")]
        public int BookId { get; set; }

        [Column("price")]
        public decimal Price { get; set; }
    }
}
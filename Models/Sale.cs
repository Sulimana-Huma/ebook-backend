namespace ebook_backend_api.Models
{
    public class Sale
    {
        public int SaleId { get; set; }          // sale_id (Primary Key)
        public int BookId { get; set; }          // book_id (Foreign Key)
        public int CustomerId { get; set; }      // customer_id (Foreign Key)
        public int Quantity { get; set; }        // quantity
        public decimal TotalPrice { get; set; }  // total_price
        public DateTime SaleDate { get; set; }   // sale_date
    }
}

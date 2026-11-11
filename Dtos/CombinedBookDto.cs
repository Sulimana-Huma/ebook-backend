namespace ebook_backend_api.Dtos
{
    public class CombinedBookDto
    {
        public int? BookId { get; set; }
        public int? Id { get; set; }
        public string Title { get; set; }
        public decimal? Price { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Description { get; set; }
        public string TableSource { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

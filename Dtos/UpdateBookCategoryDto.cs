using System.Text.Json.Serialization;

namespace ebook_backend_api.Dtos
{
    public class UpdateBookCategoryDto
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }
        
        [JsonPropertyName("bookId")]
        public int? BookId { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; }
        
        [JsonPropertyName("category")]
        public string Category { get; set; }
        
        [JsonPropertyName("subCategory")]
        public string SubCategory { get; set; }
        
        [JsonPropertyName("price")]
        public decimal? Price { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
        
        [JsonPropertyName("tableSource")]
        public string TableSource { get; set; }
    }
}












/*namespace ebook_backend_api.Dtos
{
    public class UpdateBookCategoryDto
    {
        public int? Id { get; set; }
        public int? BookId { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public decimal? Price { get; set; }
        public string Description { get; set; }
        public string TableSource { get; set; }
    }
}*/

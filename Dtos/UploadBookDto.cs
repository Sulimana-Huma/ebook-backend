namespace ebook_backend_api.Dtos
{
    public class UploadBookDto
    {
        public IFormFile File { get; set; }            // ✅ PDF book file
        public IFormFile? BookCover { get; set; }      // ✅ Book cover (optional)
        public string Title { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public int VendorId { get; set; }
        public string Description { get; set; }
    }
}

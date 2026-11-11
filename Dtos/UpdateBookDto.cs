namespace ebook_backend_api.Dtos
{
    public class UpdateBookDto
    {
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int? CategoryId { get; set; }
    }
}







﻿
/*namespace ebook_backend_api.Dtos
{
    public class UpdateBookDto
    {
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
    }
}*/

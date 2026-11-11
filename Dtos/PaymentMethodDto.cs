namespace ebook_backend_api.Dtos
{
    public class PaymentMethodDto
    {
        public bool Enabled { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountTitle { get; set; }
        public string? Email { get; set; }
    }
}

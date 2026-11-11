using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("countries")]
    public class Country
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("country")]
        public string CountryName { get; set; }
    }
}

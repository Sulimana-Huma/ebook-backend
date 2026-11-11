namespace ebook_backend_api.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    [Table("books_folder_settings")]
    public class BooksFolderSetting
    {
        [Column("folder_path")]
        [JsonPropertyName("Path")]
        public string Path { get; set; }

        [Key]
        [Column("id")]
        [JsonPropertyName("Id")]
        public int Id { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
















/*namespace ebook_backend_api.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    [Table("books_folder_settings")]
    public class BooksFolderSetting
    {
        [Key]
        [Column("id")]
        [JsonPropertyName("Id")]
        public int Id { get; set; }

        [Column("folder_path")]
        [JsonPropertyName("Path")]
        public string Path { get; set; }

        [Column("is_active")]
        [JsonPropertyName("IsActive")]
        public bool IsActive { get; set; } = true;

        [Column("last_scanned")]
        [JsonPropertyName("LastScanned")]
        public DateTime? LastScanned { get; set; }

        [Column("created_at")]
        [JsonPropertyName("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        [JsonPropertyName("UpdatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
*/

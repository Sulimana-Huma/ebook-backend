using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("Hard Drive Books")]
public class HardDriveBook
{
    [Key]
    [Column("id")]  // Add this explicitly
    public int Id { get; set; }
    
    [Column("book_name")]  // Make sure this matches exactly
    public string? BookName { get; set; }
    
    [Column("category")]
    public string? Category { get; set; }
    
    [Column("sub_category")]  // Note the underscore
    public string? SubCategory { get; set; }
    
    [Column("book_path")]
    public string? BookPath { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
}























/*using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("Hard Drive Books")]
    public class HardDriveBook
    {
        [Key]
        public int Id { get; set; }
        
        [Column("book_name")]
        public string BookName { get; set; } = string.Empty;
        
        [Column("category")]
        public string Category { get; set; } = string.Empty;
        
        [Column("sub_category")]
        public string SubCategory { get; set; } = string.Empty;
        
        [Column("book_path")]
        public string BookPath { get; set; } = string.Empty;
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}*/

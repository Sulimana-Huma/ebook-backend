using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("books")]
    public class Book
    {
        [Key]
        [Column("book_id")]
        public int BookId { get; set; }

        [Required]
        [Column("vendor_id")]
        public int VendorId { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        // Single category FK (may point to a top-level OR child category)
        [Column("category_id")]
        public int? CategoryId { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        // Writable path columns
        [Column("book_path")]
        public string? BookPath { get; set; }

        [Column("book_cover_path")]
        public string? BookCoverPath { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Vendor? Vendor { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
    }
}






























/*using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("books")]
    public class Book
    {
        [Key]
        [Column("book_id")]
        public int BookId { get; set; }

        [Required]
        [Column("vendor_id")]
        public int VendorId { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        // Points to either a top-level category (parent_id=null)
        // OR a subcategory (parent_id = some parent)
        [Column("category_id")]
        public int? CategoryId { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("book_path")]
        public string? BookPath { get; set; }

        [Column("book_cover_path")]
        public string? BookCoverPath { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // MySQL computed columns — read only
        [Column("file_path")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? FilePath { get; set; }

        [Column("book_cover")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? BookCover { get; set; }

        // Navigation
        public Vendor? Vendor { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
    }
}*/





































/*using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("books")]
    public class Book
    {
        [Key]
        [Column("book_id")]
        public int BookId { get; set; }

        [Required]
        [Column("vendor_id")]
        public int VendorId { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Column("category_id")]
        public int? CategoryId { get; set; }

        [Column("sub_category")]
        public int? SubCategory { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("book_path")]
        public string? BookPath { get; set; }

        [Column("book_cover_path")]
        public string? BookCoverPath { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Vendor? Vendor { get; set; }
        
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
        
        [ForeignKey("SubCategory")]
        public virtual Category? SubCategoryCategory { get; set; }
        
        [NotMapped]
        public string FullFilePath => !string.IsNullOrEmpty(BookPath) ? $"/api/books/serve-file/{VendorId}/{BookPath}" : "";
        
        [NotMapped]
        public string FullBookCover => !string.IsNullOrEmpty(BookCoverPath) ? $"/api/books/serve-file/{VendorId}/{BookCoverPath}" : "";
    }
}
*/




























/*using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("books")]
    public class Book
    {
        [Key]
        [Column("book_id")]
        public int BookId { get; set; }

        [Required]
        [Column("vendor_id")]
        public int VendorId { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Column("sub_category")]  // This stores the subcategory ID (INT)
        public int SubCategory { get; set; }

        [Required]
        [Column("file_path")]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("book_cover")]
        public string BookCover { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Vendor? Vendor { get; set; }
        
        [ForeignKey("SubCategory")]
        public virtual Category? Category { get; set; }
        
        [NotMapped]
        public string FullFilePath => $"/api/books/serve-file/{VendorId}/{FilePath}";
        
        [NotMapped]
        public string FullBookCover => string.IsNullOrEmpty(BookCover) ? "" : $"/api/books/serve-file/{VendorId}/{BookCover}";
        
        [NotMapped]
        public string SubCategoryName => Category?.CategoryName ?? "";
        
        [NotMapped]
        public string MainCategoryName => Category?.ParentCategory?.CategoryName ?? "";
        
        [NotMapped]
        public string FullCategoryPath
        {
            get
            {
                if (Category == null) return "";
                var path = Category.CategoryName;
                if (Category.ParentCategory != null)
                {
                    path = $"{Category.ParentCategory.CategoryName} → {path}";
                }
                return path;
            }
        }
    }
}﻿
*/


























/*using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("books")]
    public class Book
    {
        [Key]
        [Column("book_id")]
        public int BookId { get; set; }   // ✅ matches DB column

        [Required]
        [Column("vendor_id")]
        public int VendorId { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("category")]
        public string Category { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("sub_category")]
        public string SubCategory { get; set; } = string.Empty;


        [Required]
        [Column("file_path")]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("book_cover")]
        public string BookCover { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Vendor? Vendor { get; set; }

    }
}
*/











/*using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("books")]
    public class Book
    {
        [Key]
        [Column("book_id")]
        public int BookId { get; set; }

        [Required]
        [Column("vendor_id")]
        public int VendorId { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("category")]
        public string Category { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("sub_category")]
        public string SubCategory { get; set; } = string.Empty;

        [Required]
        [Column("file_path")]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("book_cover")]
        public string BookCover { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // New columns for hard drive books
        [Column("is_from_hard_drive")]
        public bool IsFromHardDrive { get; set; } = false;

        [MaxLength(500)]
        [Column("source_relative_path")]
        public string SourceRelativePath { get; set; } = string.Empty;

        [Column("file_size")]
        public long? FileSize { get; set; }

        [MaxLength(500)]
        [Column("preview_image_path")]
        public string PreviewImagePath { get; set; } = string.Empty;

        public Vendor? Vendor { get; set; }
    }
}*/

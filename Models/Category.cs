using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ebook_backend_api.Models
{
    [Table("categories")]
    public class Category
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("category_name")]
        public string CategoryName { get; set; } = string.Empty;

        [Column("parent_id")]
        public int? ParentId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("ParentId")]
        [JsonIgnore]
        public virtual Category? ParentCategory { get; set; }

        [InverseProperty("ParentCategory")]
        public virtual ICollection<Category>? SubCategories { get; set; }
    }
}





































/*using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ebook_backend_api.Models
{
    [Table("categories")]
    public class Category
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("category_name")]
        public string CategoryName { get; set; } = string.Empty;

        [Column("parent_id")]
        public int? ParentId { get; set; }

        // Navigation properties for self-referencing hierarchy
        [ForeignKey("ParentId")]
        [JsonIgnore] // Prevents circular reference during JSON serialization
        public virtual Category? ParentCategory { get; set; }

        [InverseProperty("ParentCategory")]
        public virtual ICollection<Category>? SubCategories { get; set; }
    }
}
*/
























/*﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("categories")]
    public class Category
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("category_name")]
        public string CategoryName { get; set; } = string.Empty;
    }
}*/

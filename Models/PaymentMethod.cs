using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    [Table("payment_methods")]
    public class PaymentMethod
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = "";

        [Column("description")]
        public string? Description { get; set; }

        [Column("is_enabled")]
        public bool IsEnabled { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}






















/*﻿using System.ComponentModel.DataAnnotations.Schema;

namespace ebook_backend_api.Models
{
    public class PaymentMethod
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";

        [Column("is_enabled")]
        public bool IsEnabled { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}*/

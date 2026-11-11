using Microsoft.EntityFrameworkCore;
using ebook_backend_api.Models;

namespace ebook_backend_api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<VendorPaymentMethod> VendorPaymentMethods { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<BooksFolderSetting> BooksFolderSettings { get; set; }
        public DbSet<HardDriveBook> HardDriveBooks { get; set; }
        public DbSet<PurchaseRequest> PurchaseRequests { get; set; }
        public DbSet<PurchaseRequestItem> PurchaseRequestItems { get; set; }
        public DbSet<PurchasedBook> PurchasedBooks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>().ToTable("customers");
            modelBuilder.Entity<Vendor>().ToTable("vendors");
            modelBuilder.Entity<Cart>().ToTable("cart");
            modelBuilder.Entity<Book>().ToTable("books");
            modelBuilder.Entity<Category>().ToTable("categories");
            modelBuilder.Entity<Sale>().ToTable("sales");
            modelBuilder.Entity<VendorPaymentMethod>().ToTable("vendor_payment_methods");
            modelBuilder.Entity<PaymentMethod>().ToTable("payment_methods");
            modelBuilder.Entity<PurchaseRequest>().ToTable("purchase_requests");
            modelBuilder.Entity<PurchaseRequestItem>().ToTable("purchase_request_items");
            modelBuilder.Entity<PurchasedBook>().ToTable("purchased_books");

            modelBuilder.Entity<BooksFolderSetting>(entity =>
            {
                entity.ToTable("books_folder_settings");
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Path).HasColumnName("folder_path");
                entity.Property(b => b.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<HardDriveBook>(entity =>
            {
                entity.ToTable("Hard Drive Books");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.BookName).HasColumnName("book_name");
                entity.Property(e => e.Category).HasColumnName("category");
                entity.Property(e => e.SubCategory).HasColumnName("sub_category");
                entity.Property(e => e.BookPath).HasColumnName("book_path");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            // Self-referencing Category tree
            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Book → Category (single FK)
            modelBuilder.Entity<Book>()
                .HasOne(b => b.Category)
                .WithMany()
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

           
           
        }
    }
}

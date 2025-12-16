using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Martify.Models
{
    public class MartifyDbContext : DbContext
    {
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<ImportReceipt> ImportReceipts { get; set; }
        public DbSet<ImportReceiptDetail> ImportReceiptDetails { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceDetail> InvoiceDetails { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // [SỬA LỖI]: Luôn lấy đường dẫn tại thư mục chứa file .exe đang chạy (dù là Debug hay Release)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dbPath = Path.Combine(baseDir, "Martify.db");

            // Kết nối SQLite
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình Composite Key (Khóa chính gồm 2 trường)
            modelBuilder.Entity<ImportReceiptDetail>()
                .HasKey(ird => new { ird.ReceiptID, ird.ProductID });

            modelBuilder.Entity<InvoiceDetail>()
                .HasKey(id => new { id.InvoiceID, id.ProductID });

            // Thêm ràng buộc CHECK
            modelBuilder.Entity<Employee>(e =>
            {
                e.ToTable(t => t.HasCheckConstraint("CK_Employee_Gender", "Gender IS NULL OR Gender IN ('Nam', 'Nữ')"));
            });
        }
    }
}
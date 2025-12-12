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
            // 1. Lấy đường dẫn nơi file .exe đang chạy (thường là .../bin/Debug/net8.0-windows)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 2. Lùi lại 3 cấp thư mục để về folder Project (Martify)
            // Cấu trúc: Martify/bin/Debug/net... -> Lùi 3 lần sẽ về Martify/
            string projectPath = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\"));

            // 3. Trỏ vào folder Models
            string dbPath = Path.Combine(projectPath, "Models", "Martify.db");

            // Kiểm tra folder Models, nếu chưa có thì tạo (đề phòng)
            string modelsDir = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(modelsDir))
            {
                Directory.CreateDirectory(modelsDir);
            }

            // 4. Kết nối SQLite
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

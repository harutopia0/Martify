using Martify.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Martify.Helpers
{
    public static class DatabaseRenderIfEmpty
    {
        // Hàm này thực hiện logic: "Render If Empty" (Chỉ tạo khi chưa có dữ liệu)
        public static void EnsureDatabaseCreatedAndSeed(MartifyDbContext db)
        {
            // Kiểm tra: Nếu đã có nhân viên rồi thì coi như DB đã có dữ liệu -> Dừng lại.
            if (db.Employees.Any()) return;

            try
            {
                // ================== 1. DANH MỤC & NHÀ CUNG CẤP ==================
                var categories = new List<ProductCategory>
                {
                    new ProductCategory { CategoryID = "C01", CategoryName = "Đồ uống" },
                    new ProductCategory { CategoryID = "C02", CategoryName = "Thực phẩm ăn liền" },
                    new ProductCategory { CategoryID = "C03", CategoryName = "Gia vị & Đồ nấu" },
                    new ProductCategory { CategoryID = "C04", CategoryName = "Bánh kẹo" },
                    new ProductCategory { CategoryID = "C05", CategoryName = "Hóa mỹ phẩm" },
                    new ProductCategory { CategoryID = "C06", CategoryName = "Đồ gia dụng" }
                };
                db.ProductCategories.AddRange(categories);

                var suppliers = new List<Supplier>
                {
                    new Supplier { SupplierID = "NCC01", SupplierName = "Công ty Coca-Cola", Address = "TP.HCM", Phone = "02838966999" },
                    new Supplier { SupplierID = "NCC02", SupplierName = "Acecook Việt Nam", Address = "Bình Dương", Phone = "02838154064" },
                    new Supplier { SupplierID = "NCC03", SupplierName = "Unilever VN", Address = "TP.HCM", Phone = "02838236651" },
                    new Supplier { SupplierID = "NCC04", SupplierName = "Vinamilk", Address = "TP.HCM", Phone = "02854155555" },
                    new Supplier { SupplierID = "NCC05", SupplierName = "Công ty CP Bánh kẹo Kinh Đô", Address = "Hà Nội", Phone = "02438534798" }
                };
                db.Suppliers.AddRange(suppliers);
                db.SaveChanges();

                // ================== 2. NHÂN VIÊN & TÀI KHOẢN ==================
                // A. Admin (Duy nhất 1 người - Role 0 - Được nhập hàng)
                var adminEmp = new Employee
                {
                    EmployeeID = "AD001",
                    FullName = "Chủ cửa hàng",
                    BirthDate = new DateTime(1990, 1, 1),
                    HireDate = DateTime.Now.AddYears(-2),
                    Phone = "0909000000",
                    Email = "admin@martify.com",
                    Gender = "Nam",
                    Status = true,
                    Address = "Văn phòng quản lý",
                    ImagePath = @"Assets\Employee\adminEmp.png"
                };
                db.Employees.Add(adminEmp);

                var adminAcc = new Account
                {
                    Username = "admin",
                    HashPassword = HashPass("admin"), // Mật khẩu: admin
                    Role = 0,
                    EmployeeID = "AD001"
                };
                db.Accounts.Add(adminAcc);

                // B. Nhân viên (10 người - Role 1 - Chỉ bán hàng)
                var staffList = new List<string>();
                for (int i = 1; i <= 10; i++)
                {
                    string empId = $"NV{i:D3}"; // NV001 -> NV010
                    staffList.Add(empId);

                    db.Employees.Add(new Employee
                    {
                        EmployeeID = empId,
                        FullName = $"Nhân viên {i}",
                        BirthDate = new DateTime(1995 + (i % 5), 1 + i, 10),
                        HireDate = DateTime.Now.AddMonths(-i),
                        Phone = $"09123456{i:D2}",
                        Email = $"staff{i}@martify.com",
                        Gender = (i % 2 == 0) ? "Nữ" : "Nam",
                        Status = true,
                        Address = $"Khu vực {i}, TP.HCM",
                        ImagePath = null
                    });

                    db.Accounts.Add(new Account
                    {
                        Username = $"staff{i}",
                        HashPassword = HashPass("123"), // Mật khẩu: 123
                        Role = 1,
                        EmployeeID = empId
                    });
                }
                db.SaveChanges();

                // ================== 3. SẢN PHẨM ==================
                var products = new List<Product>();
                var rand = new Random();
                for (int i = 1; i <= 50; i++)
                {
                    string catId = categories[rand.Next(categories.Count)].CategoryID;
                    decimal basePrice = rand.Next(1, 100) * 1000 + 5000;

                    products.Add(new Product
                    {
                        ProductID = $"SP{i:D3}",
                        ProductName = $"Demo Martify {i}",
                        Unit = (i % 3 == 0) ? "Hộp" : ((i % 2 == 0) ? "Cái" : "Gói"),
                        Price = basePrice,
                        StockQuantity = rand.Next(50, 500),
                        CategoryID = catId,
                        ImagePath = null
                    });
                }
                db.Products.AddRange(products);
                db.SaveChanges();

                // ================== 4. PHIẾU NHẬP (Chỉ Admin AD001) ==================
                for (int i = 1; i <= 10; i++)
                {
                    string rID = $"IMP{i:D3}";
                    var receipt = new ImportReceipt
                    {
                        ReceiptID = rID,
                        ImportDate = DateTime.Now.AddMonths(-rand.Next(1, 6)).AddDays(-rand.Next(1, 30)),
                        EmployeeID = "AD001", // Chỉ Admin nhập
                        SupplierID = suppliers[rand.Next(suppliers.Count)].SupplierID,
                        TotalAmount = 0
                    };

                    decimal totalImport = 0;
                    int numDetails = rand.Next(5, 15);
                    var usedProds = new HashSet<string>();

                    for (int j = 0; j < numDetails; j++)
                    {
                        var prod = products[rand.Next(products.Count)];
                        if (usedProds.Contains(prod.ProductID)) continue;
                        usedProds.Add(prod.ProductID);

                        int qty = rand.Next(50, 200);
                        decimal cost = prod.Price * 0.75m;

                        db.ImportReceiptDetails.Add(new ImportReceiptDetail
                        {
                            ReceiptID = rID,
                            ProductID = prod.ProductID,
                            Quantity = qty,
                            UnitPrice = cost
                        });
                        totalImport += (cost * qty);
                    }
                    receipt.TotalAmount = totalImport;
                    db.ImportReceipts.Add(receipt);
                }
                db.SaveChanges();

                // ================== 5. HÓA ĐƠN (Nhân viên bán) ==================
                for (int i = 1; i <= 100; i++)
                {
                    string invID = $"HD{i:D4}";
                    var invoice = new Invoice
                    {
                        InvoiceID = invID,
                        CreatedDate = DateTime.Now.AddDays(-rand.Next(0, 60)).AddHours(rand.Next(8, 22)),
                        EmployeeID = staffList[rand.Next(staffList.Count)], // Nhân viên bất kỳ
                        TotalAmount = 0
                    };

                    decimal totalSale = 0;
                    int itemsCount = rand.Next(1, 8);
                    var pickedProducts = new HashSet<string>();

                    for (int k = 0; k < itemsCount; k++)
                    {
                        var prod = products[rand.Next(products.Count)];
                        if (pickedProducts.Contains(prod.ProductID)) continue;
                        pickedProducts.Add(prod.ProductID);

                        int qty = rand.Next(1, 10);

                        db.InvoiceDetails.Add(new InvoiceDetail
                        {
                            InvoiceID = invID,
                            ProductID = prod.ProductID,
                            Quantity = qty,
                            SalePrice = prod.Price
                        });
                        totalSale += (prod.Price * qty);
                    }
                    invoice.TotalAmount = totalSale;
                    db.Invoices.Add(invoice);
                }
                db.SaveChanges();
            }
            catch { }
        }

        private static string HashPass(string raw)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(raw));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
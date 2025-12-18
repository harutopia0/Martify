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
                    new ProductCategory { CategoryID = "C02", CategoryName = "Đồ gia dụng" },
                    new ProductCategory { CategoryID = "C03", CategoryName = "Hóa mỹ phẩm" },
                    new ProductCategory { CategoryID = "C04", CategoryName = "Thực phẩm ăn liền" },
                    new ProductCategory { CategoryID = "C05", CategoryName = "Bánh kẹo" },
                    new ProductCategory { CategoryID = "C06", CategoryName = "Gia vị & Đồ nấu" }
                };
                db.ProductCategories.AddRange(categories);

                var suppliers = new List<Supplier>
                {
                    new Supplier { SupplierID = "NCC01", SupplierName = "Công ty Coca-Cola VN", Address = "TP.HCM", Phone = "02838966999" },
                    new Supplier { SupplierID = "NCC02", SupplierName = "Acecook Việt Nam", Address = "Bình Dương", Phone = "02838154064" },
                    new Supplier { SupplierID = "NCC03", SupplierName = "Unilever Việt Nam", Address = "TP.HCM", Phone = "02838236651" },
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

                // ================== 3. SẢN PHẨM (Tối ưu cho Dashboard) ==================
                var products = new List<Product>();
                var rand = new Random(42); // Fixed seed for consistent data
                
                // Realistic product names by category
                var productTemplates = new Dictionary<string, List<(string Name, string Unit, int BasePrice)>>
                {
                    ["C01"] = new List<(string, string, int)> // Đồ uống
                    {
                        ("Coca Cola", "Chai", 15000), ("Pepsi", "Chai", 14000), ("Nước khoáng Lavie", "Chai", 6000),
                        ("Trà xanh C2", "Chai", 10000), ("Sữa tươi Vinamilk", "Hộp", 8000), ("Nước cam Twister", "Chai", 12000),
                        ("Sting dâu", "Chai", 10000), ("Number 1 Chanh muối", "Chai", 9000)
                    },
                    ["C02"] = new List<(string, string, int)> // Đồ gia dụng
                    {
                        ("Xà phòng Lifebuoy", "Cái", 15000), ("Giấy ăn Bless", "Gói", 8000), ("Túi rác đen", "Cuộn", 25000),
                        ("Khăn lau", "Gói", 12000), ("Bàn chải đánh răng", "Cái", 20000)
                    },
                    ["C03"] = new List<(string, string, int)> // Hóa mỹ phẩm
                    {
                        ("Dầu gội Clear", "Chai", 85000), ("Sữa tắm Dove", "Chai", 95000), ("Kem đánh răng PS", "Tuýp", 25000),
                        ("Nước rửa tay Lifebuoy", "Chai", 45000), ("Dầu xả Sunsilk", "Chai", 90000)
                    },
                    ["C04"] = new List<(string, string, int)> // Thực phẩm ăn liền
                    {
                        ("Mì Hảo Hảo tôm chua cay", "Gói", 5000), ("Hủ tiếu Nam Vang Acecook", "Gói", 7000),
                        ("Cháo tươi Việt San", "Hộp", 18000), ("Phở bò Acecook", "Gói", 8000), ("Miến gà Kokomi", "Gói", 5500),
                        ("Bún bò Huế Vifon", "Gói", 6000), ("Mì 3 Miền tôm", "Gói", 4500)
                    },
                    ["C05"] = new List<(string, string, int)> // Bánh kẹo
                    {
                        ("Bánh Oreo", "Gói", 12000), ("Snack Oishi", "Gói", 8000), ("Kẹo Alpenliebe", "Gói", 15000),
                        ("Bánh Chocopie", "Hộp", 35000), ("Kẹo bạc hà Mentos", "Gói", 10000), ("Bánh quy Cosy", "Gói", 18000)
                    },
                    ["C06"] = new List<(string, string, int)> // Gia vị & Đồ nấu
                    {
                        ("Nước mắm Nam Ngư", "Chai", 25000), ("Dầu ăn Simply", "Chai", 45000), ("Mì chính Ajinomoto", "Gói", 8000),
                        ("Hạt nêm Knorr", "Gói", 12000), ("Tương ớt Chinsu", "Chai", 18000), ("Nước tương Maggi", "Chai", 15000)
                    }
                };

                int productIndex = 1;
                foreach (var cat in categories)
                {
                    if (productTemplates.ContainsKey(cat.CategoryID))
                    {
                        foreach (var template in productTemplates[cat.CategoryID])
                        {
                            // Optimized stock levels: 30% alerts (15% out of stock + 15% low stock)
                            int stock;
                            double stockRoll = rand.NextDouble();
                            
                            if (stockRoll < 0.15) // 15% Out of Stock (0 units) - ~6-7 products
                                stock = 0;
                            else if (stockRoll < 0.30) // 15% Low Stock (1-10 units) - ~6-7 products
                                stock = rand.Next(1, 11);
                            else // 70% Normal Stock (30-300 units) - varied but not excessive
                                stock = rand.Next(30, 301);

                            products.Add(new Product
                            {
                                ProductID = $"SP{productIndex:D3}",
                                ProductName = template.Name,
                                Unit = template.Unit,
                                Price = template.BasePrice + rand.Next(-2000, 3000),
                                StockQuantity = stock,
                                CategoryID = cat.CategoryID,
                                ImagePath = null
                            });
                            productIndex++;
                        }
                    }
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

                // ================== 5. HÓA ĐƠN (Tối ưu cho Dashboard - 250 invoices) ==================
                // Generate realistic sales data with diverse product variety
                var now = DateTime.Now;
                
                // Track product popularity for realistic sales patterns (some products sell more)
                var productWeights = new Dictionary<string, double>();
                foreach (var prod in products)
                {
                    // Lower-priced items (drinks, snacks) sell more frequently
                    if (prod.Price < 15000)
                        productWeights[prod.ProductID] = 3.0; // High popularity
                    else if (prod.Price < 50000)
                        productWeights[prod.ProductID] = 2.0; // Medium popularity
                    else
                        productWeights[prod.ProductID] = 1.0; // Lower popularity
                }
                
                // Create 250 invoices distributed over last 60 days
                for (int i = 1; i <= 250; i++)
                {
                    string invID = $"HD{i:D4}";
                    
                    // Weight distribution: 40% last 7 days, 30% last 14 days, 30% older
                    int daysAgo;
                    double weightRoll = rand.NextDouble();
                    if (weightRoll < 0.4) // 40% in last 7 days
                        daysAgo = rand.Next(0, 7);
                    else if (weightRoll < 0.7) // 30% in days 7-14
                        daysAgo = rand.Next(7, 14);
                    else // 30% in days 14-60
                        daysAgo = rand.Next(14, 60);

                    // Business hours: 8 AM to 10 PM, with peak hours 11-13 and 18-20
                    int hour;
                    if (rand.NextDouble() < 0.5) // 50% during peak hours
                        hour = rand.Next(0, 2) == 0 ? rand.Next(11, 14) : rand.Next(18, 21);
                    else
                        hour = rand.Next(8, 22);

                    var invoice = new Invoice
                    {
                        InvoiceID = invID,
                        CreatedDate = now.AddDays(-daysAgo).Date.AddHours(hour).AddMinutes(rand.Next(0, 60)),
                        EmployeeID = staffList[rand.Next(staffList.Count)],
                        TotalAmount = 0
                    };

                    decimal totalSale = 0;
                    // Varied cart sizes: favor smaller carts for more product variety
                    int itemsCount;
                    double cartRoll = rand.NextDouble();
                    if (cartRoll < 0.4) // 40% small carts (1-3 items)
                        itemsCount = rand.Next(1, 4);
                    else if (cartRoll < 0.8) // 40% medium carts (4-6 items)
                        itemsCount = rand.Next(4, 7);
                    else // 20% large carts (7-10 items)
                        itemsCount = rand.Next(7, 11);
                    
                    var pickedProducts = new HashSet<string>();

                    for (int k = 0; k < itemsCount; k++)
                    {
                        // Weighted random selection for realistic top products
                        Product prod;
                        int attempts = 0;
                        do
                        {
                            prod = products[rand.Next(products.Count)];
                            double chance = rand.NextDouble() * 3.0; // Max weight is 3.0
                            
                            // Higher weight = more likely to be selected
                            if (chance <= productWeights[prod.ProductID] && !pickedProducts.Contains(prod.ProductID))
                                break;
                            
                            attempts++;
                        } while (attempts < 10); // Prevent infinite loop
                        
                        if (pickedProducts.Contains(prod.ProductID)) continue;
                        pickedProducts.Add(prod.ProductID);

                        // Realistic quantities: mostly 1-2, occasionally 3-5, rarely 6-8
                        int qty;
                        double qtyRoll = rand.NextDouble();
                        if (qtyRoll < 0.6) // 60% buy 1-2 units
                            qty = rand.Next(1, 3);
                        else if (qtyRoll < 0.9) // 30% buy 3-5 units
                            qty = rand.Next(3, 6);
                        else // 10% buy 6-8 units (bulk)
                            qty = rand.Next(6, 9);

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
using Martify.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class ProductDetailVM : BaseVM
    {
        public event EventHandler RequestClose;

        // =================================================================================================
        // THUỘC TÍNH (PROPERTIES)
        // =================================================================================================

        private Product _selectedDetailProduct;
        public Product SelectedDetailProduct
        {
            get => _selectedDetailProduct;
            set { _selectedDetailProduct = value; OnPropertyChanged(); }
        }

        private string _editProductName;
        public string EditProductName
        {
            get => _editProductName;
            set { _editProductName = value; OnPropertyChanged(); CheckModified(); }
        }

        private decimal _editPrice;
        public decimal EditPrice
        {
            get => _editPrice;
            set { _editPrice = value; OnPropertyChanged(); CheckModified(); }
        }

        private int _editStockQuantity;
        public int EditStockQuantity
        {
            get => _editStockQuantity;
            set { _editStockQuantity = value; OnPropertyChanged(); CheckModified(); }
        }

        private string _editImagePath;
        public string EditImagePath
        {
            get => _editImagePath;
            set { _editImagePath = value; OnPropertyChanged(); }
        }

        private string _supplierName;
        public string SupplierName
        {
            get => _supplierName;
            set { _supplierName = value; OnPropertyChanged(); }
        }

        private string _saveMessage;
        public string SaveMessage
        {
            get => _saveMessage;
            set { _saveMessage = value; OnPropertyChanged(); }
        }

        private bool _isModified;
        public bool IsModified
        {
            get => _isModified;
            set { _isModified = value; OnPropertyChanged(); }
        }

        // --- DANH MỤC (CATEGORY) LOGIC ---

        private ObservableCollection<ProductCategory> _categories;
        public ObservableCollection<ProductCategory> Categories
        {
            get => _categories;
            set { _categories = value; OnPropertyChanged(); }
        }

        private string _editCategoryID;
        public string EditCategoryID
        {
            get => _editCategoryID;
            set
            {
                if (_editCategoryID == value) return;
                _editCategoryID = value;

                // Đồng bộ Text khi chọn từ danh sách
                if (!string.IsNullOrEmpty(_editCategoryID) && Categories != null)
                {
                    var cat = Categories.FirstOrDefault(c => c.CategoryID == _editCategoryID);
                    if (cat != null && _selectedCategoryText != cat.CategoryName)
                    {
                        _selectedCategoryText = cat.CategoryName;
                        OnPropertyChanged(nameof(SelectedCategoryText));
                    }
                }
                OnPropertyChanged();
                CheckModified();
            }
        }

        private string _selectedCategoryText;
        public string SelectedCategoryText
        {
            get => _selectedCategoryText;
            set
            {
                if (_selectedCategoryText == value) return;
                _selectedCategoryText = value;

                // Tìm ID tương ứng nếu Text khớp với tên đã có
                if (!string.IsNullOrWhiteSpace(_selectedCategoryText) && Categories != null)
                {
                    var match = Categories.FirstOrDefault(c => string.Equals(c.CategoryName?.Trim(), _selectedCategoryText.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        if (EditCategoryID != match.CategoryID)
                            EditCategoryID = match.CategoryID;
                    }
                    else
                    {
                        EditCategoryID = null; // Text mới hoàn toàn
                    }
                }
                OnPropertyChanged();
                CheckModified();
            }
        }

        // --- ĐƠN VỊ (UNIT) LOGIC ---

        private List<string> _unitList;
        public List<string> UnitList
        {
            get => _unitList;
            set { _unitList = value; OnPropertyChanged(); }
        }

        private string _editUnit; // Lưu giá trị thực tế của Product
        public string EditUnit
        {
            get => _editUnit;
            set { _editUnit = value; OnPropertyChanged(); CheckModified(); }
        }

        public string SelectedUnitText
        {
            get => EditUnit;
            set
            {
                EditUnit = value; // Gán trực tiếp vì Unit chỉ là string
                OnPropertyChanged();
            }
        }

        // =================================================================================================
        // COMMANDS & CONSTRUCTOR
        // =================================================================================================

        public ICommand SaveChangesCommand { get; set; }
        public ICommand CloseWindowCommand { get; set; }
        public Action OnSaveCompleted { get; set; }

        public ProductDetailVM(Product product)
        {
            SelectedDetailProduct = product;

            LoadCategories();
            LoadUnit();

            // Khởi tạo dữ liệu ban đầu
            EditProductName = product.ProductName;
            EditPrice = product.Price;
            EditStockQuantity = product.StockQuantity;
            EditUnit = product.Unit;
            EditCategoryID = product.CategoryID;
            EditImagePath = product.ImagePath;

            // Hiển thị Text của Category ban đầu
            var currentCat = Categories?.FirstOrDefault(c => c.CategoryID == product.CategoryID);
            _selectedCategoryText = currentCat?.CategoryName;

            var lastImport = product.ImportReceiptDetails?.OrderByDescending(d => d.ImportReceipt.ImportDate).FirstOrDefault();
            SupplierName = lastImport?.ImportReceipt?.Supplier?.SupplierName ?? "Chưa có đợt nhập hàng";

            SaveChangesCommand = new RelayCommand<object>((p) => IsModified, async (p) => await SaveChangesAsync());
            CloseWindowCommand = new RelayCommand<object>((p) => true, (p) => RequestClose?.Invoke(this, EventArgs.Empty));

            IsModified = false;
        }

        // =================================================================================================
        // PHƯƠNG THỨC XỬ LÝ
        // =================================================================================================

        private void LoadCategories()
        {
            try
            {
                var list = DataProvider.Ins.DB.ProductCategories.AsNoTracking().OrderBy(c => c.CategoryName).ToList();
                Categories = new ObservableCollection<ProductCategory>(list);
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        private void LoadUnit()
        {
            try
            {
                UnitList = DataProvider.Ins.DB.Products.Select(p => p.Unit).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        private void CheckModified()
        {
            if (SelectedDetailProduct == null) return;
            IsModified = EditProductName != SelectedDetailProduct.ProductName ||
                         EditPrice != SelectedDetailProduct.Price ||
                         EditStockQuantity != SelectedDetailProduct.StockQuantity ||
                         EditUnit != SelectedDetailProduct.Unit ||
                         EditCategoryID != SelectedDetailProduct.CategoryID ||
                         SelectedCategoryText != Categories.FirstOrDefault(c => c.CategoryID == SelectedDetailProduct.CategoryID)?.CategoryName;
        }

        private async Task SaveChangesAsync()
        {
            if (SelectedDetailProduct == null) return;

            // Validate cơ bản (phải có tên và danh mục)
            if (string.IsNullOrWhiteSpace(EditProductName) || string.IsNullOrWhiteSpace(SelectedCategoryText))
            {
                SaveMessage = "Vui lòng nhập đầy đủ thông tin!";
                return;
            }

            try
            {
                string finalCategoryID = EditCategoryID;
                string typedCategory = SelectedCategoryText?.Trim();

                // 1. XỬ LÝ CATEGORY MỚI (Nếu ID trống nhưng có Text)
                if (string.IsNullOrEmpty(finalCategoryID) && !string.IsNullOrEmpty(typedCategory))
                {
                    var newCatId = GenerateCategoryID();
                    var newCat = new ProductCategory { CategoryID = newCatId, CategoryName = typedCategory };
                    DataProvider.Ins.DB.ProductCategories.Add(newCat);
                    DataProvider.Ins.DB.SaveChanges();
                    finalCategoryID = newCatId;
                }

                // 2. CẬP NHẬT PRODUCT
                var productInDb = DataProvider.Ins.DB.Products.FirstOrDefault(x => x.ProductID == SelectedDetailProduct.ProductID);
                if (productInDb != null)
                {
                    productInDb.ProductName = EditProductName.Trim();
                    productInDb.Price = EditPrice;
                    productInDb.StockQuantity = EditStockQuantity;
                    productInDb.Unit = EditUnit?.Trim();
                    productInDb.CategoryID = finalCategoryID;

                    DataProvider.Ins.DB.SaveChanges();

                    SaveMessage = "Đã cập nhật thành công!";
                    IsModified = false;

                    // Đồng bộ object gốc để UI bên ngoài cập nhật theo
                    SelectedDetailProduct.ProductName = productInDb.ProductName;
                    SelectedDetailProduct.Price = productInDb.Price;
                    SelectedDetailProduct.Unit = productInDb.Unit;
                    SelectedDetailProduct.CategoryID = productInDb.CategoryID;

                    OnSaveCompleted?.Invoke();
                    await Task.Delay(1000);
                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu: {ex.Message}");
            }
        }

        private string GenerateCategoryID()
        {
            var ids = DataProvider.Ins.DB.ProductCategories.Select(x => x.CategoryID).ToList();
            int max = 0;
            foreach (var id in ids)
            {
                var digits = new string(id?.SkipWhile(c => !char.IsDigit(c)).ToArray());
                if (int.TryParse(digits, out int n) && n > max) max = n;
            }
            return "C" + (max + 1).ToString("D3");
        }
    }
}
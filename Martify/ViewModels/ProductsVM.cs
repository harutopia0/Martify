using Martify.Models;
using Martify.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class ProductsVM : BaseVM
    {

        // --- CÁC BIẾN LỌC (FILTER) --
        private ProductCategory? _selectedCategory;
        public ProductCategory? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                LoadProducts();
            }
        }



        // --- NGUỒN DỮ LIỆU CHO COMBOBOX ---
        public ObservableCollection<ProductCategory> _categories;
        public ObservableCollection<ProductCategory> Categories
        {
            get => _categories;
            set
            {
                _categories = value;
                OnPropertyChanged();
            }
        }

        // =================================================================================================
        // THUỘC TÍNH
        // =================================================================================================

        private ObservableCollection<Product> _products;
        public ObservableCollection<Product> Products
        {
            get => _products;
            set
            {
                _products = value;
                OnPropertyChanged();
            }
        }

        private Product _selectedProduct;
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                LoadProducts();
            }
        }

        // =================================================================================================
        // LỆNH
        // =================================================================================================

        public ICommand AddProductCommand { get; set; }
        public ICommand EditProductCommand { get; set; }
        public ICommand DeleteProductCommand { get; set; }
        public ICommand RefreshCommand { get; set; }

        // =================================================================================================
        // HÀM KHỞI TẠO
        // =================================================================================================

        public ProductsVM()
        {
            LoadProducts();

            Categories = new ObservableCollection<ProductCategory>(
                DataProvider.Ins.DB.ProductCategories.OrderBy(c => c.CategoryName).ToList()
            );
            // Khởi tạo lệnh
            AddProductCommand = new RelayCommand<object>(
                (p) => true,
                (p) => AddProduct());

            EditProductCommand = new RelayCommand<Product>(
                (p) => p != null,
                (p) => EditProduct(p));

            DeleteProductCommand = new RelayCommand<Product>(
                (p) => p != null,
                (p) => DeleteProduct(p));

            RefreshCommand = new RelayCommand<object>(
                (p) => true,
                (p) => LoadProducts());
        }

        // =================================================================================================
        // PHƯƠNG THỨC
        // =================================================================================================

        private void LoadProducts()
        {
            try
            {
                var query = DataProvider.Ins.DB.Products
                    .Include("Category")
                    .AsQueryable();

                // 2. Áp dụng lọc theo Danh mục (SelectedCategory)
                if (SelectedCategory != null)
                {
                    query = query.Where(p => p.CategoryID == SelectedCategory.CategoryID);
                }

                // 3. Áp dụng tìm kiếm theo Văn bản (SearchText)
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchTerm = SearchText.Trim().ToLower();
                    query = query.Where(p =>
                        p.ProductID.ToLower().Contains(searchTerm) ||
                        p.ProductName.ToLower().Contains(searchTerm) ||
                        p.Unit.ToLower().Contains(searchTerm) ||
                        (p.Category != null && p.Category.CategoryName.ToLower().Contains(searchTerm))
                    );
                }

                // 4. Sắp xếp và thực thi truy vấn
                var productList = query
                    .OrderBy(p => p.ProductID)
                    .ToList();

                // 5. Cập nhật ObservableCollection
                Products = new ObservableCollection<Product>(productList);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi lọc và tìm kiếm sản phẩm: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void AddProduct()
        {
            try
            {
                var addWindow = new AddProduct();
                addWindow.ShowDialog();

                // Làm mới danh sách sau khi đóng cửa sổ thêm
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi mở cửa sổ thêm sản phẩm: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void EditProduct(Product product)
        {
            try
            {
                // TODO: Tạo cửa sổ EditProduct tương tự AddProduct
                MessageBox.Show(
                    $"Chức năng chỉnh sửa sản phẩm: {product.ProductName}\n" +
                    "Tính năng đang được phát triển.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Sau khi triển khai cửa sổ EditProduct:
                // var editWindow = new EditProduct(product);
                // editWindow.ShowDialog();
                // LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DeleteProduct(Product product)
        {
            try
            {
                var result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa sản phẩm:\n\n" +
                    $"Mã: {product.ProductID}\n" +
                    $"Tên: {product.ProductName}\n\n" +
                    "Hành động này không thể hoàn tác!",
                    "Xác nhận xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Kiểm tra sản phẩm có được sử dụng trong đơn hàng không
                    var isUsedInOrders = DataProvider.Ins.DB.InvoiceDetails
                        .Any(od => od.ProductID == product.ProductID);

                    if (isUsedInOrders)
                    {
                        MessageBox.Show(
                            "Không thể xóa sản phẩm này vì đã được sử dụng trong các đơn hàng.\n" +
                            "Bạn có thể ngừng kinh doanh sản phẩm này thay vì xóa.",
                            "Không thể xóa",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    // Xóa sản phẩm
                    DataProvider.Ins.DB.Products.Remove(product);
                    DataProvider.Ins.DB.SaveChanges();

                    MessageBox.Show(
                        "Đã xóa sản phẩm thành công!",
                        "Thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    LoadProducts();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi xóa sản phẩm: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
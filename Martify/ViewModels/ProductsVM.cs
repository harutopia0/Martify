using Martify.Models;
using Martify.Views;
using Microsoft.EntityFrameworkCore; // Giả định bạn đang dùng EF Core
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

// Cần đảm bảo có class RelayCommand và BaseVM trong cùng project
namespace Martify.ViewModels
{
    public class ProductsVM : BaseVM
    {
        private ObservableCollection<Models.Product> _Products;

        // Thuộc tính để hiển thị danh sách sản phẩm trên giao diện
        public ObservableCollection<Models.Product> Products
        {
            get { return _Products; }
            set
            {
                _Products = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddProductCommand { get; set; }

        public ProductsVM()
        {
            // Kiểm tra chế độ thiết kế (Design Mode)
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject())) return;

            // 1. Tải danh sách sản phẩm khi khởi tạo ViewModel
            LoadList();

            // 2. Khởi tạo Command Thêm Sản phẩm
            AddProductCommand = new RelayCommand<object>((p) => { return true; }, (p) =>
            {
                // Mở cửa sổ Thêm Sản phẩm
                Window addProductWindow = new AddProduct();
                addProductWindow.ShowDialog();

                // 3. Tải lại danh sách sau khi cửa sổ AddProduct đóng
                // (Giả định rằng quá trình thêm sản phẩm thành công)
                LoadList();
            });

            // 4. Các Command khác (nếu cần: Edit, Delete, Search)
            // ...
        }

        /// <summary>
        /// Hàm tải danh sách sản phẩm từ cơ sở dữ liệu.
        /// </summary>
        public void LoadList()
        {
            //try
            //{
            //    // Giả định bạn có một DbContext tên là MartifyContext
            //    using (var context = new MaritfyDbContext())
            //    {
            //        // Lấy tất cả sản phẩm, bao gồm thông tin liên quan (Category, Supplier)
            //        var productList = context.Products
            //            .Include(p => p.Category) // Load thông tin danh mục
            //            .Include(p => p.Supplier) // Load thông tin nhà cung cấp
            //            .ToList();

            //        // Cập nhật ObservableCollection Products
            //        Products = new ObservableCollection<Models.Product>(productList);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    // Xử lý lỗi khi kết nối hoặc tải dữ liệu
            //    MessageBox.Show($"Lỗi khi tải danh sách sản phẩm: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            //    // Khởi tạo Products rỗng nếu có lỗi để tránh lỗi NullReference
            //    Products = new ObservableCollection<Models.Product>();
            //}
        }
    }
}
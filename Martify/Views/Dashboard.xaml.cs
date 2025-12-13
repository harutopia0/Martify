using Martify.Models;
using Martify.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Martify.Views
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : UserControl
    {
        public Dashboard()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Xử lý khi click vào cảnh báo "Sắp hết hàng"
        /// </summary>
        private void LowStockAlert_Click(object sender, MouseButtonEventArgs e)
        {
            ShowInventoryAlertWindow(InventoryAlertType.LowStock);
        }

        /// <summary>
        /// Xử lý khi click vào cảnh báo "Hết hàng"
        /// </summary>
        private void OutOfStockAlert_Click(object sender, MouseButtonEventArgs e)
        {
            ShowInventoryAlertWindow(InventoryAlertType.OutOfStock);
        }

        /// <summary>
        /// Hiển thị popup window với danh sách sản phẩm cảnh báo
        /// </summary>
        private void ShowInventoryAlertWindow(InventoryAlertType alertType)
        {
            var window = new InventoryAlertWindow
            {
                DataContext = new InventoryAlertWindowVM(alertType),
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
        }

        /// <summary>
        /// Xử lý khi click vào card "Sản Phẩm" - Chuyển đến trang Products
        /// </summary>
        private void ProductsCard_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(2); // Products menu index = 2
        }

        /// <summary>
        /// Xử lý khi click vào card "Đơn Hàng" - Chuyển đến trang Invoices
        /// </summary>
        private void InvoicesCard_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(4); // Invoices menu index = 4
        }

        /// <summary>
        /// Xử lý khi click vào card "Nhân Viên" - Chuyển đến trang Employees
        /// </summary>
        private void EmployeesCard_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(3); // Employees menu index = 3
        }

        /// <summary>
        /// Điều hướng đến trang tương ứng và cập nhật SidePanel với animation
        /// </summary>
        private void NavigateToPage(int menuIndex)
        {
            // Lấy MainWindow và MainVM
            var mainWindow = Window.GetWindow(this);
            if (mainWindow?.DataContext is MainVM mainVM)
            {
                // Cập nhật SelectedMenuIndex sẽ trigger animation trong SidePanel
                mainVM.SelectedMenuIndex = menuIndex;

                // Execute navigation command tương ứng
                switch (menuIndex)
                {
                    case 2: // Products
                        mainVM.Navigation.ProductsCommand.Execute(null);
                        break;
                    case 3: // Employees
                        mainVM.Navigation.EmployeesCommand.Execute(null);
                        break;
                    case 4: // Invoices
                        mainVM.Navigation.InvoicesCommand.Execute(null);
                        break;
                }
            }
        }
    }
}



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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Martify.Views
{
    /// <summary>
    /// Interaction logic for ProductSelection.xaml
    /// </summary>
    public partial class ProductSelection : UserControl
    {
        public ProductSelection()
        {
            InitializeComponent();
        }

        // Sự kiện xảy ra khi UserControl bị ẩn đi (chuyển Tab) hoặc đóng Window
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is ProductSelectionVM vm)
            {
                vm.Dispose(); // Gọi hàm hủy Camera
            }
        }
    }
}
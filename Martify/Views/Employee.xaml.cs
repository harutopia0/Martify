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
    /// Interaction logic for Employee.xaml
    /// </summary>
    public partial class Employee : UserControl
    {
        public Employee()
        {
            InitializeComponent();
        }



        // Hàm này chạy mỗi khi DataContext của Detail thay đổi (đổi nhân viên)
        private void DetailView_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            // Cuộn lên đầu trang
            DetailScrollViewer?.ScrollToTop();
        }
    }
}

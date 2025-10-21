using Martify.ViewModel;
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

namespace Martify.UserCtrl
{
    /// <summary>
    /// Interaction logic for WindowControlUC.xaml
    /// </summary>
    public partial class WindowControlUC : UserControl
    {
        public WindowControlViewModel Viewmodel { get; set; }
        public WindowControlUC()
        {
            InitializeComponent();
            this.DataContext = Viewmodel = new WindowControlViewModel();
        }
    }
}

using System.Windows;
using System.Windows.Input;

namespace Martify.Views
{
    /// <summary>
    /// Interaction logic for ImportProducts.xaml
    /// </summary>
    public partial class ImportProducts : Window
    {
        public ImportProducts()
        {
            InitializeComponent();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}
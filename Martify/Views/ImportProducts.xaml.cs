using Martify.ViewModels;
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

        /// <summary>
        /// Constructor for pre-loading products (used for restocking from inventory alerts)
        /// </summary>
        /// <param name="viewModel">Pre-configured ViewModel with products</param>
        public ImportProducts(ImportProductsVM viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
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
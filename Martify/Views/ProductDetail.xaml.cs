using Martify.ViewModels;
using System.Windows;

namespace Martify.Views
{
    public partial class ProductDetail : Window
    {
        private ProductDetailVM _viewModel;

        public ProductDetail(ProductDetailVM viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // Subscribe to close window event
            _viewModel.RequestClose += (sender, e) => Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
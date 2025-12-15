using Martify.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Martify.Views
{
    /// <summary>
    /// Interaction logic for PriceCheckDialog.xaml
    /// Price Check Dialog with camera-based QR/Barcode scanning
    /// </summary>
    public partial class PriceCheckDialog : Window
    {
        public PriceCheckDialog()
        {
            InitializeComponent();

            // Allow window dragging from header
            MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    DragMove();
            };
        }

        /// <summary>
        /// Handle window closing - ensure camera is properly stopped and disposed
        /// </summary>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Properly dispose ViewModel to stop camera and free resources
            if (DataContext is PriceCheckVM viewModel)
            {
                viewModel.Dispose();
            }
        }
    }
}

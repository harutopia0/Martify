using Martify.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace Martify.Views
{
    /// <summary>
    /// Interaction logic for InventoryAlertWindow.xaml
    /// </summary>
    public partial class InventoryAlertWindow : Window
    {
        public InventoryAlertWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

using System.Windows;
using System.Windows.Input;

namespace Martify.Views
{
    public partial class AddSupplier : Window
    {
        public AddSupplier()
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
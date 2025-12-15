using System.Windows;
using System.Windows.Media.Animation;

namespace Martify.Views
{
    public partial class PrinterWindow : Window
    {
        public PrinterWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            var sb = this.FindResource("PrintAnimation") as Storyboard;
            sb?.Begin();
        }

        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace loginForm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CloseProgram_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void usernameTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            usernameBorder.BorderThickness = new Thickness(3);
            usernameBorder.CornerRadius = new CornerRadius(5);

            string hexColor = "#4b91cf";
            Color color = (Color)ColorConverter.ConvertFromString(hexColor);
            SolidColorBrush brush = new SolidColorBrush(color);
            usernameBorder.BorderBrush = brush;
        }

        private void usernameTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            usernameBorder.BorderThickness = new Thickness(1);
            usernameBorder.CornerRadius = new CornerRadius(3);
            usernameBorder.BorderBrush = Brushes.Gray;
        }

        private void passwordBoxGotFocus(object sender, RoutedEventArgs e)
        {
            passwordBorder.BorderThickness = new Thickness(3);
            passwordBorder.CornerRadius = new CornerRadius(5);

            string hexColor = "#4b91cf";
            Color color = (Color)ColorConverter.ConvertFromString(hexColor);
            passwordBorder.BorderBrush = new SolidColorBrush(color);
        }

        private void passwordBoxLostFocus(object sender, RoutedEventArgs e)
        {
            passwordBorder.BorderThickness = new Thickness(1);
            passwordBorder.CornerRadius = new CornerRadius(3);
            passwordBorder.BorderBrush = Brushes.Gray;
        }
    }
}
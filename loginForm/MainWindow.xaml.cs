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
            // Apply focus effect border
            usernameFocusBorder.Opacity = 1;
        }

        private void usernameTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            // Hide focus effect border
            usernameFocusBorder.Opacity = 0;
        }

        private void passwordBoxGotFocus(object sender, RoutedEventArgs e)
        {
            // Apply focus effect border
            passwordFocusBorder.Opacity = 1;
        }

        private void passwordBoxLostFocus(object sender, RoutedEventArgs e)
        {
            // Hide focus effect border
            passwordFocusBorder.Opacity = 0;
        }
    }
}
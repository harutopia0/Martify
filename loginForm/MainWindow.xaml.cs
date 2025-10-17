using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace loginForm
{
    public partial class MainWindow : Window
    {
        private bool isPasswordVisible = false;

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
            usernameFocusBorder.Visibility = Visibility.Visible;
        }

        private void usernameTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            usernameFocusBorder.Visibility = Visibility.Collapsed;
        }

        private void passwordBoxGotFocus(object sender, RoutedEventArgs e)
        {
            passwordFocusBorder.Visibility = Visibility.Visible;
        }

        private void passwordBoxLostFocus(object sender, RoutedEventArgs e)
        {
            passwordFocusBorder.Visibility = Visibility.Collapsed;
        }

        private void EyeIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;

            if (isPasswordVisible)
            {
                // ---- Show Password ----
                showPasswordTextBox.Text = passwordBox.Password;
                showPasswordTextBox.Visibility = Visibility.Visible;
                passwordBox.Visibility = Visibility.Collapsed;

                eyeIcon.Source = new BitmapImage(new Uri("assets/eye_open.png", UriKind.Relative));
            }
            else
            {
                // ---- Hide Password ----
                passwordBox.Password = showPasswordTextBox.Text;
                showPasswordTextBox.Visibility = Visibility.Collapsed;
                passwordBox.Visibility = Visibility.Visible;

                eyeIcon.Source = new BitmapImage(new Uri("assets/eye_closed.png", UriKind.Relative));
            }

            // Move the keyboard focus to a hidden focus sink to clear focus from other controls
            FocusSink.Focus();
        }
    }
}
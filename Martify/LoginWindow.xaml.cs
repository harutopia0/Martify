using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Martify
{
    public partial class LoginWindow : Window
    {
        private bool isPasswordVisible = false;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void showAndHidePasswordClick(object sender, MouseButtonEventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;

            if (isPasswordVisible)
            {
                // ---- Show Password ----
                showPasswordTextBox.Text = passwordBox.Password;
                showPasswordTextBox.Visibility = Visibility.Visible;
                passwordBox.Visibility = Visibility.Collapsed;
                showPasswordTextBox.CaretIndex = showPasswordTextBox.Text.Length;

                eyeIcon.Source = new BitmapImage(new Uri("Resources/images/eye_open.png", UriKind.Relative));
            }
            else
            {
                // ---- Hide Password ----
                passwordBox.Password = showPasswordTextBox.Text;
                showPasswordTextBox.Visibility = Visibility.Collapsed;
                passwordBox.Visibility = Visibility.Visible;

                eyeIcon.Source = new BitmapImage(new Uri("Resources/images/eye_closed.png", UriKind.Relative));
            }

            // Move focus to a hidden focus sink to clear focus from other controls
            FocusSink.Focus();
        }

        private void moveFocustoFocusSink(object sender, MouseButtonEventArgs e)
        {
            FocusSink.Focus();
        }

        private void loginButtonClick(object sender, RoutedEventArgs e)
        {
            //MainWindow mainWindow = new MainWindow();
            //mainWindow.Show();
            //this.Close();
        }
    }
}
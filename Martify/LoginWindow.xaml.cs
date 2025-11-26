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
    }
}
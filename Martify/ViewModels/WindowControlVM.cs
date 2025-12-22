using Martify.ViewModels;
using Martify.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class WindowControlVM : BaseVM
    {
        #region Commands
        public ICommand CloseWindowCommand { get; set; }
        public ICommand HideWindowCommand { get; set; }
        public ICommand DragWindowCommand { get; set; }

        public ICommand ConfirmExitCommand { get; set; }
        public ICommand CancelExitCommand { get; set; }
        #endregion

        #region Properties
        private Visibility _controlButtonsVisibility = Visibility.Collapsed;
        public Visibility ControlButtonsVisibility
        {
            get => _controlButtonsVisibility;
            set { _controlButtonsVisibility = value; OnPropertyChanged(); }
        }

        private Visibility _isAlertVisible = Visibility.Collapsed;
        public Visibility IsAlertVisible
        {
            get => _isAlertVisible;
            set { _isAlertVisible = value; OnPropertyChanged(); }
        }
        #endregion

        public WindowControlVM()
        {
            CloseWindowCommand = new RelayCommand<UserControl>((p) => { return p != null; }, async (p) =>
            {
                Window parentWindow = Window.GetWindow(p);
                if (parentWindow != null)
                {
                    if (parentWindow is LoginWindow)
                    {
                        System.Environment.Exit(0);
                    }
                    else
                    {
                        // FIX LỖI: Tìm view Settings trong cây giao diện thay vì check CurrentPage
                        if (parentWindow is MainWindow mainWindow)
                        {
                            // Dùng hàm FindVisualChild để tìm view Settings thật sự
                            var settingsPage = FindVisualChild<Martify.Views.Settings>(mainWindow);

                            if (settingsPage != null && settingsPage.IsVisible)
                            {
                                // Gọi hàm đóng băng (Chụp ảnh & Ẩn WebView)
                                await settingsPage.FreezeWebView();
                            }
                        }


                        IsAlertVisible = Visibility.Visible;
                    }
                }
            });

            ConfirmExitCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                System.Environment.Exit(0);
            });

            CancelExitCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                IsAlertVisible = Visibility.Collapsed;



                // FIX LỖI: Tìm view Settings để rã đông
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    var settingsPage = FindVisualChild<Martify.Views.Settings>(mainWindow);

                    if (settingsPage != null)
                    {
                        // Hiện lại WebView thật
                        settingsPage.UnfreezeWebView();
                    }
                }
            });

            HideWindowCommand = new RelayCommand<UserControl>((p) => p != null, (p) =>
            {
                var win = Window.GetWindow(p);
                if (win != null)
                {
                    win.WindowState = WindowState.Minimized;
                }
            });

            DragWindowCommand = new RelayCommand<UserControl>((p) => p != null, (p) =>
            {
                var win = Window.GetWindow(p);
                if (win != null)
                {
                    win.DragMove();
                }
            });
        }


        // Hàm hỗ trợ tìm kiếm control con trong giao diện WPF
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T t)
                    return t;

                var childItem = FindVisualChild<T>(child);
                if (childItem != null) return childItem;
            }
            return null;
        }
    }
}
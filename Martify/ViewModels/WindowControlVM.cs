using Martify.ViewModels;
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

            CloseWindowCommand = new RelayCommand<UserControl>((p) => { return true; }, (p) =>
            {
                IsAlertVisible = Visibility.Visible;
            });


            ConfirmExitCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                Application.Current.Shutdown();
            });


            CancelExitCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                IsAlertVisible = Visibility.Collapsed;
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
    }
}
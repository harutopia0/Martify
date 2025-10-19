using Martify.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Martify.ViewModel
{
    public class WindowCommandViewModel : BaseViewModel
    {
        #region commands
        public ICommand CloseWindowCommand { get; set; }
        public ICommand ToggleMaximizeWindowCommand { get; set; }
        public ICommand MinimizeWindowCommand { get; set; }
        #endregion

        public WindowCommandViewModel()
        {
            CloseWindowCommand = new RelayCommand<UserControl>((p) => { return p != null; }, (p) =>
            {
                var win = Window.GetWindow(p);

                if (win != null)
                {
                    win.Close();
                }
            });

            ToggleMaximizeWindowCommand = new RelayCommand<UserControl>((p) => { return p != null; }, (p) =>
            {
                var win = Window.GetWindow(p);

                if (win != null)
                {
                    if(win.WindowState == WindowState.Maximized)
                        win.WindowState = WindowState.Normal;
                    else
                        win.WindowState = WindowState.Maximized;
                }
            });

            MinimizeWindowCommand = new RelayCommand<UserControl>((p) => { return p != null; }, (p) =>
            {
                var win = Window.GetWindow(p);

                if (win != null)
                {
                    if(win.WindowState != WindowState.Minimized)
                        win.WindowState = WindowState.Minimized;
                }
            });
        }
    }
}

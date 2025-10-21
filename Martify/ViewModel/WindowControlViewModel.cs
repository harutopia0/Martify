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
    public class WindowControlViewModel : BaseViewModel
    {
        #region commands
        public ICommand CloseWindowControl { get; set; }
        #endregion

        public WindowControlViewModel()
        {
            CloseWindowControl = new RelayCommand<UserControl>((p) => { return p != null; }, (p) =>
            {
                var win = Window.GetWindow(p);

                if (win != null)
                {
                    win.Close();
                }
            });
        }
    }
}

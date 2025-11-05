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
    public class WindowControlViewModels : BaseViewModels
    {
        #region commands
        public ICommand CloseWindowControl { get; set; }
        #endregion

        public WindowControlViewModels()
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

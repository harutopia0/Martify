using System.ComponentModel;
using Martify.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Martify.ViewModels
{
    public class MainVM : BaseVM
    {
        // Xóa 'public bool isLoaded = false;'

        public NavigationVM Navigation { get; }

        public MainVM()
        {
            // CHỈ KHỞI TẠO CÁC THUỘC TÍNH CỦA VIEWMODEL Ở ĐÂY
            Navigation = new NavigationVM();

            // XÓA TẤT CẢ CODE SAU:
            /*
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                if (!isLoaded)
                {
                    isLoaded = true;
                    LoginWindow login = new LoginWindow();
                    login.ShowDialog();
                }
            }
            */
        }
    }
}
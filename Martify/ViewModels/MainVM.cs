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
        public bool isLoaded = false;

        public NavigationVM Navigation { get; }


        //Mọi xử lý nằm ở đây.
        public MainVM()
        {
            // Chỉ chạy code này khi chương trình KHÔNG ở chế độ Design
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                if (!isLoaded)
                {
                    isLoaded = true;
                    LoginWindow login = new LoginWindow();
                    login.ShowDialog();
                }
            }

            Navigation = new NavigationVM();
        }
    }
}
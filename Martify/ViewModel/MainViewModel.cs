using System.ComponentModel;
using Martify.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Martify.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        public bool isLoaded = false;


        //Mọi xử lý nằm ở đây.
        public MainViewModel()
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
        }
    }
}
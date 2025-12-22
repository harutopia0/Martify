using Martify.Models;
using Martify.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class MainVM : BaseVM
    {
        public bool isLoaded = false;
        public ICommand LoadedWindowCommand
        {
            get;
            set;
        }
        public NavigationVM Navigation
        {
            get;
        }

        private bool _isAdmin;
        public bool IsAdmin
        {
            get => _isAdmin;
            set
            {
                _isAdmin = value;
                OnPropertyChanged();
            }
        }

        private int _selectedMenuIndex = -1;
        public int SelectedMenuIndex
        {
            get => _selectedMenuIndex;
            set
            {
                _selectedMenuIndex = value;
                OnPropertyChanged();
            }
        }

        private string _FullName;
        public string FullName
        {
            get => _FullName;
            set
            {
                _FullName = value;
                OnPropertyChanged();
            }
        }

        private string _Email;
        public string Email
        {
            get => _Email;
            set
            {
                _Email = value;
                OnPropertyChanged();
            }
        }

        private string _ImagePath;
        public string ImagePath
        {
            get => _ImagePath;
            set
            {
                _ImagePath = value;
                OnPropertyChanged();
            }
        }

        public MainVM()
        {
            LoadedWindowCommand = new RelayCommand<Window>((p) => { return true; }, (p) =>
            {
                isLoaded = true;
                if (p == null) return;

                if (DataProvider.Ins.CurrentAccount == null)
                {
                    p.Hide();
                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.ShowDialog();

                    if (loginWindow.DataContext is LoginVM loginVM && loginVM.isLogin)
                    {
                        LoadCurrentUserData();
                        p.Show();
                    }
                    else
                    {
                        p.Close();
                    }
                }
                else
                {
                    LoadCurrentUserData();
                }
            });

            Navigation = new NavigationVM();
        }

        public void ResetSession()
        {
            SelectedMenuIndex = -1;
            Navigation.CurrentView = null;
            FullName = string.Empty;
            Email = string.Empty;
            ImagePath = null;
        }

        public void LoadCurrentUserData()
        {
            var acc = DataProvider.Ins.CurrentAccount;

            if (acc != null && acc.Employee != null)
            {
                FullName = acc.Employee.FullName;
                Email = acc.Employee.Email;
                ImagePath = acc.Employee.ImagePath;

                if (acc.Role == 0)
                {
                    IsAdmin = true;
                    Navigation.DashboardCommand.Execute(null);
                    SelectedMenuIndex = 0;
                }
                else
                {
                    IsAdmin = false;
                    Navigation.ProductSelectionCommand.Execute(null);
                    SelectedMenuIndex = 1;
                }
            }
            else
            {
                FullName = "N/A";
                Email = "Chưa cập nhật";
                ImagePath = null;
                IsAdmin = false;
                SelectedMenuIndex = -1;
            }
        }
    }
}
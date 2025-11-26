using Martify.Models;
using Martify.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using System.Threading.Tasks;

namespace Martify.ViewModels
{
    class LoginVM : BaseVM
    {
        public bool isLogin { get; set; }

        private string _Username;
        public string Username
        {
            get { return _Username; }
            set { _Username = value; OnPropertyChanged(); }
        }

        private string _Password;
        public string Password
        {
            get { return _Password; }
            set { _Password = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; set; }
        public ICommand PasswordChangedCommand { get; set; }

        public LoginVM()
        {
            isLogin = false;
            LoginCommand = new RelayCommand<Window>((p) => { return true; }, (p) => { Login(p); });
            PasswordChangedCommand = new RelayCommand<PasswordBox>((p) => { return true; }, (p) => { Password = p.Password; });




            FailedLoginCommand = new RelayCommand(LoginErrorPopup);
            Status = null;


            //CreateAdminAccountIfMissing();
        }


        //Tạo tài khoản admin mặc định, không kích hoạt lại code này!!!
        /*void CreateAdminAccountIfMissing()
        {
            var db = DataProvider.Ins.DB;

            if (!db.Accounts.Any(x => x.Username == "admin"))
            {
                var adminEmp = db.Employees.FirstOrDefault(e => e.EmployeeID == "ADMIN001");
                if (adminEmp == null)
                {
                    adminEmp = new Employee()
                    {
                        EmployeeID = "ADMIN001",
                        FullName = "Administrator",


                        Phone = "0000000000",

                        Email = "systen_admin@martify.com",

                        BirthDate = DateTime.Now,
                        HireDate = DateTime.Now
                    };

                    db.Employees.Add(adminEmp);
                    db.SaveChanges();
                }

                var newAdmin = new Account()
                {
                    Username = "admin",
                    HashPassword = ConvertToSHA256("admin"),
                    Role = 1,
                    EmployeeID = "ADMIN001"
                };

                db.Accounts.Add(newAdmin);
                db.SaveChanges();
            }
        }*/

        void Login(Window p)
        {
            if (p == null) return;
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                //MessageBox.Show("Please enter username and password!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                FailedLoginCommand.Execute(null);

                return;
            }


            string passHash = ConvertToSHA256(Password);

            var acc = DataProvider.Ins.DB.Accounts
                .Include(x => x.Employee)
                .Where(x => x.Username == Username && x.HashPassword == passHash)
                .FirstOrDefault();

            if (acc != null)
            {
                DataProvider.Ins.CurrentAccount = acc;
                isLogin = true;
                p.Close();
            }
            else
            {
                isLogin = false;
                //MessageBox.Show("Invalid username or password!", "Login failed", MessageBoxButton.OK, MessageBoxImage.Error);
                FailedLoginCommand.Execute(null);
            }
        }

        public static string ConvertToSHA256(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

















        private object _status;
        public object Status
        {
            get { return _status; }
            set { _status = value; OnPropertyChanged(); }
        }


        private CancellationTokenSource _cts;

        public ICommand FailedLoginCommand { get; set; }

        private async void LoginErrorPopup(object obj)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            _cts = new CancellationTokenSource();

            try
            {
                Status = new LoginErrorPopupVM();

                await Task.Delay(3000, _cts.Token);

                Status = null;
            }
            catch (TaskCanceledException)
            {
                // Nếu bị hủy (do người dùng bấm Login lần nữa khi chưa hết 3s) 
                // thì code sẽ nhảy vào đây -> Không làm gì cả -> Để lần bấm mới tự xử lý.
            }
        }


    }
}
using Martify.Models;
using Martify.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class EmployeeVM : BaseVM
    {
        private ObservableCollection<Models.Employee> _Employees;
        public ObservableCollection<Models.Employee> Employees
        {
            get { return _Employees; }
            set
            {
                _Employees = value;
                OnPropertyChanged();
            }
        }


        public ICommand AddEmployeeCommand { get; set; }



        public EmployeeVM()
        {
            //Không cố gắng lấy dữ liêu mẫu để hiển thị ở chế độ Designer
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject())) return;

            LoadList();

            AddEmployeeCommand = new RelayCommand<object>((p) => { return true; }, (p) =>
            {
                Window addEmployeeWindow = new AddEmployee();
                addEmployeeWindow.ShowDialog();

                LoadList();
            });
        }

        void LoadList()
        {
            var list = DataProvider.Ins.DB.Employees.Include(emp => emp.Accounts).ToList();
            Employees = new ObservableCollection<Models.Employee>(list);
        }
    }
}
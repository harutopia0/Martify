using Martify.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Martify.ViewModels
{
    public class EmployeeVM : BaseVM
    {
        private ObservableCollection<Employee> _Employees;
        public ObservableCollection<Employee> Employees
        {
            get { return _Employees; }
            set
            {
                _Employees = value;
                OnPropertyChanged();
            }
        }

        public EmployeeVM()
        {
            //Không cố gắng lấy dữ liêu mẫu để hiển thị ở chế độ Designer
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject())) return;

            Employees = new ObservableCollection<Employee>(DataProvider.Ins.DB.Employees.Include(emp => emp.Accounts));
        }
    }
}
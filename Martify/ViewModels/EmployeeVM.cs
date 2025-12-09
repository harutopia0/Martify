using Martify.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System;
using System.Windows;
// Alias để tránh xung đột tên với View
using EmployeeModel = Martify.Models.Employee;

namespace Martify.ViewModels
{
    public class EmployeeVM : BaseVM
    {
        private ObservableCollection<EmployeeModel> _Employees;
        public ObservableCollection<EmployeeModel> Employees
        {
            get { return _Employees; }
            set { _Employees = value; OnPropertyChanged(); }
        }

        private EmployeeModel _selectedDetailEmployee;
        public EmployeeModel SelectedDetailEmployee
        {
            get => _selectedDetailEmployee;
            set { _selectedDetailEmployee = value; OnPropertyChanged(); }
        }

        private bool _isDetailsPanelOpen;
        public bool IsDetailsPanelOpen
        {
            get => _isDetailsPanelOpen;
            set { _isDetailsPanelOpen = value; OnPropertyChanged(); }
        }

        private string _keyword;
        public string Keyword
        {
            get => _keyword;
            set { _keyword = value; OnPropertyChanged(); LoadList(); }
        }

        private int? _selectedMonth;
        public int? SelectedMonth
        {
            get => _selectedMonth;
            set { _selectedMonth = value; OnPropertyChanged(); LoadList(); }
        }

        private int? _selectedYear;
        public int? SelectedYear
        {
            get => _selectedYear;
            set { _selectedYear = value; OnPropertyChanged(); LoadList(); }
        }

        public ObservableCollection<int> Months { get; set; } = new ObservableCollection<int>();
        public ObservableCollection<int> Years { get; set; } = new ObservableCollection<int>();

        public ICommand AddEmployeeCommand { get; set; }
        public ICommand ClearFilterCommand { get; set; }
        public ICommand OpenDetailsCommand { get; set; }

        public EmployeeVM()
        {
            InitFilterData();
            LoadList();

            AddEmployeeCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                Martify.Views.AddEmployee addEmployeeWindow = new Martify.Views.AddEmployee();
                addEmployeeWindow.ShowDialog();
                InitFilterData();
                LoadList();
            });

            // SỬA LỖI AN TOÀN: Nhận object thay vì ép kiểu cứng ngay từ đầu
            OpenDetailsCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                // Kiểm tra xem p có đúng là nhân viên không
                if (p is EmployeeModel emp)
                {
                    SelectedDetailEmployee = emp;
                    IsDetailsPanelOpen = true;
                }
            });

            ClearFilterCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                Keyword = string.Empty;
                SelectedMonth = null;
                SelectedYear = null;
                IsDetailsPanelOpen = false; // Đóng panel
                LoadList();
            });
        }

        void InitFilterData()
        {
            Months.Clear();
            for (int i = 1; i <= 12; i++) Months.Add(i);

            Years.Clear();
            var dbYears = DataProvider.Ins.DB.Employees.Select(x => x.HireDate.Year).Distinct().OrderByDescending(y => y).ToList();
            foreach (var y in dbYears) Years.Add(y);
        }

        void LoadList()
        {
            var query = DataProvider.Ins.DB.Employees.AsQueryable();

            if (SelectedMonth.HasValue) query = query.Where(x => x.HireDate.Month == SelectedMonth.Value);
            if (SelectedYear.HasValue) query = query.Where(x => x.HireDate.Year == SelectedYear.Value);

            var list = query.ToList();

            if (!string.IsNullOrEmpty(Keyword))
            {
                string searchKey = ConvertToUnSign(Keyword).ToLower();
                list = list.Where(x => ConvertToUnSign(x.FullName).ToLower().Contains(searchKey)).ToList();
            }
            Employees = new ObservableCollection<EmployeeModel>(list);
        }

        private string ConvertToUnSign(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = text.Normalize(System.Text.NormalizationForm.FormD);
            return regex.Replace(temp, String.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D');
        }
    }
}
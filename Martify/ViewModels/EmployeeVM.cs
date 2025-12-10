using Martify.Models;
using Martify.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions; // Cần thêm thư viện này
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Martify.ViewModels
{
    public class EmployeeVM : BaseVM
    {
        // --- DANH SÁCH HIỂN THỊ ---
        private ObservableCollection<Models.Employee> _Employees;
        public ObservableCollection<Models.Employee> Employees
        {
            get { return _Employees; }
            set { _Employees = value; OnPropertyChanged(); }
        }

        // --- CÁC BIẾN LỌC (FILTER) ---

        // 1. Tìm kiếm theo tên
        private string _keyword;
        public string Keyword
        {
            get => _keyword;
            set
            {
                _keyword = value;
                OnPropertyChanged();
                LoadList(); // Gọi lại hàm load mỗi khi gõ phím
            }
        }

        // 2. Lọc theo Tháng
        private int? _selectedMonth;
        public int? SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                _selectedMonth = value;
                OnPropertyChanged();
                LoadList(); // Gọi lại hàm load khi chọn tháng
            }
        }

        // 3. Lọc theo Năm
        private int? _selectedYear;
        public int? SelectedYear
        {
            get => _selectedYear;
            set
            {
                _selectedYear = value;
                OnPropertyChanged();
                LoadList(); // Gọi lại hàm load khi chọn năm
            }
        }

        // --- NGUỒN DỮ LIỆU CHO COMBOBOX ---
        public ObservableCollection<int> Months { get; set; } = new ObservableCollection<int>();
        public ObservableCollection<int> Years { get; set; } = new ObservableCollection<int>();

        // --- COMMANDS ---
        public ICommand AddEmployeeCommand { get; set; }
        public ICommand ClearFilterCommand { get; set; }

        public EmployeeVM()
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject())) return;

            InitFilterData();
            LoadList();

            AddEmployeeCommand = new RelayCommand<object>((p) => { return true; }, (p) =>
            {
                Window addEmployeeWindow = new AddEmployee();
                addEmployeeWindow.ShowDialog();

                InitFilterData();
                LoadList();
            });

            ClearFilterCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                Keyword = string.Empty;
                SelectedMonth = null;
                SelectedYear = null;
                LoadList();
            });
        }

        void InitFilterData()
        {
            Months.Clear();
            for (int i = 1; i <= 12; i++) Months.Add(i);

            Years.Clear();
            var dbYears = DataProvider.Ins.DB.Employees
                            .Select(x => x.HireDate.Year)
                            .Distinct()
                            .OrderByDescending(y => y)
                            .ToList();

            foreach (var y in dbYears) Years.Add(y);
        }

        void LoadList()
        {
            // B1: Tạo truy vấn cơ bản
            var query = DataProvider.Ins.DB.Employees.Include(emp => emp.Accounts).AsQueryable();

            // B2: Lọc Tháng/Năm NGAY TẠI DATABASE (Hiệu năng cao)
            if (SelectedMonth.HasValue)
            {
                query = query.Where(x => x.HireDate.Month == SelectedMonth.Value);
            }

            if (SelectedYear.HasValue)
            {
                query = query.Where(x => x.HireDate.Year == SelectedYear.Value);
            }

            // B3: Lấy dữ liệu về RAM
            var list = query.ToList();

            // B4: Lọc Tên TẠI RAM (Thông minh: Bỏ dấu, không phân biệt hoa thường)
            if (!string.IsNullOrEmpty(Keyword))
            {
                string searchKey = ConvertToUnSign(Keyword).ToLower();

                list = list.Where(x => ConvertToUnSign(x.FullName).ToLower().Contains(searchKey)).ToList();

            }

            // B5: Hiển thị
            Employees = new ObservableCollection<Models.Employee>(list);
        }

        // Hàm hỗ trợ: Chuyển tiếng Việt có dấu thành không dấu
        private string ConvertToUnSign(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = text.Normalize(NormalizationForm.FormD);
            return regex.Replace(temp, String.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D');
        }
    }
}
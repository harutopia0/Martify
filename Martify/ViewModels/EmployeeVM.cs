using Martify.Helpers; // Sử dụng Helper
using Martify.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EmployeeModel = Martify.Models.Employee;

namespace Martify.ViewModels
{
    public class EmployeeVM : BaseVM, IDataErrorInfo
    {
        private ObservableCollection<EmployeeModel> _Employees;
        public ObservableCollection<EmployeeModel> Employees
        {
            get => _Employees;
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

        // --- BUFFER DATA ---
        private string _editFullName;
        public string EditFullName
        {
            get => _editFullName;
            set { _editFullName = value; OnPropertyChanged(); CheckModified(); }
        }

        private string _editEmail;
        public string EditEmail
        {
            get => _editEmail;
            set { _editEmail = value; OnPropertyChanged(); CheckModified(); }
        }

        private string _editPhone;
        public string EditPhone
        {
            get => _editPhone;
            set { _editPhone = value; OnPropertyChanged(); CheckModified(); }
        }

        private string _editGender;
        public string EditGender
        {
            get => _editGender;
            set { _editGender = value; OnPropertyChanged(); CheckModified(); }
        }

        private string _editAddress;
        public string EditAddress
        {
            get => _editAddress;
            set { _editAddress = value; OnPropertyChanged(); CheckModified(); }
        }

        private DateTime? _editBirthDate;
        public DateTime? EditBirthDate
        {
            get => _editBirthDate;
            set
            {
                _editBirthDate = value;
                OnPropertyChanged();
                CheckModified();
                OnPropertyChanged(nameof(EditHireDate));
            }
        }

        private DateTime? _editHireDate;
        public DateTime? EditHireDate
        {
            get => _editHireDate;
            set
            {
                _editHireDate = value;
                OnPropertyChanged();
                CheckModified();
                OnPropertyChanged(nameof(EditBirthDate));
            }
        }

        private bool _editStatus;
        public bool EditStatus
        {
            get => _editStatus;
            set { _editStatus = value; OnPropertyChanged(); CheckModified(); }
        }

        private string _editImagePath;
        public string EditImagePath
        {
            get => _editImagePath;
            set { _editImagePath = value; OnPropertyChanged(); CheckModified(); }
        }

        private string _sourceImageFile;

        private bool _isModified;
        public bool IsModified
        {
            get => _isModified;
            set { _isModified = value; OnPropertyChanged(); }
        }

        private string _saveMessage;
        public string SaveMessage
        {
            get => _saveMessage;
            set { _saveMessage = value; OnPropertyChanged(); }
        }

        public List<string> GenderList { get; set; } = new List<string> { "Nam", "Nữ" };

        // --- Filters ---
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
        public ICommand ToggleStatusCommand { get; set; }
        public ICommand SaveChangesCommand { get; set; }
        public ICommand SelectImageCommand { get; set; }

        // VALIDATION
        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                if (SelectedDetailEmployee == null) return null;

                string error = null;
                string currentId = SelectedDetailEmployee.EmployeeID;

                switch (columnName)
                {
                    case nameof(EditFullName):
                        error = EmployeeValidator.CheckFullName(EditFullName);
                        break;

                    case nameof(EditAddress):
                        error = EmployeeValidator.CheckAddress(EditAddress);
                        break;

                    case nameof(EditPhone):
                        error = EmployeeValidator.CheckPhone(EditPhone, currentId);
                        break;

                    case nameof(EditEmail):
                        error = EmployeeValidator.CheckEmail(EditEmail, currentId);
                        break;

                    case nameof(EditGender):
                        error = EmployeeValidator.CheckGender(EditGender);
                        break;

                    case nameof(EditBirthDate):
                        error = EmployeeValidator.CheckBirthDate(EditBirthDate, EditHireDate);
                        break;

                    case nameof(EditHireDate):
                        error = EmployeeValidator.CheckHireDate(EditHireDate, EditBirthDate);
                        break;
                }

                return error;
            }
        }

        private bool IsValid()
        {
            if (SelectedDetailEmployee == null) return false;

            string id = SelectedDetailEmployee.EmployeeID;

            if (EmployeeValidator.CheckFullName(EditFullName) != null) return false;
            if (EmployeeValidator.CheckAddress(EditAddress) != null) return false;
            if (EmployeeValidator.CheckPhone(EditPhone, id) != null) return false;
            if (EmployeeValidator.CheckEmail(EditEmail, id) != null) return false;
            if (EmployeeValidator.CheckGender(EditGender) != null) return false;
            if (EmployeeValidator.CheckBirthDate(EditBirthDate, EditHireDate) != null) return false;
            if (EmployeeValidator.CheckHireDate(EditHireDate, EditBirthDate) != null) return false;

            return true;
        }

        public EmployeeVM()
        {
            InitFilterData();
            LoadList();

            AddEmployeeCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                var addEmployeeWindow = new Martify.Views.AddEmployee();
                addEmployeeWindow.ShowDialog();
                InitFilterData();
                LoadList();
            });

            OpenDetailsCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                if (p is EmployeeModel emp)
                {
                    SelectedDetailEmployee = emp;

                    EditFullName = emp.FullName;
                    EditEmail = emp.Email;
                    EditPhone = emp.Phone;
                    EditGender = emp.Gender;
                    EditAddress = emp.Address;
                    EditBirthDate = emp.BirthDate;
                    EditHireDate = emp.HireDate;
                    EditStatus = emp.Status.GetValueOrDefault();
                    EditImagePath = emp.ImagePath;

                    _sourceImageFile = null;

                    IsModified = false;
                    SaveMessage = string.Empty;
                    IsDetailsPanelOpen = true;
                }
            });

            ClearFilterCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                Keyword = string.Empty;
                SelectedMonth = null;
                SelectedYear = null;
                IsDetailsPanelOpen = false;

                SelectedDetailEmployee = null;

                EditFullName = null;
                EditAddress = null;
                EditBirthDate = null;
                EditEmail = null;
                EditGender = null;
                EditHireDate = null;
                EditPhone = null;

                LoadList();
            });

            ToggleStatusCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                EditStatus = !EditStatus;
            });

            SelectImageCommand = new RelayCommand<object>((p) => true, (p) =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
                };

                if (dlg.ShowDialog() == true)
                {
                    _sourceImageFile = dlg.FileName;
                    EditImagePath = _sourceImageFile;
                }
            });

            SaveChangesCommand = new RelayCommand<object>((p) => IsModified, async (p) =>
            {
                if (SelectedDetailEmployee == null) return;

                if (!IsValid())
                {
                    SaveMessage = "Vui lòng kiểm tra lại thông tin lỗi!";
                    await Task.Delay(3000);
                    if (SaveMessage == "Vui lòng kiểm tra lại thông tin lỗi!") SaveMessage = "";
                    return;
                }

                var empInDb = DataProvider.Ins.DB.Employees
                    .FirstOrDefault(x => x.EmployeeID == SelectedDetailEmployee.EmployeeID);

                if (empInDb != null)
                {
                    if (!string.IsNullOrEmpty(_sourceImageFile))
                    {
                        string newPath = HandleImageSave(empInDb.EmployeeID, _sourceImageFile);
                        if (newPath != "ERROR") empInDb.ImagePath = newPath;
                    }

                    empInDb.FullName = EditFullName;
                    empInDb.Email = EditEmail;
                    empInDb.Phone = EditPhone;
                    empInDb.Gender = EditGender;
                    empInDb.Address = EditAddress;
                    empInDb.BirthDate = EditBirthDate ?? empInDb.BirthDate;
                    empInDb.HireDate = EditHireDate ?? empInDb.HireDate;
                    empInDb.Status = EditStatus;

                    DataProvider.Ins.DB.SaveChanges();

                    SaveMessage = "Đã lưu thay đổi thành công!";
                    IsModified = false;

                    LoadList();

                    SelectedDetailEmployee = Employees.FirstOrDefault(
                        x => x.EmployeeID == empInDb.EmployeeID);

                    await Task.Delay(3000);
                    if (SaveMessage == "Đã lưu thay đổi thành công!") SaveMessage = "";
                }
            });
        }

        private string HandleImageSave(string empId, string sourceFile)
        {
            try
            {
                string ext = System.IO.Path.GetExtension(sourceFile);
                string fileName = $"{empId}_{DateTime.Now:yyyyMMddHHmmss}{ext}";

                string binFolder = AppDomain.CurrentDomain.BaseDirectory;
                string binAssets = System.IO.Path.Combine(binFolder, "Assets", "Employee");

                if (!System.IO.Directory.Exists(binAssets))
                    System.IO.Directory.CreateDirectory(binAssets);

                string destFile = System.IO.Path.Combine(binAssets, fileName);
                System.IO.File.Copy(sourceFile, destFile, true);

                try
                {
                    string projectFolder = System.IO.Path.GetFullPath(
                        System.IO.Path.Combine(binFolder, @"..\..\..\"));

                    string projectAssets = System.IO.Path.Combine(projectFolder, "Assets", "Employee");

                    if (System.IO.Directory.Exists(System.IO.Path.Combine(projectFolder, "Assets")))
                    {
                        if (!System.IO.Directory.Exists(projectAssets))
                            System.IO.Directory.CreateDirectory(projectAssets);

                        System.IO.File.Copy(
                            sourceFile,
                            System.IO.Path.Combine(projectAssets, fileName),
                            true
                        );
                    }
                }
                catch { }

                return System.IO.Path.Combine("Assets", "Employee", fileName);
            }
            catch
            {
                return "ERROR";
            }
        }

        private void CheckModified()
        {
            if (SelectedDetailEmployee == null) return;

            bool changed =
                EditFullName != SelectedDetailEmployee.FullName ||
                EditEmail != SelectedDetailEmployee.Email ||
                EditPhone != SelectedDetailEmployee.Phone ||
                EditGender != SelectedDetailEmployee.Gender ||
                EditAddress != SelectedDetailEmployee.Address ||
                EditBirthDate != SelectedDetailEmployee.BirthDate ||
                EditHireDate != SelectedDetailEmployee.HireDate ||
                EditStatus != SelectedDetailEmployee.Status ||
                EditImagePath != SelectedDetailEmployee.ImagePath;

            IsModified = changed;

            if (changed) SaveMessage = "";
        }

        // Filter loader
        private void InitFilterData()
        {
            Months.Clear();
            for (int i = 1; i <= 12; i++) Months.Add(i);

            Years.Clear();
            var dbYears = DataProvider.Ins.DB.Employees
                .Select(x => x.HireDate.Year)
                .Distinct()
                .OrderByDescending(x => x)
                .ToList();

            foreach (var y in dbYears) Years.Add(y);
        }

        private void LoadList()
        {
            var query = DataProvider.Ins.DB.Employees.AsNoTracking().AsQueryable();

            if (SelectedMonth.HasValue)
                query = query.Where(x => x.HireDate.Month == SelectedMonth.Value);

            if (SelectedYear.HasValue)
                query = query.Where(x => x.HireDate.Year == SelectedYear.Value);

            var list = query.ToList();

            // --- CẬP NHẬT LOGIC TÌM KIẾM Ở ĐÂY ---
            if (!string.IsNullOrEmpty(Keyword))
            {
                string k = ConvertToUnSign(Keyword).ToLower();
                list = list.Where(x =>
                        ConvertToUnSign(x.FullName).ToLower().Contains(k) || // Tìm theo Tên
                        x.EmployeeID.ToLower().Contains(k))                  // Tìm theo Mã NV
                    .ToList();
            }

            Employees = new ObservableCollection<EmployeeModel>(list);
        }

        private string ConvertToUnSign(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = text.Normalize(System.Text.NormalizationForm.FormD);

            return regex
                .Replace(temp, string.Empty)
                .Replace('\u0111', 'd')
                .Replace('\u0110', 'D');
        }
    }
}
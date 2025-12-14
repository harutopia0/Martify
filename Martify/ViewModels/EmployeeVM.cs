using ClosedXML.Excel;
using Martify.Helpers; // Sử dụng Helper
using Martify.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
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

        // --- COMMAND MỚI ---
        public ICommand ExportExcelCommand { get; set; }
        public ICommand ImportExcelCommand { get; set; }

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

            // 1. Lệnh Xuất Excel (ClosedXML)
            ExportExcelCommand = new RelayCommand<object>((p) => true, (p) => ExportToExcel());

            // 2. Lệnh Nhập Excel (ClosedXML)
            ImportExcelCommand = new RelayCommand<object>((p) => true, (p) => ImportFromExcel());
        }


        // --- LOGIC XUẤT EXCEL VỚI CLOSEDXML ---
        private void ExportToExcel()
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = $"DanhSachNhanVien_{DateTime.Now:ddMMyyyy}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Employees");

                        // 1. Tạo Header
                        string[] headers = { "Mã NV", "Họ Tên", "Ngày Sinh", "SĐT", "Giới Tính", "Email", "Ngày Vào Làm", "Địa Chỉ", "Trạng Thái" };
                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = worksheet.Cell(1, i + 1);
                            cell.Value = headers[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }

                        // 2. Đổ dữ liệu
                        var listExport = Employees ?? new ObservableCollection<EmployeeModel>();
                        int row = 2;
                        foreach (var emp in listExport)
                        {
                            worksheet.Cell(row, 1).Value = emp.EmployeeID;
                            worksheet.Cell(row, 2).Value = emp.FullName;
                            worksheet.Cell(row, 3).Value = emp.BirthDate; // ClosedXML tự xử lý format Date
                            worksheet.Cell(row, 4).Value = emp.Phone;
                            worksheet.Cell(row, 5).Value = emp.Gender;
                            worksheet.Cell(row, 6).Value = emp.Email;
                            worksheet.Cell(row, 7).Value = emp.HireDate;
                            worksheet.Cell(row, 8).Value = emp.Address;
                            worksheet.Cell(row, 9).Value = (emp.Status == true) ? "Đang làm việc" : "Đã nghỉ việc";

                            // Format cột ngày tháng
                            worksheet.Cell(row, 3).Style.DateFormat.Format = "dd/MM/yyyy";
                            worksheet.Cell(row, 7).Style.DateFormat.Format = "dd/MM/yyyy";

                            row++;
                        }

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(saveFileDialog.FileName);
                    }
                    MessageBox.Show("Xuất file Excel thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất file: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- LOGIC NHẬP EXCEL (PHIÊN BẢN SỬA LỖI & BÁO CỤ THỂ LÝ DO) ---
        private void ImportFromExcel()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook(openFileDialog.FileName))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Bỏ qua dòng tiêu đề

                        int successCount = 0;
                        int errorCount = 0;
                        StringBuilder errorLog = new StringBuilder(); // Dùng để ghi lại lỗi chi tiết

                        int rowIndex = 2; // Bắt đầu từ dòng 2 (vì dòng 1 là header)

                        foreach (var row in rows)
                        {
                            try
                            {
                                // --- BƯỚC 1: ĐỌC DỮ LIỆU ---
                                // Giả định cấu trúc cột:
                                // Col 1: Họ Tên | Col 2: Ngày Sinh | Col 3: SĐT | Col 4: Giới Tính | Col 5: Email | Col 6: Ngày Vào | Col 7: Địa Chỉ

                                string fullName = row.Cell(1).GetValue<string>().Trim();

                                // Nếu tên trống -> Coi như dòng rác hoặc do lệch cột -> Ghi lỗi để kiểm tra
                                if (string.IsNullOrWhiteSpace(fullName))
                                {
                                    errorCount++;
                                    errorLog.AppendLine($"Dòng {rowIndex}: Họ tên bị trống (Kiểm tra lại thứ tự cột).");
                                    rowIndex++;
                                    continue;
                                }

                                DateTime? parsedDob = ParseExcelDate(row.Cell(2));
                                string phone = row.Cell(3).GetValue<string>().Trim();
                                string gender = row.Cell(4).GetValue<string>().Trim();
                                string email = row.Cell(5).GetValue<string>().Trim();
                                DateTime? parsedHireDate = ParseExcelDate(row.Cell(6));
                                string address = row.Cell(7).GetValue<string>().Trim();

                                // --- BƯỚC 2: VALIDATION (KIỂM TRA HỢP LỆ) ---
                                bool isValid = true;
                                string rowError = "";

                                // 1. Check Tên
                                string nameErr = EmployeeValidator.CheckFullName(fullName);
                                if (nameErr != null) { isValid = false; rowError += $"Tên lỗi ({nameErr}); "; }

                                // 2. Check Ngày sinh
                                if (parsedDob == null) { isValid = false; rowError += "Ngày sinh sai định dạng; "; }
                                else if (EmployeeValidator.CheckBirthDate(parsedDob, null) != null) { isValid = false; rowError += "Ngày sinh không hợp lệ; "; }

                                // 3. Check SĐT
                                string phoneErr = EmployeeValidator.CheckPhone(phone, null);
                                if (phoneErr != null) { isValid = false; rowError += $"SĐT lỗi ({phoneErr}); "; }

                                // 4. Check Email
                                string emailErr = EmployeeValidator.CheckEmail(email, null);
                                if (emailErr != null) { isValid = false; rowError += $"Email lỗi ({emailErr}); "; }

                                // 5. Check Giới tính
                                if (string.IsNullOrEmpty(gender) || (gender != "Nam" && gender != "Nữ"))
                                {
                                    isValid = false;
                                    rowError += "Giới tính phải là 'Nam' hoặc 'Nữ'; ";
                                }

                                // 6. Check Ngày vào làm
                                DateTime hireDate = parsedHireDate ?? DateTime.Now;
                                if (parsedDob != null && EmployeeValidator.CheckHireDate(hireDate, parsedDob) != null)
                                {
                                    isValid = false;
                                    rowError += "Ngày vào làm không hợp lệ (hoặc chưa đủ 18 tuổi); ";
                                }

                                // NẾU CÓ LỖI -> GHI LẠI VÀ BỎ QUA
                                if (!isValid)
                                {
                                    errorCount++;
                                    if (errorCount <= 5) // Chỉ ghi chi tiết 5 lỗi đầu tiên để tránh spam
                                        errorLog.AppendLine($"Dòng {rowIndex}: {rowError}");
                                    rowIndex++;
                                    continue;
                                }

                                // --- BƯỚC 3: THÊM VÀO DB ---
                                string newID = GenerateEmployeeID();

                                var newEmp = new EmployeeModel
                                {
                                    EmployeeID = newID,
                                    FullName = fullName,
                                    BirthDate = parsedDob.Value,
                                    Phone = phone,
                                    Gender = gender,
                                    Email = email,
                                    Address = address,
                                    HireDate = hireDate,
                                    Status = true,
                                    ImagePath = null
                                };

                                string firstName = GetFirstName(fullName);
                                string username = (ConvertToUnSign(firstName) + newID).ToLower().Replace(" ", "");
                                string rawPass = parsedDob.Value.ToString("ddMMyyyy");
                                string hashPass = CalculateSHA256(rawPass);

                                var newAcc = new Account
                                {
                                    Username = username,
                                    HashPassword = hashPass,
                                    Role = 1,
                                    EmployeeID = newID
                                };
                                newEmp.Accounts = new List<Account> { newAcc };

                                DataProvider.Ins.DB.Employees.Add(newEmp);
                                DataProvider.Ins.DB.SaveChanges();
                                successCount++;
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                errorLog.AppendLine($"Dòng {rowIndex}: Lỗi hệ thống ({ex.Message})");
                            }

                            rowIndex++;
                        }

                        LoadList();

                        // Hiển thị thông báo chi tiết
                        string msg = $"Thành công: {successCount}\nThất bại: {errorCount}";
                        if (errorCount > 0)
                        {
                            msg += "\n\nChi tiết lỗi (5 dòng đầu):\n" + errorLog.ToString();
                        }

                        MessageBox.Show(msg, "Kết quả Import", MessageBoxButton.OK,
                            errorCount == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể đọc file Excel. Hãy đảm bảo file không bị khóa.\n" + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Hàm xử lý ngày tháng mạnh mẽ hơn (Chấp nhận cả Date, Number, String)
        private DateTime? ParseExcelDate(IXLCell cell)
        {
            if (cell.IsEmpty()) return null;

            try
            {
                // Trường hợp 1: Excel nhận diện đúng là Ngày tháng
                if (cell.DataType == XLDataType.DateTime)
                {
                    return cell.GetDateTime();
                }

                // Trường hợp 2: Excel lưu dưới dạng số (Number)
                if (cell.DataType == XLDataType.Number)
                {
                    return DateTime.FromOADate(cell.GetDouble());
                }

                // Trường hợp 3: Excel lưu dạng Text ("15/02/1990")
                string text = cell.GetString().Trim();
                if (DateTime.TryParseExact(text, new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime result))
                {
                    return result;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // --- CÁC HÀM HELPER ---
        private string GenerateEmployeeID()
        {
            var empIds = DataProvider.Ins.DB.Employees.Where(x => x.EmployeeID.StartsWith("NV")).Select(x => x.EmployeeID).ToList();
            if (empIds.Count == 0) return "NV001";
            int maxId = 0;
            foreach (var id in empIds) { if (id.Length > 2 && int.TryParse(id.Substring(2), out int num)) if (num > maxId) maxId = num; }
            return "NV" + (maxId + 1).ToString("D3");
        }
        private string GetFirstName(string fullName) { if (string.IsNullOrWhiteSpace(fullName)) return ""; return fullName.Trim().Split(' ').Last(); }
        // Hàm ConvertToUnSign đã có sẵn trong class (như code bạn gửi trước đó)
        private string CalculateSHA256(string rawData) { using (SHA256 sha256Hash = SHA256.Create()) { byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData)); StringBuilder builder = new StringBuilder(); for (int i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2")); return builder.ToString(); } }


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
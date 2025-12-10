using Martify.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Martify.Helpers
{
    public static class EmployeeValidator
    {
        // 1. Validate Họ Tên
        public static string CheckFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "Vui lòng nhập họ và tên.";

            if (!Regex.IsMatch(fullName, @"^[\p{L}\s]+$"))
                return "Họ tên không được chứa số hoặc ký tự đặc biệt.";

            return null;
        }

        // 2. Validate Địa chỉ
        public static string CheckAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return "Vui lòng nhập nơi cư trú.";
            return null;
        }

        // 3. Validate SĐT (Có kiểm tra trùng)
        // tham số 'excludeId' dùng cho trường hợp Sửa (bỏ qua chính mình)
        public static string CheckPhone(string phone, string excludeId = null)
        {
            if (string.IsNullOrEmpty(phone))
                return "Vui lòng nhập SĐT.";

            if (!Regex.IsMatch(phone, @"^[0-9]+$"))
                return "SĐT chỉ được chứa số.";

            if (phone.Length < 9 || phone.Length > 12)
                return "SĐT phải từ 9-12 số.";

            // Kiểm tra trùng trong DB
            var query = DataProvider.Ins.DB.Employees.AsQueryable();

            // Nếu có excludeId (đang sửa), loại bỏ nhân viên đó ra khỏi check
            if (!string.IsNullOrEmpty(excludeId))
            {
                query = query.Where(x => x.EmployeeID != excludeId);
            }

            if (query.Any(x => x.Phone == phone))
                return "SĐT này đã tồn tại trong hệ thống.";

            return null;
        }

        // 4. Validate Email (Có kiểm tra trùng)
        public static string CheckEmail(string email, string excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "Vui lòng nhập Email.";

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return "Email không đúng định dạng.";

            // Kiểm tra trùng
            var query = DataProvider.Ins.DB.Employees.AsQueryable();
            if (!string.IsNullOrEmpty(excludeId))
            {
                query = query.Where(x => x.EmployeeID != excludeId);
            }

            if (query.Any(x => x.Email == email))
                return "Email này đã tồn tại trong hệ thống.";

            return null;
        }

        // 5. Validate Giới tính
        public static string CheckGender(string gender)
        {
            if (string.IsNullOrEmpty(gender))
                return "Vui lòng chọn giới tính.";
            return null;
        }

        // 6. Validate Ngày sinh
        public static string CheckBirthDate(DateTime? birthDate, DateTime? hireDate)
        {
            if (birthDate == null)
                return "Vui lòng chọn ngày sinh.";

            if (birthDate.Value.Date > DateTime.Now.Date)
                return "Ngày sinh không được lớn hơn hiện tại.";

            if (hireDate != null && birthDate.Value >= hireDate.Value)
                return "Ngày sinh phải nhỏ hơn ngày vào làm.";

            return null;
        }

        // 7. Validate Ngày vào làm (Có check tuổi 18)
        public static string CheckHireDate(DateTime? hireDate, DateTime? birthDate)
        {
            if (hireDate == null)
                return "Vui lòng chọn ngày vào làm.";

            if (hireDate.Value.Date > DateTime.Now.Date)
                return "Ngày vào làm không được lớn hơn hiện tại.";

            if (birthDate != null)
            {
                if (hireDate.Value <= birthDate.Value)
                    return "Ngày vào làm phải lớn hơn ngày sinh.";

                // Check đủ 18 tuổi
                int age = hireDate.Value.Year - birthDate.Value.Year;
                if (birthDate.Value > hireDate.Value.AddYears(-age)) age--;

                if (age < 18)
                    return $"Chưa đủ 18 tuổi.";
            }
            return null;
        }
    }
}
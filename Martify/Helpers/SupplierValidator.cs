using System.Text.RegularExpressions;

namespace Martify.Helpers
{
    public static class SupplierValidator
    {
        /// <summary>
        /// Validates supplier name
        /// </summary>
        public static string ValidateSupplierName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Tên nhà cung cấp không được để trống.";

            if (name.Trim().Length < 3)
                return "Tên nhà cung cấp phải có ít nhất 3 ký tự.";

            if (name.Trim().Length > 100)
                return "Tên nhà cung cấp không được vượt quá 100 ký tự.";

            return null;
        }

        /// <summary>
        /// Validates phone number (Vietnamese format)
        /// </summary>
        public static string ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return "Số điện thoại không được để trống.";

            // Remove spaces and special characters for validation
            string cleanPhone = Regex.Replace(phone, @"[\s\-\(\)]", "");

            // Check if contains only digits
            if (!Regex.IsMatch(cleanPhone, @"^\d+$"))
                return "Số điện thoại chỉ được chứa chữ số.";

            // Vietnamese phone numbers: 10-11 digits, starting with 0
            if (cleanPhone.Length < 10 || cleanPhone.Length > 11)
                return "Số điện thoại phải có 10-11 chữ số.";

            if (!cleanPhone.StartsWith("0"))
                return "Số điện thoại phải bắt đầu bằng số 0.";

            return null;
        }

        /// <summary>
        /// Validates email address
        /// </summary>
        public static string ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null; // Email is optional

            email = email.Trim();

            if (email.Length > 255)
                return "Email không được vượt quá 255 ký tự.";

            // Basic email regex pattern
            string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

            if (!Regex.IsMatch(email, pattern))
                return "Email không hợp lệ.";

            return null;
        }

        /// <summary>
        /// Validates address
        /// </summary>
        public static string ValidateAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null; // Address is optional

            if (address.Trim().Length > 300)
                return "Địa chỉ không được vượt quá 300 ký tự.";

            return null;
        }

        
    }
}
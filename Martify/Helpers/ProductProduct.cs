using Martify.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Martify.Helpers
{
    // Lớp ProductValidator được thiết kế để kiểm tra các thuộc tính của sản phẩm
    public static class ProductValidator
    {
        // 1. Validate Tên sản phẩm (ProductName)
        public static string CheckProductName(string productName, string excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return "Vui lòng nhập tên sản phẩm.";

            string name = productName.Trim();

            if (name.Length > 100)
                return "Tên sản phẩm không được quá 100 ký tự.";

            // Kiểm tra trùng lặp (Bỏ qua chính sản phẩm đang sửa nếu có excludeId)
            try
            {
                var query = DataProvider.Ins.DB.Products.AsQueryable();

                if (!string.IsNullOrEmpty(excludeId))
                {
                    query = query.Where(x => x.ProductID != excludeId);
                }

                if (query.Any(x => x.ProductName.Trim().ToLower() == name.ToLower()))
                    return "Tên sản phẩm này đã tồn tại.";
            }
            catch (Exception)
            {
                // Bỏ qua lỗi DB nếu không thể kết nối, vẫn cho phép tiếp tục nhập liệu
                return null;
            }

            return null;
        }

        // 2. Validate Đơn vị tính (Unit)
        public static string CheckUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit))
                return "Vui lòng nhập đơn vị tính.";

            if (unit.Trim().Length > 20)
                return "Đơn vị tính không được quá 20 ký tự.";

            return null;
        }

        // 3. Validate Giá bán (Price)
        public static string CheckPrice(decimal? price)
        {
            if (price == null)
                return "Vui lòng nhập giá bán.";

            if (price.Value <= 0)
                return "Giá bán phải lớn hơn 0.";

            // Có thể thêm giới hạn max nếu cần (ví dụ: price.Value > 999999999)

            return null;
        }

        // 4. Validate Số lượng tồn kho (StockQuantity)
        public static string CheckStockQuantity(int? stockQuantity)
        {
            if (stockQuantity == null)
                return "Vui lòng nhập số lượng tồn kho.";

            if (stockQuantity.Value < 0)
                return "Số lượng tồn kho không được âm.";

            return null;
        }

        // 5. Validate Danh mục (CategoryID)
        public static string CheckCategoryID(string categoryId)
        {
            if (string.IsNullOrWhiteSpace(categoryId))
                return "Vui lòng chọn danh mục.";

            return null;
        }

        // 6. Validate Nhà cung cấp (SupplierID - Tùy chọn)
        public static string CheckSupplierID(string supplierId)
        {
            // Nếu SupplierID là bắt buộc:
            /*
            if (string.IsNullOrWhiteSpace(supplierId))
                return "Vui lòng chọn nhà cung cấp.";
            */

            // Nếu SupplierID là tùy chọn, chỉ cần trả về null
            return null;
        }
    }
}
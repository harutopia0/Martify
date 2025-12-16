using Martify.Models;
using System.Linq;

namespace Martify.Helpers
{
    public static class ProductValidator
    {
        /// <summary>
        /// Kiểm tra tên sản phẩm
        /// </summary>
        public static string CheckProductName(string productName, string currentProductID = null)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return "Vui lòng nhập tên sản phẩm.";

            if (productName.Trim().Length > 100)
                return "Tên sản phẩm không được quá 100 ký tự.";

            // Check if product name already exists (excluding current product)
            var exists = DataProvider.Ins.DB.Products
                .Any(p => p.ProductName.Trim().ToLower() == productName.Trim().ToLower()
                       && (currentProductID == null || p.ProductID != currentProductID));

            if (exists)
                return "Tên sản phẩm này đã tồn tại.";

            return null;
        }

        /// <summary>
        /// Kiểm tra đơn vị tính
        /// </summary>
        public static string CheckUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit))
                return "Vui lòng nhập đơn vị tính.";

            if (unit.Trim().Length > 20)
                return "Đơn vị tính không được quá 20 ký tự.";

            return null;
        }

        /// <summary>
        /// Kiểm tra giá bán
        /// </summary>
        public static string CheckPrice(decimal? price)
        {
            if (price == null)
                return "Vui lòng nhập giá bán.";

            if (price <= 0)
                return "Giá bán phải lớn hơn 0.";

            return null;
        }

        /// <summary>
        /// Kiểm tra số lượng tồn kho
        /// </summary>
        public static string CheckStockQuantity(int? stockQuantity)
        {
            if (stockQuantity == null)
                return "Vui lòng nhập số lượng.";

            if (stockQuantity < 0)
                return "Số lượng không được âm.";

            return null;
        }

        /// <summary>
        /// Kiểm tra danh mục
        /// </summary>
        public static string CheckCategoryID(string categoryID)
        {
            if (string.IsNullOrWhiteSpace(categoryID))
                return "Vui lòng chọn danh mục.";

            return null;
        }
    }
}
namespace Martify.Models
{
    /// <summary>
    /// Enum định nghĩa các loại cảnh báo tồn kho
    /// </summary>
    public enum InventoryAlertType
    {
        /// <summary>
        /// Không có cảnh báo (hiển thị tất cả sản phẩm)
        /// </summary>
        None = 0,

        /// <summary>
        /// Sắp hết hàng (Low Stock): Số lượng > 0 và <= 10
        /// </summary>
        LowStock = 1,

        /// <summary>
        /// Hết hàng (Out of Stock): Số lượng = 0
        /// </summary>
        OutOfStock = 2
    }
}
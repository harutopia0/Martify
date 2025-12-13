namespace Martify.Models
{
    /// <summary>
    /// Ð?nh ngh?a các lo?i c?nh báo t?n kho
    /// </summary>
    public enum InventoryAlertType
    {
        /// <summary>
        /// Không có b? l?c c?nh báo
        /// </summary>
        None = 0,

        /// <summary>
        /// S?n ph?m s?p h?t hàng (Low Stock)
        /// </summary>
        LowStock = 1,

        /// <summary>
        /// S?n ph?m h?t hàng (Out of Stock)
        /// </summary>
        OutOfStock = 2
    }
}

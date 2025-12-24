using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Martify.Models
{
    [Table("Product")]
    public class Product
    {
        [Key]
        [MaxLength(10)]
        public string ProductID { get; set; }

        [MaxLength(100)]
        public string ProductName { get; set; }

        [MaxLength(20)]
        public string Unit { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá không được âm")]
        public decimal Price { get; set; }
        public decimal Cost { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Tồn kho không được âm")]
        public int StockQuantity { get; set; }

        [MaxLength(255)]
        public string? ImagePath { get; set; }

        [MaxLength(10)]
        public string CategoryID { get; set; }

        [ForeignKey("CategoryID")]
        public virtual ProductCategory Category { get; set; }

        public virtual ICollection<ImportReceiptDetail> ImportReceiptDetails { get; set; } = new List<ImportReceiptDetail>();
        public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
    }
}

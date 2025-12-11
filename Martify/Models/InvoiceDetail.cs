using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Martify.Models
{
    [Table("InvoiceDetail")]
    public class InvoiceDetail
    {
        [MaxLength(10)]
        public string InvoiceID { get; set; }

        [MaxLength(10)]
        public string ProductID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng bán tối thiểu là 1")]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá bán không được âm")]
        public decimal SalePrice { get; set; }
        public decimal Total => SalePrice * Quantity;

        [ForeignKey("InvoiceID")]
        public virtual Invoice Invoice { get; set; }

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}

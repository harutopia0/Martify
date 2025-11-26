using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Martify.Models
{
    [Table("ImportReceiptDetail")]
    public class ImportReceiptDetail
    {
        [MaxLength(10)]
        public string ReceiptID { get; set; }

        [MaxLength(10)]
        public string ProductID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng nhập tối thiểu là 1")]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không được âm")]
        public decimal UnitPrice { get; set; }

        [ForeignKey("ReceiptID")]
        public virtual ImportReceipt ImportReceipt { get; set; }

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}

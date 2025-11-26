using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Martify.Models
{
    [Table("ImportReceipt")]
    public class ImportReceipt
    {
        [Key]
        [MaxLength(10)]
        public string ReceiptID { get; set; }

        public DateTime ImportDate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [MaxLength(10)]
        public string EmployeeID { get; set; }

        [MaxLength(10)]
        public string SupplierID { get; set; }

        [ForeignKey("EmployeeID")]
        public virtual Employee Employee { get; set; }

        [ForeignKey("SupplierID")]
        public virtual Supplier Supplier { get; set; }

        public virtual ICollection<ImportReceiptDetail> ImportReceiptDetails { get; set; } = new List<ImportReceiptDetail>();
    }
}

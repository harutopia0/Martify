using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Martify.Models
{
    [Table("Supplier")]
    public class Supplier
    {
        [Key]
        [MaxLength(10)]
        public string SupplierID { get; set; }

        [MaxLength(100)]
        public string SupplierName { get; set; }

        [MaxLength(100)]
        public string Address { get; set; }

        [MaxLength(15)]
        [RegularExpression(@"^[0-9+]+$", ErrorMessage = "Phone chỉ chứa số hoặc dấu +")]
        public string Phone { get; set; }

        public virtual ICollection<ImportReceipt> ImportReceipts { get; set; } = new List<ImportReceipt>();
    }
}

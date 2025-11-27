using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Martify.Models
{
    [Table("Employee")]
    public class Employee
    {
        [Key]
        [MaxLength(10)]
        public string EmployeeID { get; set; }

        [MaxLength(100)]
        public string FullName { get; set; }

        public DateTime BirthDate { get; set; }

        [MaxLength(15)]
        [RegularExpression(@"^[0-9+]+$", ErrorMessage = "Phone không hợp lệ")]
        public string Phone { get; set; }

        [MaxLength(255)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        public DateTime HireDate { get; set; }

        [MaxLength(255)]
        public string? ImagePath { get; set; }

        public virtual ICollection<ImportReceipt> ImportReceipts { get; set; } = new List<ImportReceipt>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}

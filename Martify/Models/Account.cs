using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Martify.Models
{
    [Table("Account")]
    public class Account
    {
        [Key]
        [MaxLength(20)]
        public string Username { get; set; }

        public string HashPassword { get; set; }

        [Range(0, 1, ErrorMessage = "Role phải là 0 hoặc 1")]
        public int Role { get; set; }

        [MaxLength(10)]
        public string EmployeeID { get; set; }

        [ForeignKey("EmployeeID")]
        public virtual Employee Employee { get; set; }
    }
}

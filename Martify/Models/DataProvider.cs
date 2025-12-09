using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;//

namespace Martify.Models
{
    public class DataProvider
    {
        
        private static DataProvider _ins;
        public static DataProvider Ins
        {
            get
            {
                if (_ins == null)
                    _ins = new DataProvider();
                return _ins;
            }
            set
            {
                _ins = value;
            }
        }

        public MartifyDbContext DB { get; set; }
        public Account CurrentAccount { get; set; }

        private DataProvider()
        {
            DB = new MartifyDbContext();
            //
            try
            {
                // If you use EF Migrations, apply them.
                DB.Database.Migrate();
            }
            catch
            {
                // If migrations are not configured, ensure database and tables exist (development fallback).
                DB.Database.EnsureCreated();
            }
            //
        }
    }
}

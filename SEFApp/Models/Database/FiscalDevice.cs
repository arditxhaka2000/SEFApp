using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEFApp.Models.Database
{
    [Table("FiscalDevices")]
    public class FiscalDevice
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public string DeviceType { get; set; } // Printer, Cash Register, etc.
        public string SerialNumber { get; set; }
        public string ConnectionString { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUsedDate { get; set; }
        public string Configuration { get; set; } // JSON configuration
    }
}

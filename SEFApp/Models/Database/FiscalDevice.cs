using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEFApp.Models.Database
{
    public class FiscalDevice
    {
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

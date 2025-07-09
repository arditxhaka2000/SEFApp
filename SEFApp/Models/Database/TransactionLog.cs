using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEFApp.Models.Database
{
    [Table("TransactionLogs")]
    public class TransactionLog
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public string Action { get; set; } // "CREATED", "UPDATED", "CANCELLED", "COMPLETED"
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public int UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Notes { get; set; }
        public string IpAddress { get; set; }
        public string DeviceInfo { get; set; }

        // Navigation properties
        public Transaction Transaction { get; set; }
        public User User { get; set; }
    }
}

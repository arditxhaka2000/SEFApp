using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEFApp.Models.Database
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string TableName { get; set; }
        public string Action { get; set; } // INSERT, UPDATE, DELETE
        public string RecordId { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        public int UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }

        // Navigation property (not stored in database)
        public User User { get; set; }
    }
}

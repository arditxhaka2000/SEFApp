using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEFApp.Models.Database
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string TableName { get; set; }
        public string Action { get; set; }
        public string RecordId { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        public int UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }
    }
}

using SQLite;

namespace SEFApp.Models.Database
{
    [SQLite.Table("TransactionLogs")]
    public class TransactionLog
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int TransactionId { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public int UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }
    }
}
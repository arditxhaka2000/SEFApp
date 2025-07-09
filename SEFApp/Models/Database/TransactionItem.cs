using SQLite;

namespace SEFApp.Models.Database
{
    [SQLite.Table("TransactionItems")]
    public class TransactionItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int TransactionId { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEFApp.Models.Database
{
    [Table("TransactionItems")]
    public class TransactionItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal LineTotal { get; set; }
        public string Unit { get; set; } = "pcs";

        // Navigation property (not stored in database)
        public Transaction Transaction { get; set; }
    }
}

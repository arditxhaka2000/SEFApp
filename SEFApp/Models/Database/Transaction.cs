using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEFApp.Models.Database
{
    public class Transaction
    {
        public int Id { get; set; }
        public string TransactionNumber { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string CustomerName { get; set; }
        public string CustomerTaxId { get; set; }
        public string Status { get; set; } // Pending, Completed, Cancelled
        public string TransactionType { get; set; } // Sale, Refund, etc.
        public string FiscalReceiptNumber { get; set; }
        public DateTime? FiscalReceiptDate { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string Notes { get; set; }

        // Navigation properties (not stored in database)
        public User CreatedByUser { get; set; }
        public List<TransactionItem> Items { get; set; } = new List<TransactionItem>();
    }
}

using SQLite;

namespace SEFApp.Models.Database
{
    [Table("Transactions")]
    public class Transaction
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public string TransactionNumber { get; set; } = string.Empty;

        [NotNull]
        public DateTime TransactionDate { get; set; }

        [NotNull]
        public string CustomerName { get; set; } = "Person Fizik";

        public string CustomerPhone { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        [NotNull]
        public string PaymentMethod { get; set; } = "Cash";

        [NotNull]
        public decimal SubTotal { get; set; }

        [NotNull]
        public decimal TaxAmount { get; set; }

        [NotNull]
        public decimal TotalAmount { get; set; }

        [NotNull]
        public string Status { get; set; } = "Pending"; // Pending, Completed, Cancelled, Refunded, Draft

        public string Notes { get; set; } = string.Empty;

        [NotNull]
        public int UserId { get; set; }

        [NotNull]
        public DateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        // Navigation properties (not stored in database)
        [Ignore]
        public List<TransactionItem> Items { get; set; } = new List<TransactionItem>();
    }
}
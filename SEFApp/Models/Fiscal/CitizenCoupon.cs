using System.ComponentModel.DataAnnotations;

namespace SEFApp.Models.Fiscal
{
    public enum CouponType
    {
        Sale = 1,
        Return = 2,
        Cancel = 3
    }

    public enum PaymentType
    {
        Cash = 1,
        CreditCard = 2,
        Voucher = 3,
        Cheque = 4,
        CryptoCurrency = 5,
        Other = 6
    }

    public class CitizenCoupon
    {
        public long BusinessId { get; set; }
        public long PosId { get; set; }
        public long CouponId { get; set; }
        public long BranchId { get; set; }
        public CouponType Type { get; set; }
        public long Time { get; set; }
        public long Total { get; set; }
        public List<TaxGroup> TaxGroups { get; set; } = new();
        public long TotalTax { get; set; }
        public long TotalNoTax { get; set; }
        public long TotalDiscount { get; set; }
        public long ReferenceNo { get; set; }
    }

    public class PosCoupon
    {
        public long BusinessId { get; set; }
        public long CouponId { get; set; }
        public long BranchId { get; set; }
        public string Location { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public long PosId { get; set; }
        public long ApplicationId { get; set; }
        public long ReferenceNo { get; set; }
        public string VerificationNo { get; set; } = string.Empty;
        public CouponType Type { get; set; }
        public long Time { get; set; }
        public List<CouponItem> Items { get; set; } = new();
        public List<Payment> Payments { get; set; } = new();
        public long Total { get; set; }
        public List<TaxGroup> TaxGroups { get; set; } = new();
        public long TotalTax { get; set; }
        public long TotalNoTax { get; set; }
        public long TotalDiscount { get; set; }
    }

    public class TaxGroup
    {
        public string TaxRate { get; set; } = string.Empty; // "C", "D", "E"
        public long TotalForTax { get; set; }
        public long TotalTax { get; set; }
    }

    public class CouponItem
    {
        public string Name { get; set; } = string.Empty;
        public long Price { get; set; } // in cents
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public long Total { get; set; } // in cents
        public string TaxRate { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class Payment
    {
        public PaymentType Type { get; set; }
        public long Amount { get; set; } // in cents
    }

    // Response models
    public class FiscalResponse
    {
        public string Message { get; set; } = string.Empty;
        public long? TransactionId { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
    }

    public class QrCodeRequest
    {
        public long CitizenId { get; set; }
        public string QrCode { get; set; } = string.Empty;
    }

    public class PosCouponRequest
    {
        public string Details { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }
}
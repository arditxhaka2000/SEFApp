using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using SEFApp.Models.Fiscal;

namespace SEFApp.Services.Interfaces
{
    public interface IFiscalCertificateService
    {
        Task<bool> IsOnboardedAsync();
        Task<bool> OnboardBusinessAsync(OnboardingRequest request);
        Task<string> SignDataAsync(byte[] data);
        Task<string> GenerateSimpleQrCodeAsync(decimal totalAmount, string transactionNumber, DateTime transactionDate);
        Task<(string details, string signature)> SignPosCouponAsync(PosCoupon coupon);
    }

    public class OnboardingRequest
    {
        public long BusinessId { get; set; }
        public long PosId { get; set; }
        public long BranchId { get; set; }
        public long ApplicationId { get; set; }
        public string FiscalizationNo { get; set; } = string.Empty;
    }
}

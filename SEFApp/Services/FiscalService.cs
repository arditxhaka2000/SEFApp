using SEFApp.Models.Database;
using SEFApp.Models.Fiscal;
using SEFApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SEFApp.Services
{
    public class FiscalService : IFiscalService
    {
        private readonly HttpClient _httpClient;
        private readonly IFiscalCertificateService _certificateService;
        private readonly IDatabaseService _databaseService;
        private readonly IPreferencesService _preferences;
        private readonly string _testBaseUrl = "https://fiskalizimi-test.atk-ks.org";
        private readonly string _prodBaseUrl = "https://fiskalizimi.atk-ks.org";

        public FiscalService(
            HttpClient httpClient,
            IFiscalCertificateService certificateService,
            IDatabaseService databaseService,
            IPreferencesService preferences)
        {
            _httpClient = httpClient;
            _certificateService = certificateService;
            _databaseService = databaseService;
            _preferences = preferences;
        }

        public async Task<bool> IsConfiguredAsync()
        {
            return await _certificateService.IsOnboardedAsync();
        }

        public async Task<FiscalResponse> SubmitTransactionAsync(Transaction transaction)
        {
            try
            {
                if (!await IsConfiguredAsync())
                {
                    return new FiscalResponse
                    {
                        Success = false,
                        Error = "Fiscal system not configured. Please complete onboarding first."
                    };
                }

                // Load transaction items
                var items = await _databaseService.GetTransactionItemsAsync(transaction.Id);
                transaction.Items = items;

                // Create POS Coupon
                var posCoupon = await CreatePosCouponFromTransaction(transaction);

                // Sign the coupon
                var (details, signature) = await _certificateService.SignPosCouponAsync(posCoupon);

                // Submit to ATK
                var response = await SubmitPosCouponAsync(new PosCouponRequest
                {
                    Details = details,
                    Signature = signature
                });

                if (response.Success)
                {
                    // Update transaction with fiscal information
                    await UpdateTransactionWithFiscalData(transaction, response);

                    // Generate QR code for receipt
                    var citizenCoupon = CreateCitizenCouponFromTransaction(transaction);

                    var qrCode = await _certificateService.GenerateSimpleQrCodeAsync(
                    transaction.TotalAmount,
                    transaction.TransactionNumber,
                    transaction.TransactionDate);
                

                    // Store QR code in transaction notes or separate field
                    transaction.Notes = $"{transaction.Notes}\nQR: {qrCode}";
                    await _databaseService.UpdateTransactionAsync(transaction);
                }

                return response;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fiscal submission failed: {ex.Message}");
                return new FiscalResponse
                {
                    Success = false,
                    Error = $"Submission failed: {ex.Message}"
                };
            }
        }

        public async Task<string> GenerateReceiptQrCodeAsync(Transaction transaction)
        {
            try
            {
                return await _certificateService.GenerateSimpleQrCodeAsync(
                    transaction.TotalAmount,
                    transaction.TransactionNumber,
                    transaction.TransactionDate);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"QR code generation failed: {ex.Message}", ex);
            }
        }

        public async Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            try
            {
                // Basic validation rules
                if (transaction.TotalAmount <= 0)
                    return false;
                if (string.IsNullOrEmpty(transaction.CustomerName))
                    return false;
                if (transaction.Items == null || !transaction.Items.Any())
                    return false;

                return true; 
            }
            catch
            {
                return false;
            }
        }

        public async Task<FiscalResponse> CancelTransactionAsync(Transaction originalTransaction, string reason)
        {
            try
            {
                // Create cancellation transaction
                var cancelTransaction = new Transaction
                {
                    TransactionNumber = GenerateTransactionNumber(),
                    TransactionDate = DateTime.Now,
                    CustomerName = originalTransaction.CustomerName,
                    PaymentMethod = originalTransaction.PaymentMethod,
                    SubTotal = -originalTransaction.SubTotal,
                    TaxAmount = -originalTransaction.TaxAmount,
                    TotalAmount = -originalTransaction.TotalAmount,
                    Status = "Cancelled",
                    Notes = $"Cancellation of {originalTransaction.TransactionNumber}. Reason: {reason}",
                    UserId = originalTransaction.UserId,
                    CreatedDate = DateTime.Now
                };

                // Create POS Coupon with Cancel type
                var posCoupon = await CreatePosCouponFromTransaction(cancelTransaction);
                posCoupon.Type = CouponType.Cancel;
                posCoupon.ReferenceNo = GetCouponIdFromTransaction(originalTransaction);

                // Sign and submit
                var (details, signature) = await _certificateService.SignPosCouponAsync(posCoupon);
                var response = await SubmitPosCouponAsync(new PosCouponRequest
                {
                    Details = details,
                    Signature = signature
                });

                if (response.Success)
                {
                    // Save cancellation transaction
                    await _databaseService.CreateTransactionAsync(cancelTransaction);

                    // Update original transaction status
                    originalTransaction.Status = "Cancelled";
                    originalTransaction.ModifiedDate = DateTime.Now;
                    await _databaseService.UpdateTransactionAsync(originalTransaction);
                }

                return response;
            }
            catch (Exception ex)
            {
                return new FiscalResponse
                {
                    Success = false,
                    Error = $"Cancellation failed: {ex.Message}"
                };
            }
        }

        public async Task<FiscalResponse> ReturnTransactionAsync(Transaction originalTransaction, decimal returnAmount, string reason)
        {
            try
            {
                // Create return transaction
                var returnTransaction = new Transaction
                {
                    TransactionNumber = GenerateTransactionNumber(),
                    TransactionDate = DateTime.Now,
                    CustomerName = originalTransaction.CustomerName,
                    PaymentMethod = originalTransaction.PaymentMethod,
                    SubTotal = -returnAmount,
                    TaxAmount = -CalculateTaxForAmount(returnAmount),
                    TotalAmount = -returnAmount,
                    Status = "Refunded",
                    Notes = $"Return from {originalTransaction.TransactionNumber}. Reason: {reason}",
                    UserId = originalTransaction.UserId,
                    CreatedDate = DateTime.Now
                };

                // Create POS Coupon with Return type
                var posCoupon = await CreatePosCouponFromTransaction(returnTransaction);
                posCoupon.Type = CouponType.Return;
                posCoupon.ReferenceNo = GetCouponIdFromTransaction(originalTransaction);

                // Sign and submit
                var (details, signature) = await _certificateService.SignPosCouponAsync(posCoupon);
                var response = await SubmitPosCouponAsync(new PosCouponRequest
                {
                    Details = details,
                    Signature = signature
                });

                if (response.Success)
                {
                    // Save return transaction
                    await _databaseService.CreateTransactionAsync(returnTransaction);
                }

                return response;
            }
            catch (Exception ex)
            {
                return new FiscalResponse
                {
                    Success = false,
                    Error = $"Return failed: {ex.Message}"
                };
            }
        }

        private async Task<PosCoupon> CreatePosCouponFromTransaction(Transaction transaction)
        {
            var businessId = long.Parse(await _preferences.GetAsync("fiscal_business_id", "0"));
            var posId = long.Parse(await _preferences.GetAsync("fiscal_pos_id", "1"));
            var branchId = long.Parse(await _preferences.GetAsync("fiscal_branch_id", "1"));
            var applicationId = long.Parse(await _preferences.GetAsync("fiscal_application_id", "1234"));

            return new PosCoupon
            {
                BusinessId = businessId,
                CouponId = GetCouponIdFromTransaction(transaction),
                BranchId = branchId,
                Location = "Main Branch", // You might want to make this configurable
                OperatorId = "Cashier", // You might want to get actual user name
                PosId = posId,
                ApplicationId = applicationId,
                ReferenceNo = 0,
                VerificationNo = GenerateVerificationNumber(),
                Type = CouponType.Sale,
                Time = ((DateTimeOffset)transaction.TransactionDate).ToUnixTimeSeconds(),
                Items = ConvertTransactionItemsToCouponItems(transaction.Items),
                Payments = ConvertPaymentMethodToPayments(transaction),
                Total = ConvertToFiscalAmount(transaction.TotalAmount),
                TaxGroups = CalculateTaxGroups(transaction.Items),
                TotalTax = ConvertToFiscalAmount(transaction.TaxAmount),
                TotalNoTax = ConvertToFiscalAmount(transaction.SubTotal),
                TotalDiscount = 0 // You might want to add discount handling
            };
        }

        private CitizenCoupon CreateCitizenCouponFromTransaction(Transaction transaction)
        {
            var posCoupon = CreatePosCouponFromTransaction(transaction).Result;

            return new CitizenCoupon
            {
                BusinessId = posCoupon.BusinessId,
                PosId = posCoupon.PosId,
                CouponId = posCoupon.CouponId,
                BranchId = posCoupon.BranchId,
                Type = posCoupon.Type,
                Time = posCoupon.Time,
                Total = posCoupon.Total,
                TaxGroups = posCoupon.TaxGroups,
                TotalTax = posCoupon.TotalTax,
                TotalNoTax = posCoupon.TotalNoTax,
                TotalDiscount = posCoupon.TotalDiscount,
                ReferenceNo = posCoupon.ReferenceNo
            };
        }

        private List<CouponItem> ConvertTransactionItemsToCouponItems(List<TransactionItem> items)
        {
            return items.Select(item => new CouponItem
            {
                Name = item.ProductName,
                Price = ConvertToFiscalAmount(item.UnitPrice),
                Unit = "pcs", // You might want to get this from product
                Quantity = item.Quantity,
                Total = ConvertToFiscalAmount(item.TotalAmount),
                TaxRate = GetTaxRateCode(item.TaxRate),
                Type = "TT" // Standard type
            }).ToList();
        }

        private List<Payment> ConvertPaymentMethodToPayments(Transaction transaction)
        {
            var paymentType = transaction.PaymentMethod.ToLower() switch
            {
                "cash" => PaymentType.Cash,
                "card" => PaymentType.CreditCard,
                "credit card" => PaymentType.CreditCard,
                "voucher" => PaymentType.Voucher,
                "cheque" => PaymentType.Cheque,
                _ => PaymentType.Cash
            };

            return new List<Payment>
            {
                new Payment
                {
                    Type = paymentType,
                    Amount = ConvertToFiscalAmount(transaction.TotalAmount)
                }
            };
        }

        private List<TaxGroup> CalculateTaxGroups(List<TransactionItem> items)
        {
            return items
                .GroupBy(item => GetTaxRateCode(item.TaxRate))
                .Select(group => new TaxGroup
                {
                    TaxRate = group.Key,
                    TotalForTax = ConvertToFiscalAmount(group.Sum(i => i.TotalAmount - i.TaxAmount)),
                    TotalTax = ConvertToFiscalAmount(group.Sum(i => i.TaxAmount))
                })
                .ToList();
        }

        private string GetTaxRateCode(decimal taxRate)
        {
            return taxRate switch
            {
                0m => "C",    // 0% VAT
                8m => "D",    // 8% VAT
                18m => "E",   // 18% VAT
                _ => "E"      // Default to 18%
            };
        }

        private long ConvertToFiscalAmount(decimal amount)
        {
            // Convert to cents (fiscal amounts are in cents)
            return (long)(amount * 100);
        }

        private long GetCouponIdFromTransaction(Transaction transaction)
        {
            // Extract numeric part from transaction number or use ID
            if (long.TryParse(transaction.TransactionNumber.Replace("TXN", ""), out long couponId))
                return couponId;

            return transaction.Id;
        }

        private string GenerateTransactionNumber()
        {
            return $"TXN{DateTime.Now:yyyyMMdd}{Random.Shared.Next(1000, 9999)}";
        }

        private string GenerateVerificationNumber()
        {
            return Random.Shared.Next(1000000000, int.MaxValue).ToString();
        }

        private async Task<FiscalResponse> SubmitPosCouponAsync(PosCouponRequest request)
        {
            try
            {
                var url = $"{GetBaseUrl()}/pos/coupon";
                var json = JsonSerializer.Serialize(request);

                // DEBUG: Print what you're sending
                System.Diagnostics.Debug.WriteLine($"Sending to ATK: {json}");
                System.Diagnostics.Debug.WriteLine($"URL: {url}");

                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                // DEBUG: Print ATK response
                System.Diagnostics.Debug.WriteLine($"ATK Response: {responseContent}");
                System.Diagnostics.Debug.WriteLine($"Status Code: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var successResponse = JsonSerializer.Deserialize<SuccessResponse>(responseContent);
                    return new FiscalResponse
                    {
                        Success = true,
                        Message = successResponse.Message,
                        TransactionId = successResponse.TransactionId
                    };
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent);
                    return new FiscalResponse
                    {
                        Success = false,
                        Error = errorResponse.Error
                    };
                }
            }
            catch (Exception ex)
            {
                return new FiscalResponse
                {
                    Success = false,
                    Error = $"HTTP request failed: {ex.Message}"
                };
            }
        }

        private async Task UpdateTransactionWithFiscalData(Transaction transaction, FiscalResponse response)
        {
            // Store fiscal information in transaction notes or create separate fiscal fields
            var fiscalInfo = $"ATK Transaction ID: {response.TransactionId}";
            transaction.Notes = string.IsNullOrEmpty(transaction.Notes) ? fiscalInfo : $"{transaction.Notes}\n{fiscalInfo}";
            transaction.Status = "Fiscalized";
            transaction.ModifiedDate = DateTime.Now;

            await _databaseService.UpdateTransactionAsync(transaction);
        }

        private decimal CalculateTaxAmount(List<TransactionItem> items)
        {
            return items.Sum(item => item.TaxAmount);
        }

        private decimal CalculateTaxForAmount(decimal amount)
        {
            // Default to 18% tax - you might want to make this configurable
            return amount * 0.18m;
        }

        private string GetBaseUrl()
        {
            // You can make this configurable via settings
            return _testBaseUrl; // Use _prodBaseUrl for production
        }

        // Response models for ATK API
        private class SuccessResponse
        {
            public string message { get; set; } = string.Empty;
            public long transaction_id { get; set; }

            public string Message => message;
            public long TransactionId => transaction_id;
        }

        private class ErrorResponse
        {
            public string error { get; set; } = string.Empty;
            public string Error => error;
        }
    }
}

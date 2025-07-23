using SEFApp.Models.Database;
using SEFApp.Models.Fiscal;
using SEFApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEFApp.Services
{
    public class TransactionFiscalService : ITransactionFiscalService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IFiscalService _fiscalService;
        private readonly IAlertService _alertService;

        public TransactionFiscalService(
            IDatabaseService databaseService,
            IFiscalService fiscalService,
            IAlertService alertService)
        {
            _databaseService = databaseService;
            _fiscalService = fiscalService;
            _alertService = alertService;
        }

        public async Task<bool> ProcessTransactionForFiscalizationAsync(Transaction transaction)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Processing transaction {transaction.TransactionNumber} for fiscalization");

                // Validate transaction before sending
                if (!await _fiscalService.ValidateTransactionAsync(transaction))
                {
                    await _alertService.ShowErrorAsync("Transaction validation failed. Please check transaction data.");
                    return false;
                }

                // Check if fiscal service is configured
                if (!await _fiscalService.IsConfiguredAsync())
                {
                    await _alertService.ShowErrorAsync("Fiscal system not configured. Please complete onboarding first.");
                    return false;
                }

                // Submit to ATK
                var result = await _fiscalService.SubmitTransactionAsync(transaction);

                if (result.Success)
                {
                    await _alertService.ShowSuccessAsync($"Transaction {transaction.TransactionNumber} fiscalized successfully!");

                    // Update transaction status
                    transaction.Status = "Fiscalized";
                    transaction.ModifiedDate = DateTime.Now;
                    await _databaseService.UpdateTransactionAsync(transaction);

                    System.Diagnostics.Debug.WriteLine($"Transaction {transaction.TransactionNumber} fiscalized with ID: {result.TransactionId}");
                    return true;
                }
                else
                {
                    await _alertService.ShowErrorAsync($"Fiscalization failed: {result.Error}");

                    // Mark as failed
                    transaction.Status = "Fiscal Failed";
                    transaction.Notes = $"{transaction.Notes}\nFiscal Error: {result.Error}";
                    transaction.ModifiedDate = DateTime.Now;
                    await _databaseService.UpdateTransactionAsync(transaction);

                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fiscalization error: {ex.Message}");
                await _alertService.ShowErrorAsync($"Fiscalization error: {ex.Message}");

                // Mark as failed
                transaction.Status = "Fiscal Failed";
                transaction.Notes = $"{transaction.Notes}\nFiscal Error: {ex.Message}";
                transaction.ModifiedDate = DateTime.Now;
                await _databaseService.UpdateTransactionAsync(transaction);

                return false;
            }
        }

        public async Task<string> GenerateReceiptAsync(Transaction transaction, bool includeQrCode = true)
        {
            try
            {
                var receipt = new System.Text.StringBuilder();

                // Get company info (you might want to add this to your database)
                var company = await _databaseService.GetCompanyAsync() ?? new Company { Name = "My Company" };

                // Header
                receipt.AppendLine("═══════════════════════════════");
                receipt.AppendLine("         FISCAL RECEIPT");
                receipt.AppendLine("═══════════════════════════════");
                receipt.AppendLine($"Company: {company.Name}");
                receipt.AppendLine($"Tax ID: {company.TaxId}");
                receipt.AppendLine($"Address: {company.Address}");
                receipt.AppendLine($"Phone: {company.Phone}");
                receipt.AppendLine();

                // Transaction Info
                receipt.AppendLine($"Transaction: {transaction.TransactionNumber}");
                receipt.AppendLine($"Date: {transaction.TransactionDate:yyyy-MM-dd HH:mm:ss}");
                receipt.AppendLine($"Customer: {transaction.CustomerName}");
                receipt.AppendLine($"Payment: {transaction.PaymentMethod}");
                receipt.AppendLine();

                // Items
                receipt.AppendLine("Items:");
                receipt.AppendLine("───────────────────────────────");

                var items = await _databaseService.GetTransactionItemsAsync(transaction.Id);
                foreach (var item in items)
                {
                    receipt.AppendLine($"{item.ProductName}");
                    receipt.AppendLine($"  {item.Quantity} x €{item.UnitPrice:F2} = €{item.TotalAmount:F2}");
                }

                receipt.AppendLine("───────────────────────────────");
                receipt.AppendLine($"Subtotal:     €{transaction.SubTotal:F2}");
                receipt.AppendLine($"Tax:          €{transaction.TaxAmount:F2}");
                receipt.AppendLine($"TOTAL:        €{transaction.TotalAmount:F2}");
                receipt.AppendLine();

                // QR Code
                if (includeQrCode && transaction.Status == "Fiscalized")
                {
                    try
                    {
                        var qrCode = await _fiscalService.GenerateReceiptQrCodeAsync(transaction);
                        receipt.AppendLine("QR Code:");
                        receipt.AppendLine(qrCode);
                        receipt.AppendLine();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"QR code generation failed: {ex.Message}");
                        receipt.AppendLine("QR Code: Generation failed");
                        receipt.AppendLine();
                    }
                }

                // Footer
                receipt.AppendLine("Thank you for your business!");
                receipt.AppendLine("═══════════════════════════════");

                return receipt.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Receipt generation failed: {ex.Message}");
                return $"Receipt generation failed: {ex.Message}";
            }
        }

        public async Task<FiscalResponse> RetryFailedFiscalizationAsync(int transactionId)
        {
            try
            {
                var transaction = await _databaseService.GetTransactionByIdAsync(transactionId);
                if (transaction == null)
                {
                    return new FiscalResponse
                    {
                        Success = false,
                        Error = "Transaction not found"
                    };
                }

                if (transaction.Status == "Fiscalized")
                {
                    return new FiscalResponse
                    {
                        Success = true,
                        Message = "Transaction already fiscalized"
                    };
                }

                return await _fiscalService.SubmitTransactionAsync(transaction);
            }
            catch (Exception ex)
            {
                return new FiscalResponse
                {
                    Success = false,
                    Error = $"Retry failed: {ex.Message}"
                };
            }
        }

        public async Task<List<Transaction>> GetUnfiscalizedTransactionsAsync()
        {
            try
            {
                // Get all completed transactions that are not fiscalized
                var today = DateTime.Today;
                var endDate = today.AddDays(1);
                var startDate = today.AddDays(-7); // Last 7 days

                var transactions = await _databaseService.GetTransactionsByDateRangeAsync(startDate, endDate);

                return transactions
                    .Where(t => t.Status == "Completed" || t.Status == "Fiscal Failed")
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get unfiscalized transactions: {ex.Message}");
                return new List<Transaction>();
            }
        }
    }

    // Extension methods for easier transaction processing
    public static class TransactionExtensions
    {
        public static bool IsFiscalized(this Transaction transaction)
        {
            return transaction.Status == "Fiscalized";
        }

        public static bool NeedsFiscalization(this Transaction transaction)
        {
            return transaction.Status == "Completed" || transaction.Status == "Fiscal Failed";
        }

        public static bool CanBeFiscalized(this Transaction transaction)
        {
            return transaction.TotalAmount > 0 &&
                   !string.IsNullOrEmpty(transaction.CustomerName) &&
                   transaction.Status != "Cancelled";
        }
    }
}

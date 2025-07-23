using SEFApp.Models.Database;
using SEFApp.Models.Fiscal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEFApp.Services.Interfaces
{
    public interface ITransactionFiscalService
    {
        Task<bool> ProcessTransactionForFiscalizationAsync(Transaction transaction);
        Task<string> GenerateReceiptAsync(Transaction transaction, bool includeQrCode = true);
        Task<FiscalResponse> RetryFailedFiscalizationAsync(int transactionId);
        Task<List<Transaction>> GetUnfiscalizedTransactionsAsync();
    }
}

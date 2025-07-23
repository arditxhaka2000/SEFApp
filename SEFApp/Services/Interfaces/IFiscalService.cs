using SEFApp.Models.Database;
using SEFApp.Models.Fiscal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEFApp.Services.Interfaces
{
    public interface IFiscalService
    {
        Task<bool> IsConfiguredAsync();
        Task<FiscalResponse> SubmitTransactionAsync(Transaction transaction);
        Task<string> GenerateReceiptQrCodeAsync(Transaction transaction);
        Task<bool> ValidateTransactionAsync(Transaction transaction);
        Task<FiscalResponse> CancelTransactionAsync(Transaction originalTransaction, string reason);
        Task<FiscalResponse> ReturnTransactionAsync(Transaction originalTransaction, decimal returnAmount, string reason);
    }
}

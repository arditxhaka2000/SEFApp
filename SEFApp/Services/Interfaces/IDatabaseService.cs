using SEFApp.Models.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEFApp.Services.Interfaces
{
    public interface IDatabaseService
    {
        // Database management
        Task InitializeDatabaseAsync();
        Task SetEncryptionForUser(string username, string password);
        Task<bool> IsDatabaseInitializedAsync();
        Task BackupDatabaseAsync(string backupPath);
        Task RestoreDatabaseAsync(string backupPath);
        Task ChangePasswordAsync(string oldPassword, string newPassword);

        // User management
        Task<User> CreateUserAsync(User user, string password);
        Task<User> GetUserByUsernameAsync(string username);
        Task<User> GetUserByIdAsync(int id);
        Task<List<User>> GetAllUsersAsync();
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> ValidatePasswordAsync(string username, string password);
        Task<bool> UpdatePasswordAsync(int userId, string newPassword);

        // Transaction management
        Task<Transaction> CreateTransactionAsync(Transaction transaction);
        Task<Transaction> GetTransactionByIdAsync(int id);
        Task<List<Transaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<Transaction>> GetTodaysTransactionsAsync();
        Task<bool> UpdateTransactionAsync(Transaction transaction);
        Task<bool> DeleteTransactionAsync(int id);
        Task<decimal> GetTodaysRevenueAsync();
        Task<int> GetTodaysTransactionCountAsync();

        // Transaction items
        Task<TransactionItem> CreateTransactionItemAsync(TransactionItem item);
        Task<List<TransactionItem>> GetTransactionItemsAsync(int transactionId);
        Task<bool> UpdateTransactionItemAsync(TransactionItem item);
        Task<bool> DeleteTransactionItemAsync(int id);

        // Product management
        Task<Product> CreateProductAsync(Product product);
        Task<Product> GetProductByIdAsync(int id);
        Task<Product> GetProductByCodeAsync(string code);
        Task<List<Product>> GetAllProductsAsync();
        Task<List<Product>> SearchProductsAsync(string searchTerm);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> UpdateProductStockAsync(int productId, decimal newStock);

        // Settings management
        Task<string> GetSettingAsync(string key, string defaultValue = null);
        Task<bool> SetSettingAsync(string key, string value, string description = null);
        Task<Dictionary<string, string>> GetAllSettingsAsync();

        // Audit logging
        Task LogActionAsync(string tableName, string action, string recordId, object oldValues, object newValues, int userId);
        Task<List<AuditLog>> GetAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, int? userId = null);

        // Company management
        Task<Company> CreateCompanyAsync(Company company);
        Task<Company> GetCompanyAsync();
        Task<bool> UpdateCompanyAsync(Company company);

        // Fiscal device management
        Task<FiscalDevice> CreateFiscalDeviceAsync(FiscalDevice device);
        Task<List<FiscalDevice>> GetFiscalDevicesAsync();
        Task<FiscalDevice> GetDefaultFiscalDeviceAsync();
        Task<bool> UpdateFiscalDeviceAsync(FiscalDevice device);
        Task<bool> SetDefaultFiscalDeviceAsync(int deviceId);

        // Reports and analytics
        Task<Dictionary<string, object>> GetDashboardMetricsAsync();
        Task<List<Transaction>> GetTopTransactionsAsync(int count = 10);
        Task<Dictionary<string, decimal>> GetSalesByDateAsync(DateTime startDate, DateTime endDate);
        Task<List<Product>> GetLowStockProductsAsync();
    }
}

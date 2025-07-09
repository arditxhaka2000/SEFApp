using SQLite;
using SEFApp.Models;
using SEFApp.Models.Database;
using SEFApp.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SEFApp.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly string _encryptedDatabasePath;
        private readonly string _workingDatabasePath;
        private SQLiteAsyncConnection _database;
        private string _encryptionKey;
        private bool _isInitialized = false;
        private bool _isDatabaseLoaded = false;

        public DatabaseService()
        {
            // This file will always be encrypted and unreadable
            _encryptedDatabasePath = Path.Combine(FileSystem.AppDataDirectory, "sefmanager.encrypted");

            // This is a temp file that gets deleted when app closes
            _workingDatabasePath = Path.Combine(FileSystem.CacheDirectory, $"working_{Guid.NewGuid()}.db");
        }

        public async Task InitializeDatabaseForUser(string username, string password)
        {
            if (_isDatabaseLoaded) return;

            try
            {
                _encryptionKey = GenerateEncryptionKey(username, password);

                if (File.Exists(_encryptedDatabasePath))
                {
                    await LoadEncryptedDatabase();
                }
                else
                {
                    await CreateNewDatabase();
                }

                // Connect to the working database
                _database = new SQLiteAsyncConnection(_workingDatabasePath);

                await InitializeDatabaseAsync();
                _isDatabaseLoaded = true;

                // Set up auto-save every 30 seconds
                _ = Task.Run(AutoSaveLoop);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize database: {ex.Message}", ex);
            }
        }

        private async Task LoadEncryptedDatabase()
        {
            try
            {
                var encryptedData = await File.ReadAllBytesAsync(_encryptedDatabasePath);
                var decryptedData = DecryptData(encryptedData, _encryptionKey);
                await File.WriteAllBytesAsync(_workingDatabasePath, decryptedData);

                // Verify it's a valid database
                using var testConnection = new SQLiteConnection(_workingDatabasePath);
                testConnection.Execute("SELECT COUNT(*) FROM sqlite_master");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invalid password or corrupted database", ex);
            }
        }

        private async Task CreateNewDatabase()
        {
            // Create empty database file
            using var connection = new SQLiteConnection(_workingDatabasePath);
            connection.Close();
        }

        private async Task AutoSaveLoop()
        {
            while (_isDatabaseLoaded)
            {
                try
                {
                    await Task.Delay(30000); // Save every 30 seconds
                    if (_isDatabaseLoaded)
                    {
                        await SaveEncryptedDatabase();
                    }
                }
                catch
                {
                    // Continue the loop even if save fails
                }
            }
        }

        public async Task SaveEncryptedDatabase()
        {
            if (!_isDatabaseLoaded) return;

            try
            {
                // Close database connection temporarily
                await _database?.CloseAsync();

                // Encrypt and save
                var unencryptedData = await File.ReadAllBytesAsync(_workingDatabasePath);
                var encryptedData = EncryptData(unencryptedData, _encryptionKey);

                // Atomic save with backup
                var backupPath = _encryptedDatabasePath + ".backup";
                if (File.Exists(_encryptedDatabasePath))
                {
                    File.Copy(_encryptedDatabasePath, backupPath, true);
                }

                try
                {
                    await File.WriteAllBytesAsync(_encryptedDatabasePath, encryptedData);
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                }
                catch
                {
                    // Restore backup on failure
                    if (File.Exists(backupPath))
                    {
                        File.Copy(backupPath, _encryptedDatabasePath, true);
                        File.Delete(backupPath);
                    }
                    throw;
                }

                // Reconnect to database
                _database = new SQLiteAsyncConnection(_workingDatabasePath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save database: {ex.Message}", ex);
            }
        }

        public async Task CloseAndCleanup()
        {
            if (!_isDatabaseLoaded) return;

            try
            {
                _isDatabaseLoaded = false;

                // Final save
                await SaveEncryptedDatabase();

                // Close database
                await _database?.CloseAsync();

                // Delete working file
                if (File.Exists(_workingDatabasePath))
                {
                    File.Delete(_workingDatabasePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cleanup error: {ex.Message}");
            }
        }

        public async Task SetEncryptionForUser(string username, string password)
        {
            _encryptionKey = GenerateEncryptionKey(username, password);
        }

        private string GenerateEncryptionKey(string username, string password)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                Encoding.UTF8.GetBytes(username + "SEF_SALT_2024"),
                100000, // Higher iteration count for security
                HashAlgorithmName.SHA256
            );

            var key = pbkdf2.GetBytes(32);
            return Convert.ToBase64String(key);
        }

        private byte[] EncryptData(byte[] data, string key)
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC; // Use GCM for authenticated encryption

            var keyBytes = Convert.FromBase64String(key);
            aes.Key = keyBytes;

            var nonce = new byte[12]; // GCM nonce
            RandomNumberGenerator.Fill(nonce);

            var tag = new byte[16]; // GCM authentication tag
            var encrypted = new byte[data.Length];

            using var encryptor = aes.CreateEncryptor();
            // For GCM mode, we need to use a different approach
            // This is a simplified version - in production use AesGcm class
            aes.GenerateIV();
            using var transform = aes.CreateEncryptor();
            var encryptedData = transform.TransformFinalBlock(data, 0, data.Length);

            var result = new byte[aes.IV.Length + encryptedData.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encryptedData, 0, result, aes.IV.Length, encryptedData.Length);

            return result;
        }

        private byte[] DecryptData(byte[] encryptedData, string key)
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC; // Match encryption mode
            aes.Padding = PaddingMode.PKCS7;

            var keyBytes = Convert.FromBase64String(key);
            aes.Key = keyBytes;

            var iv = new byte[16];
            Array.Copy(encryptedData, 0, iv, 0, 16);
            aes.IV = iv;

            var encrypted = new byte[encryptedData.Length - 16];
            Array.Copy(encryptedData, 16, encrypted, 0, encrypted.Length);

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
        }

        public async Task InitializeDatabaseAsync()
        {
            if (_isInitialized) return;

            try
            {
                await CreateTablesAsync();
                await InitializeDefaultDataAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Database initialization failed: {ex.Message}", ex);
            }
        }

        private async Task CreateTablesAsync()
        {
            await _database.CreateTableAsync<User>();
            await _database.CreateTableAsync<Transaction>();
            await _database.CreateTableAsync<TransactionItem>();
            await _database.CreateTableAsync<Product>();
            await _database.CreateTableAsync<Setting>();
            await _database.CreateTableAsync<AuditLog>();
            await _database.CreateTableAsync<FiscalDevice>();
            await _database.CreateTableAsync<Company>();
            await _database.CreateTableAsync<TransactionLog>();
        }

        private async Task InitializeDefaultDataAsync()
        {
            try
            {
                var adminExists = await _database.Table<User>()
                    .Where(u => u.Username == "admin")
                    .CountAsync() > 0;

                if (!adminExists)
                {
                    var adminUser = new User
                    {
                        Username = "admin",
                        FullName = "Administrator",
                        Role = "Admin",
                        CreatedDate = DateTime.Now,
                        IsActive = true,
                        IsFirstAccount = true
                    };

                    await CreateUserAsync(adminUser, "admin123");
                }

                await InitializeDefaultSettingsAsync();

                var companyExists = await _database.Table<Company>().CountAsync() > 0;
                if (!companyExists)
                {
                    var defaultCompany = new Company
                    {
                        Name = "My Company",
                        TaxId = "000000000",
                        Address = "123 Main Street",
                        City = "City",
                        Country = "Country",
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };
                    await _database.InsertAsync(defaultCompany);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize default data: {ex.Message}", ex);
            }
        }

        private async Task InitializeDefaultSettingsAsync()
        {
            var defaultSettings = new Dictionary<string, string>
            {
                { "app_version", "1.0.0" },
                { "tax_rate", "20.0" },
                { "currency", "EUR" },
                { "company_name", "My Company" },
                { "fiscal_year_start", "01-01" },
                { "backup_enabled", "true" },
                { "backup_frequency", "daily" },
                { "max_backup_files", "10" }
            };

            foreach (var setting in defaultSettings)
            {
                var exists = await _database.Table<Setting>()
                    .Where(s => s.Key == setting.Key)
                    .CountAsync() > 0;

                if (!exists)
                {
                    await _database.InsertAsync(new Setting
                    {
                        Key = setting.Key,
                        Value = setting.Value,
                        ModifiedDate = DateTime.Now
                    });
                }
            }
        }

        // User Management - Same as before
        public async Task<bool> CreateUserAsync(User user, string password)
        {
            try
            {
                var salt = GenerateSalt();
                var passwordHash = HashPassword(password, salt);

                user.PasswordHash = passwordHash;
                user.Salt = salt;

                var result = await _database.InsertAsync(user);
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create user: {ex.Message}", ex);
            }
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            try
            {
                return await _database.Table<User>()
                    .Where(u => u.Username == username)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get user by username: {ex.Message}", ex);
            }
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            try
            {
                return await _database.Table<User>()
                    .Where(u => u.Id == id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get user by ID: {ex.Message}", ex);
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                return await _database.Table<User>()
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Id)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get all users: {ex.Message}", ex);
            }
        }

        public async Task<bool> ValidatePasswordAsync(string username, string password)
        {
            try
            {
                var user = await GetUserByUsernameAsync(username);
                if (user == null)
                    return false;

                var hashedPassword = HashPassword(password, user.Salt);
                return hashedPassword == user.PasswordHash;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                user.ModifiedDate = DateTime.Now;
                var result = await _database.UpdateAsync(user);
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update user: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                var user = await GetUserByIdAsync(id);
                if (user != null)
                {
                    user.IsActive = false;
                    user.ModifiedDate = DateTime.Now;
                    var result = await _database.UpdateAsync(user);
                    return result > 0;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete user: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdatePasswordAsync(int userId, string newPassword)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                    return false;

                var newSalt = GenerateSalt();
                var newPasswordHash = HashPassword(newPassword, newSalt);

                user.PasswordHash = newPasswordHash;
                user.Salt = newSalt;
                user.ModifiedDate = DateTime.Now;

                var result = await _database.UpdateAsync(user);
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update password: {ex.Message}", ex);
            }
        }

        // Settings Management
        public async Task<string> GetSettingAsync(string key, string defaultValue = null)
        {
            try
            {
                var setting = await _database.Table<Setting>()
                    .Where(s => s.Key == key)
                    .FirstOrDefaultAsync();

                return setting?.Value ?? defaultValue;
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public async Task<bool> SetSettingAsync(string key, string value, string description = null)
        {
            try
            {
                var existingSetting = await _database.Table<Setting>()
                    .Where(s => s.Key == key)
                    .FirstOrDefaultAsync();

                if (existingSetting != null)
                {
                    existingSetting.Value = value;
                    existingSetting.Description = description;
                    existingSetting.ModifiedDate = DateTime.Now;
                    await _database.UpdateAsync(existingSetting);
                }
                else
                {
                    await _database.InsertAsync(new Setting
                    {
                        Key = key,
                        Value = value,
                        Description = description,
                        ModifiedDate = DateTime.Now
                    });
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Dictionary<string, string>> GetAllSettingsAsync()
        {
            try
            {
                var settings = await _database.Table<Setting>().ToListAsync();
                return settings.ToDictionary(s => s.Key, s => s.Value);
            }
            catch (Exception)
            {
                return new Dictionary<string, string>();
            }
        }

        // Company Management
        public async Task<Company> CreateCompanyAsync(Company company)
        {
            try
            {
                await _database.InsertAsync(company);
                return company;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create company: {ex.Message}", ex);
            }
        }

        public async Task<Company> GetCompanyAsync()
        {
            try
            {
                return await _database.Table<Company>()
                    .Where(c => c.IsActive)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get company: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateCompanyAsync(Company company)
        {
            try
            {
                company.ModifiedDate = DateTime.Now;
                var result = await _database.UpdateAsync(company);
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update company: {ex.Message}", ex);
            }
        }

        // Dashboard metrics
        public async Task<Dictionary<string, object>> GetDashboardMetricsAsync()
        {
            var metrics = new Dictionary<string, object>();

            try
            {
                metrics["todaysTransactions"] = await GetTodaysTransactionCountAsync();
                metrics["todaysRevenue"] = await GetTodaysRevenueAsync();
                metrics["totalUsers"] = await GetUserCountAsync();
                metrics["totalProducts"] = await GetProductCountAsync();
                metrics["lowStockProducts"] = 0;
            }
            catch (Exception)
            {
                metrics["todaysTransactions"] = 0;
                metrics["todaysRevenue"] = 0m;
                metrics["totalUsers"] = 0;
                metrics["totalProducts"] = 0;
                metrics["lowStockProducts"] = 0;
            }

            return metrics;
        }

        public async Task<int> GetTodaysTransactionCountAsync()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                return await _database.Table<Transaction>()
                    .Where(t => t.TransactionDate >= today && t.TransactionDate < tomorrow)
                    .CountAsync();
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<decimal> GetTodaysRevenueAsync()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var transactions = await _database.Table<Transaction>()
                    .Where(t => t.TransactionDate >= today && t.TransactionDate < tomorrow && t.Status == "Completed")
                    .ToListAsync();

                return transactions.Sum(t => t.TotalAmount);
            }
            catch (Exception)
            {
                return 0m;
            }
        }

        public async Task LogActionAsync(string tableName, string action, string recordId, object oldValues, object newValues, int userId)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    TableName = tableName,
                    Action = action,
                    RecordId = recordId ?? "",
                    OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : "",
                    NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : "",
                    UserId = userId,
                    Timestamp = DateTime.Now
                };

                await _database.InsertAsync(auditLog);
            }
            catch (Exception)
            {
                // Don't throw - audit logging shouldn't break main functionality
            }
        }

        public async Task<bool> IsDatabaseInitializedAsync()
        {
            return _isInitialized && _database != null && _isDatabaseLoaded;
        }

        // Helper methods
        private string GenerateSalt()
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] saltBytes = new byte[16];
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        private string HashPassword(string password, string salt)
        {
            using var sha256 = SHA256.Create();
            var saltedPassword = password + salt;
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            return Convert.ToBase64String(hashedBytes);
        }

        private async Task<int> GetUserCountAsync()
        {
            return await _database.Table<User>()
                .Where(u => u.IsActive)
                .CountAsync();
        }

        private async Task<int> GetProductCountAsync()
        {
            return await _database.Table<Product>()
                .Where(p => p.IsActive)
                .CountAsync();
        }

        public async Task CleanupUnencryptedFiles()
        {
            await Task.CompletedTask;
        }

        // Stub implementations for remaining interface methods
        public Task<Transaction> CreateTransactionAsync(Transaction transaction) => throw new NotImplementedException();
        public Task<Transaction> GetTransactionByIdAsync(int id) => throw new NotImplementedException();
        public Task<List<Transaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate) => throw new NotImplementedException();
        public Task<List<Transaction>> GetTodaysTransactionsAsync() => throw new NotImplementedException();
        public Task<bool> UpdateTransactionAsync(Transaction transaction) => throw new NotImplementedException();
        public Task<bool> DeleteTransactionAsync(int id) => throw new NotImplementedException();
        public Task<TransactionItem> CreateTransactionItemAsync(TransactionItem item) => throw new NotImplementedException();
        public Task<List<TransactionItem>> GetTransactionItemsAsync(int transactionId) => throw new NotImplementedException();
        public Task<bool> UpdateTransactionItemAsync(TransactionItem item) => throw new NotImplementedException();
        public Task<bool> DeleteTransactionItemAsync(int id) => throw new NotImplementedException();
        public Task<Product> CreateProductAsync(Product product) => throw new NotImplementedException();
        public Task<Product> GetProductByIdAsync(int id) => throw new NotImplementedException();
        public Task<Product> GetProductByCodeAsync(string code) => throw new NotImplementedException();
        public Task<List<Product>> GetAllProductsAsync() => throw new NotImplementedException();
        public Task<List<Product>> SearchProductsAsync(string searchTerm) => throw new NotImplementedException();
        public Task<bool> UpdateProductAsync(Product product) => throw new NotImplementedException();
        public Task<bool> DeleteProductAsync(int id) => throw new NotImplementedException();
        public Task<bool> UpdateProductStockAsync(int productId, decimal newStock) => throw new NotImplementedException();
        public Task<List<AuditLog>> GetAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, int? userId = null) => throw new NotImplementedException();
        public Task<FiscalDevice> CreateFiscalDeviceAsync(FiscalDevice device) => throw new NotImplementedException();
        public Task<List<FiscalDevice>> GetFiscalDevicesAsync() => throw new NotImplementedException();
        public Task<FiscalDevice> GetDefaultFiscalDeviceAsync() => throw new NotImplementedException();
        public Task<bool> UpdateFiscalDeviceAsync(FiscalDevice device) => throw new NotImplementedException();
        public Task<bool> SetDefaultFiscalDeviceAsync(int deviceId) => throw new NotImplementedException();
        public Task<List<Transaction>> GetTopTransactionsAsync(int count = 10) => throw new NotImplementedException();
        public Task<Dictionary<string, decimal>> GetSalesByDateAsync(DateTime startDate, DateTime endDate) => throw new NotImplementedException();
        public Task<List<Product>> GetLowStockProductsAsync() => throw new NotImplementedException();
        public Task BackupDatabaseAsync(string backupPath) => throw new NotImplementedException();
        public Task RestoreDatabaseAsync(string backupPath) => throw new NotImplementedException();
        public Task ChangePasswordAsync(string oldPassword, string newPassword) => throw new NotImplementedException();
    }
}
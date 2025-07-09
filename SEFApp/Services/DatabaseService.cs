using SEFApp.Models;
using SEFApp.Models.Database;
using SEFApp.Services.Interfaces;
using SQLite;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SEFApp.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly string _databasePath;
        private readonly string _keyFilePath;
        private SQLiteAsyncConnection _database;
        private bool _isInitialized = false;

        public DatabaseService()
        {
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, "sefmanager.db");
            _keyFilePath = Path.Combine(FileSystem.AppDataDirectory, "db.key");

            System.Diagnostics.Debug.WriteLine($"Database path: {_databasePath}");
            System.Diagnostics.Debug.WriteLine($"Key file path: {_keyFilePath}");
        }

        public async Task InitializeDatabaseForUser(string username, string password)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Initializing database for user: {username}");

                // Get or create the database encryption key
                var dbKey = await GetOrCreateDatabaseKeyAsync();

                System.Diagnostics.Debug.WriteLine($"Using database key: {dbKey}");

                // Create SQLite connection with encryption
                var connectionString = new SQLiteConnectionString(
                    databasePath: _databasePath,
                    storeDateTimeAsTicks: true,
                    key: dbKey);

                _database = new SQLiteAsyncConnection(connectionString);

                await InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization failed: {ex.Message}");

                // Clean up failed connection
                try
                {
                    await _database?.CloseAsync();
                }
                catch { }
                _database = null;

                throw new InvalidOperationException($"Failed to initialize database: {ex.Message}", ex);
            }
        }

        private async Task<string> GetOrCreateDatabaseKeyAsync()
        {
            try
            {
                // Check if key file exists and is valid
                if (File.Exists(_keyFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("Key file exists, reading...");

                    var keyContent = await File.ReadAllTextAsync(_keyFilePath);

                    // Validate the key (64 characters, alphanumeric)
                    if (!string.IsNullOrEmpty(keyContent) &&
                        keyContent.Length == 64 &&
                        keyContent.All(c => char.IsLetterOrDigit(c)))
                    {
                        System.Diagnostics.Debug.WriteLine("Valid key found");
                        return keyContent;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Invalid key found, regenerating...");
                    }
                }

                // Generate and save new key
                System.Diagnostics.Debug.WriteLine("Creating new database key...");
                var newKey = GenerateSecureKey();
                await SaveKeyAsync(newKey);
                return newKey;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Key management error: {ex.Message}");
                throw new InvalidOperationException($"Failed to manage database key: {ex.Message}", ex);
            }
        }

        private string GenerateSecureKey()
        {
            // Generate a safe alphanumeric key
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            using var rng = RandomNumberGenerator.Create();
            var keyLength = 64;
            var result = new char[keyLength];

            for (int i = 0; i < keyLength; i++)
            {
                var randomBytes = new byte[1];
                rng.GetBytes(randomBytes);
                result[i] = chars[randomBytes[0] % chars.Length];
            }

            return new string(result);
        }

        private async Task SaveKeyAsync(string key)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Saving key to: {_keyFilePath}");

                var directory = Path.GetDirectoryName(_keyFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Save as plain text (no encryption, no special attributes)
                await File.WriteAllTextAsync(_keyFilePath, key);

                System.Diagnostics.Debug.WriteLine("Key saved successfully");
                System.Diagnostics.Debug.WriteLine($"File exists: {File.Exists(_keyFilePath)}");

                // Verify the saved content
                var savedContent = await File.ReadAllTextAsync(_keyFilePath);
                System.Diagnostics.Debug.WriteLine($"Verified content: {savedContent}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save key: {ex.Message}");
                throw new InvalidOperationException($"Failed to save key: {ex.Message}", ex);
            }
        }

        public async Task SetEncryptionForUser(string username, string password)
        {
            await Task.CompletedTask;
        }

        public async Task InitializeDatabaseAsync()
        {
            if (_isInitialized) return;

            try
            {
                await CreateTablesAsync();

                var masterTableCount = await _database.ExecuteScalarAsync<int>("SELECT count(*) FROM sqlite_master");
                System.Diagnostics.Debug.WriteLine($"Database connection verified. Master tables: {masterTableCount}");

                await InitializeDefaultDataAsync();
                _isInitialized = true;

                System.Diagnostics.Debug.WriteLine("Database initialized successfully");
            }
            catch (SQLiteException sqlEx) when (sqlEx.Message.Contains("file is not a database") ||
                                                sqlEx.Message.Contains("file is encrypted") ||
                                                sqlEx.Message.Contains("file is not encrypted") ||
                                                sqlEx.Message.Contains("wrong password"))
            {
                System.Diagnostics.Debug.WriteLine($"SQLite password/encryption error: {sqlEx.Message}");
                throw new InvalidOperationException("Invalid database key or corrupted database", sqlEx);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
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

            System.Diagnostics.Debug.WriteLine("All tables created successfully");
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

        // Debug methods
        public string GetKeyFilePathForDebugging()
        {
            return _keyFilePath;
        }

        public bool KeyFileExistsForDebugging()
        {
            return File.Exists(_keyFilePath);
        }

        public async Task<string> GetKeyContentForDebugging()
        {
            try
            {
                if (File.Exists(_keyFilePath))
                {
                    return await File.ReadAllTextAsync(_keyFilePath);
                }
                return "Key file does not exist";
            }
            catch (Exception ex)
            {
                return $"Error reading key: {ex.Message}";
            }
        }

        // User Management
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
            return _isInitialized && _database != null;
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
        // Add these methods to your DatabaseService.cs file

        #region Product Management Methods

        public async Task<Product> CreateProductAsync(Product product)
        {
            try
            {
                // Validate product data
                if (string.IsNullOrWhiteSpace(product.ProductCode))
                    throw new ArgumentException("Product code is required");

                if (string.IsNullOrWhiteSpace(product.Name))
                    throw new ArgumentException("Product name is required");

                // Check if product code already exists
                var existingProduct = await GetProductByCodeAsync(product.ProductCode);
                if (existingProduct != null)
                    throw new InvalidOperationException("Product code already exists");

                // Set creation date
                product.CreatedDate = DateTime.Now;
                product.IsActive = true;

                // Insert product
                var result = await _database.InsertAsync(product);
                if (result > 0)
                {
                    // Get the inserted product (with ID)
                    var insertedProduct = await GetProductByCodeAsync(product.ProductCode);

                    // Log the creation
                    await LogActionAsync("Products", "CREATE", insertedProduct.Id.ToString(),
                        null, insertedProduct, GetCurrentUserId());

                    return insertedProduct;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateProductAsync error: {ex.Message}");
                throw new InvalidOperationException($"Failed to create product: {ex.Message}", ex);
            }
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            try
            {
                return await _database.Table<Product>()
                    .Where(p => p.Id == id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetProductByIdAsync error: {ex.Message}");
                throw new InvalidOperationException($"Failed to get product by ID: {ex.Message}", ex);
            }
        }

        public async Task<Product> GetProductByCodeAsync(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                    return null;

                return await _database.Table<Product>()
                    .Where(p => p.ProductCode == code)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetProductByCodeAsync error: {ex.Message}");
                throw new InvalidOperationException($"Failed to get product by code: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            try
            {
                return await _database.Table<Product>()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAllProductsAsync error: {ex.Message}");
                throw new InvalidOperationException($"Failed to get all products: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> SearchProductsAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return await GetAllProductsAsync();

                var lowerSearchTerm = searchTerm.ToLower();

                return await _database.Table<Product>()
                    .Where(p => p.IsActive &&
                               (p.Name.ToLower().Contains(lowerSearchTerm) ||
                                p.ProductCode.ToLower().Contains(lowerSearchTerm) ||
                                p.Category.ToLower().Contains(lowerSearchTerm)))
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SearchProductsAsync error: {ex.Message}");
                throw new InvalidOperationException($"Failed to search products: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                if (product == null)
                    throw new ArgumentNullException(nameof(product));

                // Validate product data
                if (string.IsNullOrWhiteSpace(product.ProductCode))
                    throw new ArgumentException("Product code is required");

                if (string.IsNullOrWhiteSpace(product.Name))
                    throw new ArgumentException("Product name is required");

                // Check if product code is taken by another product
                var existingProduct = await GetProductByCodeAsync(product.ProductCode);
                if (existingProduct != null && existingProduct.Id != product.Id)
                    throw new InvalidOperationException("Product code already exists");

                // Get old values for audit log
                var oldProduct = await GetProductByIdAsync(product.Id);

                // Update modification date
                product.ModifiedDate = DateTime.Now;

                // Update product
                var result = await _database.UpdateAsync(product);

                if (result > 0)
                {
                    // Log the update
                    await LogActionAsync("Products", "UPDATE", product.Id.ToString(),
                        oldProduct, product, GetCurrentUserId());

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateProductAsync error: {ex.Message}");
                throw new InvalidOperationException($"Failed to update product: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var product = await GetProductByIdAsync(id);
                if (product == null)
                    return false;

                // Soft delete - mark as inactive
                product.IsActive = false;
                product.ModifiedDate = DateTime.Now;

                var result = await _database.UpdateAsync(product);

                if (result > 0)
                {
                    // Log the deletion
                    await LogActionAsync("Products", "DELETE", id.ToString(),
                        product, new { IsActive = false, ModifiedDate = DateTime.Now }, GetCurrentUserId());

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteProductAsync error: {ex.Message}");
                throw new InvalidOperationException($"Failed to delete product: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateProductStockAsync(int productId, decimal newStock)
        {
            try
            {
                var product = await GetProductByIdAsync(productId);
                if (product == null)
                    return false;

                var oldStock = product.Stock;
                product.Stock = newStock;
                product.ModifiedDate = DateTime.Now;

                var result = await _database.UpdateAsync(product);

                if (result > 0)
                {
                    // Log the stock update
                    await LogActionAsync("Products", "STOCK_UPDATE", productId.ToString(),
                        new { OldStock = oldStock },
                        new { NewStock = newStock, ModifiedDate = DateTime.Now },
                        GetCurrentUserId());

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateProductStockAsync error: {ex.Message}");
                throw new InvalidOperationException($"Failed to update product stock: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> GetLowStockProductsAsync()
        {
            try
            {
                return await _database.Table<Product>()
                    .Where(p => p.IsActive && p.Stock <= p.MinStock)
                    .OrderBy(p => p.Stock)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetLowStockProductsAsync error: {ex.Message}");
                throw new InvalidOperationException($"Failed to get low stock products: {ex.Message}", ex);
            }
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(string category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category))
                    return await GetAllProductsAsync();

                return await _database.Table<Product>()
                    .Where(p => p.IsActive && p.Category == category)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetProductsByCategoryAsync error: {ex.Message}");
                throw new InvalidOperationException($"Failed to get products by category: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GetProductCategoriesAsync()
        {
            try
            {
                var categories = await _database.Table<Product>()
                    .Where(p => p.IsActive && !string.IsNullOrEmpty(p.Category))
                    .ToListAsync();

                return categories.Select(p => p.Category)
                                .Distinct()
                                .Where(c => !string.IsNullOrWhiteSpace(c))
                                .OrderBy(c => c)
                                .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetProductCategoriesAsync error: {ex.Message}");
                throw new InvalidOperationException($"Failed to get product categories: {ex.Message}", ex);
            }
        }

        #endregion

        #region Helper Methods

        private int GetCurrentUserId()
        {
            // This should return the current user's ID
            // You might want to get this from your authentication service
            try
            {
                // For now, return a default user ID
                // In a real implementation, you would get this from your authentication service
                return 1; // Default admin user ID
            }
            catch
            {
                return 1; // Fallback to admin user
            }
        }

        #endregion
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
        public Task<List<AuditLog>> GetAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, int? userId = null) => throw new NotImplementedException();
        public Task<FiscalDevice> CreateFiscalDeviceAsync(FiscalDevice device) => throw new NotImplementedException();
        public Task<List<FiscalDevice>> GetFiscalDevicesAsync() => throw new NotImplementedException();
        public Task<FiscalDevice> GetDefaultFiscalDeviceAsync() => throw new NotImplementedException();
        public Task<bool> UpdateFiscalDeviceAsync(FiscalDevice device) => throw new NotImplementedException();
        public Task<bool> SetDefaultFiscalDeviceAsync(int deviceId) => throw new NotImplementedException();
        public Task<List<Transaction>> GetTopTransactionsAsync(int count = 10) => throw new NotImplementedException();
        public Task<Dictionary<string, decimal>> GetSalesByDateAsync(DateTime startDate, DateTime endDate) => throw new NotImplementedException();
        public Task BackupDatabaseAsync(string backupPath) => throw new NotImplementedException();
        public Task RestoreDatabaseAsync(string backupPath) => throw new NotImplementedException();
        public Task ChangePasswordAsync(string oldPassword, string newPassword) => throw new NotImplementedException();
    }
}
using Microsoft.Data.Sqlite;
using SEFApp.Models;
using SEFApp.Models.Database;
using SEFApp.Services.Interfaces;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SEFApp.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly string _baseDatabasePath;
        private string _databasePath;
        private string _connectionString;
        private string _encryptionKey;
        private string _userPassword;
        private bool _isDatabaseEncrypted = false;
        private bool _isInitialized = false;

        public DatabaseService()
        {
            _baseDatabasePath = Path.Combine(FileSystem.AppDataDirectory, "sefmanager.db");
            _databasePath = _baseDatabasePath;
            _connectionString = $"Data Source={_databasePath}";
        }
        public async Task InitializeDatabaseForUser(string username, string password)
        {
            _userPassword = password;
            _encryptionKey = GenerateEncryptionKey(username, password);

            // Store encryption key in Windows Credential Manager
            await StoreEncryptionKeySecurely(username, _encryptionKey);

            // Create encrypted connection string
            _connectionString = $"Data Source={_databasePath};Password={_encryptionKey}";

            await InitializeDatabaseAsync();
        }
        public async Task SetEncryptionForUser(string username, string password)
        {
            if (_isDatabaseEncrypted) return;

            _encryptionKey = GenerateEncryptionKey(username, password);
            await StoreEncryptionKeySecurely(username, _encryptionKey);

            var encryptedPath = _baseDatabasePath + ".enc";

            try
            {
                // Close all SQLite connections to release file locks
                SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Wait a moment for file handles to be released
                await Task.Delay(100);

                // If unencrypted DB exists, encrypt it
                if (File.Exists(_baseDatabasePath) && !File.Exists(encryptedPath))
                {
                    await EncryptDatabaseFile(_baseDatabasePath, encryptedPath);

                    // Try to delete original with retries
                    await DeleteFileWithRetry(_baseDatabasePath);
                    System.Diagnostics.Debug.WriteLine("Database encrypted and original deleted");
                }

                // Use encrypted database
                if (File.Exists(encryptedPath))
                {
                    await DecryptToTempDatabase(encryptedPath);
                }

                _isDatabaseEncrypted = true;
                System.Diagnostics.Debug.WriteLine("Database encryption enabled");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Encryption setup failed: {ex.Message}");
                throw;
            }
        }
        private async Task DeleteFileWithRetry(string filePath, int maxRetries = 5)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    File.Delete(filePath);
                    System.Diagnostics.Debug.WriteLine($"File deleted successfully: {filePath}");
                    return;
                }
                catch (IOException ex) when (i < maxRetries - 1)
                {
                    System.Diagnostics.Debug.WriteLine($"Delete attempt {i + 1} failed: {ex.Message}");
                    await Task.Delay(200 * (i + 1)); // Exponential backoff

                    // Force garbage collection
                    SqliteConnection.ClearAllPools();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            System.Diagnostics.Debug.WriteLine($"Warning: Could not delete file after {maxRetries} attempts: {filePath}");
        }
        private string GenerateEncryptionKey(string username, string password)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                Encoding.UTF8.GetBytes(username + "SEF_SALT"),
                10000, // iterations
                HashAlgorithmName.SHA256
            );

            var key = pbkdf2.GetBytes(32); // 256-bit key
            return Convert.ToBase64String(key);
        }
        private async Task DecryptToTempDatabase(string encryptedPath)
        {
            var tempPath = Path.Combine(FileSystem.CacheDirectory, $"temp_{Guid.NewGuid()}.db");
            var encryptedData = await File.ReadAllBytesAsync(encryptedPath);
            var decryptedData = DecryptData(encryptedData, _encryptionKey);
            await File.WriteAllBytesAsync(tempPath, decryptedData);

            _databasePath = tempPath;
            _connectionString = $"Data Source={_databasePath}";

            System.Diagnostics.Debug.WriteLine($"Database decrypted to temp: {tempPath}");
        }


        private async Task EncryptDatabaseFile(string sourcePath, string encryptedPath)
        {
            var data = await File.ReadAllBytesAsync(sourcePath);
            var encryptedData = EncryptData(data, _encryptionKey);
            await File.WriteAllBytesAsync(encryptedPath, encryptedData);
        }

        private byte[] EncryptData(byte[] data, string key)
        {
            using var aes = System.Security.Cryptography.Aes.Create();
            var keyBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key));
            aes.Key = keyBytes;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);

            var result = new byte[aes.IV.Length + encrypted.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

            return result;
        }

        private byte[] DecryptData(byte[] encryptedData, string key)
        {
            using var aes = System.Security.Cryptography.Aes.Create();
            var keyBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key));
            aes.Key = keyBytes;

            // Extract IV
            var iv = new byte[16];
            Buffer.BlockCopy(encryptedData, 0, iv, 0, 16);
            aes.IV = iv;

            // Extract encrypted data
            var encrypted = new byte[encryptedData.Length - 16];
            Buffer.BlockCopy(encryptedData, 16, encrypted, 0, encrypted.Length);

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
        }

        // Add cleanup on app shutdown
        public async Task SaveAndEncryptDatabase()
        {
            if (!_isDatabaseEncrypted) return;

            var encryptedPath = _baseDatabasePath + ".enc";
            await EncryptDatabaseFile(_databasePath, encryptedPath);

            // Clean up temp file
            if (_databasePath.Contains("temp_") && File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }

            System.Diagnostics.Debug.WriteLine("Database saved and encrypted");
        }
        private async Task StoreEncryptionKeySecurely(string username, string encryptionKey)
        {
            var credentialName = $"SEFApp_DBKey_{username}";

            try
            {
                // Use Windows Data Protection API
                var protectedKey = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(encryptionKey),
                    Encoding.UTF8.GetBytes(username), // Additional entropy
                    DataProtectionScope.CurrentUser
                );

                // Store in registry or credential manager
                Microsoft.Win32.Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\SEFApp\Keys",
                    credentialName,
                    Convert.ToBase64String(protectedKey)
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to store key securely: {ex.Message}");
                // Fallback to SecureStorage
                await SecureStorage.SetAsync(credentialName, encryptionKey);
            }
            // For other platforms, use SecureStorage
            await SecureStorage.SetAsync($"SEFApp_DBKey_{username}", encryptionKey);
        }
        private async Task<string> RetrieveEncryptionKeySecurely(string username)
        {
            var credentialName = $"SEFApp_DBKey_{username}";

            try
            {
                // Retrieve from registry
                var protectedKeyBase64 = Microsoft.Win32.Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\SEFApp\Keys",
                    credentialName,
                    null
                )?.ToString();

                if (!string.IsNullOrEmpty(protectedKeyBase64))
                {
                    var protectedKey = Convert.FromBase64String(protectedKeyBase64);
                    var key = ProtectedData.Unprotect(
                        protectedKey,
                        Encoding.UTF8.GetBytes(username),
                        DataProtectionScope.CurrentUser
                    );

                    return Encoding.UTF8.GetString(key);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to retrieve key securely: {ex.Message}");
            }

            // Fallback to SecureStorage
            return await SecureStorage.GetAsync(credentialName);
            return await SecureStorage.GetAsync($"SEFApp_DBKey_{username}");
        }
        public async Task InitializeDatabaseAsync()
        {
            if (_isInitialized)
            {
                System.Diagnostics.Debug.WriteLine("Database already initialized, skipping");
                return;
            }
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== INITIALIZING DATABASE ===");
                System.Diagnostics.Debug.WriteLine($"Connection String: {_connectionString}");

                // Test connection
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // Apply encryption if set
                if (_isDatabaseEncrypted && !string.IsNullOrEmpty(_encryptionKey))
                {
                    using var keyCommand = new SqliteCommand($"PRAGMA key = '{_encryptionKey}'", connection);
                    await keyCommand.ExecuteNonQueryAsync();
                }

                System.Diagnostics.Debug.WriteLine("Database connection opened successfully");

                // Create tables
                await CreateTablesAsync(connection);
                System.Diagnostics.Debug.WriteLine("Tables created successfully");

                // Initialize default data
                await InitializeDefaultDataAsync();
                System.Diagnostics.Debug.WriteLine("Default data initialized successfully");

                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("=== DATABASE INITIALIZATION COMPLETE ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== DATABASE INITIALIZATION FAILED ===");
                System.Diagnostics.Debug.WriteLine($"Error: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task CreateTablesAsync(SqliteConnection connection)
        {
            var createTableQueries = new[]
            {
                // Users table
                @"CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    Salt TEXT NOT NULL,
                    FullName TEXT,
                    Role TEXT NOT NULL,
                    CreatedDate TEXT NOT NULL,
                    LastLoginDate TEXT,
                    ModifiedDate TEXT,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    IsFirstAccount INTEGER NOT NULL DEFAULT 0
                )",

                // Transactions table
                @"CREATE TABLE IF NOT EXISTS Transactions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TransactionNumber TEXT NOT NULL,
                    TransactionDate TEXT NOT NULL,
                    Amount REAL NOT NULL,
                    TaxAmount REAL NOT NULL,
                    TotalAmount REAL NOT NULL,
                    CustomerName TEXT,
                    CustomerTaxId TEXT,
                    Status TEXT NOT NULL,
                    TransactionType TEXT NOT NULL,
                    FiscalReceiptNumber TEXT,
                    FiscalReceiptDate TEXT,
                    CreatedByUserId INTEGER NOT NULL,
                    CreatedDate TEXT NOT NULL,
                    ModifiedDate TEXT,
                    Notes TEXT
                )",

                // TransactionItems table
                @"CREATE TABLE IF NOT EXISTS TransactionItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TransactionId INTEGER NOT NULL,
                    ProductName TEXT NOT NULL,
                    ProductCode TEXT,
                    Quantity REAL NOT NULL,
                    UnitPrice REAL NOT NULL,
                    TaxRate REAL NOT NULL,
                    LineTotal REAL NOT NULL,
                    Unit TEXT DEFAULT 'pcs'
                )",

                // Products table
                @"CREATE TABLE IF NOT EXISTS Products (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProductCode TEXT NOT NULL UNIQUE,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    Price REAL NOT NULL,
                    TaxRate REAL NOT NULL,
                    Category TEXT,
                    Unit TEXT DEFAULT 'pcs',
                    Stock REAL DEFAULT 0,
                    MinStock REAL DEFAULT 0,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedDate TEXT NOT NULL,
                    ModifiedDate TEXT
                )",

                // Settings table
                @"CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL,
                    Description TEXT,
                    ModifiedDate TEXT NOT NULL
                )",

                // AuditLogs table
                @"CREATE TABLE IF NOT EXISTS AuditLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TableName TEXT NOT NULL,
                    Action TEXT NOT NULL,
                    RecordId TEXT,
                    OldValues TEXT,
                    NewValues TEXT,
                    UserId INTEGER NOT NULL,
                    Timestamp TEXT NOT NULL,
                    IpAddress TEXT
                )",

                // FiscalDevices table
                @"CREATE TABLE IF NOT EXISTS FiscalDevices (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    DeviceType TEXT NOT NULL,
                    SerialNumber TEXT,
                    ConnectionString TEXT,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    IsDefault INTEGER NOT NULL DEFAULT 0,
                    CreatedDate TEXT NOT NULL,
                    LastUsedDate TEXT,
                    Configuration TEXT
                )",

                // Companies table
                @"CREATE TABLE IF NOT EXISTS Companies (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    TaxId TEXT NOT NULL,
                    Address TEXT,
                    City TEXT,
                    PostalCode TEXT,
                    Country TEXT,
                    Phone TEXT,
                    Email TEXT,
                    Website TEXT,
                    Logo BLOB,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedDate TEXT NOT NULL,
                    ModifiedDate TEXT
                )",
                // TransactionLogs table
                @"CREATE TABLE IF NOT EXISTS TransactionLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TransactionId INTEGER NOT NULL,
                    Action TEXT NOT NULL,
                    OldStatus TEXT,
                    NewStatus TEXT,
                    UserId INTEGER NOT NULL,
                    Timestamp TEXT NOT NULL,
                    Notes TEXT,
                    IpAddress TEXT,
                    DeviceInfo TEXT
                )"

            };

            foreach (var query in createTableQueries)
            {
                using var command = new SqliteCommand(query, connection);
                await command.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine($"Table created: {query.Split(' ')[5]}"); // Extract table name
            }
        }

        private async Task InitializeDefaultDataAsync()
        {
            try
            {
                // Check if admin user exists
                var adminExists = await UserExistsAsync("admin");

                if (!adminExists)
                {
                    System.Diagnostics.Debug.WriteLine("Creating default admin user...");

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
                    System.Diagnostics.Debug.WriteLine("Default admin user created");
                }

                // Initialize default settings
                await InitializeDefaultSettingsAsync();

                // Initialize default company if not exists
                var companyExists = await CompanyExistsAsync();
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
                    await CreateCompanyAsync(defaultCompany);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Default data initialization error: {ex.Message}");
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
                var exists = await SettingExistsAsync(setting.Key);
                if (!exists)
                {
                    await SetSettingAsync(setting.Key, setting.Value);
                }
            }
        }

        // User Management
        public async Task<User> CreateUserAsync(User user, string password)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Creating user: {user.Username}");

                var salt = GenerateSalt();
                var passwordHash = HashPassword(password, salt);

                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"INSERT INTO Users (Username, PasswordHash, Salt, FullName, Role, CreatedDate, IsActive, IsFirstAccount)
                             VALUES (@Username, @PasswordHash, @Salt, @FullName, @Role, @CreatedDate, @IsActive, @IsFirstAccount);
                             SELECT last_insert_rowid();";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Username", user.Username);
                command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                command.Parameters.AddWithValue("@Salt", salt);
                command.Parameters.AddWithValue("@FullName", user.FullName ?? "");
                command.Parameters.AddWithValue("@Role", user.Role);
                command.Parameters.AddWithValue("@CreatedDate", user.CreatedDate.ToString("O"));
                command.Parameters.AddWithValue("@IsActive", user.IsActive ? 1 : 0);
                command.Parameters.AddWithValue("@IsFirstAccount", user.IsFirstAccount ? 1 : 0);

                var result = await command.ExecuteScalarAsync();
                user.Id = Convert.ToInt32(result);

                System.Diagnostics.Debug.WriteLine($"User created with ID: {user.Id}");
                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Create user error: {ex.Message}");
                throw;
            }
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // Set encryption key using PRAGMA
                if (_isDatabaseEncrypted && !string.IsNullOrEmpty(_encryptionKey))
                {
                    using var keyCommand = new SqliteCommand($"PRAGMA key = '{_encryptionKey}'", connection);
                    await keyCommand.ExecuteNonQueryAsync();
                }

                var query = "SELECT * FROM Users WHERE Username = @Username";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapUserFromReader(reader);
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get user by username error: {ex.Message}");
                return null;
            }
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT * FROM Users WHERE Id = @Id";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapUserFromReader(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get user by ID error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                var users = new List<User>();

                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT * FROM Users WHERE IsActive = 1";
                using var command = new SqliteCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    users.Add(MapUserFromReader(reader));
                }

                return users;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get all users error: {ex.Message}");
                return new List<User>();
            }
        }

        public async Task<bool> ValidatePasswordAsync(string username, string password)
        {
            try
            {
                if (!_isDatabaseEncrypted)
                {
                    await SetEncryptionForUser(username, password);
                }

                var user = await GetUserByUsernameAsync(username);
                if (user == null)
                    return false;

                var hashedPassword = HashPassword(password, user.Salt);
                return hashedPassword == user.PasswordHash;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Password validation error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"UPDATE Users SET 
                             Username = @Username, FullName = @FullName, Role = @Role,
                             LastLoginDate = @LastLoginDate, ModifiedDate = @ModifiedDate,
                             IsActive = @IsActive
                             WHERE Id = @Id";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Username", user.Username);
                command.Parameters.AddWithValue("@FullName", user.FullName ?? "");
                command.Parameters.AddWithValue("@Role", user.Role);
                command.Parameters.AddWithValue("@LastLoginDate", user.LastLoginDate.ToString("O"));
                command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now.ToString("O"));
                command.Parameters.AddWithValue("@IsActive", user.IsActive ? 1 : 0);
                command.Parameters.AddWithValue("@Id", user.Id);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update user error: {ex.Message}");
                return false;
            }
        }

        // Settings Management
        public async Task<string> GetSettingAsync(string key, string defaultValue = null)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT Value FROM Settings WHERE Key = @Key";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Key", key);

                var result = await command.ExecuteScalarAsync();
                return result?.ToString() ?? defaultValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get setting error: {ex.Message}");
                return defaultValue;
            }
        }

        public async Task<bool> SetSettingAsync(string key, string value, string description = null)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"INSERT OR REPLACE INTO Settings (Key, Value, Description, ModifiedDate)
                             VALUES (@Key, @Value, @Description, @ModifiedDate)";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Key", key);
                command.Parameters.AddWithValue("@Value", value);
                command.Parameters.AddWithValue("@Description", description ?? "");
                command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now.ToString("O"));

                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Set setting error: {ex.Message}");
                return false;
            }
        }

        // Company Management
        public async Task<Company> CreateCompanyAsync(Company company)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"INSERT INTO Companies (Name, TaxId, Address, City, PostalCode, Country, Phone, Email, Website, IsActive, CreatedDate)
                             VALUES (@Name, @TaxId, @Address, @City, @PostalCode, @Country, @Phone, @Email, @Website, @IsActive, @CreatedDate);
                             SELECT last_insert_rowid();";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Name", company.Name);
                command.Parameters.AddWithValue("@TaxId", company.TaxId);
                command.Parameters.AddWithValue("@Address", company.Address ?? "");
                command.Parameters.AddWithValue("@City", company.City ?? "");
                command.Parameters.AddWithValue("@PostalCode", company.PostalCode ?? "");
                command.Parameters.AddWithValue("@Country", company.Country ?? "");
                command.Parameters.AddWithValue("@Phone", company.Phone ?? "");
                command.Parameters.AddWithValue("@Email", company.Email ?? "");
                command.Parameters.AddWithValue("@Website", company.Website ?? "");
                command.Parameters.AddWithValue("@IsActive", company.IsActive ? 1 : 0);
                command.Parameters.AddWithValue("@CreatedDate", company.CreatedDate.ToString("O"));

                var result = await command.ExecuteScalarAsync();
                company.Id = Convert.ToInt32(result);

                return company;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Create company error: {ex.Message}");
                throw;
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
                metrics["lowStockProducts"] = 0; // Implement when needed
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dashboard metrics error: {ex.Message}");
                // Return default values
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
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var today = DateTime.Today.ToString("yyyy-MM-dd");
                var query = "SELECT COUNT(*) FROM Transactions WHERE date(TransactionDate) = @Today";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Today", today);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get today's transaction count error: {ex.Message}");
                return 0;
            }
        }

        public async Task<decimal> GetTodaysRevenueAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var today = DateTime.Today.ToString("yyyy-MM-dd");
                var query = "SELECT COALESCE(SUM(TotalAmount), 0) FROM Transactions WHERE date(TransactionDate) = @Today AND Status = 'Completed'";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Today", today);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToDecimal(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get today's revenue error: {ex.Message}");
                return 0m;
            }
        }

        // Helper methods
        private User MapUserFromReader(SqliteDataReader reader)
        {
            return new User
            {
                Id = reader.GetInt32("Id"),
                Username = reader.GetString("Username"),
                PasswordHash = reader.GetString("PasswordHash"),
                Salt = reader.GetString("Salt"),
                FullName = reader.IsDBNull("FullName") ? null : reader.GetString("FullName"),
                Role = reader.GetString("Role"),
                CreatedDate = DateTime.Parse(reader.GetString("CreatedDate")),
                LastLoginDate = reader.IsDBNull("LastLoginDate") ? DateTime.MinValue : DateTime.Parse(reader.GetString("LastLoginDate")),
                ModifiedDate = reader.IsDBNull("ModifiedDate") ? null : DateTime.Parse(reader.GetString("ModifiedDate")),
                IsActive = reader.GetInt32("IsActive") == 1,
                IsFirstAccount = reader.GetInt32("IsFirstAccount") == 1
            };
        }

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

        private async Task<bool> UserExistsAsync(string username)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        private async Task<bool> CompanyExistsAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM Companies";
            using var command = new SqliteCommand(query, connection);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        private async Task<bool> SettingExistsAsync(string key)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM Settings WHERE Key = @Key";
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@Key", key);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        private async Task<int> GetUserCountAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM Users WHERE IsActive = 1";
            using var command = new SqliteCommand(query, connection);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        private async Task<int> GetProductCountAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM Products WHERE IsActive = 1";
            using var command = new SqliteCommand(query, connection);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> IsDatabaseInitializedAsync()
        {
            return _isInitialized && File.Exists(_databasePath);
        }

        public async Task LogActionAsync(string tableName, string action, string recordId, object oldValues, object newValues, int userId)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"INSERT INTO AuditLogs (TableName, Action, RecordId, OldValues, NewValues, UserId, Timestamp)
                             VALUES (@TableName, @Action, @RecordId, @OldValues, @NewValues, @UserId, @Timestamp)";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@TableName", tableName);
                command.Parameters.AddWithValue("@Action", action);
                command.Parameters.AddWithValue("@RecordId", recordId ?? "");
                command.Parameters.AddWithValue("@OldValues", oldValues != null ? JsonSerializer.Serialize(oldValues) : "");
                command.Parameters.AddWithValue("@NewValues", newValues != null ? JsonSerializer.Serialize(newValues) : "");
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@Timestamp", DateTime.Now.ToString("O"));

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audit log error: {ex.Message}");
                // Don't throw - audit logging shouldn't break main functionality
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

                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"UPDATE Users SET PasswordHash = @PasswordHash, Salt = @Salt, ModifiedDate = @ModifiedDate WHERE Id = @Id";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@PasswordHash", newPasswordHash);
                command.Parameters.AddWithValue("@Salt", newSalt);
                command.Parameters.AddWithValue("@ModifiedDate", DateTime.Now.ToString("O"));
                command.Parameters.AddWithValue("@Id", userId);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update password error: {ex.Message}");
                return false;
            }
        }

        // Stub implementations for remaining interface methods
        public Task<bool> DeleteUserAsync(int id) => throw new NotImplementedException();
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
        public Task<Dictionary<string, string>> GetAllSettingsAsync() => throw new NotImplementedException();
        public Task<List<AuditLog>> GetAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, int? userId = null) => throw new NotImplementedException();
        public Task<Company> GetCompanyAsync() => throw new NotImplementedException();
        public Task<bool> UpdateCompanyAsync(Company company) => throw new NotImplementedException();
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
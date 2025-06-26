using SEFApp.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SEFApp.Services
{
    public class PreferencesService : IPreferencesService
    {
        private const string EncryptionKey = "SEFManager2024EncryptionKey123456"; // Should be configurable in production

        #region String Methods
        public async Task SetAsync(string key, string value)
        {
            try
            {
                await Task.CompletedTask; // Make it async-compatible
                Preferences.Set(key, value ?? string.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences SetAsync (string) error: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GetAsync(string key, string defaultValue)
        {
            try
            {
                await Task.CompletedTask; // Make it async-compatible
                return Preferences.Get(key, defaultValue ?? string.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences GetAsync (string) error: {ex.Message}");
                return defaultValue ?? string.Empty;
            }
        }
        #endregion

        #region Boolean Methods
        public async Task SetAsync(string key, bool value)
        {
            try
            {
                await Task.CompletedTask;
                Preferences.Set(key, value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences SetAsync (bool) error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> GetAsync(string key, bool defaultValue)
        {
            try
            {
                await Task.CompletedTask;
                return Preferences.Get(key, defaultValue);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences GetAsync (bool) error: {ex.Message}");
                return defaultValue;
            }
        }
        #endregion

        #region Integer Methods
        public async Task SetAsync(string key, int value)
        {
            try
            {
                await Task.CompletedTask;
                Preferences.Set(key, value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences SetAsync (int) error: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetAsync(string key, int defaultValue)
        {
            try
            {
                await Task.CompletedTask;
                return Preferences.Get(key, defaultValue);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences GetAsync (int) error: {ex.Message}");
                return defaultValue;
            }
        }
        #endregion

        #region Double Methods
        public async Task SetAsync(string key, double value)
        {
            try
            {
                await Task.CompletedTask;
                Preferences.Set(key, value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences SetAsync (double) error: {ex.Message}");
                throw;
            }
        }

        public async Task<double> GetAsync(string key, double defaultValue)
        {
            try
            {
                await Task.CompletedTask;
                return Preferences.Get(key, defaultValue);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences GetAsync (double) error: {ex.Message}");
                return defaultValue;
            }
        }
        #endregion

        #region Long Methods
        public async Task SetAsync(string key, long value)
        {
            try
            {
                await Task.CompletedTask;
                Preferences.Set(key, value.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences SetAsync (long) error: {ex.Message}");
                throw;
            }
        }

        public async Task<long> GetAsync(string key, long defaultValue)
        {
            try
            {
                await Task.CompletedTask;
                var stringValue = Preferences.Get(key, defaultValue.ToString());
                return long.TryParse(stringValue, out long result) ? result : defaultValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences GetAsync (long) error: {ex.Message}");
                return defaultValue;
            }
        }
        #endregion

        #region DateTime Methods
        public async Task SetAsync(string key, DateTime value)
        {
            try
            {
                await Task.CompletedTask;
                Preferences.Set(key, value.ToString("O")); // ISO 8601 format
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences SetAsync (DateTime) error: {ex.Message}");
                throw;
            }
        }

        public async Task<DateTime> GetAsync(string key, DateTime defaultValue)
        {
            try
            {
                await Task.CompletedTask;
                var stringValue = Preferences.Get(key, defaultValue.ToString("O"));
                return DateTime.TryParse(stringValue, out DateTime result) ? result : defaultValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences GetAsync (DateTime) error: {ex.Message}");
                return defaultValue;
            }
        }
        #endregion

        #region Object Methods (JSON Serialization)
        public async Task SetObjectAsync<T>(string key, T value)
        {
            try
            {
                await Task.CompletedTask;
                if (value == null)
                {
                    Preferences.Remove(key);
                    return;
                }

                var jsonString = JsonSerializer.Serialize(value);
                Preferences.Set(key, jsonString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences SetObjectAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<T> GetObjectAsync<T>(string key, T defaultValue = default)
        {
            try
            {
                await Task.CompletedTask;
                var jsonString = Preferences.Get(key, string.Empty);

                if (string.IsNullOrEmpty(jsonString))
                    return defaultValue;

                return JsonSerializer.Deserialize<T>(jsonString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences GetObjectAsync error: {ex.Message}");
                return defaultValue;
            }
        }
        #endregion

        #region Collection Methods
        public async Task SetStringListAsync(string key, List<string> values)
        {
            try
            {
                await Task.CompletedTask;
                if (values == null)
                {
                    Preferences.Remove(key);
                    return;
                }

                var jsonString = JsonSerializer.Serialize(values);
                Preferences.Set(key, jsonString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences SetStringListAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<string>> GetStringListAsync(string key, List<string> defaultValue = null)
        {
            try
            {
                await Task.CompletedTask;
                var jsonString = Preferences.Get(key, string.Empty);

                if (string.IsNullOrEmpty(jsonString))
                    return defaultValue ?? new List<string>();

                return JsonSerializer.Deserialize<List<string>>(jsonString) ?? defaultValue ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences GetStringListAsync error: {ex.Message}");
                return defaultValue ?? new List<string>();
            }
        }
        #endregion

        #region Utility Methods
        public async Task RemoveAsync(string key)
        {
            try
            {
                await Task.CompletedTask;
                Preferences.Remove(key);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences RemoveAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task RemoveAsync(params string[] keys)
        {
            try
            {
                await Task.CompletedTask;
                foreach (var key in keys)
                {
                    Preferences.Remove(key);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences RemoveAsync (multiple) error: {ex.Message}");
                throw;
            }
        }

        public async Task ClearAllAsync()
        {
            try
            {
                await Task.CompletedTask;
                Preferences.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences ClearAllAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ContainsKeyAsync(string key)
        {
            try
            {
                await Task.CompletedTask;
                return Preferences.ContainsKey(key);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences ContainsKeyAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<List<string>> GetAllKeysAsync()
        {
            try
            {
                await Task.CompletedTask;
                // Note: .NET MAUI Preferences doesn't have a direct way to get all keys
                // This is a limitation of the platform. We'd need to implement our own storage for this.
                System.Diagnostics.Debug.WriteLine("GetAllKeysAsync: Not supported by platform Preferences");
                return new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences GetAllKeysAsync error: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<int> GetCountAsync()
        {
            try
            {
                await Task.CompletedTask;
                // Note: .NET MAUI Preferences doesn't have a direct way to count keys
                System.Diagnostics.Debug.WriteLine("GetCountAsync: Not supported by platform Preferences");
                return 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences GetCountAsync error: {ex.Message}");
                return 0;
            }
        }
        #endregion

        #region Security Methods
        public async Task SetSecureAsync(string key, string value)
        {
            try
            {
                await Task.CompletedTask;
                if (string.IsNullOrEmpty(value))
                {
                    Preferences.Remove(key);
                    return;
                }

                var encryptedValue = EncryptString(value, EncryptionKey);
                Preferences.Set(key, encryptedValue);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences SetSecureAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GetSecureAsync(string key, string defaultValue = "")
        {
            try
            {
                await Task.CompletedTask;
                var encryptedValue = Preferences.Get(key, string.Empty);

                if (string.IsNullOrEmpty(encryptedValue))
                    return defaultValue;

                return DecryptString(encryptedValue, EncryptionKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences GetSecureAsync error: {ex.Message}");
                return defaultValue;
            }
        }
        #endregion

        #region Backup/Restore Methods
        public async Task<string> ExportToJsonAsync()
        {
            try
            {
                await Task.CompletedTask;
                // Note: This is limited since we can't enumerate all keys in MAUI Preferences
                // You'd need to maintain a list of known keys or implement custom storage
                var exportData = new Dictionary<string, string>();

                System.Diagnostics.Debug.WriteLine("ExportToJsonAsync: Limited implementation - cannot enumerate all keys");
                return JsonSerializer.Serialize(exportData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences ExportToJsonAsync error: {ex.Message}");
                return "{}";
            }
        }

        public async Task ImportFromJsonAsync(string jsonData, bool overwriteExisting = false)
        {
            try
            {
                await Task.CompletedTask;
                if (string.IsNullOrEmpty(jsonData))
                    return;

                var importData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData);
                if (importData == null)
                    return;

                foreach (var kvp in importData)
                {
                    if (!overwriteExisting && Preferences.ContainsKey(kvp.Key))
                        continue;

                    Preferences.Set(kvp.Key, kvp.Value);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preferences ImportFromJsonAsync error: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region Private Helper Methods
        private static string EncryptString(string plainText, string key)
        {
            try
            {
                byte[] iv = new byte[16];
                byte[] array;

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                    aes.IV = iv;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                            {
                                streamWriter.Write(plainText);
                            }
                            array = memoryStream.ToArray();
                        }
                    }
                }

                return Convert.ToBase64String(array);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Encryption error: {ex.Message}");
                return plainText; // Fallback to plain text
            }
        }

        private static string DecryptString(string cipherText, string key)
        {
            try
            {
                byte[] iv = new byte[16];
                byte[] buffer = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                    aes.IV = iv;
                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream(buffer))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader streamReader = new StreamReader(cryptoStream))
                            {
                                return streamReader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Decryption error: {ex.Message}");
                return cipherText; // Fallback to cipher text
            }
        }
        #endregion
    }
}
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using SEFApp.Services.Interfaces;

namespace SEFApp.Services
{
    public class PreferencesService : IPreferencesService
    {
        private const string APP_PREFIX = "SEFApp_";
        private const string ENCRYPTION_KEY = "SEF_SECURE_KEY_2025"; // In production, use a proper key management system
        private readonly HashSet<string> _keyRegistry = new();

        #region String Methods
        public async Task SetAsync(string key, string value)
        {
            try
            {
                await Task.Run(() =>
                {
                    var prefKey = GetKey(key);
                    Preferences.Set(prefKey, value ?? string.Empty);
                    _keyRegistry.Add(key);
                });

                System.Diagnostics.Debug.WriteLine($"Preferences: Set string '{key}' = '{value?.Substring(0, Math.Min(value?.Length ?? 0, 20))}...'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Set string preference error for key '{key}': {ex.Message}");
            }
        }

        public async Task<string> GetAsync(string key, string defaultValue)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var prefKey = GetKey(key);
                    var value = Preferences.Get(prefKey, defaultValue ?? string.Empty);
                    System.Diagnostics.Debug.WriteLine($"Preferences: Get string '{key}' = '{value?.Substring(0, Math.Min(value?.Length ?? 0, 20))}...'");
                    return value;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get string preference error for key '{key}': {ex.Message}");
                return defaultValue ?? string.Empty;
            }
        }



        #region Boolean Methods
        public async Task SetAsync(string key, bool value)
        {
            try
            {
                await Task.Run(() =>
                {
                    var prefKey = GetKey(key);
                    Preferences.Set(prefKey, value);
                    _keyRegistry.Add(key);
                });

                System.Diagnostics.Debug.WriteLine($"Preferences: Set bool '{key}' = {value}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Set bool preference error for key '{key}': {ex.Message}");
            }
        }

        public async Task<bool> GetAsync(string key, bool defaultValue)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var prefKey = GetKey(key);
                    var value = Preferences.Get(prefKey, defaultValue);
                    System.Diagnostics.Debug.WriteLine($"Preferences: Get bool '{key}' = {value}");
                    return value;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get bool preference error for key '{key}': {ex.Message}");
                return defaultValue;
            }
        }
        #endregion

        #region Integer Methods
        public async Task SetAsync(string key, int value)
        {
            try
            {
                await Task.Run(() =>
                {
                    var prefKey = GetKey(key);
                    Preferences.Set(prefKey, value);
                    _keyRegistry.Add(key);
                });

                System.Diagnostics.Debug.WriteLine($"Preferences: Set int '{key}' = {value}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Set int preference error for key '{key}': {ex.Message}");
            }
        }

        public async Task<int> GetAsync(string key, int defaultValue)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var prefKey = GetKey(key);
                    var value = Preferences.Get(prefKey, defaultValue);
                    System.Diagnostics.Debug.WriteLine($"Preferences: Get int '{key}' = {value}");
                    return value;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get int preference error for key '{key}': {ex.Message}");
                return defaultValue;
            }
        }
        #endregion

        #region Double Methods
        public async Task SetAsync(string key, double value)
        {
            try
            {
                await Task.Run(() =>
                {
                    var prefKey = GetKey(key);
                    Preferences.Set(prefKey, value);
                    _keyRegistry.Add(key);
                });

                System.Diagnostics.Debug.WriteLine($"Preferences: Set double '{key}' = {value}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Set double preference error for key '{key}': {ex.Message}");
            }
        }

        public async Task<double> GetAsync(string key, double defaultValue)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var prefKey = GetKey(key);
                    var value = Preferences.Get(prefKey, defaultValue);
                    System.Diagnostics.Debug.WriteLine($"Preferences: Get double '{key}' = {value}");
                    return value;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get double preference error for key '{key}': {ex.Message}");
                return defaultValue;
            }
        }
        #endregion

        #region Long Methods
        public async Task SetAsync(string key, long value)
        {
            try
            {
                await Task.Run(() =>
                {
                    var prefKey = GetKey(key);
                    Preferences.Set(prefKey, value);
                    _keyRegistry.Add(key);
                });

                System.Diagnostics.Debug.WriteLine($"Preferences: Set long '{key}' = {value}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Set long preference error for key '{key}': {ex.Message}");
            }
        }

        public async Task<long> GetAsync(string key, long defaultValue)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var prefKey = GetKey(key);
                    var value = Preferences.Get(prefKey, defaultValue);
                    System.Diagnostics.Debug.WriteLine($"Preferences: Get long '{key}' = {value}");
                    return value;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get long preference error for key '{key}': {ex.Message}");
                return defaultValue;
            }
        }
        #endregion

        #region DateTime Methods
        public async Task SetAsync(string key, DateTime value)
        {
            try
            {
                var binaryValue = value.ToBinary();
                await SetAsync(key + "_DateTime", binaryValue.ToString());
                System.Diagnostics.Debug.WriteLine($"Preferences: Set DateTime '{key}' = {value}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Set DateTime preference error for key '{key}': {ex.Message}");
            }
        }

        public async Task<DateTime> GetAsync(string key, DateTime defaultValue)
        {
            try
            {
                var storedValue = await GetAsync(key + "_DateTime", string.Empty);
                if (!string.IsNullOrEmpty(storedValue) && long.TryParse(storedValue, out long binary))
                {
                    var value = DateTime.FromBinary(binary);
                    System.Diagnostics.Debug.WriteLine($"Preferences: Get DateTime '{key}' = {value}");
                    return value;
                }

                System.Diagnostics.Debug.WriteLine($"Preferences: Get DateTime '{key}' = {defaultValue} (default)");
                return defaultValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get DateTime preference error for key '{key}': {ex.Message}");
                return defaultValue;
            }
        }
        #endregion

        #region Object Methods (JSON Serialization)
        public async Task SetObjectAsync<T>(string key, T value)
        {
            try
            {
                if (value == null)
                {
                    await RemoveAsync(key);
                    return;
                }

                var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await SetAsync(key + "_Object", json);
                System.Diagnostics.Debug.WriteLine($"Preferences: Set object '{key}' of type {typeof(T).Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Set object preference error for key '{key}': {ex.Message}");
            }
        }

        public async Task<T> GetObjectAsync<T>(string key, T defaultValue = default)
        {
            try
            {
                var json = await GetAsync(key + "_Object", string.Empty);
                if (string.IsNullOrEmpty(json))
                {
                    System.Diagnostics.Debug.WriteLine($"Preferences: Get object '{key}' = default (no data)");
                    return defaultValue;
                }

                var value = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                System.Diagnostics.Debug.WriteLine($"Preferences: Get object '{key}' of type {typeof(T).Name}");
                return value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get object preference error for key '{key}': {ex.Message}");
                return defaultValue;
            }
        }
        #endregion

        #region Collection Methods
        public async Task SetStringListAsync(string key, List<string> values)
        {
            try
            {
                if (values == null)
                {
                    await RemoveAsync(key);
                    return;
                }

                var json = JsonSerializer.Serialize(values);
                await SetAsync(key + "_StringList", json);
                System.Diagnostics.Debug.WriteLine($"Preferences: Set string list '{key}' with {values.Count} items");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Set string list preference error for key '{key}': {ex.Message}");
            }
        }

        public async Task<List<string>> GetStringListAsync(string key, List<string> defaultValue = null)
        {
            try
            {
                var json = await GetAsync(key + "_StringList", string.Empty);
                if (string.IsNullOrEmpty(json))
                {
                    System.Diagnostics.Debug.WriteLine($"Preferences: Get string list '{key}' = default (no data)");
                    return defaultValue ?? new List<string>();
                }

                var value = JsonSerializer.Deserialize<List<string>>(json);
                System.Diagnostics.Debug.WriteLine($"Preferences: Get string list '{key}' with {value?.Count ?? 0} items");
                return value ?? defaultValue ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get string list preference error for key '{key}': {ex.Message}");
                return defaultValue ?? new List<string>();
            }
        }
        #endregion

        #region Utility Methods
        public async Task RemoveAsync(string key)
        {
            try
            {
                await Task.Run(() =>
                {
                    var prefKey = GetKey(key);
                    Preferences.Remove(prefKey);

                    // Also remove related keys
                    Preferences.Remove(prefKey + "_DateTime");
                    Preferences.Remove(prefKey + "_Object");
                    Preferences.Remove(prefKey + "_StringList");

                    _keyRegistry.Remove(key);
                });

                System.Diagnostics.Debug.WriteLine($"Preferences: Removed '{key}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Remove preference error for key '{key}': {ex.Message}");
            }
        }

        public async Task RemoveAsync(params string[] keys)
        {
            try
            {
                if (keys == null || keys.Length == 0)
                    return;

                foreach (var key in keys)
                {
                    await RemoveAsync(key);
                }

                System.Diagnostics.Debug.WriteLine($"Preferences: Removed {keys.Length} keys");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Remove multiple preferences error: {ex.Message}");
            }
        }

        public async Task ClearAllAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    Preferences.Clear();
                    _keyRegistry.Clear();
                });

                System.Diagnostics.Debug.WriteLine("Preferences: Cleared all preferences");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clear all preferences error: {ex.Message}");
            }
        }

        public async Task<bool> ContainsKeyAsync(string key)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var prefKey = GetKey(key);
                    var exists = Preferences.ContainsKey(prefKey);
                    System.Diagnostics.Debug.WriteLine($"Preferences: Key '{key}' exists = {exists}");
                    return exists;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Contains key error for '{key}': {ex.Message}");
                return false;
            }
        }

        public async Task<List<string>> GetAllKeysAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    var keys = _keyRegistry.ToList();
                    System.Diagnostics.Debug.WriteLine($"Preferences: Retrieved {keys.Count} keys");
                    return keys;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get all keys error: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<int> GetCountAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    var count = _keyRegistry.Count;
                    System.Diagnostics.Debug.WriteLine($"Preferences: Total count = {count}");
                    return count;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get count error: {ex.Message}");
                return 0;
            }
        }
        #endregion

        #region Security Methods
        public async Task SetSecureAsync(string key, string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                {
                    await RemoveAsync(key + "_Secure");
                    return;
                }

                var encryptedValue = EncryptString(value);
                await SetAsync(key + "_Secure", encryptedValue);
                System.Diagnostics.Debug.WriteLine($"Preferences: Set secure '{key}' (encrypted)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Set secure preference error for key '{key}': {ex.Message}");
            }
        }

        public async Task<string> GetSecureAsync(string key, string defaultValue = "")
        {
            try
            {
                var encryptedValue = await GetAsync(key + "_Secure", string.Empty);
                if (string.IsNullOrEmpty(encryptedValue))
                {
                    System.Diagnostics.Debug.WriteLine($"Preferences: Get secure '{key}' = default (no data)");
                    return defaultValue;
                }

                var decryptedValue = DecryptString(encryptedValue);
                System.Diagnostics.Debug.WriteLine($"Preferences: Get secure '{key}' (decrypted)");
                return decryptedValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get secure preference error for key '{key}': {ex.Message}");
                return defaultValue;
            }
        }
        #endregion

        #region Backup/Restore Methods
        public async Task<string> ExportToJsonAsync()
        {
            try
            {
                var exportData = new Dictionary<string, object>();
                var keys = await GetAllKeysAsync();

                foreach (var key in keys)
                {
                    // Skip secure keys in export for security
                    if (key.EndsWith("_Secure"))
                        continue;

                    var prefKey = GetKey(key);
                    if (Preferences.ContainsKey(prefKey))
                    {
                        // Try to get the raw value
                        var stringValue = Preferences.Get(prefKey, string.Empty);
                        if (!string.IsNullOrEmpty(stringValue))
                        {
                            exportData[key] = stringValue;
                        }
                    }
                }

                var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                System.Diagnostics.Debug.WriteLine($"Preferences: Exported {exportData.Count} preferences to JSON");
                return json;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Export to JSON error: {ex.Message}");
                return "{}";
            }
        }

        public async Task ImportFromJsonAsync(string jsonData, bool overwriteExisting = false)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonData))
                    return;

                var importData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);
                if (importData == null)
                    return;

                int importedCount = 0;
                foreach (var kvp in importData)
                {
                    var key = kvp.Key;
                    var value = kvp.Value?.ToString() ?? string.Empty;

                    // Skip if key exists and not overwriting
                    if (!overwriteExisting && await ContainsKeyAsync(key))
                        continue;

                    await SetAsync(key, value);
                    importedCount++;
                }

                System.Diagnostics.Debug.WriteLine($"Preferences: Imported {importedCount} preferences from JSON");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Import from JSON error: {ex.Message}");
            }
        }
        #endregion

        #region Private Helper Methods
        /// <summary>
        /// Add app prefix to keys to avoid conflicts
        /// </summary>
        private string GetKey(string key)
        {
            return $"{APP_PREFIX}{key}";
        }

        /// <summary>
        /// Simple string encryption (for demonstration - use proper encryption in production)
        /// </summary>
        private string EncryptString(string plainText)
        {
            try
            {
                var data = Encoding.UTF8.GetBytes(plainText);
                var key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY.PadRight(32).Substring(0, 32));

                using var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor();
                var encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);

                // Combine IV and encrypted data
                var result = new byte[aes.IV.Length + encryptedData.Length];
                Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
                Array.Copy(encryptedData, 0, result, aes.IV.Length, encryptedData.Length);

                return Convert.ToBase64String(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Encryption error: {ex.Message}");
                return plainText; // Fallback to plain text
            }
        }

        /// <summary>
        /// Simple string decryption (for demonstration - use proper encryption in production)
        /// </summary>
        private string DecryptString(string cipherText)
        {
            try
            {
                var data = Convert.FromBase64String(cipherText);
                var key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY.PadRight(32).Substring(0, 32));

                using var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = CipherMode.CBC;

                // Extract IV
                var iv = new byte[16];
                Array.Copy(data, 0, iv, 0, 16);
                aes.IV = iv;

                // Extract encrypted data
                var encryptedData = new byte[data.Length - 16];
                Array.Copy(data, 16, encryptedData, 0, encryptedData.Length);

                using var decryptor = aes.CreateDecryptor();
                var decryptedData = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

                return Encoding.UTF8.GetString(decryptedData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Decryption error: {ex.Message}");
                return cipherText; // Fallback to cipher text
            }
        }
        #endregion
    }
    #endregion
}
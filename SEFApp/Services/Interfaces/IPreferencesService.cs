namespace SEFApp.Services.Interfaces
{
    public interface IPreferencesService
    {
        #region String Methods
        /// <summary>
        /// Store a string value
        /// </summary>
        /// <param name="key">The key to store the value under</param>
        /// <param name="value">The string value to store</param>
        Task SetAsync(string key, string value);

        /// <summary>
        /// Retrieve a string value
        /// </summary>
        /// <param name="key">The key to retrieve</param>
        /// <param name="defaultValue">Default value if key doesn't exist</param>
        /// <returns>The stored string or default value</returns>
        Task<string> GetAsync(string key, string defaultValue);
        #endregion

        #region Boolean Methods
        /// <summary>
        /// Store a boolean value
        /// </summary>
        /// <param name="key">The key to store the value under</param>
        /// <param name="value">The boolean value to store</param>
        Task SetAsync(string key, bool value);

        /// <summary>
        /// Retrieve a boolean value
        /// </summary>
        /// <param name="key">The key to retrieve</param>
        /// <param name="defaultValue">Default value if key doesn't exist</param>
        /// <returns>The stored boolean or default value</returns>
        Task<bool> GetAsync(string key, bool defaultValue);
        #endregion

        #region Integer Methods
        /// <summary>
        /// Store an integer value
        /// </summary>
        /// <param name="key">The key to store the value under</param>
        /// <param name="value">The integer value to store</param>
        Task SetAsync(string key, int value);

        /// <summary>
        /// Retrieve an integer value
        /// </summary>
        /// <param name="key">The key to retrieve</param>
        /// <param name="defaultValue">Default value if key doesn't exist</param>
        /// <returns>The stored integer or default value</returns>
        Task<int> GetAsync(string key, int defaultValue);
        #endregion

        #region Double Methods
        /// <summary>
        /// Store a double value
        /// </summary>
        /// <param name="key">The key to store the value under</param>
        /// <param name="value">The double value to store</param>
        Task SetAsync(string key, double value);

        /// <summary>
        /// Retrieve a double value
        /// </summary>
        /// <param name="key">The key to retrieve</param>
        /// <param name="defaultValue">Default value if key doesn't exist</param>
        /// <returns>The stored double or default value</returns>
        Task<double> GetAsync(string key, double defaultValue);
        #endregion

        #region Long Methods
        /// <summary>
        /// Store a long value
        /// </summary>
        /// <param name="key">The key to store the value under</param>
        /// <param name="value">The long value to store</param>
        Task SetAsync(string key, long value);

        /// <summary>
        /// Retrieve a long value
        /// </summary>
        /// <param name="key">The key to retrieve</param>
        /// <param name="defaultValue">Default value if key doesn't exist</param>
        /// <returns>The stored long or default value</returns>
        Task<long> GetAsync(string key, long defaultValue);
        #endregion

        #region DateTime Methods
        /// <summary>
        /// Store a DateTime value
        /// </summary>
        /// <param name="key">The key to store the value under</param>
        /// <param name="value">The DateTime value to store</param>
        Task SetAsync(string key, DateTime value);

        /// <summary>
        /// Retrieve a DateTime value
        /// </summary>
        /// <param name="key">The key to retrieve</param>
        /// <param name="defaultValue">Default value if key doesn't exist</param>
        /// <returns>The stored DateTime or default value</returns>
        Task<DateTime> GetAsync(string key, DateTime defaultValue);
        #endregion

        #region Object Methods (JSON Serialization)
        /// <summary>
        /// Store an object as JSON
        /// </summary>
        /// <typeparam name="T">Type of object to store</typeparam>
        /// <param name="key">The key to store the object under</param>
        /// <param name="value">The object to store</param>
        Task SetObjectAsync<T>(string key, T value);

        /// <summary>
        /// Retrieve an object from JSON
        /// </summary>
        /// <typeparam name="T">Type of object to retrieve</typeparam>
        /// <param name="key">The key to retrieve</param>
        /// <param name="defaultValue">Default value if key doesn't exist</param>
        /// <returns>The stored object or default value</returns>
        Task<T> GetObjectAsync<T>(string key, T defaultValue = default);
        #endregion

        #region Collection Methods
        /// <summary>
        /// Store a list of strings
        /// </summary>
        /// <param name="key">The key to store the list under</param>
        /// <param name="values">List of strings to store</param>
        Task SetStringListAsync(string key, List<string> values);

        /// <summary>
        /// Retrieve a list of strings
        /// </summary>
        /// <param name="key">The key to retrieve</param>
        /// <param name="defaultValue">Default value if key doesn't exist</param>
        /// <returns>The stored list or default value</returns>
        Task<List<string>> GetStringListAsync(string key, List<string> defaultValue = null);
        #endregion

        #region Utility Methods
        /// <summary>
        /// Remove a specific key and its value
        /// </summary>
        /// <param name="key">The key to remove</param>
        Task RemoveAsync(string key);

        /// <summary>
        /// Remove multiple keys at once
        /// </summary>
        /// <param name="keys">Array of keys to remove</param>
        Task RemoveAsync(params string[] keys);

        /// <summary>
        /// Clear all stored preferences
        /// </summary>
        Task ClearAllAsync();

        /// <summary>
        /// Check if a key exists
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if key exists, false otherwise</returns>
        Task<bool> ContainsKeyAsync(string key);

        /// <summary>
        /// Get all stored keys (for debugging/admin purposes)
        /// </summary>
        /// <returns>List of all keys</returns>
        Task<List<string>> GetAllKeysAsync();

        /// <summary>
        /// Get the total number of stored preferences
        /// </summary>
        /// <returns>Count of stored preferences</returns>
        Task<int> GetCountAsync();
        #endregion

        #region Security Methods
        /// <summary>
        /// Store a secure string (encrypted)
        /// </summary>
        /// <param name="key">The key to store the value under</param>
        /// <param name="value">The string value to encrypt and store</param>
        Task SetSecureAsync(string key, string value);

        /// <summary>
        /// Retrieve a secure string (decrypted)
        /// </summary>
        /// <param name="key">The key to retrieve</param>
        /// <param name="defaultValue">Default value if key doesn't exist</param>
        /// <returns>The decrypted string or default value</returns>
        Task<string> GetSecureAsync(string key, string defaultValue = "");
        #endregion

        #region Backup/Restore Methods
        /// <summary>
        /// Export all preferences to a JSON string
        /// </summary>
        /// <returns>JSON string containing all preferences</returns>
        Task<string> ExportToJsonAsync();

        /// <summary>
        /// Import preferences from a JSON string
        /// </summary>
        /// <param name="jsonData">JSON string containing preferences</param>
        /// <param name="overwriteExisting">Whether to overwrite existing keys</param>
        Task ImportFromJsonAsync(string jsonData, bool overwriteExisting = false);
        #endregion
    }
}
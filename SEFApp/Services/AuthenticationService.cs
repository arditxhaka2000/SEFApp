using SEFApp.Models;
using SEFApp.Models.Database;
using SEFApp.Services.Interfaces;

namespace SEFApp.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IPreferencesService _preferencesService;
        private readonly IDatabaseService _databaseService;
        private User _currentUser;

        public AuthenticationService(IPreferencesService preferencesService, IDatabaseService databaseService)
        {
            _preferencesService = preferencesService;
            _databaseService = databaseService;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return false;
                }

                // Initialize database for this user - this is where the magic happens!
                await _databaseService.InitializeDatabaseForUser(username, password);

                // Validate password
                var isValid = await _databaseService.ValidatePasswordAsync(username, password);
                if (isValid)
                {
                    var user = await _databaseService.GetUserByUsernameAsync(username);

                    if (user != null && user.IsActive)
                    {
                        _currentUser = user;

                        user.LastLoginDate = DateTime.Now;
                        await _databaseService.UpdateUserAsync(user);

                        await _preferencesService.SetAsync("IsAuthenticated", true);
                        await _preferencesService.SetAsync("CurrentUserId", user.Id);
                        await _preferencesService.SetAsync("CurrentUsername", user.Username);
                        await _preferencesService.SetAsync("LoginTime", DateTime.Now);

                        await _databaseService.LogActionAsync("Users", "LOGIN", user.Id.ToString(), null,
                            new { LoginTime = DateTime.Now, IPAddress = "Local" }, user.Id);

                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> LogoutAsync()
        {
            try
            {
                if (_currentUser != null)
                {
                    // Log the logout
                    await _databaseService.LogActionAsync("Users", "LOGOUT", _currentUser.Id.ToString(), null,
                        new { LogoutTime = DateTime.Now }, _currentUser.Id);
                }

                // Clear authentication state
                await _preferencesService.RemoveAsync("IsAuthenticated");
                await _preferencesService.RemoveAsync("CurrentUserId");
                await _preferencesService.RemoveAsync("CurrentUsername");
                await _preferencesService.RemoveAsync("LoginTime");

                _currentUser = null;

                System.Diagnostics.Debug.WriteLine("Logout successful");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var isAuthenticated = await _preferencesService.GetAsync("IsAuthenticated", false);
                var userId = await _preferencesService.GetAsync("CurrentUserId", 0);

                if (isAuthenticated && userId > 0)
                {
                    // Verify user still exists and is active
                    var user = await _databaseService.GetUserByIdAsync(userId);
                    if (user != null && user.IsActive)
                    {
                        _currentUser = user;
                        return true;
                    }
                    else
                    {
                        // User no longer exists or is inactive, clear auth state
                        await LogoutAsync();
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IsAuthenticated check error: {ex.Message}");
                return false;
            }
        }

        public async Task<User> GetCurrentUserAsync()
        {
            try
            {
                if (_currentUser != null)
                    return _currentUser;

                var userId = await _preferencesService.GetAsync("CurrentUserId", 0);
                if (userId > 0)
                {
                    _currentUser = await _databaseService.GetUserByIdAsync(userId);
                }

                return _currentUser;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCurrentUser error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            try
            {
                if (_currentUser == null)
                    return false;

                // Verify current password
                var isCurrentPasswordValid = await _databaseService.ValidatePasswordAsync(_currentUser.Username, currentPassword);
                if (!isCurrentPasswordValid)
                    return false;

                // Update password
                var success = await _databaseService.UpdatePasswordAsync(_currentUser.Id, newPassword);

                if (success)
                {
                    // Log password change
                    await _databaseService.LogActionAsync("Users", "PASSWORD_CHANGE", _currentUser.Id.ToString(), null,
                        new { ChangeTime = DateTime.Now }, _currentUser.Id);
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Change password error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RegisterUserAsync(string username, string password, string fullName, string role = "User")
        {
            try
            {
                // Check if username already exists
                var existingUser = await _databaseService.GetUserByUsernameAsync(username);
                if (existingUser != null)
                {
                    System.Diagnostics.Debug.WriteLine("Registration failed: Username already exists");
                    return false;
                }

                // Create new user
                var newUser = new User
                {
                    Username = username,
                    FullName = fullName,
                    Role = role,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                var createdUser = await _databaseService.CreateUserAsync(newUser, password);

                System.Diagnostics.Debug.WriteLine($"User registered successfully: {username}");
                return createdUser != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Registration error: {ex.Message}");
                return false;
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                return await _databaseService.GetAllUsersAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAllUsers error: {ex.Message}");
                return new List<User>();
            }
        }

        public async Task<bool> IsFirstRunAsync()
        {
            try
            {
                // Check if encrypted database file exists
                var databasePath = Path.Combine(FileSystem.AppDataDirectory, "sefmanager.db");
                return !File.Exists(databasePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IsFirstRun check error: {ex.Message}");
                return true; // Assume first run on error
            }
        }
    }
}
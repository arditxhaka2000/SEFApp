using SEFApp.Services.Interfaces;
using SEFApp.ViewModels;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SEFApp.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IPreferencesService _preferencesService;

        // Remove HttpClient dependency for now
        public AuthenticationService(IPreferencesService preferencesService)
        {
            _preferencesService = preferencesService;
        }

        public async Task<LoginResult> LoginAsync(LoginRequest request)
        {
            try
            {
                // Check if this is the first account (admin setup)
                var isFirstAccount = await IsFirstAccountAsync();

                if (isFirstAccount)
                {
                    return await CreateAdminAccountAsync(request);
                }

                // Check against stored admin credentials
                var storedUsername = await _preferencesService.GetAsync("admin_username", string.Empty);
                var storedPasswordHash = await _preferencesService.GetAsync("admin_password", string.Empty);

                if (request.Username == storedUsername && VerifyPassword(request.Password, storedPasswordHash))
                {
                    var adminUser = new UserInfo
                    {
                        Id = "admin-001",
                        Username = request.Username,
                        FullName = "Administrator",
                        Role = UserRole.Administrator,
                        CreatedDate = DateTime.Now,
                        IsFirstAccount = true
                    };

                    var token = GenerateToken(adminUser);
                    await _preferencesService.SetAsync("auth_token", token);
                    await _preferencesService.SetAsync("user_info", JsonSerializer.Serialize(adminUser));

                    return new LoginResult
                    {
                        IsSuccess = true,
                        Token = token,
                        User = adminUser
                    };
                }

                return new LoginResult
                {
                    IsSuccess = false,
                    ErrorCode = "INVALID_CREDENTIALS"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                return new LoginResult { IsSuccess = false, ErrorCode = "SERVER_ERROR" };
            }
        }

        public async Task<bool> LogoutAsync()
        {
            try
            {
                await _preferencesService.RemoveAsync("auth_token");
                await _preferencesService.RemoveAsync("user_info");
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
            var token = await _preferencesService.GetAsync("auth_token", string.Empty);
            return !string.IsNullOrEmpty(token);
        }

        public async Task<UserInfo> GetCurrentUserAsync()
        {
            try
            {
                var userJson = await _preferencesService.GetAsync("user_info", string.Empty);
                if (!string.IsNullOrEmpty(userJson))
                {
                    return JsonSerializer.Deserialize<UserInfo>(userJson);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get current user error: {ex.Message}");
            }

            return null;
        }

        private async Task<bool> IsFirstAccountAsync()
        {
            var hasAccounts = await _preferencesService.GetAsync("has_accounts", false);
            return !hasAccounts;
        }

        private async Task<LoginResult> CreateAdminAccountAsync(LoginRequest request)
        {
            try
            {
                // Validate admin account creation
                if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
                {
                    return new LoginResult { IsSuccess = false, ErrorCode = "INVALID_USERNAME" };
                }

                if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 4)
                {
                    return new LoginResult { IsSuccess = false, ErrorCode = "INVALID_PASSWORD" };
                }

                // Create the first admin account
                var adminUser = new UserInfo
                {
                    Id = "admin-001",
                    Username = request.Username,
                    FullName = "Administrator",
                    Role = UserRole.Administrator,
                    CreatedDate = DateTime.Now,
                    IsFirstAccount = true
                };

                // Store admin credentials
                var hashedPassword = HashPassword(request.Password);
                await _preferencesService.SetAsync("admin_username", request.Username);
                await _preferencesService.SetAsync("admin_password", hashedPassword);
                await _preferencesService.SetAsync("has_accounts", true);
                await _preferencesService.SetAsync("admin_created_date", DateTime.Now.ToString());

                // Generate token for admin
                var token = GenerateToken(adminUser);
                await _preferencesService.SetAsync("auth_token", token);
                await _preferencesService.SetAsync("user_info", JsonSerializer.Serialize(adminUser));

                return new LoginResult
                {
                    IsSuccess = true,
                    Token = token,
                    User = adminUser
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Admin creation error: {ex.Message}");
                return new LoginResult { IsSuccess = false, ErrorCode = "ADMIN_CREATION_FAILED" };
            }
        }

        private string GenerateToken(UserInfo user)
        {
            var tokenData = new
            {
                UserId = user.Id,
                Username = user.Username,
                Role = user.Role.ToString(),
                IssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds()
            };

            var tokenJson = JsonSerializer.Serialize(tokenData);
            var tokenBytes = Encoding.UTF8.GetBytes(tokenJson);
            return Convert.ToBase64String(tokenBytes);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "SEF_SALT"));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            var hashToVerify = HashPassword(password);
            return hashToVerify == hashedPassword;
        }
    }
}

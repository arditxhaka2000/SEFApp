using SEFApp.Services.Interfaces;
using SEFApp.ViewModels;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SEFApp.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<LoginResult> LoginAsync(LoginRequest request);
        Task<bool> LogoutAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<UserInfo> GetCurrentUserAsync();
    }
}

using SEFApp.Models.Database;
using SEFApp.Services.Interfaces;
using SEFApp.ViewModels;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SEFApp.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<bool> LoginAsync(string username, string password);
        Task<bool> LogoutAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<User> GetCurrentUserAsync(); 
        Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);
        Task<bool> RegisterUserAsync(string username, string password, string fullName, string role = "User");
        Task<List<User>> GetAllUsersAsync();
        Task<bool> IsFirstRunAsync();
    }
}

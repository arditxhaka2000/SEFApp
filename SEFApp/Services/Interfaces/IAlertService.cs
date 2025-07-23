using SEFApp.Services.Interfaces;

namespace SEFApp.Services.Interfaces
{
    public interface IAlertService
    {
        Task ShowAlertAsync(string title, string message, string cancel = "OK");
        Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No");
        Task<string> ShowActionSheetAsync(string title, string cancel, string destruction, params string[] buttons);
        Task ShowLoadingAsync(string message = "Loading...");
        Task HideLoadingAsync();
        Task ShowToastAsync(string message, int duration = 3000);
        Task ShowErrorAsync(string message, string title = "Error");
        Task ShowSuccessAsync(string message, string title = "Success");
        Task ShowWarningAsync(string message, string title = "Warning");
        Task ShowInfoAsync(string message, string title = "Information");
    }
}
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
    }
}

namespace SEFApp.Services
{
    public class AlertService : IAlertService
    {
        private Page CurrentPage => Application.Current?.MainPage ?? Shell.Current;
        private bool _isLoadingVisible = false;

        public async Task ShowAlertAsync(string title, string message, string cancel = "OK")
        {
            try
            {
                if (CurrentPage != null)
                {
                    await CurrentPage.DisplayAlert(title, message, cancel);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Alert: {title} - {message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowAlert error: {ex.Message}");
            }
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No")
        {
            try
            {
                if (CurrentPage != null)
                {
                    return await CurrentPage.DisplayAlert(title, message, accept, cancel);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Confirmation: {title} - {message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowConfirmation error: {ex.Message}");
                return false;
            }
        }

        public async Task<string> ShowActionSheetAsync(string title, string cancel, string destruction, params string[] buttons)
        {
            try
            {
                if (CurrentPage != null)
                {
                    return await CurrentPage.DisplayActionSheet(title, cancel, destruction, buttons);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ActionSheet: {title}");
                    return cancel;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowActionSheet error: {ex.Message}");
                return cancel;
            }
        }

        public async Task ShowLoadingAsync(string message = "Loading...")
        {
            try
            {
                if (_isLoadingVisible) return;

                _isLoadingVisible = true;

                // Create a simple loading overlay
                var loadingPage = new ContentPage
                {
                    BackgroundColor = Colors.Black.WithAlpha(0.7f),
                    Content = new StackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        Children =
                        {
                            new ActivityIndicator
                            {
                                IsRunning = true,
                                Color = Colors.White,
                                WidthRequest = 50,
                                HeightRequest = 50
                            },
                            new Label
                            {
                                Text = message,
                                TextColor = Colors.White,
                                FontSize = 16,
                                HorizontalOptions = LayoutOptions.Center,
                                Margin = new Thickness(0, 10, 0, 0)
                            }
                        }
                    }
                };

                if (CurrentPage != null)
                {
                    await CurrentPage.Navigation.PushModalAsync(loadingPage, false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowLoading error: {ex.Message}");
                _isLoadingVisible = false;
            }
        }

        public async Task HideLoadingAsync()
        {
            try
            {
                if (!_isLoadingVisible) return;

                if (CurrentPage?.Navigation?.ModalStack?.Count > 0)
                {
                    await CurrentPage.Navigation.PopModalAsync(false);
                }

                _isLoadingVisible = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HideLoading error: {ex.Message}");
                _isLoadingVisible = false;
            }
        }

        public async Task ShowToastAsync(string message, int duration = 3000)
        {
            try
            {
                // Simple toast implementation using DisplayAlert with timer
                _ = Task.Run(async () =>
                {
                    await ShowAlertAsync("Info", message, "OK");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowToast error: {ex.Message}");
            }
        }

        public async Task ShowErrorAsync(string message, string title = "Error")
        {
            await ShowAlertAsync(title, message, "OK");
        }

        public async Task ShowSuccessAsync(string message, string title = "Success")
        {
            await ShowAlertAsync(title, message, "OK");
        }

        public async Task ShowWarningAsync(string message, string title = "Warning")
        {
            await ShowAlertAsync(title, message, "OK");
        }
    }
}
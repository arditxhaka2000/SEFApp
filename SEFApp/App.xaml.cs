using SEFApp.Services.Interfaces;
using SEFApp.Views;

namespace SEFApp;

public partial class App : Application
{
    private readonly IAuthenticationService _authService;

    public App(IAuthenticationService authService)
    {
        InitializeComponent();
        _authService = authService;

        // Start with login page by default
        MainPage = new AppShell();
    }

    protected override async void OnStart()
    {
        // Handle initial navigation here instead of constructor
        await NavigateToInitialPage();
    }

    private async Task NavigateToInitialPage()
    {
        try
        {
            // Simple check without complex logic
            bool isAuthenticated = await _authService.IsAuthenticatedAsync();

            if (isAuthenticated)
            {
                System.Diagnostics.Debug.WriteLine("User authenticated - staying in main app");
                // Already on AppShell, navigate to main content
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("User not authenticated - going to login");
                // Navigate to login
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Initial navigation error: {ex.Message}");
            // Default to login on any error
            try
            {
                await Shell.Current.GoToAsync("//LoginPage");
            }
            catch
            {
                // Last resort - direct page assignment
                MainPage = new LoginPage(Handler.MauiContext?.Services?.GetService<ViewModels.LoginViewModel>());
            }
        }
    }
}
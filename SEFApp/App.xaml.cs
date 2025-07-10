using SEFApp.Services;
using SEFApp.Services.Interfaces;

namespace SEFApp
{
    public partial class App : Application
    {
        private readonly IAuthenticationService _authService;
        private readonly IDatabaseService _databaseService;
        private readonly IAlertService _alertService;
        private readonly AppShell _appShell;

        public App(IAuthenticationService authService, IDatabaseService databaseService, IAlertService alertService, AppShell appShell)
        {
            InitializeComponent();
            _authService = authService;
            _databaseService = databaseService;
            _alertService = alertService;
            _appShell = appShell;

            MainPage = _appShell;
        }

        protected override async void OnStart()
        {
            await InitializeApplicationAsync();
        }

        protected override async void OnSleep()
        {
            base.OnSleep();
        }

        private async Task InitializeApplicationAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting application initialization...");

                // Show loading
                await _alertService.ShowLoadingAsync("Initializing application...");

                // Check if encrypted database exists to determine if this is first run
                var databasePath = Path.Combine(FileSystem.AppDataDirectory, "sefmanager.db");
                var isFirstRun = !File.Exists(databasePath);

                if (isFirstRun)
                {
                    await _alertService.HideLoadingAsync();
                    await HandleFirstRunAsync();
                    return;
                }

                // Check if user has stored credentials
                System.Diagnostics.Debug.WriteLine("Checking authentication status...");
                var isAuthenticated = await _authService.IsAuthenticatedAsync();

                // Hide loading
                await _alertService.HideLoadingAsync();

                // Navigate based on authentication status
                if (isAuthenticated)
                {
                    var currentUser = await _authService.GetCurrentUserAsync();
                    System.Diagnostics.Debug.WriteLine($"User authenticated: {currentUser?.Username} - navigating to dashboard");
                    await Shell.Current.GoToAsync("DashboardPage");

                    // Show welcome message
                    await _alertService.ShowSuccessAsync($"Welcome back, {currentUser?.FullName ?? currentUser?.Username}!", "Welcome");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("User not authenticated - navigating to login");
                    await Shell.Current.GoToAsync("//LoginPage");
                }
            }
            catch (Exception ex)
            {
                await HandleInitializationError(ex);
            }
        }

        private async Task HandleFirstRunAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("First run detected");

                // Show welcome message
                await _alertService.ShowAlertAsync(
                    "Welcome to SEF Manager",
                    "This is your first time running SEF Manager. Please use the default admin credentials:\n\n" +
                    "Username: admin\n" +
                    "Password: admin123\n\n" +
                    "Please change these credentials after logging in.",
                    "Continue"
                );

                // Navigate to login
                await Shell.Current.GoToAsync("//LoginPage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"First run setup error: {ex.Message}");
                await _alertService.ShowErrorAsync(
                    "There was an error setting up the application for first use. Please restart the app.",
                    "Setup Error"
                );
            }
        }

        private async Task HandleInitializationError(Exception ex)
        {
            try
            {
                // Hide loading if visible
                await _alertService.HideLoadingAsync();

                System.Diagnostics.Debug.WriteLine($"App initialization error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Show user-friendly error message
                var errorMessage = "Failed to initialize the application. This could be due to:\n\n" +
                                 "• Database initialization error\n" +
                                 "• File system permissions\n" +
                                 "• Corrupted data\n\n" +
                                 "Please try restarting the application.";

                var retry = await _alertService.ShowConfirmationAsync(
                    "Initialization Error",
                    errorMessage,
                    "Retry",
                    "Exit"
                );

                if (retry)
                {
                    await InitializeApplicationAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("User chose to exit after initialization error");
                    Application.Current?.Quit();
                }
            }
            catch (Exception alertEx)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing alert: {alertEx.Message}");
                try
                {
                    await Shell.Current.GoToAsync("//LoginPage");
                }
                catch (Exception navEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Critical error - cannot navigate: {navEx.Message}");
                }
            }
        }

        protected override async void OnResume()
        {
            System.Diagnostics.Debug.WriteLine("App resuming");
            try
            {
                // Only check auth if database is already initialized
                if (_databaseService is DatabaseService dbService && await dbService.IsDatabaseInitializedAsync())
                {
                    var isAuthenticated = await _authService.IsAuthenticatedAsync();
                    if (!isAuthenticated)
                    {
                        await Shell.Current.GoToAsync("//LoginPage");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnResume error: {ex.Message}");
            }
        }
    }
}
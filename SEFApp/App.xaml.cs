using SEFApp.Services.Interfaces;

namespace SEFApp
{
    public partial class App : Application
    {
        private readonly IAuthenticationService _authService;
        private readonly IDatabaseService _databaseService;
        private readonly IAlertService _alertService;

        public App(IAuthenticationService authService, IDatabaseService databaseService, IAlertService alertService)
        {
            InitializeComponent();

            _authService = authService;
            _databaseService = databaseService;
            _alertService = alertService;

            // Set initial shell
            MainPage = new AppShell();
        }

        protected override async void OnStart()
        {
            await InitializeApplicationAsync();
        }

        private async Task InitializeApplicationAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting application initialization...");

                // Show loading
                await _alertService.ShowLoadingAsync("Initializing application...");

                // Initialize database
                System.Diagnostics.Debug.WriteLine("Initializing database...");
                await _databaseService.InitializeDatabaseAsync();
                System.Diagnostics.Debug.WriteLine("Database initialized successfully");

                // Check if this is first run
                var isFirstRun = await _authService.IsFirstRunAsync();
                if (isFirstRun)
                {
                    await _alertService.HideLoadingAsync();
                    await HandleFirstRunAsync();
                    return;
                }

                // Check authentication status
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
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("User not authenticated - navigating to login");
                    await Shell.Current.GoToAsync("//LoginPage");
                }

                // Show welcome message for authenticated users
                if (isAuthenticated)
                {
                    var user = await _authService.GetCurrentUserAsync();
                    await _alertService.ShowSuccessAsync($"Welcome back, {user?.FullName ?? user?.Username}!", "Welcome");
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
                await Shell.Current.GoToAsync("LoginPage");
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
                    // Retry initialization
                    await InitializeApplicationAsync();
                }
                else
                {
                    // Exit application
                    System.Diagnostics.Debug.WriteLine("User chose to exit after initialization error");
                    Application.Current?.Quit();
                }
            }
            catch (Exception alertEx)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing alert: {alertEx.Message}");

                // Last resort - try to navigate to login page
                try
                {
                    await Shell.Current.GoToAsync("LoginPage");
                }
                catch (Exception navEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Critical error - cannot navigate: {navEx.Message}");
                }
            }
        }

        protected override void OnSleep()
        {
            System.Diagnostics.Debug.WriteLine("App going to sleep");

            // Optionally clear sensitive data or pause operations
            try
            {
                // You could implement auto-logout after sleep time
                // or pause real-time updates
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnSleep error: {ex.Message}");
            }
        }

        protected override async void OnResume()
        {
            System.Diagnostics.Debug.WriteLine("App resuming");

            try
            {
                // Optionally re-verify authentication or refresh data
                var isAuthenticated = await _authService.IsAuthenticatedAsync();
                if (!isAuthenticated)
                {
                    // Redirect to login if authentication expired
                    await Shell.Current.GoToAsync("//LoginPage");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnResume error: {ex.Message}");
            }
        }
    }
}
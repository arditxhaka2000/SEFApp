using SEFApp.Services.Interfaces;
using SEFApp.Views;
using System.Windows.Input;

namespace SEFApp
{
    public partial class AppShell : Shell
    {
        private readonly IAuthenticationService _authService;
        private readonly IAlertService _alertService;
        private bool _hasNavigatedOnce = false;

        public AppShell(IAuthenticationService authService, IAlertService alertService)
        {
            InitializeComponent();

            _authService = authService;
            _alertService = alertService;

            // Initialize commands
            LogoutCommand = new Command(async () => await LogoutAsync());

            // Set binding context
            BindingContext = this;

            // Register routes
            RegisterRoutes();
        }

        public ICommand LogoutCommand { get; }

        // Simplified navigation override - only handle LoginPage specifically
        protected override void OnNavigating(ShellNavigatingEventArgs args)
        {
            // Only intercept navigation to LoginPage specifically
            if (args.Target.Location.OriginalString == "LoginPage" ||
                args.Target.Location.OriginalString == "//LoginPage")
            {
                // Cancel the navigation
                args.Cancel();

                // Handle logout confirmation
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(100); // Small delay to ensure Shell is ready
                    await HandleLoginNavigation();
                });

                return;
            }

            base.OnNavigating(args);
        }

        private async Task HandleLoginNavigation()
        {
            try
            {
                if (Shell.Current == null)
                {
                    System.Diagnostics.Debug.WriteLine("Shell.Current is null, skipping navigation check");
                    return;
                }

                if (_authService == null || _alertService == null)
                {
                    System.Diagnostics.Debug.WriteLine("Services not available, proceeding to login");
                    await Shell.Current.GoToAsync("//LoginPage");
                    return;
                }

                bool isAuthenticated = await _authService.IsAuthenticatedAsync();

                // Skip prompt on first navigation
                if (!_hasNavigatedOnce)
                {
                    _hasNavigatedOnce = true;

                    if (isAuthenticated)
                    {
                        // Just skip to dashboard or stay on current page
                        return;
                    }
                    else
                    {
                        await Shell.Current.GoToAsync("//LoginPage");
                        return;
                    }
                }

                // After first navigation, show prompt if logged in
                if (isAuthenticated)
                {
                    var result = await _alertService.ShowConfirmationAsync(
                        "Already Logged In",
                        "You are already logged in. Do you want to logout?",
                        "Yes, Logout",
                        "Cancel");

                    if (result)
                    {
                        await _authService.LogoutAsync();
                        await Shell.Current.GoToAsync("//LoginPage");
                    }
                }
                else
                {
                    await Shell.Current.GoToAsync("//LoginPage");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HandleLoginNavigation error: {ex.Message}");

                try
                {
                    if (Shell.Current != null)
                    {
                        await Shell.Current.GoToAsync("//LoginPage");
                    }
                }
                catch (Exception navEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Fallback navigation also failed: {navEx.Message}");
                }
            }
        }


        private void RegisterRoutes()
        {
            // Register routes for navigation
            Routing.RegisterRoute("LoginPage", typeof(LoginPage));
            Routing.RegisterRoute("DashboardPage", typeof(DashboardPage));
            Routing.RegisterRoute("TransactionsPage", typeof(TransactionsPage));
            Routing.RegisterRoute("Settings", typeof(SettingsView));

            // Register modal routes
            Routing.RegisterRoute("AddProductModal", typeof(AddProductModal));

            // Register new routes
            Routing.RegisterRoute("ProducsPage", typeof(ProductsPage));
            Routing.RegisterRoute("ReportsPage", typeof(ReportsPage));
            Routing.RegisterRoute("NewTransactionPage", typeof(NewTransactionPage));
            Routing.RegisterRoute("SalesPage", typeof(SalesPage));
        }

        private async Task LogoutAsync()
        {
            try
            {
                var result = await _alertService.ShowConfirmationAsync(
                    "Logout",
                    "Are you sure you want to logout?",
                    "Yes",
                    "No");

                if (result)
                {
                    // Perform logout
                    await _authService.LogoutAsync();

                    // Navigate to login page
                    await Shell.Current.GoToAsync("//LoginPage");
                }
            }
            catch (Exception ex)
            {
                await _alertService.ShowErrorAsync($"Logout failed: {ex.Message}");
            }
        }
    }
}
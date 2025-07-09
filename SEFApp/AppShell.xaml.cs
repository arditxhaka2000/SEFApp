using SEFApp.Services.Interfaces;
using SEFApp.Views;
using System.Windows.Input;

namespace SEFApp
{
    public partial class AppShell : Shell
    {
        private readonly IAuthenticationService _authService;
        private readonly IAlertService _alertService;

        public AppShell()
        {
            InitializeComponent();

            // Get services
            _authService = DependencyService.Get<IAuthenticationService>();
            _alertService = DependencyService.Get<IAlertService>();

            // Initialize commands
            LogoutCommand = new Command(async () => await LogoutAsync());

            // Set binding context
            BindingContext = this;

            // Register routes
            RegisterRoutes();
        }

        public ICommand LogoutCommand { get; }

        private void RegisterRoutes()
        {
            // Register routes for navigation
            Routing.RegisterRoute("LoginPage", typeof(LoginPage));
            Routing.RegisterRoute("DashboardPage", typeof(DashboardPage));
            Routing.RegisterRoute("ProductsPage", typeof(ProductsPage));
            Routing.RegisterRoute("TransactionsPage", typeof(TransactionsPage));
            Routing.RegisterRoute("Settings", typeof(SettingsView));

            // Register modal routes
            Routing.RegisterRoute("AddProductModal", typeof(AddProductModal));

            // Register future routes
            Routing.RegisterRoute("ReportsPage", typeof(ReportsPage));
            Routing.RegisterRoute("NewTransactionPage", typeof(NewTransactionPage));
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

    // Placeholder page classes for future implementation
    public class ReportsPage : ContentPage
    {
        public ReportsPage()
        {
            Title = "Reports";
            Content = new StackLayout
            {
                Children = {
                    new Label { Text = "Reports Page - Coming Soon!",
                              HorizontalOptions = LayoutOptions.Center,
                              VerticalOptions = LayoutOptions.Center }
                }
            };
        }
    }

    public class NewTransactionPage : ContentPage
    {
        public NewTransactionPage()
        {
            Title = "New Transaction";
            Content = new StackLayout
            {
                Children = {
                    new Label { Text = "New Transaction Page - Coming Soon!",
                              HorizontalOptions = LayoutOptions.Center,
                              VerticalOptions = LayoutOptions.Center }
                }
            };
        }
    }
}
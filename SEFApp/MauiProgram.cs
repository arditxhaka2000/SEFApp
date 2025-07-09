using Microsoft.Extensions.Logging;
using SEFApp.Services;
using SEFApp.Services.Interfaces;
using SEFApp.ViewModels;
using SEFApp.Views;

namespace SEFApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Remove the Blazor WebView line - not needed for native MAUI app
            // builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddLogging(logging =>
            {
                logging.AddDebug();
            });
#endif

            // Register Services
            builder.Services.AddSingleton<IPreferencesService, PreferencesService>();
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<IAlertService, AlertService>();

            // Register ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<ProductViewModel>();
            builder.Services.AddTransient<AddProductModalViewModel>();
            builder.Services.AddTransient<TransactionsViewModel>();

            // Register Views
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<ProductsPage>();
            builder.Services.AddTransient<AddProductModal>();
            builder.Services.AddTransient<TransactionsPage>();
            builder.Services.AddTransient<SettingsView>();

            return builder.Build();
        }
    }
}
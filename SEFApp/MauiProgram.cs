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

#if DEBUG
            builder.Services.AddLogging(logging =>
            {
                logging.AddDebug();
            });
#endif

            // Register HttpClient for fiscal services
            builder.Services.AddHttpClient();

            // Register Core Services (Order matters for dependencies)
            builder.Services.AddSingleton<IPreferencesService, PreferencesService>();
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
            builder.Services.AddSingleton<IAlertService, AlertService>();

            // Register NavigationService with IServiceProvider access
            builder.Services.AddSingleton<INavigationService>(serviceProvider =>
                new NavigationService(serviceProvider));

            // Register Fiscal Services
            builder.Services.AddSingleton<IFiscalCertificateService, FiscalCertificateService>();
            builder.Services.AddSingleton<IFiscalService, FiscalService>();
            builder.Services.AddSingleton<ITransactionFiscalService, TransactionFiscalService>();

            // Register AppShell
            builder.Services.AddSingleton<AppShell>();

            // Register ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<ProductViewModel>();
            builder.Services.AddTransient<AddProductModalViewModel>();
            builder.Services.AddTransient<TransactionsViewModel>();
            builder.Services.AddTransient<SalesViewModel>(); // Updated with fiscal integration
            builder.Services.AddTransient<ReportsViewModel>();
            builder.Services.AddTransient<NewTransactionViewModel>();

            // Register Views
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<ProductsPage>();
            builder.Services.AddTransient<AddProductModal>();
            builder.Services.AddTransient<TransactionsPage>();
            builder.Services.AddTransient<SettingsView>();
            builder.Services.AddTransient<SalesPage>();
            builder.Services.AddTransient<ReportsPage>();

            return builder.Build();
        }
    }
}
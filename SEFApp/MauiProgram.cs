using Microsoft.Extensions.Logging;
using SEFApp.Services;
using SEFApp.Services.Interfaces;
using SEFApp.ViewModels;
using SEFApp.Views;
using SEFApp.Converters;
using System.Text.Json;
using System.Text;
using System.Globalization;

namespace SEFApp;

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
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register Services (no HttpClient needed for local auth)
        builder.Services.AddSingleton<IPreferencesService, PreferencesService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();

        // Register ViewModels
        builder.Services.AddTransient<LoginViewModel>();

        // Register Views
        builder.Services.AddTransient<LoginPage>();

        // Register Converters
        builder.Services.AddSingleton<InvertedBoolConverter>();


        return builder.Build();
    }
}
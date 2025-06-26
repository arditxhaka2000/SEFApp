namespace SEFApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("DashboardPage", typeof(Views.DashboardPage));
        Routing.RegisterRoute("Settings", typeof(Views.SettingsView));
    }
}
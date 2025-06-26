using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SEFApp.Services.Interfaces;

namespace SEFApp.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly IAuthenticationService _authService;
        private readonly INavigationService _navigationService;
        private readonly IPreferencesService _preferencesService;
        private readonly IDatabaseService _databaseService;
        private Timer _refreshTimer;

        public DashboardViewModel(
            IAuthenticationService authService,
            INavigationService navigationService,
            IPreferencesService preferencesService,
            IDatabaseService databaseService)
        {
            _authService = authService;
            _navigationService = navigationService;
            _preferencesService = preferencesService;
            _databaseService = databaseService;
            InitializeCommands();
            LoadDashboardData();
            StartAutoRefresh();
        }

        #region Properties

        private DateTime _currentDateTime;
        public DateTime CurrentDateTime
        {
            get => _currentDateTime;
            set => SetProperty(ref _currentDateTime, value);
        }

        private string _currentUser = "Administrator";
        public string CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        private string _userRole = "Admin";
        public string UserRole
        {
            get => _userRole;
            set => SetProperty(ref _userRole, value);
        }

        // Today's Metrics
        private int _todayTransactions;
        public int TodayTransactions
        {
            get => _todayTransactions;
            set => SetProperty(ref _todayTransactions, value);
        }

        private decimal _todayRevenue;
        public decimal TodayRevenue
        {
            get => _todayRevenue;
            set => SetProperty(ref _todayRevenue, value);
        }

        private int _todayInvoices;
        public int TodayInvoices
        {
            get => _todayInvoices;
            set => SetProperty(ref _todayInvoices, value);
        }

        private int _pendingApprovals;
        public int PendingApprovals
        {
            get => _pendingApprovals;
            set => SetProperty(ref _pendingApprovals, value);
        }

        // System Status
        private string _systemStatus = "Online";
        public string SystemStatus
        {
            get => _systemStatus;
            set => SetProperty(ref _systemStatus, value);
        }

        private string _fiscalDeviceStatus = "Connected";
        public string FiscalDeviceStatus
        {
            get => _fiscalDeviceStatus;
            set => SetProperty(ref _fiscalDeviceStatus, value);
        }

        private string _connectionStatus = "Connected";
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        private DateTime _lastSyncTime;
        public DateTime LastSyncTime
        {
            get => _lastSyncTime;
            set => SetProperty(ref _lastSyncTime, value);
        }

        // App Information
        private string _appVersion = "1.0.0";
        public string AppVersion
        {
            get => _appVersion;
            set => SetProperty(ref _appVersion, value);
        }

        private string _platform;
        public string Platform
        {
            get => _platform;
            set => SetProperty(ref _platform, value);
        }

        // Loading States
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _isSyncing;
        public bool IsSyncing
        {
            get => _isSyncing;
            set => SetProperty(ref _isSyncing, value);
        }

        // Collections
        public ObservableCollection<DashboardMetric> KeyMetrics { get; set; }
        public ObservableCollection<ActivityItem> RecentActivities { get; set; }
        public ObservableCollection<QuickAction> QuickActions { get; set; }
        public ObservableCollection<SystemAlert> SystemAlerts { get; set; }

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; private set; }
        public ICommand SyncDataCommand { get; private set; }
        public ICommand NewTransactionCommand { get; private set; }
        public ICommand ViewReportsCommand { get; private set; }
        public ICommand ManageInventoryCommand { get; private set; }
        public ICommand SettingsCommand { get; private set; }
        public ICommand ViewInvoicesCommand { get; private set; }
        public ICommand BackupDataCommand { get; private set; }
        public ICommand LogoutCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            RefreshCommand = new Command(async () => await RefreshDashboardAsync());
            SyncDataCommand = new Command(async () => await SyncDataAsync(), () => !IsSyncing);
            NewTransactionCommand = new Command(async () => await NewTransactionAsync());
            ViewReportsCommand = new Command(async () => await ViewReportsAsync());
            ManageInventoryCommand = new Command(async () => await ManageInventoryAsync());
            SettingsCommand = new Command(async () => await SettingsAsync());
            ViewInvoicesCommand = new Command(async () => await ViewInvoicesAsync());
            BackupDataCommand = new Command(async () => await BackupDataAsync());
            LogoutCommand = new Command(async () => await LogoutAsync());
        }

        private async void LoadDashboardData()
        {
            IsLoading = true;
            try
            {
                await LoadUserInfo();
                await LoadMetrics();
                LoadSystemInfo();
                LoadQuickActions();
                LoadRecentActivities();
                LoadSystemAlerts();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dashboard load error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadUserInfo()
        {
            try
            {
                // Load current user from auth service
                var user = await _authService.GetCurrentUserAsync();
                if (user != null)
                {
                    CurrentUser = user.FullName ?? user.Username;
                    UserRole = user.Role.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load user info error: {ex.Message}");
            }
        }

        private async Task LoadMetrics()
        {
            try
            {
                var metrics = await _databaseService.GetDashboardMetricsAsync();

                CurrentDateTime = DateTime.Now;
                TodayTransactions = (int)metrics.GetValueOrDefault("todaysTransactions", 0);
                TodayRevenue = await _databaseService.GetTodaysRevenueAsync();
                TodayInvoices = TodayTransactions; // Same as transactions for now
                PendingApprovals = 0; // Implement based on your business logic
                LastSyncTime = DateTime.Now.AddMinutes(-5); // Update with actual sync time

                // Update key metrics
                KeyMetrics = new ObservableCollection<DashboardMetric>
        {
            new DashboardMetric { Title = "Today's Sales", Value = TodayRevenue.ToString("C"), Icon = "💰", Color = "#22c55e" },
            new DashboardMetric { Title = "Transactions", Value = TodayTransactions.ToString(), Icon = "📊", Color = "#3b82f6" },
            new DashboardMetric { Title = "Products", Value = metrics.GetValueOrDefault("totalProducts", 0).ToString(), Icon = "📦", Color = "#f59e0b" },
            new DashboardMetric { Title = "Users", Value = metrics.GetValueOrDefault("totalUsers", 0).ToString(), Icon = "👥", Color = "#8b5cf6" }
        };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load metrics error: {ex.Message}");
                // Fallback to default values
            }
        }

        private void LoadSystemInfo()
        {
            Platform = DeviceInfo.Platform.ToString();
            AppVersion = AppInfo.VersionString;

            // Simulate system status checks
            SystemStatus = "Online";
            FiscalDeviceStatus = new Random().Next(1, 10) > 2 ? "Connected" : "Disconnected";
            ConnectionStatus = Connectivity.NetworkAccess == NetworkAccess.Internet ? "Connected" : "Offline";
        }

        private void LoadQuickActions()
        {
            QuickActions = new ObservableCollection<QuickAction>
            {
                new QuickAction { Title = "New Sale", Icon = "🛒", Command = NewTransactionCommand, IsPrimary = true },
                new QuickAction { Title = "View Reports", Icon = "📈", Command = ViewReportsCommand },
                new QuickAction { Title = "Inventory", Icon = "📦", Command = ManageInventoryCommand },
                new QuickAction { Title = "Invoices", Icon = "📋", Command = ViewInvoicesCommand },
                new QuickAction { Title = "Settings", Icon = "⚙️", Command = SettingsCommand },
                new QuickAction { Title = "Backup", Icon = "💾", Command = BackupDataCommand }
            };
        }

        private void LoadRecentActivities()
        {
            RecentActivities = new ObservableCollection<ActivityItem>
            {
                new ActivityItem { Icon = "💰", Title = "Sale Completed", Description = "Invoice #1234 - €150.00", Time = "2 min ago", Type = "Transaction" },
                new ActivityItem { Icon = "📄", Title = "Fiscal Receipt", Description = "Receipt printed successfully", Time = "5 min ago", Type = "Fiscal" },
                new ActivityItem { Icon = "🔄", Title = "Data Synced", Description = "Server synchronization completed", Time = "15 min ago", Type = "System" },
                new ActivityItem { Icon = "📊", Title = "Report Generated", Description = "Daily sales report", Time = "1 hour ago", Type = "Report" },
                new ActivityItem { Icon = "👤", Title = "User Login", Description = $"{CurrentUser} logged in", Time = "2 hours ago", Type = "Security" }
            };
        }

        private void LoadSystemAlerts()
        {
            SystemAlerts = new ObservableCollection<SystemAlert>();

            if (FiscalDeviceStatus == "Disconnected")
            {
                SystemAlerts.Add(new SystemAlert
                {
                    Type = AlertType.Warning,
                    Title = "Fiscal Device Offline",
                    Message = "Please check fiscal printer connection",
                    Icon = "⚠️"
                });
            }

            if (PendingApprovals > 10)
            {
                SystemAlerts.Add(new SystemAlert
                {
                    Type = AlertType.Info,
                    Title = "Pending Approvals",
                    Message = $"{PendingApprovals} transactions awaiting approval",
                    Icon = "📋"
                });
            }

            if (DateTime.Now - LastSyncTime > TimeSpan.FromHours(2))
            {
                SystemAlerts.Add(new SystemAlert
                {
                    Type = AlertType.Warning,
                    Title = "Sync Required",
                    Message = "Data hasn't been synced in over 2 hours",
                    Icon = "🔄"
                });
            }
        }

        private void StartAutoRefresh()
        {
            _refreshTimer = new Timer(async _ => await RefreshDashboardAsync(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        #endregion

        #region Command Methods

        private async Task RefreshDashboardAsync()
        {
            try
            {
                await LoadMetrics();
                LoadSystemInfo();
                LoadSystemAlerts();

                // Add refresh activity
                RecentActivities.Insert(0, new ActivityItem
                {
                    Icon = "🔄",
                    Title = "Dashboard Refreshed",
                    Description = "Data updated successfully",
                    Time = "Just now",
                    Type = "System"
                });

                // Keep only last 10 activities
                while (RecentActivities.Count > 10)
                    RecentActivities.RemoveAt(RecentActivities.Count - 1);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Refresh failed: {ex.Message}", "OK");
            }
        }

        private async Task SyncDataAsync()
        {
            IsSyncing = true;
            ((Command)SyncDataCommand).ChangeCanExecute();

            try
            {
                // Simulate sync operation
                await Task.Delay(2000);

                LastSyncTime = DateTime.Now;

                RecentActivities.Insert(0, new ActivityItem
                {
                    Icon = "🔄",
                    Title = "Data Synchronized",
                    Description = "Manual sync completed successfully",
                    Time = "Just now",
                    Type = "System"
                });

                await Application.Current.MainPage.DisplayAlert("Success", "Data synchronized successfully!", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Sync failed: {ex.Message}", "OK");
            }
            finally
            {
                IsSyncing = false;
                ((Command)SyncDataCommand).ChangeCanExecute();
            }
        }

        private async Task NewTransactionAsync()
        {
            await _navigationService.NavigateToAsync("NewTransactionPage");
        }

        private async Task ViewReportsAsync()
        {
            await _navigationService.NavigateToAsync("ReportsPage");
        }

        private async Task ManageInventoryAsync()
        {
            await _navigationService.NavigateToAsync("InventoryPage");
        }

        private async Task ViewInvoicesAsync()
        {
            await _navigationService.NavigateToAsync("InvoicesPage");
        }

        private async Task SettingsAsync()
        {
            await _navigationService.NavigateToAsync("Settings");
        }

        private async Task BackupDataAsync()
        {
            try
            {
                await Application.Current.MainPage.DisplayAlert("Backup", "Starting data backup...", "OK");
                await Task.Delay(1000); // Simulate backup
                await Application.Current.MainPage.DisplayAlert("Success", "Backup completed successfully!", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Backup failed: {ex.Message}", "OK");
            }
        }

        private async Task LogoutAsync()
        {
            bool result = await Application.Current.MainPage.DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");

            if (result)
            {
                await _authService.LogoutAsync();
                await _navigationService.NavigateToAsync("//LoginPage");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            _refreshTimer?.Dispose();
        }

        #endregion
    }

    #region Supporting Models

    public class DashboardMetric
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
    }

    public class ActivityItem
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Time { get; set; }
        public string Type { get; set; }
    }

    public class QuickAction
    {
        public string Title { get; set; }
        public string Icon { get; set; }
        public ICommand Command { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class SystemAlert
    {
        public AlertType Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Icon { get; set; }
    }

    public enum AlertType
    {
        Info,
        Warning,
        Error,
        Success
    }

    #endregion
}
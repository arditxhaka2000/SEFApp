using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SEFApp.Models.Database;
using SEFApp.Services.Interfaces;

namespace SEFApp.ViewModels
{
    public class TransactionsViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private readonly INavigationService _navigationService;
        private readonly IAlertService _alertService;

        public TransactionsViewModel(
            IDatabaseService databaseService,
            INavigationService navigationService,
            IAlertService alertService)
        {
            _databaseService = databaseService;
            _navigationService = navigationService;
            _alertService = alertService;

            System.Diagnostics.Debug.WriteLine("TransactionsViewModel created");
            System.Diagnostics.Debug.WriteLine($"NavigationService is null: {_navigationService == null}");

            InitializeCommands();
            InitializeData();

            // Load transactions immediately
            _ = Task.Run(async () => await LoadTransactions());
        }

        #region Properties

        private ObservableCollection<Transaction> _transactions = new();
        public ObservableCollection<Transaction> Transactions
        {
            get => _transactions;
            set => SetProperty(ref _transactions, value);
        }

        private ObservableCollection<Transaction> _filteredTransactions = new();
        public ObservableCollection<Transaction> FilteredTransactions
        {
            get => _filteredTransactions;
            set => SetProperty(ref _filteredTransactions, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    // Use Task.Run to avoid blocking UI and prevent multiple rapid calls
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(300); // Debounce search
                        await MainThread.InvokeOnMainThreadAsync(() => FilterTransactions());
                    });
                }
            }
        }

        private string _selectedStatus = "All";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                {
                    FilterTransactions();
                }
            }
        }

        // Summary Properties
        private int _todayCount;
        public int TodayCount
        {
            get => _todayCount;
            set => SetProperty(ref _todayCount, value);
        }

        private decimal _todayRevenue;
        public decimal TodayRevenue
        {
            get => _todayRevenue;
            set => SetProperty(ref _todayRevenue, value);
        }

        private int _totalTransactions;
        public int TotalTransactions
        {
            get => _totalTransactions;
            set => SetProperty(ref _totalTransactions, value);
        }

        // Options
        public ObservableCollection<string> StatusOptions { get; set; } = new ObservableCollection<string>
        {
            "All", "Completed", "Pending", "Cancelled", "Refunded", "Draft"
        };

        #endregion

        #region Commands

        public ICommand NewTransactionCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand FilterByDateCommand { get; private set; }
        public ICommand ViewTransactionCommand { get; private set; }
        public ICommand PrintReceiptCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            NewTransactionCommand = new Command(async () => await CreateNewTransaction());
            RefreshCommand = new Command(async () => await LoadTransactions());
            SearchCommand = new Command(() => FilterTransactions());
            FilterByDateCommand = new Command(async () => await FilterByDate());
            ViewTransactionCommand = new Command<Transaction>(async (transaction) => await ViewTransaction(transaction));
            PrintReceiptCommand = new Command<Transaction>(async (transaction) => await PrintReceipt(transaction));

            System.Diagnostics.Debug.WriteLine($"NewTransactionCommand created: {NewTransactionCommand != null}");
        }

        private void InitializeData()
        {
            Transactions.Clear();
            FilteredTransactions.Clear();

            // Reset summary data
            TodayCount = 0;
            TodayRevenue = 0;
            TotalTransactions = 0;
        }

        #endregion

        #region Methods

        private async Task LoadTransactions()
        {
            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine("Loading transactions...");

                // Load transactions from the last 30 days
                var endDate = DateTime.Today.AddDays(1);
                var startDate = DateTime.Today.AddDays(-30);

                var transactions = await _databaseService.GetTransactionsByDateRangeAsync(startDate, endDate);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Clear existing transactions first
                    Transactions.Clear();
                    FilteredTransactions.Clear();

                    // Add new transactions
                    foreach (var transaction in transactions)
                    {
                        Transactions.Add(transaction);
                    }

                    System.Diagnostics.Debug.WriteLine($"Added {transactions.Count} transactions to collection");
                });

                // Load summary data
                await LoadSummaryData();

                // Apply current filters ONCE after loading
                await MainThread.InvokeOnMainThreadAsync(() => FilterTransactions());

                System.Diagnostics.Debug.WriteLine($"Loaded {transactions.Count} transactions");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTransactions error: {ex.Message}");
                await _alertService.ShowErrorAsync($"Failed to load transactions: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadSummaryData()
        {
            try
            {
                // Today's transactions
                TodayCount = await _databaseService.GetTodaysTransactionCountAsync();
                TodayRevenue = await _databaseService.GetTodaysRevenueAsync();

                // Total transactions in current view
                TotalTransactions = Transactions.Count;

                System.Diagnostics.Debug.WriteLine($"Summary - Today: {TodayCount} transactions, €{TodayRevenue:F2} revenue");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load summary data: {ex.Message}");
                // Set default values
                TodayCount = 0;
                TodayRevenue = 0;
                TotalTransactions = 0;
            }
        }

        private void FilterTransactions()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Filtering transactions. Search: '{SearchText}', Status: '{SelectedStatus}'");
                System.Diagnostics.Debug.WriteLine($"Source transactions count: {Transactions.Count}");

                // Clear filtered transactions first
                FilteredTransactions.Clear();

                var filtered = Transactions.AsEnumerable();

                // Filter by search text
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    filtered = filtered.Where(t =>
                        (t.TransactionNumber?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (t.CustomerName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (t.PaymentMethod?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
                }

                // Filter by status
                if (SelectedStatus != "All")
                {
                    filtered = filtered.Where(t => t.Status == SelectedStatus);
                }

                // Order by date (newest first)
                filtered = filtered.OrderByDescending(t => t.TransactionDate);

                // Add filtered transactions to collection
                foreach (var transaction in filtered)
                {
                    FilteredTransactions.Add(transaction);
                }

                System.Diagnostics.Debug.WriteLine($"Filtered to {FilteredTransactions.Count} transactions");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Filter error: {ex.Message}");
            }
        }

        private async Task CreateNewTransaction()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Navigating to Point of Sale...");

                if (_navigationService == null)
                {
                    System.Diagnostics.Debug.WriteLine("NavigationService is null!");
                    await _alertService.ShowErrorAsync("Navigation service not available");
                    return;
                }

                // Navigate to Point of Sale instead of NewTransactionPage
                await _navigationService.NavigateToAsync("SalesPage");
                System.Diagnostics.Debug.WriteLine("Navigation to SalesPage executed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                await _alertService.ShowErrorAsync($"Failed to open point of sale: {ex.Message}");
            }
        }

        private async Task FilterByDate()
        {
            try
            {
                // This would open a date picker dialog
                await _alertService.ShowAlertAsync("Date Filter", "Date filtering will be implemented in the next version");
            }
            catch (Exception ex)
            {
                await _alertService.ShowErrorAsync($"Date filter error: {ex.Message}");
            }
        }

        private async Task ViewTransaction(Transaction transaction)
        {
            if (transaction == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Viewing transaction: {transaction.TransactionNumber}");

                // Show transaction details
                var details = $@"
Transaction: {transaction.TransactionNumber}
Date: {transaction.TransactionDate:yyyy-MM-dd HH:mm}
Customer: {transaction.CustomerName}
Payment: {transaction.PaymentMethod}
Status: {transaction.Status}

Subtotal: €{transaction.SubTotal:F2}
Tax: €{transaction.TaxAmount:F2}
Total: €{transaction.TotalAmount:F2}

Notes: {transaction.Notes}";

                await _alertService.ShowAlertAsync("Transaction Details", details, "Close");
            }
            catch (Exception ex)
            {
                await _alertService.ShowErrorAsync($"Failed to view transaction: {ex.Message}");
            }
        }

        private async Task PrintReceipt(Transaction transaction)
        {
            if (transaction == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Printing receipt for: {transaction.TransactionNumber}");

                // Generate receipt content
                var receiptContent = $@"
═══════════════════════════════
          FISCAL RECEIPT
═══════════════════════════════

Transaction: {transaction.TransactionNumber}
Date: {transaction.TransactionDate:yyyy-MM-dd HH:mm:ss}
Customer: {transaction.CustomerName}
Payment: {transaction.PaymentMethod}

───────────────────────────────
Subtotal:     €{transaction.SubTotal:F2}
Tax (18%):    €{transaction.TaxAmount:F2}
───────────────────────────────
TOTAL:        €{transaction.TotalAmount:F2}

Thank you for your business!
═══════════════════════════════";

                await _alertService.ShowAlertAsync("Receipt", receiptContent, "Close");
            }
            catch (Exception ex)
            {
                await _alertService.ShowErrorAsync($"Print receipt error: {ex.Message}");
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
    }
}
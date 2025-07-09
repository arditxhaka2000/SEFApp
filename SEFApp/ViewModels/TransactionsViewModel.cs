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

            InitializeCommands();
            LoadTransactions();
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
                    FilterTransactions();
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
            "All", "Completed", "Pending", "Cancelled", "Refunded"
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
        }

        #endregion

        #region Methods

        private async Task LoadTransactions()
        {
            try
            {
                IsLoading = true;

                // Load transactions from database
                var transactions = await _databaseService.GetTransactionsByDateRangeAsync(
                    DateTime.Today.AddDays(-30), DateTime.Today.AddDays(1));

                Transactions.Clear();
                foreach (var transaction in transactions.OrderByDescending(t => t.TransactionDate))
                {
                    Transactions.Add(transaction);
                }

                // Load summary data
                await LoadSummaryData();

                // Apply current filters
                FilterTransactions();
            }
            catch (Exception ex)
            {
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

                // Total transactions
                TotalTransactions = Transactions.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load summary data: {ex.Message}");
            }
        }

        private void FilterTransactions()
        {
            try
            {
                FilteredTransactions.Clear();

                var filtered = Transactions.AsEnumerable();

                // Filter by search text
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    filtered = filtered.Where(t =>
                        t.TransactionNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        t.CustomerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        t.PaymentMethod.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
                }

                // Filter by status
                if (SelectedStatus != "All")
                {
                    filtered = filtered.Where(t => t.Status == SelectedStatus);
                }

                foreach (var transaction in filtered)
                {
                    FilteredTransactions.Add(transaction);
                }
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
                await _navigationService.NavigateToAsync("NewTransactionPage");
            }
            catch (Exception ex)
            {
                await _alertService.ShowErrorAsync($"Failed to create new transaction: {ex.Message}");
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
                // Navigate to transaction detail page
                await _navigationService.NavigateToAsync($"TransactionDetailPage?id={transaction.Id}");
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
                await _alertService.ShowAlertAsync("Print Receipt",
                    $"Receipt printing for transaction {transaction.TransactionNumber} will be implemented in the next version");
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
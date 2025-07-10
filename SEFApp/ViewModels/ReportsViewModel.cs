using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SEFApp.Models.Database;
using SEFApp.Services.Interfaces;

namespace SEFApp.ViewModels
{
    public class ReportsViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAlertService _alertService;
        private readonly INavigationService _navigationService;

        public ReportsViewModel(
            IDatabaseService databaseService,
            IAlertService alertService,
            INavigationService navigationService)
        {
            _databaseService = databaseService;
            _alertService = alertService;
            _navigationService = navigationService;

            InitializeCommands();
            InitializeDateRange();
            LoadReportData();
        }

        #region Properties

        private DateTime _startDate;
        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        private DateTime _endDate;
        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        private decimal _totalSales;
        public decimal TotalSales
        {
            get => _totalSales;
            set => SetProperty(ref _totalSales, value);
        }

        private int _totalTransactions;
        public int TotalTransactions
        {
            get => _totalTransactions;
            set => SetProperty(ref _totalTransactions, value);
        }

        private decimal _averageSale;
        public decimal AverageSale
        {
            get => _averageSale;
            set => SetProperty(ref _averageSale, value);
        }

        private decimal _totalTax;
        public decimal TotalTax
        {
            get => _totalTax;
            set => SetProperty(ref _totalTax, value);
        }

        private double _salesGrowth;
        public double SalesGrowth
        {
            get => _salesGrowth;
            set => SetProperty(ref _salesGrowth, value);
        }

        private double _transactionGrowth;
        public double TransactionGrowth
        {
            get => _transactionGrowth;
            set => SetProperty(ref _transactionGrowth, value);
        }

        private double _averageSaleGrowth;
        public double AverageSaleGrowth
        {
            get => _averageSaleGrowth;
            set => SetProperty(ref _averageSaleGrowth, value);
        }

        private double _taxGrowth;
        public double TaxGrowth
        {
            get => _taxGrowth;
            set => SetProperty(ref _taxGrowth, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<TopProductReport> TopProducts { get; set; } = new();
        public ObservableCollection<PaymentMethodReport> PaymentMethodStats { get; set; } = new();
        public ObservableCollection<HourlySalesReport> SalesByHour { get; set; } = new();
        public ObservableCollection<Transaction> RecentTransactions { get; set; } = new();

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; private set; }
        public ICommand ApplyDateFilterCommand { get; private set; }
        public ICommand SetTodayCommand { get; private set; }
        public ICommand SetThisWeekCommand { get; private set; }
        public ICommand SetThisMonthCommand { get; private set; }
        public ICommand SetThisYearCommand { get; private set; }
        public ICommand ExportReportCommand { get; private set; }
        public ICommand ExportPdfCommand { get; private set; }
        public ICommand ExportExcelCommand { get; private set; }
        public ICommand EmailReportCommand { get; private set; }
        public ICommand ViewAllTransactionsCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            RefreshCommand = new Command(async () => await LoadReportData());
            ApplyDateFilterCommand = new Command(async () => await LoadReportData());
            SetTodayCommand = new Command(() => SetDateRange(DateRangeType.Today));
            SetThisWeekCommand = new Command(() => SetDateRange(DateRangeType.ThisWeek));
            SetThisMonthCommand = new Command(() => SetDateRange(DateRangeType.ThisMonth));
            SetThisYearCommand = new Command(() => SetDateRange(DateRangeType.ThisYear));
            ExportReportCommand = new Command(async () => await ShowExportOptions());
            ExportPdfCommand = new Command(async () => await ExportToPdf());
            ExportExcelCommand = new Command(async () => await ExportToExcel());
            EmailReportCommand = new Command(async () => await EmailReport());
            ViewAllTransactionsCommand = new Command(async () => await ViewAllTransactions());
        }

        private void InitializeDateRange()
        {
            // Default to current month
            SetDateRange(DateRangeType.ThisMonth);
        }

        #endregion

        #region Data Loading

        private async Task LoadReportData()
        {
            try
            {
                IsLoading = true;

                // Load main metrics
                await LoadMainMetrics();

                // Load detailed reports
                await LoadTopProducts();
                await LoadPaymentMethodStats();
                await LoadSalesByHour();
                await LoadRecentTransactions();

                // Calculate growth percentages
                await CalculateGrowthMetrics();
            }
            catch (Exception ex)
            {
                await _alertService.ShowErrorAsync($"Failed to load report data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadMainMetrics()
        {
            try
            {
                var transactions = await _databaseService.GetTransactionsByDateRangeAsync(StartDate, EndDate.AddDays(1));
                var completedTransactions = transactions.Where(t => t.Status == "Completed").ToList();

                TotalSales = completedTransactions.Sum(t => t.TotalAmount);
                TotalTransactions = completedTransactions.Count;
                TotalTax = completedTransactions.Sum(t => t.TaxAmount);
                AverageSale = TotalTransactions > 0 ? TotalSales / TotalTransactions : 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load main metrics: {ex.Message}");
            }
        }

        private async Task LoadTopProducts()
        {
            try
            {
                TopProducts.Clear();

                // Note: This requires transaction items/products table to be implemented
                // For now, this method will not populate data until the database schema includes product sales
                // You'll need to implement transaction items with product references to get actual top products

                // Example of what this would look like when implemented:
                // var productSales = await _databaseService.GetProductSalesByDateRangeAsync(StartDate, EndDate.AddDays(1));
                // var topProducts = productSales
                //     .GroupBy(p => new { p.ProductId, p.ProductName, p.ProductCode })
                //     .Select(g => new TopProductReport
                //     {
                //         ProductName = g.Key.ProductName,
                //         ProductCode = g.Key.ProductCode,
                //         QuantitySold = g.Sum(p => p.Quantity),
                //         Revenue = g.Sum(p => p.TotalAmount)
                //     })
                //     .OrderByDescending(p => p.Revenue)
                //     .Take(10)
                //     .ToList();

                // Calculate rankings and percentages
                // for (int i = 0; i < topProducts.Count; i++)
                // {
                //     topProducts[i].Rank = i + 1;
                //     topProducts[i].SalesPercentage = topProducts.Count > 0 ? (double)topProducts[i].Revenue / (double)topProducts[0].Revenue : 0;
                //     TopProducts.Add(topProducts[i]);
                // }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load top products: {ex.Message}");
            }
        }

        private async Task LoadPaymentMethodStats()
        {
            try
            {
                PaymentMethodStats.Clear();

                var transactions = await _databaseService.GetTransactionsByDateRangeAsync(StartDate, EndDate.AddDays(1));
                var completedTransactions = transactions.Where(t => t.Status == "Completed").ToList();

                if (completedTransactions.Any())
                {
                    var paymentGroups = completedTransactions
                        .GroupBy(t => t.PaymentMethod)
                        .Select(g => new PaymentMethodReport
                        {
                            PaymentMethod = g.Key,
                            TransactionCount = g.Count(),
                            Amount = g.Sum(t => t.TotalAmount),
                            Percentage = (double)g.Count() / completedTransactions.Count,
                            Icon = GetPaymentMethodIcon(g.Key)
                        })
                        .OrderByDescending(p => p.Amount);

                    foreach (var payment in paymentGroups)
                    {
                        PaymentMethodStats.Add(payment);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load payment method stats: {ex.Message}");
            }
        }

        private async Task LoadSalesByHour()
        {
            try
            {
                SalesByHour.Clear();

                var transactions = await _databaseService.GetTransactionsByDateRangeAsync(StartDate, EndDate.AddDays(1));
                var completedTransactions = transactions.Where(t => t.Status == "Completed").ToList();

                if (completedTransactions.Any())
                {
                    var hourlyGroups = completedTransactions
                        .GroupBy(t => t.TransactionDate.Hour)
                        .Select(g => new HourlySalesReport
                        {
                            Hour = g.Key,
                            Amount = g.Sum(t => t.TotalAmount),
                            TransactionCount = g.Count()
                        })
                        .OrderBy(h => h.Hour);

                    var maxAmount = hourlyGroups.Any() ? hourlyGroups.Max(h => h.Amount) : 1;

                    foreach (var hour in hourlyGroups)
                    {
                        hour.BarHeight = maxAmount > 0 ? (double)(hour.Amount / maxAmount) * 80 : 0;
                        SalesByHour.Add(hour);
                    }
                }

                // Fill missing hours with zero values for complete 24-hour display
                for (int hour = 0; hour < 24; hour++)
                {
                    if (!SalesByHour.Any(h => h.Hour == hour))
                    {
                        SalesByHour.Add(new HourlySalesReport { Hour = hour, Amount = 0, TransactionCount = 0, BarHeight = 0 });
                    }
                }

                // Sort by hour
                var sortedHours = SalesByHour.OrderBy(h => h.Hour).ToList();
                SalesByHour.Clear();
                foreach (var hour in sortedHours)
                {
                    SalesByHour.Add(hour);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load sales by hour: {ex.Message}");
            }
        }

        private async Task LoadRecentTransactions()
        {
            try
            {
                RecentTransactions.Clear();

                var transactions = await _databaseService.GetTransactionsByDateRangeAsync(StartDate, EndDate.AddDays(1));
                var recentTransactions = transactions
                    .OrderByDescending(t => t.TransactionDate)
                    .Take(20);

                foreach (var transaction in recentTransactions)
                {
                    RecentTransactions.Add(transaction);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load recent transactions: {ex.Message}");
            }
        }

        private async Task CalculateGrowthMetrics()
        {
            try
            {
                // Calculate previous period metrics for comparison
                var periodDays = (EndDate - StartDate).Days;
                var previousStartDate = StartDate.AddDays(-periodDays);
                var previousEndDate = StartDate;

                var previousTransactions = await _databaseService.GetTransactionsByDateRangeAsync(previousStartDate, previousEndDate);
                var previousCompleted = previousTransactions.Where(t => t.Status == "Completed").ToList();

                var previousSales = previousCompleted.Sum(t => t.TotalAmount);
                var previousTransactionCount = previousCompleted.Count;
                var previousTax = previousCompleted.Sum(t => t.TaxAmount);
                var previousAverage = previousTransactionCount > 0 ? previousSales / previousTransactionCount : 0;

                // Calculate growth percentages
                SalesGrowth = previousSales > 0 ? (double)(TotalSales - previousSales) / (double)previousSales : 0;
                TransactionGrowth = previousTransactionCount > 0 ? (double)(TotalTransactions - previousTransactionCount) / (double)previousTransactionCount : 0;
                TaxGrowth = previousTax > 0 ? (double)(TotalTax - previousTax) / (double)previousTax : 0;
                AverageSaleGrowth = previousAverage > 0 ? (double)(AverageSale - previousAverage) / (double)previousAverage : 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to calculate growth metrics: {ex.Message}");
            }
        }

        #endregion

        #region Date Range Methods

        private void SetDateRange(DateRangeType rangeType)
        {
            var now = DateTime.Now;

            switch (rangeType)
            {
                case DateRangeType.Today:
                    StartDate = now.Date;
                    EndDate = now.Date;
                    break;

                case DateRangeType.ThisWeek:
                    var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
                    StartDate = startOfWeek;
                    EndDate = startOfWeek.AddDays(6);
                    break;

                case DateRangeType.ThisMonth:
                    StartDate = new DateTime(now.Year, now.Month, 1);
                    EndDate = StartDate.AddMonths(1).AddDays(-1);
                    break;

                case DateRangeType.ThisYear:
                    StartDate = new DateTime(now.Year, 1, 1);
                    EndDate = new DateTime(now.Year, 12, 31);
                    break;
            }

            // Auto-refresh when date range changes
            _ = Task.Run(async () => await LoadReportData());
        }

        #endregion

        #region Export Methods

        private async Task ShowExportOptions()
        {
            var choice = await _alertService.ShowActionSheetAsync(
                "Export Report",
                "Cancel",
                null,
                "PDF Report",
                "Excel Spreadsheet",
                "Email Report");

            switch (choice)
            {
                case "PDF Report":
                    await ExportToPdf();
                    break;
                case "Excel Spreadsheet":
                    await ExportToExcel();
                    break;
                case "Email Report":
                    await EmailReport();
                    break;
            }
        }

        private async Task ExportToPdf()
        {
            try
            {
                await _alertService.ShowLoadingAsync("Generating PDF report...");

                // Simulate PDF generation
                await Task.Delay(2000);

                var fileName = $"Sales_Report_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}.pdf";
                var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                // In a real app, you would use a PDF library like iText7 or Syncfusion
                var reportContent = GenerateReportContent();
                await File.WriteAllTextAsync(filePath.Replace(".pdf", ".txt"), reportContent);

                await _alertService.HideLoadingAsync();
                await _alertService.ShowSuccessAsync($"PDF report saved as {fileName}");
            }
            catch (Exception ex)
            {
                await _alertService.HideLoadingAsync();
                await _alertService.ShowErrorAsync($"PDF export failed: {ex.Message}");
            }
        }

        private async Task ExportToExcel()
        {
            try
            {
                await _alertService.ShowLoadingAsync("Generating Excel report...");

                // Simulate Excel generation
                await Task.Delay(2000);

                var fileName = $"Sales_Report_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}.xlsx";
                var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                // In a real app, you would use EPPlus or ClosedXML
                var reportContent = GenerateReportContent();
                await File.WriteAllTextAsync(filePath.Replace(".xlsx", ".csv"), GenerateCsvContent());

                await _alertService.HideLoadingAsync();
                await _alertService.ShowSuccessAsync($"Excel report saved as {fileName}");
            }
            catch (Exception ex)
            {
                await _alertService.HideLoadingAsync();
                await _alertService.ShowErrorAsync($"Excel export failed: {ex.Message}");
            }
        }

        private async Task EmailReport()
        {
            try
            {
                await _alertService.ShowAlertAsync(
                    "Email Report",
                    "Email functionality will be implemented with a mail service integration.",
                    "OK");
            }
            catch (Exception ex)
            {
                await _alertService.ShowErrorAsync($"Email failed: {ex.Message}");
            }
        }

        private async Task ViewAllTransactions()
        {
            await _navigationService.NavigateToAsync("TransactionsPage");
        }

        #endregion

        #region Helper Methods

        private string GetPaymentMethodIcon(string paymentMethod)
        {
            return paymentMethod?.ToLower() switch
            {
                "cash" => "💵",
                "card" => "💳",
                "bank transfer" => "🏦",
                "check" => "📄",
                _ => "💰"
            };
        }

        private string GenerateReportContent()
        {
            return $@"
SALES REPORT
═══════════════════════════════
Period: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}

SUMMARY METRICS
───────────────────────────────
Total Sales:      €{TotalSales:F2}
Transactions:     {TotalTransactions}
Average Sale:     €{AverageSale:F2}
Tax Collected:    €{TotalTax:F2}

GROWTH COMPARISON
───────────────────────────────
Sales Growth:     {SalesGrowth:P2}
Transaction Growth: {TransactionGrowth:P2}
Average Sale Growth: {AverageSaleGrowth:P2}
Tax Growth:       {TaxGrowth:P2}

PAYMENT METHODS
───────────────────────────────
{string.Join("\n", PaymentMethodStats.Select(p => $"{p.PaymentMethod}: {p.TransactionCount} transactions - €{p.Amount:F2} ({p.Percentage:P0})"))}

HOURLY SALES BREAKDOWN
───────────────────────────────
{string.Join("\n", SalesByHour.Where(h => h.Amount > 0).Select(h => $"{h.Hour:00}:00 - €{h.Amount:F2} ({h.TransactionCount} transactions)"))}

Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
";
        }

        private string GenerateCsvContent()
        {
            var csv = "Type,Name,Value,Count,Percentage\n";

            // Add summary metrics
            csv += $"Summary,Total Sales,{TotalSales:F2},,\n";
            csv += $"Summary,Total Transactions,{TotalTransactions},,\n";
            csv += $"Summary,Average Sale,{AverageSale:F2},,\n";
            csv += $"Summary,Tax Collected,{TotalTax:F2},,\n";

            // Add payment methods
            foreach (var payment in PaymentMethodStats)
            {
                csv += $"Payment,{payment.PaymentMethod},{payment.Amount:F2},{payment.TransactionCount},{payment.Percentage:P2}\n";
            }

            // Add hourly sales
            foreach (var hour in SalesByHour.Where(h => h.Amount > 0))
            {
                csv += $"Hourly,{hour.Hour:00}:00,{hour.Amount:F2},{hour.TransactionCount},\n";
            }

            return csv;
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

    #region Supporting Models and Enums

    public enum DateRangeType
    {
        Today,
        ThisWeek,
        ThisMonth,
        ThisYear
    }

    public class TopProductReport
    {
        public int Rank { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public double SalesPercentage { get; set; }
    }

    public class PaymentMethodReport
    {
        public string PaymentMethod { get; set; }
        public int TransactionCount { get; set; }
        public decimal Amount { get; set; }
        public double Percentage { get; set; }
        public string Icon { get; set; }
    }

    public class HourlySalesReport
    {
        public int Hour { get; set; }
        public decimal Amount { get; set; }
        public int TransactionCount { get; set; }
        public double BarHeight { get; set; }
    }

    #endregion
}
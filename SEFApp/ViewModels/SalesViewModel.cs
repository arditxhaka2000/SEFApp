using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SEFApp.Models.Database;
using SEFApp.Services.Interfaces;

namespace SEFApp.ViewModels
{
    public class SalesViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAlertService _alertService;
        private readonly IAuthenticationService _authService;

        public SalesViewModel(
            IDatabaseService databaseService,
            IAlertService alertService,
            IAuthenticationService authService)
        {
            _databaseService = databaseService;
            _alertService = alertService;
            _authService = authService;

            InitializeCommands();
            InitializeData();
            LoadProducts();
        }

        #region Properties

        private ObservableCollection<Product> _allProducts = new();
        public ObservableCollection<Product> AllProducts
        {
            get => _allProducts;
            set => SetProperty(ref _allProducts, value);
        }

        private ObservableCollection<Product> _filteredProducts = new();
        public ObservableCollection<Product> FilteredProducts
        {
            get => _filteredProducts;
            set => SetProperty(ref _filteredProducts, value);
        }

        private ObservableCollection<CartItem> _cartItems = new();
        public ObservableCollection<CartItem> CartItems
        {
            get => _cartItems;
            set => SetProperty(ref _cartItems, value);
        }

        private ObservableCollection<Transaction> _recentTransactions = new();
        public ObservableCollection<Transaction> RecentTransactions
        {
            get => _recentTransactions;
            set => SetProperty(ref _recentTransactions, value);
        }

        private string _productSearchText = string.Empty;
        public string ProductSearchText
        {
            get => _productSearchText;
            set
            {
                if (SetProperty(ref _productSearchText, value))
                {
                    FilterProducts();
                }
            }
        }

        private string _customerName = string.Empty;
        public string CustomerName
        {
            get => _customerName;
            set => SetProperty(ref _customerName, value);
        }

        private string _customerPhone = string.Empty;
        public string CustomerPhone
        {
            get => _customerPhone;
            set => SetProperty(ref _customerPhone, value);
        }

        private string _customerEmail = string.Empty;
        public string CustomerEmail
        {
            get => _customerEmail;
            set => SetProperty(ref _customerEmail, value);
        }

        private string _selectedPaymentMethod = "Cash";
        public string SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set
            {
                if (SetProperty(ref _selectedPaymentMethod, value))
                {
                    OnPropertyChanged(nameof(IsCashPayment));
                    CalculateChange();
                }
            }
        }

        private string _cashReceived = "0.00";
        public string CashReceived
        {
            get => _cashReceived;
            set
            {
                if (SetProperty(ref _cashReceived, value))
                {
                    CalculateChange();
                }
            }
        }

        private decimal _changeAmount;
        public decimal ChangeAmount
        {
            get => _changeAmount;
            set => SetProperty(ref _changeAmount, value);
        }

        private bool _isProcessing;
        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        private string _processingMessage = "Processing...";
        public string ProcessingMessage
        {
            get => _processingMessage;
            set => SetProperty(ref _processingMessage, value);
        }

        // Calculated Properties
        public decimal SubTotal => CartItems.Sum(item => item.TotalPrice);
        public decimal TaxRate => 0.18m; // 18% VAT for Kosovo
        public decimal TaxAmount => SubTotal * TaxRate;
        public decimal TotalAmount => SubTotal + TaxAmount;
        public bool HasCartItems => CartItems.Any();
        public bool IsCashPayment => SelectedPaymentMethod == "Cash";
        public bool HasChange => ChangeAmount > 0;

        public bool CanProcessPayment => HasCartItems &&
            (SelectedPaymentMethod != "Cash" ||
             (decimal.TryParse(CashReceived, out decimal cash) && cash >= TotalAmount));

        public ObservableCollection<string> PaymentMethods { get; set; } = new ObservableCollection<string>
        {
            "Cash", "Card", "Bank Transfer", "Check"
        };

        #endregion

        #region Commands

        public ICommand SearchProductsCommand { get; private set; }
        public ICommand ScanBarcodeCommand { get; private set; }
        public ICommand AddToCartCommand { get; private set; }
        public ICommand RemoveFromCartCommand { get; private set; }
        public ICommand IncreaseQuantityCommand { get; private set; }
        public ICommand DecreaseQuantityCommand { get; private set; }
        public ICommand ClearCartCommand { get; private set; }
        public ICommand ProcessPaymentCommand { get; private set; }
        public ICommand SaveDraftCommand { get; private set; }
        public ICommand NewSaleCommand { get; private set; }
        public ICommand PrintLastReceiptCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            SearchProductsCommand = new Command(() => FilterProducts());
            ScanBarcodeCommand = new Command(async () => await ScanBarcode());
            AddToCartCommand = new Command<Product>(async (product) => await AddToCart(product));
            RemoveFromCartCommand = new Command<CartItem>((item) => RemoveFromCart(item));
            IncreaseQuantityCommand = new Command<CartItem>((item) => IncreaseQuantity(item));
            DecreaseQuantityCommand = new Command<CartItem>((item) => DecreaseQuantity(item));
            ClearCartCommand = new Command(async () => await ClearCart());
            ProcessPaymentCommand = new Command(async () => await ProcessPayment());
            SaveDraftCommand = new Command(async () => await SaveDraft());
            NewSaleCommand = new Command(() => StartNewSale());
            PrintLastReceiptCommand = new Command(async () => await PrintLastReceipt());
        }

        private void InitializeData()
        {
            // Initialize with empty collections
            AllProducts.Clear();
            FilteredProducts.Clear();
            CartItems.Clear();
            RecentTransactions.Clear();
        }

        private async void LoadProducts()
        {
            try
            {
                var products = await _databaseService.GetAllProductsAsync();
                AllProducts.Clear();
                FilteredProducts.Clear();

                foreach (var product in products.Where(p => p.IsActive && p.Stock > 0))
                {
                    AllProducts.Add(product);
                    FilteredProducts.Add(product);
                }

                await LoadRecentTransactions();
            }
            catch (Exception ex)
            {
                await _alertService.ShowErrorAsync($"Failed to load products: {ex.Message}");
            }
        }

        private async Task LoadRecentTransactions()
        {
            try
            {
                var today = DateTime.Today;
                var transactions = await _databaseService.GetTransactionsByDateRangeAsync(today, today.AddDays(1));

                RecentTransactions.Clear();
                foreach (var transaction in transactions.OrderByDescending(t => t.TransactionDate).Take(10))
                {
                    RecentTransactions.Add(transaction);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load recent transactions: {ex.Message}");
            }
        }

        #endregion

        #region Product Methods

        private void FilterProducts()
        {
            FilteredProducts.Clear();

            var filtered = string.IsNullOrWhiteSpace(ProductSearchText)
                ? AllProducts
                : AllProducts.Where(p =>
                    p.Name.Contains(ProductSearchText, StringComparison.OrdinalIgnoreCase) ||
                    p.ProductCode.Contains(ProductSearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var product in filtered.Take(20)) // Limit to 20 results for performance
            {
                FilteredProducts.Add(product);
            }
        }

        private async Task ScanBarcode()
        {
            try
            {
                // Simulate barcode scanning - in real app, use a barcode scanner library
                var result = await _alertService.ShowConfirmationAsync(
                    "Barcode Scanner",
                    "Barcode scanning will be implemented with a camera library.",
                    "OK",
                    "Cancel");

                if (result)
                {
                    // For demo, search for first product
                    if (FilteredProducts.Any())
                    {
                        await AddToCart(FilteredProducts.First());
                    }
                }
            }
            catch (Exception ex)
            {
                await _alertService.ShowErrorAsync($"Barcode scan failed: {ex.Message}");
            }
        }

        #endregion

        #region Cart Methods

        private async Task AddToCart(Product product)
        {
            try
            {
                if (product == null) return;

                // Check if product already in cart
                var existingItem = CartItems.FirstOrDefault(item => item.Product.Id == product.Id);

                if (existingItem != null)
                {
                    // Increase quantity if stock allows
                    if (existingItem.Quantity < product.Stock)
                    {
                        existingItem.Quantity++;
                        existingItem.TotalPrice = existingItem.Quantity * existingItem.Product.Price;
                    }
                    else
                    {
                        await _alertService.ShowWarningAsync($"Not enough stock. Available: {product.Stock}");
                        return;
                    }
                }
                else
                {
                    // Add new item to cart
                    CartItems.Add(new CartItem
                    {
                        Product = product,
                        Quantity = 1,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price
                    });
                }

                UpdateCalculatedProperties();
            }
            catch (Exception ex)
            {
                await _alertService.ShowErrorAsync($"Failed to add product to cart: {ex.Message}");
            }
        }

        private void RemoveFromCart(CartItem item)
        {
            if (item != null)
            {
                CartItems.Remove(item);
                UpdateCalculatedProperties();
            }
        }

        private void IncreaseQuantity(CartItem item)
        {
            if (item != null && item.Quantity < item.Product.Stock)
            {
                item.Quantity++;
                item.TotalPrice = item.Quantity * item.UnitPrice;
                UpdateCalculatedProperties();
            }
        }

        private void DecreaseQuantity(CartItem item)
        {
            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                    item.TotalPrice = item.Quantity * item.UnitPrice;
                }
                else
                {
                    RemoveFromCart(item);
                }
                UpdateCalculatedProperties();
            }
        }

        private async Task ClearCart()
        {
            var result = await _alertService.ShowConfirmationAsync(
                "Clear Cart",
                "Are you sure you want to clear all items from the cart?",
                "Yes",
                "No");

            if (result)
            {
                CartItems.Clear();
                UpdateCalculatedProperties();
            }
        }

        #endregion

        #region Payment Methods

        private async Task ProcessPayment()
        {
            try
            {
                if (!CanProcessPayment)
                {
                    await _alertService.ShowErrorAsync("Cannot process payment. Please check cart and payment details.");
                    return;
                }

                IsProcessing = true;
                ProcessingMessage = "Processing payment...";

                // Create transaction
                var transaction = new Transaction
                {
                    TransactionNumber = GenerateTransactionNumber(),
                    TransactionDate = DateTime.Now,
                    CustomerName = string.IsNullOrWhiteSpace(CustomerName) ? "Walk-in Customer" : CustomerName,
                    CustomerPhone = CustomerPhone,
                    CustomerEmail = CustomerEmail,
                    PaymentMethod = SelectedPaymentMethod,
                    SubTotal = SubTotal,
                    TaxAmount = TaxAmount,
                    TotalAmount = TotalAmount,
                    Status = "Completed",
                    CreatedDate = DateTime.Now,
                    UserId = (await _authService.GetCurrentUserAsync())?.Id ?? 1
                };

                ProcessingMessage = "Creating transaction record...";

                // Save transaction to database
                var savedTransaction = await _databaseService.CreateTransactionAsync(transaction);

                if (savedTransaction != null)
                {
                    ProcessingMessage = "Updating inventory...";

                    // Update product stock and create transaction items
                    foreach (var cartItem in CartItems)
                    {
                        // Create transaction item
                        var transactionItem = new TransactionItem
                        {
                            TransactionId = savedTransaction.Id,
                            ProductId = cartItem.Product.Id,
                            ProductName = cartItem.Product.Name,
                            Quantity = cartItem.Quantity,
                            UnitPrice = cartItem.UnitPrice,
                            TotalAmount = cartItem.TotalPrice,
                            TaxRate = TaxRate
                        };

                        await _databaseService.CreateTransactionItemAsync(transactionItem);

                        // Update product stock
                        var newStock = cartItem.Product.Stock - cartItem.Quantity;
                        await _databaseService.UpdateProductStockAsync(cartItem.Product.Id, newStock);
                    }

                    ProcessingMessage = "Generating fiscal receipt...";

                    // Simulate fiscal receipt generation
                    await Task.Delay(1000);

                    // Show success and receipt
                    await ShowReceipt(savedTransaction);

                    // Clear cart and start new sale
                    StartNewSale();

                    // Reload recent transactions
                    await LoadRecentTransactions();

                    await _alertService.ShowSuccessAsync(
                        $"Payment processed successfully!\nTransaction: {transaction.TransactionNumber}",
                        "Payment Complete");
                }
                else
                {
                    await _alertService.ShowErrorAsync("Failed to create transaction record.");
                }
            }
            catch (Exception ex)
            {
                await _alertService.ShowErrorAsync($"Payment processing failed: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task SaveDraft()
        {
            try
            {
                if (!HasCartItems)
                {
                    await _alertService.ShowWarningAsync("Cart is empty. Nothing to save.");
                    return;
                }

                // Create draft transaction
                var transaction = new Transaction
                {
                    TransactionNumber = GenerateTransactionNumber() + "-DRAFT",
                    TransactionDate = DateTime.Now,
                    CustomerName = string.IsNullOrWhiteSpace(CustomerName) ? "Walk-in Customer" : CustomerName,
                    CustomerPhone = CustomerPhone,
                    CustomerEmail = CustomerEmail,
                    PaymentMethod = SelectedPaymentMethod,
                    SubTotal = SubTotal,
                    TaxAmount = TaxAmount,
                    TotalAmount = TotalAmount,
                    Status = "Draft",
                    CreatedDate = DateTime.Now,
                    UserId = (await _authService.GetCurrentUserAsync())?.Id ?? 1
                };

                var savedTransaction = await _databaseService.CreateTransactionAsync(transaction);

                if (savedTransaction != null)
                {
                    await _alertService.ShowSuccessAsync("Draft saved successfully!");
                    StartNewSale();
                }
                else
                {
                    await _alertService.ShowErrorAsync("Failed to save draft.");
                }
            }
            catch (Exception ex)
            {
                await _alertService.ShowErrorAsync($"Failed to save draft: {ex.Message}");
            }
        }

        private async Task PrintLastReceipt()
        {
            try
            {
                if (RecentTransactions.Any())
                {
                    var lastTransaction = RecentTransactions.First();
                    await ShowReceipt(lastTransaction);
                }
                else
                {
                    await _alertService.ShowWarningAsync("No recent transactions found.");
                }
            }
            catch (Exception ex)
            {
                await _alertService.ShowErrorAsync($"Failed to print receipt: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private void StartNewSale()
        {
            CartItems.Clear();
            CustomerName = string.Empty;
            CustomerPhone = string.Empty;
            CustomerEmail = string.Empty;
            SelectedPaymentMethod = "Cash";
            CashReceived = "0.00";
            ProductSearchText = string.Empty;

            UpdateCalculatedProperties();
            FilterProducts();
        }

        private void UpdateCalculatedProperties()
        {
            OnPropertyChanged(nameof(SubTotal));
            OnPropertyChanged(nameof(TaxAmount));
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(HasCartItems));
            OnPropertyChanged(nameof(CanProcessPayment));

            CalculateChange();
        }

        private void CalculateChange()
        {
            if (IsCashPayment && decimal.TryParse(CashReceived, out decimal cash))
            {
                ChangeAmount = Math.Max(0, cash - TotalAmount);
            }
            else
            {
                ChangeAmount = 0;
            }

            OnPropertyChanged(nameof(HasChange));
            OnPropertyChanged(nameof(CanProcessPayment));
        }

        private string GenerateTransactionNumber()
        {
            return $"TXN{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
        }

        private async Task ShowReceipt(Transaction transaction)
        {
            var receiptContent = GenerateReceiptContent(transaction);

            await _alertService.ShowAlertAsync("Fiscal Receipt", receiptContent, "Close");

            // In a real app, you would:
            // 1. Send to fiscal printer
            // 2. Generate PDF
            // 3. Send to email if customer email provided
            // 4. Store receipt copy in database
        }

        private string GenerateReceiptContent(Transaction transaction)
        {
            var receipt = $@"
═══════════════════════════════
          FISCAL RECEIPT
═══════════════════════════════

Transaction: {transaction.TransactionNumber}
Date: {transaction.TransactionDate:yyyy-MM-dd HH:mm:ss}
Customer: {transaction.CustomerName}
Payment: {transaction.PaymentMethod}

───────────────────────────────
Items:
";

            // Add cart items (in real app, fetch from transaction items)
            foreach (var item in CartItems)
            {
                receipt += $@"
{item.Product.Name}
  {item.Quantity} x €{item.UnitPrice:F2} = €{item.TotalPrice:F2}";
            }

            receipt += $@"

───────────────────────────────
Subtotal:     €{transaction.SubTotal:F2}
Tax (18%):    €{transaction.TaxAmount:F2}
───────────────────────────────
TOTAL:        €{transaction.TotalAmount:F2}

";

            if (IsCashPayment)
            {
                receipt += $@"Cash Received: €{CashReceived}
Change:        €{ChangeAmount:F2}

";
            }

            receipt += @"
Thank you for your business!
═══════════════════════════════";

            return receipt;
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

    // Supporting Models
    public class CartItem : INotifyPropertyChanged
    {
        private int _quantity;
        private decimal _totalPrice;

        public Product Product { get; set; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
            }
        }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice
        {
            get => _totalPrice;
            set
            {
                _totalPrice = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
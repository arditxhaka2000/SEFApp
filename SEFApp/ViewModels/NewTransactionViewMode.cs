using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SEFApp.Models.Database;
using SEFApp.Services.Interfaces;

namespace SEFApp.ViewModels
{
    public class NewTransactionViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAlertService _alertService;
        private readonly INavigationService _navigationService;
        private readonly IAuthenticationService _authService;

        public NewTransactionViewModel(
            IDatabaseService databaseService,
            IAlertService alertService,
            INavigationService navigationService,
            IAuthenticationService authService)
        {
            _databaseService = databaseService;
            _alertService = alertService;
            _navigationService = navigationService;
            _authService = authService;

            System.Diagnostics.Debug.WriteLine("NewTransactionViewModel created successfully");

            InitializeCommands();
            InitializeData();
            _ = Task.Run(async () => await InitializeTransaction());
        }

        #region Properties

        private string _transactionNumber = string.Empty;
        public string TransactionNumber
        {
            get => _transactionNumber;
            set => SetProperty(ref _transactionNumber, value);
        }

        private DateTime _transactionDate = DateTime.Today;
        public DateTime TransactionDate
        {
            get => _transactionDate;
            set => SetProperty(ref _transactionDate, value);
        }

        private TimeSpan _transactionTime = DateTime.Now.TimeOfDay;
        public TimeSpan TransactionTime
        {
            get => _transactionTime;
            set => SetProperty(ref _transactionTime, value);
        }

        private string _customerName = "Walk-in Customer";
        public string CustomerName
        {
            get => _customerName;
            set
            {
                if (SetProperty(ref _customerName, value))
                {
                    OnPropertyChanged(nameof(CanSaveTransaction));
                    ((Command)SaveTransactionCommand).ChangeCanExecute();
                }
            }
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
            set => SetProperty(ref _selectedPaymentMethod, value);
        }

        private string _selectedStatus = "Pending";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        private string _notes = string.Empty;
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _isSaving;
        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
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

        private Product _selectedProduct;
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        private int _selectedQuantity = 1;
        public int SelectedQuantity
        {
            get => _selectedQuantity;
            set
            {
                if (value > 0)
                {
                    SetProperty(ref _selectedQuantity, value);
                }
            }
        }

        private ObservableCollection<TransactionItem> _transactionItems = new();
        public ObservableCollection<TransactionItem> TransactionItems
        {
            get => _transactionItems;
            set => SetProperty(ref _transactionItems, value);
        }

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

        // Calculated Properties
        public decimal SubTotal => TransactionItems.Sum(item => item.TotalAmount);
        public decimal TaxRate => 0.18m; // 18% VAT for Kosovo
        public decimal TaxAmount => SubTotal * TaxRate;
        public decimal TotalAmount => SubTotal + TaxAmount;
        public bool HasTransactionItems => TransactionItems.Any();
        public bool CanSaveTransaction => HasTransactionItems && !string.IsNullOrWhiteSpace(CustomerName) && !IsSaving;

        // Options
        public ObservableCollection<string> PaymentMethods { get; set; } = new ObservableCollection<string>
        {
            "Cash", "Card", "Bank Transfer", "Check"
        };

        public ObservableCollection<string> StatusOptions { get; set; } = new ObservableCollection<string>
        {
            "Pending", "Completed", "Draft"
        };

        public ObservableCollection<int> QuantityOptions { get; set; } = new ObservableCollection<int>
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10
        };

        #endregion

        #region Commands

        public ICommand SaveTransactionCommand { get; private set; }
        public ICommand SaveDraftCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand AddItemCommand { get; private set; }
        public ICommand RemoveItemCommand { get; private set; }
        public ICommand SearchProductsCommand { get; private set; }
        public ICommand SelectProductCommand { get; private set; }
        public ICommand IncreaseQuantityCommand { get; private set; }
        public ICommand DecreaseQuantityCommand { get; private set; }
        public ICommand ClearTransactionCommand { get; private set; }
        public ICommand RefreshProductsCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            SaveTransactionCommand = new Command(async () => await SaveTransaction(), () => CanSaveTransaction);
            SaveDraftCommand = new Command(async () => await SaveDraft(), () => HasTransactionItems && !IsSaving);
            CancelCommand = new Command(async () => await Cancel());
            AddItemCommand = new Command(async () => await AddItem(), () => SelectedProduct != null && SelectedQuantity > 0);
            RemoveItemCommand = new Command<TransactionItem>((item) => RemoveItem(item));
            SearchProductsCommand = new Command(() => FilterProducts());
            SelectProductCommand = new Command<Product>((product) => SelectProduct(product));
            IncreaseQuantityCommand = new Command<TransactionItem>((item) => IncreaseQuantity(item));
            DecreaseQuantityCommand = new Command<TransactionItem>((item) => DecreaseQuantity(item));
            ClearTransactionCommand = new Command(async () => await ClearTransaction());
            RefreshProductsCommand = new Command(async () => await LoadProducts());
        }

        private void InitializeData()
        {
            try
            {
                // Initialize collections
                TransactionItems.Clear();
                AllProducts.Clear();
                FilteredProducts.Clear();

                // Generate transaction number
                TransactionNumber = GenerateTransactionNumber();

                System.Diagnostics.Debug.WriteLine($"Transaction initialized with number: {TransactionNumber}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeData error: {ex.Message}");
            }
        }

        private async Task InitializeTransaction()
        {
            try
            {
                IsLoading = true;

                // Load available products
                await LoadProducts();

                // Set default values
                CustomerName = "Walk-in Customer";
                TransactionDate = DateTime.Today;
                TransactionTime = DateTime.Now.TimeOfDay;
                SelectedPaymentMethod = "Cash";
                SelectedStatus = "Pending";

                System.Diagnostics.Debug.WriteLine("Transaction initialization completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeTransaction error: {ex.Message}");
                await _alertService.ShowErrorAsync($"Failed to initialize transaction: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Product Management

        private async Task LoadProducts()
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

                System.Diagnostics.Debug.WriteLine($"Loaded {AllProducts.Count} products");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadProducts error: {ex.Message}");
                await _alertService.ShowErrorAsync($"Failed to load products: {ex.Message}");
            }
        }

        private void FilterProducts()
        {
            try
            {
                FilteredProducts.Clear();

                var filtered = string.IsNullOrWhiteSpace(ProductSearchText)
                    ? AllProducts
                    : AllProducts.Where(p =>
                        p.Name.Contains(ProductSearchText, StringComparison.OrdinalIgnoreCase) ||
                        p.ProductCode.Contains(ProductSearchText, StringComparison.OrdinalIgnoreCase) ||
                        p.Category.Contains(ProductSearchText, StringComparison.OrdinalIgnoreCase));

                foreach (var product in filtered.Take(50)) // Limit results for performance
                {
                    FilteredProducts.Add(product);
                }

                System.Diagnostics.Debug.WriteLine($"Filtered to {FilteredProducts.Count} products");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FilterProducts error: {ex.Message}");
            }
        }

        private void SelectProduct(Product product)
        {
            if (product != null)
            {
                SelectedProduct = product;
                SelectedQuantity = 1;

                OnPropertyChanged(nameof(SelectedProduct));
                OnPropertyChanged(nameof(SelectedQuantity));
                ((Command)AddItemCommand).ChangeCanExecute();

                System.Diagnostics.Debug.WriteLine($"Selected product: {product.Name}");
            }
        }

        #endregion

        #region Transaction Items Management

        private async Task AddItem()
        {
            try
            {
                if (SelectedProduct == null)
                {
                    await _alertService.ShowWarningAsync("Please select a product first.");
                    return;
                }

                if (SelectedQuantity <= 0)
                {
                    await _alertService.ShowWarningAsync("Please enter a valid quantity.");
                    return;
                }

                // Check stock availability
                if (SelectedQuantity > SelectedProduct.Stock)
                {
                    await _alertService.ShowWarningAsync($"Not enough stock. Available: {SelectedProduct.Stock}");
                    return;
                }

                // Check if product already exists in transaction
                var existingItem = TransactionItems.FirstOrDefault(item => item.ProductId == SelectedProduct.Id);
                if (existingItem != null)
                {
                    var newQuantity = existingItem.Quantity + SelectedQuantity;
                    if (newQuantity > SelectedProduct.Stock)
                    {
                        await _alertService.ShowWarningAsync($"Total quantity would exceed stock. Available: {SelectedProduct.Stock}, Current in cart: {existingItem.Quantity}");
                        return;
                    }

                    existingItem.Quantity = newQuantity;
                    existingItem.TotalAmount = existingItem.Quantity * existingItem.UnitPrice;
                }
                else
                {
                    // Add new item
                    var newItem = new TransactionItem
                    {
                        ProductId = SelectedProduct.Id,
                        ProductName = SelectedProduct.Name,
                        Quantity = SelectedQuantity,
                        UnitPrice = SelectedProduct.Price,
                        TotalAmount = SelectedQuantity * SelectedProduct.Price,
                        TaxRate = TaxRate
                    };

                    TransactionItems.Add(newItem);
                }

                // Clear selection
                SelectedProduct = null;
                SelectedQuantity = 1;
                ProductSearchText = string.Empty;

                UpdateCalculatedProperties();

                System.Diagnostics.Debug.WriteLine($"Added item to transaction. Total items: {TransactionItems.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddItem error: {ex.Message}");
                await _alertService.ShowErrorAsync($"Error adding item: {ex.Message}");
            }
        }

        private void RemoveItem(TransactionItem item)
        {
            try
            {
                if (item != null)
                {
                    TransactionItems.Remove(item);
                    UpdateCalculatedProperties();
                    System.Diagnostics.Debug.WriteLine($"Removed item from transaction. Total items: {TransactionItems.Count}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RemoveItem error: {ex.Message}");
            }
        }

        private void IncreaseQuantity(TransactionItem item)
        {
            try
            {
                if (item != null)
                {
                    var product = AllProducts.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product != null && item.Quantity < product.Stock)
                    {
                        item.Quantity++;
                        item.TotalAmount = item.Quantity * item.UnitPrice;
                        UpdateCalculatedProperties();
                        System.Diagnostics.Debug.WriteLine($"Increased quantity for {item.ProductName} to {item.Quantity}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IncreaseQuantity error: {ex.Message}");
            }
        }

        private void DecreaseQuantity(TransactionItem item)
        {
            try
            {
                if (item != null)
                {
                    if (item.Quantity > 1)
                    {
                        item.Quantity--;
                        item.TotalAmount = item.Quantity * item.UnitPrice;
                        UpdateCalculatedProperties();
                        System.Diagnostics.Debug.WriteLine($"Decreased quantity for {item.ProductName} to {item.Quantity}");
                    }
                    else
                    {
                        RemoveItem(item);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DecreaseQuantity error: {ex.Message}");
            }
        }

        private async Task ClearTransaction()
        {
            try
            {
                if (!HasTransactionItems)
                {
                    await _alertService.ShowWarningAsync("Transaction is already empty.");
                    return;
                }

                var result = await _alertService.ShowConfirmationAsync(
                    "Clear Transaction",
                    "Are you sure you want to clear all items from this transaction?",
                    "Yes",
                    "No");

                if (result)
                {
                    TransactionItems.Clear();
                    UpdateCalculatedProperties();
                    System.Diagnostics.Debug.WriteLine("Transaction cleared");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClearTransaction error: {ex.Message}");
                await _alertService.ShowErrorAsync($"Error clearing transaction: {ex.Message}");
            }
        }

        #endregion

        #region Save Operations

        private async Task SaveTransaction()
        {
            try
            {
                if (!CanSaveTransaction)
                {
                    await _alertService.ShowErrorAsync("Cannot save transaction. Please check required fields and add items.");
                    return;
                }

                IsSaving = true;

                // Validate transaction
                if (!await ValidateTransaction())
                {
                    return;
                }

                // Create transaction
                var transaction = new Transaction
                {
                    TransactionNumber = TransactionNumber,
                    TransactionDate = TransactionDate.Date.Add(TransactionTime),
                    CustomerName = string.IsNullOrWhiteSpace(CustomerName) ? "Walk-in Customer" : CustomerName.Trim(),
                    CustomerPhone = CustomerPhone?.Trim() ?? string.Empty,
                    CustomerEmail = CustomerEmail?.Trim() ?? string.Empty,
                    PaymentMethod = SelectedPaymentMethod,
                    SubTotal = SubTotal,
                    TaxAmount = TaxAmount,
                    TotalAmount = TotalAmount,
                    Status = SelectedStatus,
                    Notes = Notes?.Trim() ?? string.Empty,
                    UserId = (await _authService.GetCurrentUserAsync())?.Id ?? 1,
                    CreatedDate = DateTime.Now
                };

                // Save transaction
                var savedTransaction = await _databaseService.CreateTransactionAsync(transaction);

                if (savedTransaction != null)
                {
                    // Save transaction items
                    await SaveTransactionItems(savedTransaction.Id);

                    // Update product stock if transaction is completed
                    if (SelectedStatus == "Completed")
                    {
                        await UpdateProductStocks();
                    }

                    await _alertService.ShowSuccessAsync($"Transaction {TransactionNumber} saved successfully!");
                    await _navigationService.NavigateBackAsync();
                }
                else
                {
                    await _alertService.ShowErrorAsync("Failed to save transaction.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveTransaction error: {ex.Message}");
                await _alertService.ShowErrorAsync($"Error saving transaction: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private async Task SaveDraft()
        {
            try
            {
                if (!HasTransactionItems)
                {
                    await _alertService.ShowWarningAsync("Cannot save draft. Please add items first.");
                    return;
                }

                IsSaving = true;

                // Create draft transaction
                var transaction = new Transaction
                {
                    TransactionNumber = TransactionNumber + "-DRAFT",
                    TransactionDate = TransactionDate.Date.Add(TransactionTime),
                    CustomerName = string.IsNullOrWhiteSpace(CustomerName) ? "Walk-in Customer" : CustomerName.Trim(),
                    CustomerPhone = CustomerPhone?.Trim() ?? string.Empty,
                    CustomerEmail = CustomerEmail?.Trim() ?? string.Empty,
                    PaymentMethod = SelectedPaymentMethod,
                    SubTotal = SubTotal,
                    TaxAmount = TaxAmount,
                    TotalAmount = TotalAmount,
                    Status = "Draft",
                    Notes = Notes?.Trim() ?? string.Empty,
                    UserId = (await _authService.GetCurrentUserAsync())?.Id ?? 1,
                    CreatedDate = DateTime.Now
                };

                var savedTransaction = await _databaseService.CreateTransactionAsync(transaction);

                if (savedTransaction != null)
                {
                    // Save transaction items
                    await SaveTransactionItems(savedTransaction.Id);

                    await _alertService.ShowSuccessAsync($"Draft {TransactionNumber} saved successfully!");
                    await _navigationService.NavigateBackAsync();
                }
                else
                {
                    await _alertService.ShowErrorAsync("Failed to save draft.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveDraft error: {ex.Message}");
                await _alertService.ShowErrorAsync($"Error saving draft: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private async Task SaveTransactionItems(int transactionId)
        {
            try
            {
                foreach (var item in TransactionItems)
                {
                    item.TransactionId = transactionId;
                    await _databaseService.CreateTransactionItemAsync(item);
                }

                System.Diagnostics.Debug.WriteLine($"Saved {TransactionItems.Count} transaction items");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveTransactionItems error: {ex.Message}");
                throw;
            }
        }

        private async Task UpdateProductStocks()
        {
            try
            {
                foreach (var item in TransactionItems)
                {
                    var product = AllProducts.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product != null)
                    {
                        var newStock = product.Stock - item.Quantity;
                        await _databaseService.UpdateProductStockAsync(product.Id, newStock);
                        product.Stock = newStock; // Update local copy
                    }
                }

                System.Diagnostics.Debug.WriteLine("Updated product stocks");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateProductStocks error: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Validation

        private async Task<bool> ValidateTransaction()
        {
            try
            {
                // Validate customer name
                if (string.IsNullOrWhiteSpace(CustomerName))
                {
                    await _alertService.ShowErrorAsync("Customer name is required.");
                    return false;
                }

                // Validate items
                if (!HasTransactionItems)
                {
                    await _alertService.ShowErrorAsync("Please add at least one item to the transaction.");
                    return false;
                }

                // Validate stock availability
                foreach (var item in TransactionItems)
                {
                    var product = AllProducts.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product == null)
                    {
                        await _alertService.ShowErrorAsync($"Product {item.ProductName} not found.");
                        return false;
                    }

                    if (item.Quantity > product.Stock)
                    {
                        await _alertService.ShowErrorAsync($"Not enough stock for {item.ProductName}. Available: {product.Stock}, Required: {item.Quantity}");
                        return false;
                    }
                }

                // Validate total amount
                if (TotalAmount <= 0)
                {
                    await _alertService.ShowErrorAsync("Transaction total must be greater than zero.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ValidateTransaction error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Navigation

        private async Task Cancel()
        {
            try
            {
                bool hasUnsavedChanges = HasTransactionItems ||
                                       !string.IsNullOrWhiteSpace(CustomerName) ||
                                       !string.IsNullOrWhiteSpace(Notes);

                if (hasUnsavedChanges)
                {
                    var result = await _alertService.ShowConfirmationAsync(
                        "Unsaved Changes",
                        "You have unsaved changes. Are you sure you want to cancel?",
                        "Yes",
                        "No");

                    if (!result) return;
                }

                await _navigationService.NavigateBackAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cancel error: {ex.Message}");
                await _alertService.ShowErrorAsync($"Error canceling transaction: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private void UpdateCalculatedProperties()
        {
            try
            {
                OnPropertyChanged(nameof(SubTotal));
                OnPropertyChanged(nameof(TaxAmount));
                OnPropertyChanged(nameof(TotalAmount));
                OnPropertyChanged(nameof(HasTransactionItems));
                OnPropertyChanged(nameof(CanSaveTransaction));

                ((Command)SaveTransactionCommand).ChangeCanExecute();
                ((Command)SaveDraftCommand).ChangeCanExecute();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCalculatedProperties error: {ex.Message}");
            }
        }

        private string GenerateTransactionNumber()
        {
            return $"TXN{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
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
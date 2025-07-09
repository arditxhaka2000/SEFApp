using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SEFApp.Models.Database;
using SEFApp.Services.Interfaces;

namespace SEFApp.ViewModels
{
    public class AddProductModalViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAlertService _alertService;
        private Product _originalProduct;

        public AddProductModalViewModel(IDatabaseService databaseService, IAlertService alertService)
        {
            _databaseService = databaseService;
            _alertService = alertService;

            InitializeCommands();
            InitializeData();
        }

        public event EventHandler<Product> ProductSaved;
        public event EventHandler CancelRequested;

        #region Properties

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _productCode = string.Empty;
        public string ProductCode
        {
            get => _productCode;
            set
            {
                if (SetProperty(ref _productCode, value))
                {
                    ValidateProductCode();
                    ValidateForm();
                }
            }
        }

        private string _productName = string.Empty;
        public string ProductName
        {
            get => _productName;
            set
            {
                if (SetProperty(ref _productName, value))
                {
                    ValidateProductName();
                    ValidateForm();
                }
            }
        }

        private string _category = string.Empty;
        public string Category
        {
            get => _category;
            set
            {
                if (SetProperty(ref _category, value))
                {
                    ValidateCategory();
                    ValidateForm();
                }
            }
        }

        private string _newCategory = string.Empty;
        public string NewCategory
        {
            get => _newCategory;
            set
            {
                if (SetProperty(ref _newCategory, value))
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        Category = value.Trim();
                    }
                }
            }
        }

        private bool _isAddingNewCategory;
        public bool IsAddingNewCategory
        {
            get => _isAddingNewCategory;
            set => SetProperty(ref _isAddingNewCategory, value);
        }

        private string _unit = "pcs";
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        private string _price = "0.00";
        public string Price
        {
            get => _price;
            set
            {
                if (SetProperty(ref _price, value))
                {
                    ValidatePrice();
                    ValidateForm();
                }
            }
        }

        private string _taxRateDisplay = "18% (Standard)";
        public string TaxRateDisplay
        {
            get => _taxRateDisplay;
            set => SetProperty(ref _taxRateDisplay, value);
        }

        private string _stock = "0";
        public string Stock
        {
            get => _stock;
            set => SetProperty(ref _stock, value);
        }

        private string _minStock = "0";
        public string MinStock
        {
            get => _minStock;
            set => SetProperty(ref _minStock, value);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        // Validation Properties
        private string _productCodeError = string.Empty;
        public string ProductCodeError
        {
            get => _productCodeError;
            set
            {
                if (SetProperty(ref _productCodeError, value))
                {
                    OnPropertyChanged(nameof(HasProductCodeError));
                }
            }
        }

        private string _productNameError = string.Empty;
        public string ProductNameError
        {
            get => _productNameError;
            set
            {
                if (SetProperty(ref _productNameError, value))
                {
                    OnPropertyChanged(nameof(HasProductNameError));
                }
            }
        }

        private string _categoryError = string.Empty;
        public string CategoryError
        {
            get => _categoryError;
            set
            {
                if (SetProperty(ref _categoryError, value))
                {
                    OnPropertyChanged(nameof(HasCategoryError));
                }
            }
        }

        private string _priceError = string.Empty;
        public string PriceError
        {
            get => _priceError;
            set
            {
                if (SetProperty(ref _priceError, value))
                {
                    OnPropertyChanged(nameof(HasPriceError));
                }
            }
        }

        private bool _isFormValid;
        public bool IsFormValid
        {
            get => _isFormValid;
            set => SetProperty(ref _isFormValid, value);
        }

        public bool HasProductCodeError => !string.IsNullOrEmpty(ProductCodeError);
        public bool HasProductNameError => !string.IsNullOrEmpty(ProductNameError);
        public bool HasCategoryError => !string.IsNullOrEmpty(CategoryError);
        public bool HasPriceError => !string.IsNullOrEmpty(PriceError);

        // Options
        public ObservableCollection<string> UnitOptions { get; set; }
        public ObservableCollection<string> TaxRateOptions { get; set; }
        public ObservableCollection<string> ExistingCategories { get; set; }

        #endregion

        #region Commands

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand AddNewCategoryCommand { get; private set; }
        public ICommand SelectCategoryCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            SaveCommand = new Command(async () => await SaveProduct(), () => IsFormValid && !IsLoading);
            CancelCommand = new Command(async () => await Cancel());
            AddNewCategoryCommand = new Command(() => {
                IsAddingNewCategory = true;
                NewCategory = string.Empty;
            });
            SelectCategoryCommand = new Command<string>((category) => {
                Category = category;
                IsAddingNewCategory = false;
            });
        }

        private async void InitializeData()
        {
            // Initialize unit options
            UnitOptions = new ObservableCollection<string>
            {
                "pcs", "kg", "g", "l", "ml", "m", "cm", "m²", "m³", "box", "pack", "bottle", "can", "set"
            };

            // Initialize tax rate options (Kosovo tax rates)
            TaxRateOptions = new ObservableCollection<string>
            {
                "0%", "8%", "18%"
            };

            // Initialize existing categories
            ExistingCategories = new ObservableCollection<string>();
            await LoadExistingCategories();

            
        }

        private async Task LoadExistingCategories()
        {
            try
            {
                var categories = await _databaseService.GetProductCategoriesAsync();
                ExistingCategories.Clear();
                foreach (var category in categories)
                {
                    ExistingCategories.Add(category);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load categories: {ex.Message}");
            }
        }

        #endregion

        #region Methods

        public void LoadProduct(Product product)
        {
            if (product == null) return;

            IsEditing = true;
            _originalProduct = product;

            ProductCode = product.ProductCode ?? string.Empty;
            ProductName = product.Name ?? string.Empty;
            Category = product.Category ?? string.Empty;
            Unit = product.Unit ?? "pcs";
            Price = product.Price.ToString("F2");
            Stock = product.Stock.ToString("F0");
            MinStock = product.MinStock.ToString("F0");
            Description = product.Description ?? string.Empty;
            IsActive = product.IsActive;

            // Set tax rate display based on product tax rate
            TaxRateDisplay = product.TaxRate switch
            {
                0 => "0%",
                0.08m => "8%",
                0.18m => "18%",
                _ => "18%"
            };

            // Validate the form after loading
            ValidateForm();
        }

        private async Task SaveProduct()
        {
            try
            {
                IsLoading = true;

                if (!await ValidateFormAsync())
                {
                    await _alertService.ShowErrorAsync("Please correct the validation errors before saving.");
                    return;
                }

                var product = IsEditing ? _originalProduct : new Product();

                // Map form data to product
                product.ProductCode = ProductCode.Trim();
                product.Name = ProductName.Trim();
                product.Category = Category.Trim();
                product.Unit = Unit;
                product.Price = decimal.Parse(Price);
                product.TaxRate = GetTaxRateFromDisplay(TaxRateDisplay);
                product.Stock = decimal.Parse(Stock);
                product.MinStock = decimal.Parse(MinStock);
                product.Description = Description?.Trim();
                product.IsActive = IsActive;

                if (IsEditing)
                {
                    product.ModifiedDate = DateTime.Now;
                    var success = await _databaseService.UpdateProductAsync(product);

                    if (success)
                    {
                        await _alertService.ShowSuccessAsync("Product updated successfully!");
                        ProductSaved?.Invoke(this, product);
                    }
                    else
                    {
                        await _alertService.ShowErrorAsync("Failed to update product.");
                    }
                }
                else
                {
                    product.CreatedDate = DateTime.Now;
                    var createdProduct = await _databaseService.CreateProductAsync(product);

                    if (createdProduct != null)
                    {
                        await _alertService.ShowSuccessAsync("Product created successfully!");
                        ProductSaved?.Invoke(this, createdProduct);
                    }
                    else
                    {
                        await _alertService.ShowErrorAsync("Failed to create product.");
                    }
                }
            }
            catch (Exception ex)
            {
                await _alertService.ShowErrorAsync($"Error saving product: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task Cancel()
        {
            bool hasChanges = HasUnsavedChanges();

            if (hasChanges)
            {
                var result = await _alertService.ShowConfirmationAsync(
                    "Unsaved Changes",
                    "You have unsaved changes. Are you sure you want to cancel?",
                    "Yes",
                    "No");

                if (!result) return;
            }

            CancelRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool HasUnsavedChanges()
        {
            if (!IsEditing)
            {
                return !string.IsNullOrEmpty(ProductCode) ||
                       !string.IsNullOrEmpty(ProductName) ||
                       !string.IsNullOrEmpty(Category) ||
                       Price != "0.00";
            }

            // Check if current values differ from original
            return _originalProduct.ProductCode != ProductCode ||
                   _originalProduct.Name != ProductName ||
                   _originalProduct.Category != Category ||
                   _originalProduct.Unit != Unit ||
                   _originalProduct.Price != decimal.Parse(Price) ||
                   _originalProduct.Stock != decimal.Parse(Stock) ||
                   _originalProduct.MinStock != decimal.Parse(MinStock) ||
                   _originalProduct.Description != Description ||
                   _originalProduct.IsActive != IsActive;
        }

        #endregion

        #region Validation

        private async void ValidateProductCode()
        {
            ProductCodeError = string.Empty;

            if (string.IsNullOrWhiteSpace(ProductCode))
            {
                ProductCodeError = "Product code is required";
                return;
            }

            if (ProductCode.Trim().Length < 2)
            {
                ProductCodeError = "Product code must be at least 2 characters";
                return;
            }

            // Check for special characters that might cause issues
            if (ProductCode.Contains(" ") || ProductCode.Contains(",") || ProductCode.Contains(";"))
            {
                ProductCodeError = "Product code cannot contain spaces, commas, or semicolons";
                return;
            }

            // Check for duplicate product code (only during editing or when not the same product)
            try
            {
                var existingProduct = await _databaseService.GetProductByCodeAsync(ProductCode.Trim());
                if (existingProduct != null)
                {
                    // If editing, check if it's not the same product
                    if (!IsEditing || existingProduct.Id != _originalProduct.Id)
                    {
                        ProductCodeError = "Product code already exists";
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking product code: {ex.Message}");
            }
        }

        private async void ValidateProductName()
        {
            ProductNameError = string.Empty;

            if (string.IsNullOrWhiteSpace(ProductName))
            {
                ProductNameError = "Product name is required";
                return;
            }

            if (ProductName.Trim().Length < 2)
            {
                ProductNameError = "Product name must be at least 2 characters";
                return;
            }

            if (ProductName.Trim().Length > 100)
            {
                ProductNameError = "Product name must be less than 100 characters";
                return;
            }

            // Check for duplicate product name (only during editing or when not the same product)
            try
            {
                var allProducts = await _databaseService.GetAllProductsAsync();
                var existingProduct = allProducts.FirstOrDefault(p =>
                    p.Name.Equals(ProductName.Trim(), StringComparison.OrdinalIgnoreCase));

                if (existingProduct != null)
                {
                    // If editing, check if it's not the same product
                    if (!IsEditing || existingProduct.Id != _originalProduct.Id)
                    {
                        ProductNameError = "Product name already exists";
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking product name: {ex.Message}");
            }
        }

        private void ValidateCategory()
        {
            CategoryError = string.Empty;

            if (string.IsNullOrWhiteSpace(Category))
            {
                CategoryError = "Category is required";
                return;
            }

            if (Category.Trim().Length < 2)
            {
                CategoryError = "Category must be at least 2 characters";
                return;
            }
        }

        private void ValidatePrice()
        {
            PriceError = string.Empty;

            if (string.IsNullOrWhiteSpace(Price))
            {
                PriceError = "Price is required";
                return;
            }

            if (!decimal.TryParse(Price, out decimal priceValue))
            {
                PriceError = "Please enter a valid price";
                return;
            }

            if (priceValue < 0)
            {
                PriceError = "Price cannot be negative";
                return;
            }

            if (priceValue > 999999.99m)
            {
                PriceError = "Price cannot exceed 999,999.99";
                return;
            }
        }

        private async Task<bool> ValidateFormAsync()
        {
            // Run async validations
            await Task.Run(() => ValidateProductCode());
            await Task.Run(() => ValidateProductName());

            // Run sync validations
            ValidateCategory();
            ValidatePrice();

            // Wait a bit for async validations to complete
            await Task.Delay(100);

            bool isValid = !HasProductCodeError &&
                          !HasProductNameError &&
                          !HasCategoryError &&
                          !HasPriceError;

            IsFormValid = isValid;
            ((Command)SaveCommand).ChangeCanExecute();

            return isValid;
        }

        private void ValidateForm()
        {
            // For immediate UI feedback, run sync validation
            bool isValid = !HasProductCodeError &&
                          !HasProductNameError &&
                          !HasCategoryError &&
                          !HasPriceError;

            IsFormValid = isValid;
            ((Command)SaveCommand).ChangeCanExecute();
        }

        private decimal GetTaxRateFromDisplay(string taxRateDisplay)
        {
            return taxRateDisplay switch
            {
                "0%" => 0.0m,
                "8%" => 0.08m,
                "18%" => 0.18m,
                _ => 0.18m
            };
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
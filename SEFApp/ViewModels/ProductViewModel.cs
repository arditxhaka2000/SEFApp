using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SEFApp.Models.Database;
using SEFApp.Services.Interfaces;

namespace SEFApp.ViewModels
{
    public class ProductViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAlertService _alertService;

        public ProductViewModel(IDatabaseService databaseService, IAlertService alertService)
        {
            _databaseService = databaseService;
            _alertService = alertService;

            InitializeCommands();
            LoadProducts();
        }

        #region Properties

        private ObservableCollection<Product> _products = new();
        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
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
                    FilterProducts();
                }
            }
        }

        private ObservableCollection<Product> _filteredProducts = new();
        public ObservableCollection<Product> FilteredProducts
        {
            get => _filteredProducts;
            set => SetProperty(ref _filteredProducts, value);
        }

        #endregion

        #region Commands

        public ICommand AddProductCommand { get; private set; }
        public ICommand EditProductCommand { get; private set; }
        public ICommand DeleteProductCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            AddProductCommand = new Command(async () => await ShowAddProductModal());
            EditProductCommand = new Command<Product>(async (product) => await ShowEditProductModal(product));
            DeleteProductCommand = new Command<Product>(async (product) => await DeleteProduct(product));
            RefreshCommand = new Command(async () => await LoadProducts());
            SearchCommand = new Command(() => FilterProducts());
        }

        #endregion

        #region Methods

        private async Task LoadProducts()
        {
            try
            {
                IsLoading = true;
                var products = await _databaseService.GetAllProductsAsync();
                Products.Clear();

                foreach (var product in products)
                {
                    Products.Add(product);
                }

                FilterProducts();
            }
            catch (Exception ex)
            {
                await _alertService.ShowErrorAsync($"Failed to load products: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterProducts()
        {
            FilteredProducts.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? Products
                : Products.Where(p =>
                    p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    p.ProductCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var product in filtered)
            {
                FilteredProducts.Add(product);
            }
        }

        private async Task ShowAddProductModal()
        {
            var addProductPage = new Views.AddProductModal(_databaseService, _alertService);
            addProductPage.ProductSaved += OnProductSaved;
            await Shell.Current.Navigation.PushModalAsync(addProductPage);
        }

        private async Task ShowEditProductModal(Product product)
        {
            if (product == null) return;

            var editProductPage = new Views.AddProductModal(_databaseService,_alertService);
            editProductPage.ProductSaved += OnProductSaved;
            await Shell.Current.Navigation.PushModalAsync(editProductPage);
        }

        private async Task DeleteProduct(Product product)
        {
            if (product == null) return;

            var result = await _alertService.ShowConfirmationAsync(
                "Delete Product",
                $"Are you sure you want to delete '{product.Name}'?",
                "Delete",
                "Cancel");

            if (result)
            {
                try
                {
                    IsLoading = true;
                    var success = await _databaseService.DeleteProductAsync(product.Id);

                    if (success)
                    {
                        Products.Remove(product);
                        FilterProducts();
                        await _alertService.ShowSuccessAsync("Product deleted successfully");
                    }
                    else
                    {
                        await _alertService.ShowErrorAsync("Failed to delete product");
                    }
                }
                catch (Exception ex)
                {
                    await _alertService.ShowErrorAsync($"Error deleting product: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async void OnProductSaved(object sender, Product product)
        {
            try
            {
                // Refresh the products list
                await LoadProducts();

                // Close the modal
                if (sender is ContentPage page)
                {
                    await Shell.Current.Navigation.PopModalAsync();
                }
            }
            catch (Exception ex)
            {
                await _alertService.ShowErrorAsync($"Error refreshing products: {ex.Message}");
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
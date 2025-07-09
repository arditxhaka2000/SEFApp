using SEFApp.Models.Database;
using SEFApp.Services.Interfaces;
using SEFApp.ViewModels;

namespace SEFApp.Views
{
    public partial class AddProductModal : ContentPage
    {
        private readonly AddProductModalViewModel _viewModel;

        public event EventHandler<Product> ProductSaved;

        public AddProductModal(IDatabaseService databaseService, IAlertService alertService)
        {
            InitializeComponent();
            _viewModel = new AddProductModalViewModel(databaseService, alertService);
            _viewModel.ProductSaved += OnProductSaved;
            _viewModel.CancelRequested += OnCancelRequested;
            BindingContext = _viewModel;
        }

        private async void OnProductSaved(object sender, Product product)
        {
            ProductSaved?.Invoke(this, product);
        }

        private async void OnCancelRequested(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PopModalAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            // Handle back button press
            if (BindingContext is AddProductModalViewModel viewModel)
            {
                viewModel.CancelCommand.Execute(null);
                return true; // Prevent default back button behavior
            }
            return base.OnBackButtonPressed();
        }

        // Method to load product data (called from ProductViewModel)
        public async Task LoadProductAsync(Product product)
        {
            if (_viewModel != null)
            {
                _viewModel.LoadProduct(product);
            }
        }
    }
}
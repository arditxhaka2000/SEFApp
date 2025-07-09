using SEFApp.Models.Database;
using SEFApp.Services.Interfaces;
using SEFApp.ViewModels;

namespace SEFApp.Views
{
    public partial class AddProductModal : ContentPage
    {
        public event EventHandler<Product> ProductSaved;

        public AddProductModal(IDatabaseService databaseService, IAlertService alertService)
        {
            InitializeComponent();
            var viewModel = new AddProductModalViewModel(databaseService, alertService);
            viewModel.ProductSaved += OnProductSaved;
            viewModel.CancelRequested += OnCancelRequested;
            BindingContext = viewModel;
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
    }
}
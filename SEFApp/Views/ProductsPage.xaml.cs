using SEFApp.ViewModels;

namespace SEFApp.Views
{
    public partial class ProductsPage : ContentPage
    {
        public ProductsPage(ProductViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
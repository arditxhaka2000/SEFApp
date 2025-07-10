using SEFApp.ViewModels;

namespace SEFApp.Views
{
    public partial class NewTransactionPage : ContentPage
    {
        public NewTransactionPage(NewTransactionViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
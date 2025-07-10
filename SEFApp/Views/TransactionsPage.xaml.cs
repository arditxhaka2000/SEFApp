using SEFApp.ViewModels;

namespace SEFApp.Views
{
    public partial class TransactionsPage : ContentPage
    {
        public TransactionsPage(TransactionsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Refresh data when page appears
            if (BindingContext is TransactionsViewModel viewModel)
            {
                viewModel.RefreshCommand.Execute(null);
            }
        }
    }
}
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
    }
}
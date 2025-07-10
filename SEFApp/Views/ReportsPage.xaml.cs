using SEFApp.ViewModels;

namespace SEFApp.Views
{
    public partial class ReportsPage : ContentPage
    {
        public ReportsPage(ReportsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
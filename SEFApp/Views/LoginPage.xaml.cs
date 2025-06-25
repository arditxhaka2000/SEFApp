using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SEFApp.ViewModels;

namespace SEFApp.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly LoginViewModel _viewModel;

        public LoginPage(LoginViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;  // Set it here instead of XAML
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Focus on username field when page appears
            UsernameEntry.Focus();
        }

        protected override bool OnBackButtonPressed()
        {
            // Prevent back button on login page (make it root)
            return true;
        }

        private void OnUsernameEntryCompleted(object sender, EventArgs e)
        {
            // Move focus to password field when username is completed
            PasswordEntry.Focus();
        }

        private void OnPasswordEntryCompleted(object sender, EventArgs e)
        {
            // Trigger login when password entry is completed
            if (_viewModel.LoginCommand.CanExecute(null))
            {
                _viewModel.LoginCommand.Execute(null);
            }
        }
    }
}
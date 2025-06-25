using SEFApp.Services.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SEFApp.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IAuthenticationService _authService;
        private readonly INavigationService _navigationService;
        private readonly IPreferencesService _preferencesService;

        public LoginViewModel
            (
            IAuthenticationService authService,
            INavigationService navigationService,
            IPreferencesService preferencesService)
        {
            _authService = authService;
            _navigationService = navigationService;
            _preferencesService = preferencesService;

            // Initialize commands
            LoginCommand = new Command(async () => await LoginAsync(), CanLogin);

            // Load saved preferences
            LoadSavedCredentials();
        }

        #region Properties

        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    ValidateUsername();
                    ((Command)LoginCommand).ChangeCanExecute();
                }
            }
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    ValidatePassword();
                    ((Command)LoginCommand).ChangeCanExecute();
                }
            }
        }

        private bool _rememberMe;
        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    ((Command)LoginCommand).ChangeCanExecute();
                }
            }
        }

        private string _usernameError = string.Empty;
        public string UsernameError
        {
            get => _usernameError;
            set
            {
                if (SetProperty(ref _usernameError, value))
                {
                    OnPropertyChanged(nameof(HasUsernameError));
                }
            }
        }

        private string _passwordError = string.Empty;
        public string PasswordError
        {
            get => _passwordError;
            set
            {
                if (SetProperty(ref _passwordError, value))
                {
                    OnPropertyChanged(nameof(HasPasswordError));
                }
            }
        }

        private string _loginError = string.Empty;
        public string LoginError
        {
            get => _loginError;
            set
            {
                if (SetProperty(ref _loginError, value))
                {
                    OnPropertyChanged(nameof(HasLoginError));
                }
            }
        }

        public bool HasUsernameError => !string.IsNullOrEmpty(UsernameError);
        public bool HasPasswordError => !string.IsNullOrEmpty(PasswordError);
        public bool HasLoginError => !string.IsNullOrEmpty(LoginError);

        #endregion

        #region Commands

        public ICommand LoginCommand { get; }

        #endregion

        #region Methods

        private bool CanLogin()
        {
            return !IsLoading &&
                   !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !HasUsernameError &&
                   !HasPasswordError;
        }

        private async Task LoginAsync()
        {
            IsLoading = true;
            LoginError = string.Empty;

            try
            {
                // Validate input
                if (!ValidateInput())
                {
                    IsLoading = false;
                    return;
                }

                // Attempt login
                var loginRequest = new LoginRequest
                {
                    Username = Username.Trim(),
                    Password = Password,
                    RememberMe = RememberMe
                };

                var result = await _authService.LoginAsync(loginRequest);

                if (result.IsSuccess)
                {
                    // Save credentials if remember me is checked
                    if (RememberMe)
                    {
                        await SaveCredentials();
                    }
                    else
                    {
                        await ClearSavedCredentials();
                    }

                    // Navigate to main app
                    await _navigationService.NavigateToAsync("//MainPage");
                }
                else
                {
                    LoginError = GetErrorMessage(result.ErrorCode);
                }
            }
            catch (Exception ex)
            {
                LoginError = "Login failed. Please try again.";
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool ValidateInput()
        {
            bool isValid = true;

            ValidateUsername();
            ValidatePassword();

            if (HasUsernameError || HasPasswordError)
            {
                isValid = false;
            }

            return isValid;
        }

        private void ValidateUsername()
        {
            UsernameError = string.Empty;

            if (string.IsNullOrWhiteSpace(Username))
            {
                UsernameError = "Username is required";
                return;
            }

            if (Username.Trim().Length < 3)
            {
                UsernameError = "Username must be at least 3 characters";
                return;
            }
        }

        private void ValidatePassword()
        {
            PasswordError = string.Empty;

            if (string.IsNullOrWhiteSpace(Password))
            {
                PasswordError = "Password is required";
                return;
            }

            if (Password.Length < 4)
            {
                PasswordError = "Password must be at least 4 characters";
                return;
            }
        }

        private async Task SaveCredentials()
        {
            await _preferencesService.SetAsync("saved_username", Username);
            await _preferencesService.SetAsync("remember_me", RememberMe);
        }

        private async Task ClearSavedCredentials()
        {
            await _preferencesService.RemoveAsync("saved_username");
            await _preferencesService.SetAsync("remember_me", false);
        }

        private async void LoadSavedCredentials()
        {
            try
            {
                RememberMe = await _preferencesService.GetAsync("remember_me", false);
                if (RememberMe)
                {
                    Username = await _preferencesService.GetAsync("saved_username", string.Empty);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load credentials error: {ex.Message}");
            }
        }

        private string GetErrorMessage(string errorCode)
        {
            return errorCode switch
            {
                "INVALID_CREDENTIALS" => "Invalid username or password",
                "INVALID_USERNAME" => "Username must be at least 3 characters",
                "INVALID_PASSWORD" => "Password must be at least 4 characters",
                "ADMIN_CREATION_FAILED" => "Failed to create admin account",
                _ => "Login failed. Please try again."
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

    // Supporting Models
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }

    public class LoginResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorCode { get; set; }
        public string Token { get; set; }
        public UserInfo User { get; set; }
    }

    public class UserInfo
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public UserRole Role { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsFirstAccount { get; set; }
    }

    public enum UserRole
    {
        Developer,
        Administrator
    }
}
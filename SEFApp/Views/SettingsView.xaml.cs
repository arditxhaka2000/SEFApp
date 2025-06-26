using Microsoft.Maui.Controls;
using SEFApp.Models;
using SEFApp.Models.Database;
using SEFApp.Services.Interfaces;
using System.Text.RegularExpressions;

namespace SEFApp.Views
{
    public partial class SettingsView : ContentPage
    {
        private readonly IAuthenticationService _authService;
        private readonly IDatabaseService _databaseService;
        private readonly IPreferencesService _preferencesService;
        private User _currentUser;
        private bool _isInitializing = false;
        public SettingsView(IAuthenticationService authService,
                           IDatabaseService databaseService,
                           IPreferencesService preferencesService)
        {
            InitializeComponent();
            _authService = authService;
            _databaseService = databaseService;
            _preferencesService = preferencesService;

            LoadUserData();
            LoadApplicationSettings();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadUserData();
            await LoadApplicationSettings();
        }

        private async Task LoadUserData()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                _currentUser = await _authService.GetCurrentUserAsync();

                if (_currentUser != null)
                {
                    UsernameEntry.Text = _currentUser.Username;
                    FullNameEntry.Text = _currentUser.FullName ?? "";
                    RoleEntry.Text = _currentUser.Role;
                    LastLoginEntry.Text = _currentUser.LastLoginDate != DateTime.MinValue
                        ? _currentUser.LastLoginDate.ToString("yyyy-MM-dd HH:mm")
                        : "Never";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load user data: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private async Task LoadApplicationSettings()
        {
            try
            {
                _isInitializing = true;
                // Load theme setting
                var theme = await _preferencesService.GetAsync("app_theme", "System");
                ThemePicker.SelectedItem = theme;

                // Load language setting
                var language = await _preferencesService.GetAsync("app_language", "English");
                LanguagePicker.SelectedItem = language;

                // Load auto backup setting
                var autoBackup = await _preferencesService.GetAsync("auto_backup_enabled", true);
                AutoBackupSwitch.IsToggled = autoBackup;

                // Load notifications setting
                var notifications = await _preferencesService.GetAsync("notifications_enabled", true);
                NotificationsSwitch.IsToggled = notifications;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load app settings: {ex.Message}");
            }
        }

        private async void OnUpdateProfileClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentUser == null)
                {
                    await DisplayAlert("Error", "No user data available", "OK");
                    return;
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(UsernameEntry.Text))
                {
                    await DisplayAlert("Validation Error", "Username is required", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(FullNameEntry.Text))
                {
                    await DisplayAlert("Validation Error", "Full name is required", "OK");
                    return;
                }

                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                // Check if username is already taken by another user
                if (UsernameEntry.Text != _currentUser.Username)
                {
                    var existingUser = await _databaseService.GetUserByUsernameAsync(UsernameEntry.Text);
                    if (existingUser != null && existingUser.Id != _currentUser.Id)
                    {
                        await DisplayAlert("Error", "Username is already taken", "OK");
                        return;
                    }
                }

                // Update user data
                _currentUser.Username = UsernameEntry.Text.Trim();
                _currentUser.FullName = FullNameEntry.Text.Trim();

                var success = await _databaseService.UpdateUserAsync(_currentUser);

                if (success)
                {
                    // Update preferences service with new username
                    await _preferencesService.SetAsync("CurrentUsername", _currentUser.Username);

                    await DisplayAlert("Success", "Profile updated successfully", "OK");

                    // Log the action
                    await _databaseService.LogActionAsync("Users", "PROFILE_UPDATE",
                        _currentUser.Id.ToString(), null,
                        new { Username = _currentUser.Username, FullName = _currentUser.FullName },
                        _currentUser.Id);
                }
                else
                {
                    await DisplayAlert("Error", "Failed to update profile", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private void OnNewPasswordChanged(object sender, TextChangedEventArgs e)
        {
            ValidatePasswordStrength(e.NewTextValue);
            ValidatePasswordMatch();
        }

        private void OnConfirmPasswordChanged(object sender, TextChangedEventArgs e)
        {
            ValidatePasswordMatch();
        }

        private void ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                PasswordStrengthContainer.IsVisible = false;
                return;
            }

            PasswordStrengthContainer.IsVisible = true;

            var strength = CalculatePasswordStrength(password);
            PasswordStrengthBar.Progress = strength.Score;
            PasswordStrengthLabel.Text = strength.Label;
            PasswordStrengthLabel.TextColor = strength.Color;
            PasswordStrengthBar.ProgressColor = strength.Color;

            ValidatePasswordMatch();
        }

        private (double Score, string Label, Color Color) CalculatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return (0, "Too Short", Colors.Red);

            int score = 0;

            // Length check
            if (password.Length >= 8) score++;
            if (password.Length >= 12) score++;

            // Character variety checks
            if (Regex.IsMatch(password, @"[a-z]")) score++; // lowercase
            if (Regex.IsMatch(password, @"[A-Z]")) score++; // uppercase
            if (Regex.IsMatch(password, @"[0-9]")) score++; // numbers
            if (Regex.IsMatch(password, @"[^a-zA-Z0-9]")) score++; // special characters

            return score switch
            {
                <= 2 => (0.33, "Weak", Colors.Red),
                <= 4 => (0.66, "Medium", Colors.Orange),
                _ => (1.0, "Strong", Colors.Green)
            };
        }

        private void ValidatePasswordMatch()
        {
            var newPassword = NewPasswordEntry.Text ?? "";
            var confirmPassword = ConfirmPasswordEntry.Text ?? "";

            if (string.IsNullOrEmpty(newPassword) && string.IsNullOrEmpty(confirmPassword))
            {
                PasswordMatchLabel.IsVisible = false;
                ChangePasswordButton.IsEnabled = false;
                return;
            }

            if (newPassword != confirmPassword)
            {
                PasswordMatchLabel.IsVisible = true;
                PasswordMatchLabel.Text = "Passwords do not match";
                PasswordMatchLabel.TextColor = Colors.Red;
                ChangePasswordButton.IsEnabled = false;
            }
            else if (newPassword.Length < 6)
            {
                PasswordMatchLabel.IsVisible = true;
                PasswordMatchLabel.Text = "Password must be at least 6 characters";
                PasswordMatchLabel.TextColor = Colors.Red;
                ChangePasswordButton.IsEnabled = false;
            }
            else
            {
                PasswordMatchLabel.IsVisible = true;
                PasswordMatchLabel.Text = "Passwords match ✓";
                PasswordMatchLabel.TextColor = Colors.Green;
                ChangePasswordButton.IsEnabled = !string.IsNullOrEmpty(CurrentPasswordEntry.Text);
            }
        }

        private async void OnChangePasswordClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentUser == null)
                {
                    await DisplayAlert("Error", "No user data available", "OK");
                    return;
                }

                var currentPassword = CurrentPasswordEntry.Text ?? "";
                var newPassword = NewPasswordEntry.Text ?? "";
                var confirmPassword = ConfirmPasswordEntry.Text ?? "";

                // Validate inputs
                if (string.IsNullOrEmpty(currentPassword))
                {
                    await DisplayAlert("Validation Error", "Current password is required", "OK");
                    return;
                }

                if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
                {
                    await DisplayAlert("Validation Error", "New password must be at least 6 characters", "OK");
                    return;
                }

                if (newPassword != confirmPassword)
                {
                    await DisplayAlert("Validation Error", "New passwords do not match", "OK");
                    return;
                }

                if (newPassword == currentPassword)
                {
                    await DisplayAlert("Validation Error", "New password must be different from current password", "OK");
                    return;
                }

                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                // Verify current password
                var isCurrentPasswordValid = await _databaseService.ValidatePasswordAsync(
                    _currentUser.Username, currentPassword);

                if (!isCurrentPasswordValid)
                {
                    await DisplayAlert("Error", "Current password is incorrect", "OK");
                    return;
                }

                // Update password
                var success = await _databaseService.UpdatePasswordAsync(_currentUser.Id, newPassword);

                if (success)
                {
                    // Clear password fields
                    CurrentPasswordEntry.Text = "";
                    NewPasswordEntry.Text = "";
                    ConfirmPasswordEntry.Text = "";
                    PasswordStrengthContainer.IsVisible = false;
                    PasswordMatchLabel.IsVisible = false;
                    ChangePasswordButton.IsEnabled = false;

                    await DisplayAlert("Success", "Password changed successfully", "OK");

                    // Log the action
                    await _databaseService.LogActionAsync("Users", "PASSWORD_CHANGE",
                        _currentUser.Id.ToString(), null,
                        new { Timestamp = DateTime.Now },
                        _currentUser.Id);
                }
                else
                {
                    await DisplayAlert("Error", "Failed to change password", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private async void OnThemeChanged(object sender, EventArgs e)
        {
            if (_isInitializing) return;
            if (ThemePicker.SelectedItem is string selectedTheme)
            {
                await _preferencesService.SetAsync("app_theme", selectedTheme);

                // Apply theme (you'll need to implement theme switching logic)
                // ApplyTheme(selectedTheme);

                await DisplayAlert("Theme Changed", $"Theme changed to {selectedTheme}", "OK");
            }
        }

        private async void OnLanguageChanged(object sender, EventArgs e)
        {
            if (_isInitializing) return;
            if (LanguagePicker.SelectedItem is string selectedLanguage)
            {
                await _preferencesService.SetAsync("app_language", selectedLanguage);
                await DisplayAlert("Language Changed",
                    $"Language changed to {selectedLanguage}. Restart the app to apply changes.", "OK");
            }
        }

        private async void OnAutoBackupToggled(object sender, ToggledEventArgs e)
        {
            if (_isInitializing) return;
            await _preferencesService.SetAsync("auto_backup_enabled", e.Value);

            if (_currentUser != null)
            {
                await _databaseService.LogActionAsync("Settings", "AUTO_BACKUP_TOGGLE",
                    "auto_backup_enabled", !e.Value, e.Value, _currentUser.Id);
            }
        }

        private async void OnNotificationsToggled(object sender, ToggledEventArgs e)
        {
            if (_isInitializing) return;
            await _preferencesService.SetAsync("notifications_enabled", e.Value);

            if (_currentUser != null)
            {
                await _databaseService.LogActionAsync("Settings", "NOTIFICATIONS_TOGGLE",
                    "notifications_enabled", !e.Value, e.Value, _currentUser.Id);
            }
        }

        private async void OnExportDataClicked(object sender, EventArgs e)
        {
            if (_isInitializing) return;
            try
            {
                var result = await DisplayAlert("Export Data",
                    "Do you want to export all your data? This may take a few minutes.",
                    "Export", "Cancel");

                if (!result) return;

                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                // Get all settings as export data
                var allSettings = await _databaseService.GetAllSettingsAsync();
                var userData = new
                {
                    User = _currentUser,
                    Settings = allSettings,
                    ExportDate = DateTime.Now
                };

                // Convert to JSON
                var jsonData = System.Text.Json.JsonSerializer.Serialize(userData, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Save to file (you might want to use a file picker here)
                var fileName = $"SEF_Export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                await File.WriteAllTextAsync(filePath, jsonData);

                await DisplayAlert("Export Complete",
                    $"Data exported successfully to {fileName}", "OK");

                // Log the action
                if (_currentUser != null)
                {
                    await _databaseService.LogActionAsync("System", "DATA_EXPORT",
                        fileName, null, new { FilePath = filePath, FileSize = jsonData.Length },
                        _currentUser.Id);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Export Error", $"Failed to export data: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            var result = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
            if (result)
            {
                try
                {
                    LoadingIndicator.IsVisible = true;
                    LoadingIndicator.IsRunning = true;

                    // Log the logout action
                    if (_currentUser != null)
                    {
                        await _databaseService.LogActionAsync("Users", "LOGOUT",
                            _currentUser.Id.ToString(), null,
                            new { LogoutTime = DateTime.Now },
                            _currentUser.Id);
                    }

                    await _authService.LogoutAsync();

                    // Navigate to login page
                    await Shell.Current.GoToAsync("//LoginPage");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Logout failed: {ex.Message}", "OK");
                }
                finally
                {
                    LoadingIndicator.IsVisible = false;
                    LoadingIndicator.IsRunning = false;
                }
            }
        }
    }
}
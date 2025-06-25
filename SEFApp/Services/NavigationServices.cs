using SEFApp.Services.Interfaces;

namespace SEFApp.Services
{
    public class NavigationService : INavigationService
    {
        /// <summary>
        /// Navigate to a specific route/page
        /// </summary>
        public async Task NavigateToAsync(string route)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(route))
                {
                    System.Diagnostics.Debug.WriteLine("Navigation: Route is null or empty");
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.GoToAsync(route);
                });

                System.Diagnostics.Debug.WriteLine($"Navigation: Successfully navigated to {route}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error to {route}: {ex.Message}");
                await HandleNavigationError(route, ex);
            }
        }

        /// <summary>
        /// Navigate back to the previous page
        /// </summary>
        public async Task NavigateBackAsync()
        {
            try
            {
                if (!CanNavigateBack())
                {
                    System.Diagnostics.Debug.WriteLine("Navigation: Cannot navigate back - no pages in stack");
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.GoToAsync("..");
                });

                System.Diagnostics.Debug.WriteLine("Navigation: Successfully navigated back");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigate back error: {ex.Message}");
            }
        }

        /// <summary>
        /// Navigate to a page as a modal/popup
        /// </summary>
        public async Task NavigateToModalAsync(string route)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(route))
                {
                    System.Diagnostics.Debug.WriteLine("Navigation: Modal route is null or empty");
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.GoToAsync(route, animate: true);
                });

                System.Diagnostics.Debug.WriteLine($"Navigation: Successfully opened modal {route}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Modal navigation error to {route}: {ex.Message}");
            }
        }

        /// <summary>
        /// Close/dismiss the current modal
        /// </summary>
        public async Task PopModalAsync()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.GoToAsync("..", animate: true);
                });

                System.Diagnostics.Debug.WriteLine("Navigation: Successfully closed modal");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Pop modal error: {ex.Message}");
            }
        }

        /// <summary>
        /// Navigate to the root page, clearing navigation stack
        /// </summary>
        public async Task NavigateToRootAsync()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.GoToAsync("//");
                });

                System.Diagnostics.Debug.WriteLine("Navigation: Successfully navigated to root");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigate to root error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the current route/page name
        /// </summary>
        public string GetCurrentRoute()
        {
            try
            {
                var currentState = Shell.Current?.CurrentState;
                if (currentState?.Location != null)
                {
                    var route = currentState.Location.ToString();
                    System.Diagnostics.Debug.WriteLine($"Navigation: Current route is {route}");
                    return route;
                }

                System.Diagnostics.Debug.WriteLine("Navigation: Current route is unknown");
                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get current route error: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Check if back navigation is possible
        /// </summary>
        public bool CanNavigateBack()
        {
            try
            {
                var navigationStack = Shell.Current?.Navigation?.NavigationStack;
                var canGoBack = navigationStack != null && navigationStack.Count > 1;

                System.Diagnostics.Debug.WriteLine($"Navigation: Can navigate back = {canGoBack} (Stack count: {navigationStack?.Count ?? 0})");
                return canGoBack;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Can navigate back error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Navigate with parameters
        /// </summary>
        public async Task NavigateToAsync(string route, Dictionary<string, object> parameters)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(route))
                {
                    System.Diagnostics.Debug.WriteLine("Navigation: Route is null or empty");
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (parameters?.Any() == true)
                    {
                        await Shell.Current.GoToAsync(route, parameters);
                    }
                    else
                    {
                        await Shell.Current.GoToAsync(route);
                    }
                });

                System.Diagnostics.Debug.WriteLine($"Navigation: Successfully navigated to {route} with {parameters?.Count ?? 0} parameters");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation with parameters error to {route}: {ex.Message}");
                await HandleNavigationError(route, ex);
            }
        }

        /// <summary>
        /// Replace the current page (no back navigation possible)
        /// </summary>
        public async Task ReplaceAsync(string route)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(route))
                {
                    System.Diagnostics.Debug.WriteLine("Navigation: Replace route is null or empty");
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Remove current page and navigate to new one
                    var navigationStack = Shell.Current.Navigation.NavigationStack;
                    if (navigationStack.Count > 0)
                    {
                        await Shell.Current.GoToAsync($"//{route}");
                    }
                    else
                    {
                        await Shell.Current.GoToAsync(route);
                    }
                });

                System.Diagnostics.Debug.WriteLine($"Navigation: Successfully replaced current page with {route}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Replace navigation error to {route}: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all navigation history and go to route
        /// </summary>
        public async Task ClearAndNavigateToAsync(string route)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(route))
                {
                    System.Diagnostics.Debug.WriteLine("Navigation: Clear and navigate route is null or empty");
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Use // prefix to clear navigation stack
                    var clearRoute = route.StartsWith("//") ? route : $"//{route}";
                    await Shell.Current.GoToAsync(clearRoute);
                });

                System.Diagnostics.Debug.WriteLine($"Navigation: Successfully cleared stack and navigated to {route}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clear and navigate error to {route}: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle navigation errors with fallback strategies
        /// </summary>
        private async Task HandleNavigationError(string route, Exception ex)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Navigation: Attempting fallback navigation for {route}");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Try different approaches based on route
                    if (route.Contains("Login") || route.Contains("login"))
                    {
                        // For login, try setting as main page
                        if (Application.Current != null)
                        {
                            Application.Current.MainPage = new Views.LoginPage(
                                // You'll need to resolve these dependencies
                                DependencyService.Get<ViewModels.LoginViewModel>()
                            );
                        }
                    }
                    else if (route.Contains("Main") || route.Contains("//"))
                    {
                        // For main pages, try creating new shell
                        if (Application.Current != null)
                        {
                            Application.Current.MainPage = new AppShell();
                        }
                    }
                    else
                    {
                        // Last resort: try simple navigation
                        await Shell.Current.GoToAsync("//LoginPage");
                    }
                });

                System.Diagnostics.Debug.WriteLine("Navigation: Fallback navigation completed");
            }
            catch (Exception fallbackEx)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation: Fallback also failed: {fallbackEx.Message}");
            }
        }

        /// <summary>
        /// Get navigation stack count for debugging
        /// </summary>
        public int GetNavigationStackCount()
        {
            try
            {
                return Shell.Current?.Navigation?.NavigationStack?.Count ?? 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get navigation stack count error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Check if currently on a specific route
        /// </summary>
        public bool IsCurrentRoute(string route)
        {
            try
            {
                var currentRoute = GetCurrentRoute();
                return currentRoute.Contains(route, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Is current route error: {ex.Message}");
                return false;
            }
        }
    }
}
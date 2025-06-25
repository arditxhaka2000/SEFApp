namespace SEFApp.Services.Interfaces
{
    public interface INavigationService
    {
        /// <summary>
        /// Navigate to a specific route/page
        /// </summary>
        /// <param name="route">The route to navigate to (e.g., "LoginPage", "//MainPage")</param>
        Task NavigateToAsync(string route);

        /// <summary>
        /// Navigate back to the previous page
        /// </summary>
        Task NavigateBackAsync();

        /// <summary>
        /// Navigate to a page as a modal/popup
        /// </summary>
        /// <param name="route">The route to show as modal</param>
        Task NavigateToModalAsync(string route);

        /// <summary>
        /// Close/dismiss the current modal
        /// </summary>
        Task PopModalAsync();

        /// <summary>
        /// Navigate to the root page, clearing navigation stack
        /// </summary>
        Task NavigateToRootAsync();

        /// <summary>
        /// Get the current route/page name
        /// </summary>
        /// <returns>Current route as string</returns>
        string GetCurrentRoute();

        /// <summary>
        /// Check if back navigation is possible
        /// </summary>
        /// <returns>True if can go back, false otherwise</returns>
        bool CanNavigateBack();

        /// <summary>
        /// Navigate with parameters
        /// </summary>
        /// <param name="route">Route to navigate to</param>
        /// <param name="parameters">Dictionary of parameters to pass</param>
        Task NavigateToAsync(string route, Dictionary<string, object> parameters);

        /// <summary>
        /// Replace the current page (no back navigation possible)
        /// </summary>
        /// <param name="route">Route to replace current page with</param>
        Task ReplaceAsync(string route);

        /// <summary>
        /// Clear all navigation history and go to route
        /// </summary>
        /// <param name="route">Route to navigate to</param>
        Task ClearAndNavigateToAsync(string route);
    }
}
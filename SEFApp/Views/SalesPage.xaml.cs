using SEFApp.ViewModels;

namespace SEFApp.Views
{
    public partial class SalesPage : ContentPage
    {
        private const double MOBILE_WIDTH_THRESHOLD = 800;

        public SalesPage(SalesViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            

            // Set initial layout
            HandleSizeChanged();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            HandleSizeChanged();
        }

        private void HandleSizeChanged()
        {
            if (Width < 0 || double.IsNaN(Width)) return;

            try
            {
                if (Width < MOBILE_WIDTH_THRESHOLD)
                {
                    // Stack vertically for mobile/small screens
                    MainGrid.ColumnDefinitions.Clear();
                    MainGrid.RowDefinitions.Clear();

                    MainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    MainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                    Grid.SetColumn(LeftPanel, 0);
                    Grid.SetRow(LeftPanel, 0);
                    Grid.SetColumn(RightPanel, 0);
                    Grid.SetRow(RightPanel, 1);

                    // Adjust heights for mobile
                    foreach (var frame in LeftPanel.Children.OfType<Frame>())
                    {
                        foreach (var stackLayout in frame.Content is StackLayout sl ? new[] { sl } : new StackLayout[0])
                        {
                            foreach (var collectionView in stackLayout.Children.OfType<CollectionView>())
                            {
                                collectionView.HeightRequest = 150; // Reduce height on mobile
                            }
                        }
                    }
                }
                else
                {
                    // Side by side for desktop/tablet
                    MainGrid.ColumnDefinitions.Clear();
                    MainGrid.RowDefinitions.Clear();

                    MainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                    MainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    MainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                    Grid.SetColumn(LeftPanel, 0);
                    Grid.SetRow(LeftPanel, 0);
                    Grid.SetColumn(RightPanel, 1);
                    Grid.SetRow(RightPanel, 0);

                    // Restore heights for desktop
                    foreach (var frame in LeftPanel.Children.OfType<Frame>())
                    {
                        foreach (var stackLayout in frame.Content is StackLayout sl ? new[] { sl } : new StackLayout[0])
                        {
                            foreach (var collectionView in stackLayout.Children.OfType<CollectionView>())
                            {
                                if (collectionView == frame.Content.FindByName<CollectionView>("ProductResults"))
                                {
                                    collectionView.HeightRequest = 200;
                                }
                                else
                                {
                                    collectionView.HeightRequest = 300;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Layout error: {ex.Message}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Optional: Clear sensitive data when leaving POS
            if (BindingContext is SalesViewModel viewModel)
            {
                // You could clear the cart or ask to save draft here
                // For now, we'll leave the cart intact for better UX
            }
        }
    }
}
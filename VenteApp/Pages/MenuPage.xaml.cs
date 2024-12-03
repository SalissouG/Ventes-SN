using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VenteApp
{
    public partial class MenuPage : FlyoutPage, INotifyPropertyChanged
    {
        // Define the color you want to use for the selected menu item
        private readonly Color SelectedLabelColor = Color.FromHex("#16e7ed");  // Example color

        private string connectedUserName;
        public string ConnectedUserName
        {
            get => connectedUserName;
            set
            {
                if (connectedUserName != value)
                {
                    connectedUserName = value;
                    OnPropertyChanged(nameof(ConnectedUserName));
                }
            }
        }

        // Add this if you haven't already implemented INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MenuPage()
        {
            InitializeComponent();

            BindingContext = this;  // Add this line

            // Check if the connected user is an admin and show/hide menu items accordingly
            ConfigureMenuVisibility();
            CheckLicenseExpiration();
            SetInitialPage();
            //// Set the connected user's name
            ConnectedUserName = GetConnectedUserName();
        }

        private void SetInitialPage()
        {
            // Verify if the license is valid on startup
            if (LicenseValidator.IsLicenceValid("MySuperSecretKey"))
            {
                Detail = new NavigationPage(new SalesPage());
                ResetLabelStyles();  // Reset styles for all labels
                SetSelectedStyle(SalesLabel);  // Highlight the Sales label
                SetSelectedStyleLa(SalesLayout);
            }
            else
            {
                Detail = new NavigationPage(new LicenseMenuPage());
                ResetLabelStyles();  // Reset styles for all labels
                //SetSelectedStyle(LicenseLabel);  // Highlight the License label
                //SetSelectedStyleLa(LicenseLayout);
            }
        }

        public string GetConnectedUserName()
        {
            var userJson = Preferences.Get("ConnectedUser", string.Empty);
            if (!string.IsNullOrEmpty(userJson))
            {
                var user = JsonConvert.DeserializeObject<User>(userJson);
                return $"{user.Prenom} {user.Nom}";
            }
            return "No user connected";
        }

        // Navigation methods for all menu items
        private async void OnProductsClicked(object sender, EventArgs e) => await NavigateToPage(new ProductsPage(), ProductsLabel, ProductsLayout);
        private async void OnSalesClicked(object sender, EventArgs e) => await NavigateToPage(new SalesPage(), SalesLabel, SalesLayout);
        //private async void OnBasketClicked(object sender, EventArgs e) => await NavigateToPage(new BasketPage(), BasketLabel, BasketLayout);
        private async void OnBillingClicked(object sender, EventArgs e) => await NavigateToPage(new BillingPage(), BillingLabel, BillingLayout);
        private async void OnClientsClicked(object sender, EventArgs e) => await NavigateToPage(new ClientsPage(), ClientsLabel, ClientsLayout);
        private async void OnHistoricalClicked(object sender, EventArgs e) => await NavigateToPage(new HistoricalPage(), HistoricalLabel, HistoricalLayout);
        private async void OnInventoryClicked(object sender, EventArgs e) => await NavigateToPage(new InventoryPage(), InventoryLabel, InventoryLayout);
        private async void OnSalesSummaryClicked(object sender, EventArgs e) => await NavigateToPage(new SalesSummaryPage(), SalesSummaryLabel, SalesSummaryLayout);
        //private async void OnDashboardClicked(object sender, EventArgs e) => await NavigateToPage(new DashboardPage(), DashboardLabel, DashboardLayout);
        private async void OnUsersClicked(object sender, EventArgs e) => await NavigateToPage(new UsersPage(), UsersLabel, UsersLayout);
        //private async void OnLicenseClicked(object sender, EventArgs e) => await NavigateToPage(new LicenseMenuPage(), LicenseLabel, LicenseLayout);

        private async void OnOwnerClicked(object sender, EventArgs e)
        {
            await NavigateToPage(new OwnerPage(), OwnerLabel, OwnerLayout);
        }


        // Logout handling
        private async void OnDeconnexionClicked(object sender, EventArgs e)
        {
            UserService.Instance.ClearConnectedUser();
            ConnectedUserName = UserService.Instance.GetConnectedUserName();
            OnPropertyChanged(nameof(ConnectedUserName));
            await Navigation.PushAsync(new MainPage());
            ResetLabelStyles();
        }

        // Helper method for navigation and styling
        private async Task NavigateToPage(Page page, Label label, HorizontalStackLayout layout)
        {
            if (LicenseValidator.IsLicenceValid("MySuperSecretKey"))
            {
                Detail = new NavigationPage(page);
                ResetLabelStyles();
                SetSelectedStyle(label);
                SetSelectedStyleLa(layout);
            }
            else
            {
                Detail = new NavigationPage(new LicenseMenuPage());
                ResetLabelStyles();
                /*SetSelectedStyle(LicenseLabel);
                SetSelectedStyleLa(LicenseLayout);*/
            }

            CheckLicenseExpiration();
        }

        // Configure menu visibility based on user role
        private void ConfigureMenuVisibility()
        {
            bool isAdmin = UserService.Instance.IsAdmin();
            UsersLayout.IsVisible = isAdmin;
            //LicenseLayout.IsVisible = isAdmin;
            OwnerLayout.IsVisible = isAdmin;
        }

        // Reset all labels and layouts to default style
        private void ResetLabelStyles()
        {
            // Create a list of all labels and layouts for easy resetting
            var labels = new List<Label>
            {
                ProductsLabel,
                SalesLabel,
                //BasketLabel,
                BillingLabel,
                ClientsLabel,
                HistoricalLabel,
                InventoryLabel,
                SalesSummaryLabel,
                //DashboardLabel,
                UsersLabel,
                //LicenseLabel,
                OwnerLabel // Include the OwnerLabel
            };

                    var layouts = new List<HorizontalStackLayout>
            {
                ProductsLayout,
                SalesLayout,
                //BasketLayout,
                BillingLayout,
                ClientsLayout,
                HistoricalLayout,
                InventoryLayout,
                SalesSummaryLayout,
                //DashboardLayout,
                UsersLayout,
                //LicenseLayout,
                DeconnexionLayout,
                OwnerLayout // Include the OwnerLayout
            };

            // Reset background color and text color for all labels
            foreach (var label in labels)
            {
                label.BackgroundColor = Colors.Transparent;
                label.TextColor = Colors.Black; // Reset text color to default (black)
            }

            // Reset background color for all layouts
            foreach (var layout in layouts)
            {
                layout.BackgroundColor = Colors.Transparent;
            }
        }


        // Set the selected style for the current label
        private void SetSelectedStyle(Label label)
        {
            label.BackgroundColor = SelectedLabelColor;
            label.TextColor = Colors.White;
        }

        // Set the selected style for the layout
        private void SetSelectedStyleLa(HorizontalStackLayout layout)
        {
            layout.BackgroundColor = SelectedLabelColor;
        }

        private void CheckLicenseExpiration()
        {
            // Check if the license will expire in less than 14 days
            if (LicenseValidator.WillExpireSoon("MySuperSecretKey"))
            {
                LicenseWarningLabel.IsVisible = true;
            }
            else
            {
                LicenseWarningLabel.IsVisible = false;
            }
        }
    }
}

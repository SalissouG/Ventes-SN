
namespace VenteApp
{
    public partial class SalesPage : ContentPage
    {
        public SalesPage()
        {
            InitializeComponent();
            this.Title = "Ventes";
            BindingContext = new SalesViewModel(); // Set the ViewModel as the BindingContext
        }

        // Event handler for incrementing the quantity
        private void OnIncrementClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var sale = (Sale)((Grid)button.Parent.Parent).BindingContext;

            using (var db = new AppDbContext())
            {
                // Fetch the product to check stock
                var product = db.Products.FirstOrDefault(p => p.Id == sale.ProductId);
                if (product == null)
                {
                    DisplayAlert("Erreur", "Le produit n'existe pas.", "OK");
                    return;
                }

                // Check if stock is available
                if (sale.Quantite < product.Quantite)
                {
                    // Increment the quantity
                    sale.Quantite += 1;

                    // Add to cart automatically
                    CartService.Instance.AddToCart(sale);
                }
                else
                {
                    DisplayAlert("Stock insuffisant", $"Stock insuffisant pour le produit {product.Nom}.", "OK");
                }
            }
        }

        // Event handler for decrementing the quantity
        private void OnDecrementClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var sale = (Sale)((Grid)button.Parent.Parent).BindingContext;

            // Decrement the quantity, but ensure it doesn't go below 0
            if (sale.Quantite > 0)
            {
                sale.Quantite -= 1;

                // Add to cart automatically
                CartService.Instance.AddToCart(sale);
            }
            else
            {
                // Optionally remove from cart if the quantity reaches zero
                CartService.Instance.RemoveFromCart(sale);
            }
        }


        private async void OnShowBasketClicked(object sender, EventArgs e)
        {
            // Navigate to the BasketPage to show the cart items
            await Navigation.PushAsync(new BasketPage());
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (BindingContext is SalesViewModel viewModel)
            {
                viewModel.SearchCommand.Execute(e.NewTextValue);
            }
        }

        // Event handler for manual quantity input change
        private void OnQuantityTextChanged(object sender, TextChangedEventArgs e)
        {
            var entry = (Entry)sender;
            var sale = (Sale)((Grid)entry.Parent.Parent).BindingContext;

            // Check if input is a valid integer and within stock limits
            if (int.TryParse(e.NewTextValue, out int newQuantity) && newQuantity != 0)
            {
                using (var db = new AppDbContext())
                {
                    // Fetch the product to check stock
                    var product = db.Products.FirstOrDefault(p => p.Id == sale.ProductId);
                    if (product == null)
                    {
                        DisplayAlert("Erreur", "Le produit n'existe pas.", "OK");
                        return;
                    }

                    // Verify that the input quantity does not exceed available stock
                    if (newQuantity <= product.Quantite)
                    {
                        sale.Quantite = newQuantity;

                        // Update the cart with the new quantity

                        CartService.Instance.AddToCart(sale);
                    }
                    else
                    {
                        DisplayAlert("Stock insuffisant", $"Stock insuffisant pour le produit {product.Nom}.", "OK");

                        // Revert the entry value to the current valid quantity
                        entry.Text = sale.Quantite.ToString();
                    }
                }
            }
            
        }


    }
}

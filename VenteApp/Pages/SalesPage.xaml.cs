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

                    // Convert Sale to Basket and save to database
                    AddOrUpdateBasket(db, sale);
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

                // Convert Sale to Basket and save to database
                using (var db = new AppDbContext())
                {
                    AddOrUpdateBasket(db, sale);
                }
            }
            else
            {
                // Optionally remove from cart if the quantity reaches zero
                using (var db = new AppDbContext())
                {
                    var basket = db.Baskets.FirstOrDefault(b => b.ProductId == sale.ProductId);
                    if (basket != null)
                    {
                        db.Baskets.Remove(basket);
                        db.SaveChanges();
                    }
                }
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

                        // Convert Sale to Basket and save to database
                        AddOrUpdateBasket(db, sale);
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

        // Method to convert Sale to Basket
        private Basket ConvertSaleToBasket(Sale sale)
        {
            return new Basket
            {
                Id = Guid.NewGuid(),
                ProductId = sale.ProductId,
                Nom = sale.Nom,
                Description = sale.Description,
                Prix = sale.Prix,
                Quantite = sale.Quantite,
                Categorie = sale.Categorie,
                Taille = sale.Taille,
                DateLimite = sale.DateLimite,
                DateDeVente = sale.DateDeVente
            };
        }

        // Method to add or update the basket item
        private void AddOrUpdateBasket(AppDbContext db, Sale sale)
        {
            var existingBasketItem = db.Baskets.FirstOrDefault(b => b.ProductId == sale.ProductId);
            if (existingBasketItem != null)
            {
                // Update the existing basket item
                existingBasketItem.Quantite = sale.Quantite;
                db.Baskets.Update(existingBasketItem);
            }
            else
            {
                // Add a new basket item
                var basket = ConvertSaleToBasket(sale);
                db.Baskets.Add(basket);
            }
            db.SaveChanges();
        }
    }
}

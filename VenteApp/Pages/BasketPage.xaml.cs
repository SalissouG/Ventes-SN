using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace VenteApp
{
    public partial class BasketPage : ContentPage, INotifyPropertyChanged
    {
        public ObservableCollection<Basket> CartItems { get; set; }
        public ObservableCollection<Client> Clients { get; set; } // List of clients
        public Client SelectedClient { get; set; } // Selected client from the Picker
        public ObservableCollection<string> PaymentModes { get; set; } // List of payment modes
        public string SelectedPaymentMode { get; set; } // Selected payment mode from the Picker

        private decimal totalPrice;
        public decimal TotalPrice
        {
            get => totalPrice;
            set
            {
                if (totalPrice != value)
                {
                    totalPrice = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand RemoveCommand { get; }

        public BasketPage()
        {
            InitializeComponent();

            // Set up the remove command
            RemoveCommand = new Command<Basket>(OnRemoveItem);

            // Load cart items from the database
            CartItems = new ObservableCollection<Basket>();
            LoadCartItems();

            // Load clients for the Picker
            Clients = new ObservableCollection<Client>();
            LoadClients();

            // Load payment modes
            PaymentModes = new ObservableCollection<string>
            {
               "En efectivo",
               "Tarjeta bancaria"
            };

            // Set the initial total price
            TotalPrice = CalculateTotalPrice();

            BindingContext = this;

            this.Title = "Carrito";
        }

        // Load clients from the database
        private void LoadClients()
        {
            using (var db = new AppDbContext())
            {
                var clientsFromDb = db.Clients.ToList();
                foreach (var client in clientsFromDb)
                {
                    Clients.Add(client);
                }
            }
        }

        // Load cart items from the database
        private void LoadCartItems()
        {
            using (var db = new AppDbContext())
            {
                var cartItemsFromDb = db.Baskets.ToList();
                CartItems.Clear(); // Clear existing items to avoid duplicates
                foreach (var item in cartItemsFromDb)
                {
                    CartItems.Add(item);
                }
            }
        }

        // Method to remove item from the cart with confirmation
        private async void OnRemoveItem(Basket basket)
        {
            bool confirm = await DisplayAlert("Confirmation", $"Voulez-vous vraiment retirer {basket.Nom} du panier ?", "Oui", "Non");
            if (!confirm)
                return;

            using (var db = new AppDbContext())
            {
                var itemToRemove = db.Baskets.FirstOrDefault(b => b.Id == basket.Id);
                if (itemToRemove != null)
                {
                    db.Baskets.Remove(itemToRemove);
                    db.SaveChanges();
                    CartItems.Remove(basket);
                    TotalPrice = CalculateTotalPrice();
                }
            }
        }

        // Override OnAppearing to refresh data
        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadCartItems();
            TotalPrice = CalculateTotalPrice();
        }

        // Event handler for incrementing the quantity
        private void OnIncrementClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var basket = (Basket)((Grid)button.Parent.Parent).BindingContext;

            using (var db = new AppDbContext())
            {
                var product = db.Products.FirstOrDefault(p => p.Id == basket.ProductId);
                if (product == null)
                {
                    DisplayAlert("Erreur", "Le produit n'existe pas.", "OK");
                    return;
                }

                if (basket.Quantite < product.Quantite)
                {
                    basket.Quantite += 1;
                    db.Baskets.Update(basket);
                    db.SaveChanges();
                    TotalPrice = CalculateTotalPrice();

                    LoadCartItems();
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
            var basket = (Basket)((Grid)button.Parent.Parent).BindingContext;

            if (basket.Quantite > 0)
            {
                basket.Quantite -= 1;
                using (var db = new AppDbContext())
                {
                    db.Baskets.Update(basket);
                    db.SaveChanges();
                    TotalPrice = CalculateTotalPrice();

                    LoadCartItems();
                }
            }
        }

        // Event handler for manual quantity input change
        private void OnQuantityTextChanged(object sender, TextChangedEventArgs e)
        {
            var entry = (Entry)sender;
            var basket = (Basket)((Grid)entry.Parent.Parent).BindingContext;

            // Check if input is a valid integer and within stock limits
            if (int.TryParse(e.NewTextValue, out int newQuantity) && newQuantity != 0)
            {
                using (var db = new AppDbContext())
                {
                    // Fetch the product to check stock
                    var product = db.Products.FirstOrDefault(p => p.Id == basket.ProductId);
                    if (product == null)
                    {
                        DisplayAlert("Erreur", "Le produit n'existe pas.", "OK");
                        return;
                    }

                    // Verify that the input quantity does not exceed available stock
                    if (newQuantity <= product.Quantite)
                    {
                        basket.Quantite = newQuantity;

                        // Update the cart with the new quantity
                        db.Baskets.Update(basket);
                        db.SaveChanges();

                        // Update the total price
                        TotalPrice = CalculateTotalPrice();
                    }
                    else
                    {
                        DisplayAlert("Stock insuffisant", $"Stock insuffisant pour le produit {product.Nom}.", "OK");

                        // Revert the entry value to the current valid quantity
                        entry.Text = basket.Quantite.ToString();
                    }
                }
            }
        }

        // Handle finalizing the sale and updating the product stock
        private void OnValiderClicked(object sender, EventArgs e)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    Guid orderId = Guid.NewGuid();

                    foreach (var basket in CartItems)
                    {
                        var product = db.Products.FirstOrDefault(p => p.Id == basket.ProductId);
                        if (product == null)
                        {
                            DisplayAlert("Erreur", $"Produit avec ID {basket.ProductId} n'existe pas dans la base de données.", "OK");
                            return;
                        }

                        if (product.Quantite < basket.Quantite)
                        {
                            DisplayAlert("Erreur", $"Stock insuffisant pour le produit {product.Nom}.", "OK");
                            return;
                        }

                        // Create a sale transaction and associate the selected client and payment mode
                        var saleTransaction = new SaleTransaction
                        {
                            ProductId = basket.ProductId,
                            Quantite = basket.Quantite,
                            DateDeVente = DateTime.Now,
                            OrderId = orderId,
                            ClientId = SelectedClient == null ? null : SelectedClient.Id, // Associate the selected client
                            PaymentMode = SelectedPaymentMode == "Tarjeta bancaria" ? PaymentMode.CreditCard : PaymentMode.Cash // Set the payment mode
                        };
                        db.SaleTransactions.Add(saleTransaction);

                        // Update the product stock
                        product.Quantite -= basket.Quantite;
                    }

                    int changes = db.SaveChanges();
                    if (changes > 0)
                    {
                        DisplayAlert("Succès", $"{changes} transaction(s) ajoutée(s) avec succès, stock mis à jour.", "OK");
                    }
                    else
                    {
                        DisplayAlert("Erreur", "Aucune transaction ajoutée.", "OK");
                    }
                }

                // Clear the cart and update the UI
                using (var db = new AppDbContext())
                {
                    db.Baskets.RemoveRange(CartItems);
                    db.SaveChanges();
                }
                CartItems.Clear();
                TotalPrice = 0;

                // Navigate to the ProductsPage
                Navigation.PushAsync(new ProductsPage());
            }
            catch (Exception ex)
            {
                DisplayAlert("Erreur", $"Une erreur s'est produite : {ex.Message}", "OK");
            }
        }

        private async void OnDownloadPdfClicked(object sender, EventArgs e)
        {
            // Create a new PDF document
            PdfDocument document = new PdfDocument();
            document.Info.Title = "Devis";

            // Create an empty page
            PdfPage page = document.AddPage();

            // Get an XGraphics object for drawing
            XGraphics gfx = XGraphics.FromPdfPage(page);

            // Define fonts
            XFont titleFont = new XFont("Verdana", 20, XFontStyle.Bold);
            XFont labelFont = new XFont("Verdana", 12, XFontStyle.Regular);

            // Draw title
            gfx.DrawString("Devis", titleFont, XBrushes.Black, new XRect(0, 0, page.Width, 50), XStringFormats.TopCenter);

            // Draw client information if a client is selected
            int yOffset = 80;
            if (SelectedClient != null)
            {
                gfx.DrawString($"Client: {SelectedClient.Nom}", labelFont, XBrushes.Black, new XRect(40, yOffset, page.Width - 80, 0), XStringFormats.Default);
                yOffset += 20;
            }

            // Draw payment mode information
            if (!string.IsNullOrEmpty(SelectedPaymentMode))
            {
                gfx.DrawString($"Mode de paiement: {SelectedPaymentMode}", labelFont, XBrushes.Black, new XRect(40, yOffset, page.Width - 80, 0), XStringFormats.Default);
                yOffset += 20;
            }

            // Draw table headers
            gfx.DrawString("Produit", labelFont, XBrushes.Black, new XRect(40, yOffset, 0, 0), XStringFormats.Default);
            gfx.DrawString("Prix Unitaire", labelFont, XBrushes.Black, new XRect(240, yOffset, 0, 0), XStringFormats.Default);
            gfx.DrawString("Quantité", labelFont, XBrushes.Black, new XRect(340, yOffset, 0, 0), XStringFormats.Default);
            gfx.DrawString("Total", labelFont, XBrushes.Black, new XRect(440, yOffset, 0, 0), XStringFormats.Default);

            yOffset += 20;

            // Draw a line below the headers
            gfx.DrawLine(XPens.Black, 40, yOffset, page.Width - 40, yOffset);
            yOffset += 10;

            // Add each product in the basket to the PDF
            foreach (var item in CartItems)
            {
                // Check if there's space for more content, otherwise add a new page
                if (yOffset + 20 > page.Height - 40)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    yOffset = 40;
                }

                // Draw product details
                gfx.DrawString(item.Nom, labelFont, XBrushes.Black, new XRect(40, yOffset, 0, 0), XStringFormats.Default);
                gfx.DrawString(item.Prix.ToString(), labelFont, XBrushes.Black, new XRect(240, yOffset, 0, 0), XStringFormats.Default);
                gfx.DrawString(item.Quantite.ToString(), labelFont, XBrushes.Black, new XRect(340, yOffset, 0, 0), XStringFormats.Default);
                gfx.DrawString((item.Prix * item.Quantite).ToString(), labelFont, XBrushes.Black, new XRect(440, yOffset, 0, 0), XStringFormats.Default);

                yOffset += 20;
            }

            // Draw total price
            gfx.DrawString($"Total: {TotalPrice}", titleFont, XBrushes.Black, new XRect(40, yOffset + 20, page.Width - 80, 0), XStringFormats.Default);

            try
            {
                string downloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Download");

                if (!Directory.Exists(downloadFolder))
                {
                    Directory.CreateDirectory(downloadFolder);
                }

                string currentDate = DateTime.Now.ToString("yyyyMMdd_HHmm");
                string fileName = Path.Combine(downloadFolder, $"Panier_{currentDate}.pdf");

                using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    document.Save(stream);
                }

                // Navigate to PdfViewerPage
                await Navigation.PushAsync(new PdfViewerPage(fileName));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Une erreur s'est produite lors de la création du fichier PDF : {ex.Message}", "OK");
            }
        }

        // Calculate the total price based on the items in the basket
        private decimal CalculateTotalPrice()
        {
            decimal total = 0;
            foreach (var item in CartItems)
            {
                total += item.Prix * item.Quantite;
            }
            return total;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

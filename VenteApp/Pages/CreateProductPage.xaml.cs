using System.Collections.ObjectModel;
using System.Globalization;

namespace VenteApp
{
    public partial class CreateProductPage : ContentPage
    {
        private Product _productToEdit;  // Product being edited
        public CreateProductPage(Product product = null)
        {
            InitializeComponent();

            this.Title = product == null ? "Créer un produit" : "Modifier le produit";

            if (product != null)
            {
                _productToEdit = product; // If editing, load product data
                LoadProductDetails(_productToEdit);
            }

            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

        }


        // Load product details into the form for editing
        private void LoadProductDetails(Product product)
        {
            NomEntry.Text = product.Nom;
            DescriptionEntry.Text = product.Description;
            PrixAchatEntry.Text = product.PrixAchat.ToString();
            PrixVenteEntry.Text = product.PrixVente.ToString();
            QuantiteEntry.Text = product.Quantite.ToString();
            CategorieEntry.Text = product.Categorie;
            TailleEntry.Text = product.Taille;
            UniteMesureEntry.Text = product.UniteMesure;
            ExpirationDatePicker.Date = product.DateExpiration ?? DateTime.Now;

        }

        // Real-time validation for fields
        private void OnNomTextChanged(object sender, TextChangedEventArgs e) => ValidateNom();
        private void OnDescriptionTextChanged(object sender, TextChangedEventArgs e) => ValidateDescription();
        private void OnPrixVenteTextChanged(object sender, TextChangedEventArgs e) => ValidatePrixVente();
        private void OnPrixAchatTextChanged(object sender, TextChangedEventArgs e) => ValidatePrixAchat();
        private void OnQuantiteTextChanged(object sender, TextChangedEventArgs e) => ValidateQuantite();
        private void OnExpirationDateChanged(object sender, DateChangedEventArgs e) => ValidateExpirationDate();

        // Validate all fields before saving
        private bool ValidateInputs()
        {
            bool isValid = true;

            isValid = ValidateNom() && isValid;
            isValid = ValidateDescription() && isValid;
            isValid = ValidatePrixAchat() && isValid;
            isValid = ValidatePrixVente() && isValid;
            isValid = ValidateQuantite() && isValid;
            isValid = ValidateExpirationDate() && isValid;

            return isValid;
        }

        // Field-specific validation methods
        private bool ValidateNom()
        {
            if (string.IsNullOrWhiteSpace(NomEntry.Text))
            {
                NomError.Text = "Le nom est obligatoire.";
                NomError.IsVisible = true;
                return false;
            }

            NomError.IsVisible = false;
            return true;
        }

        private bool ValidateDescription()
        {
            if (string.IsNullOrWhiteSpace(DescriptionEntry.Text))
            {
                DescriptionError.Text = "La description est obligatoire.";
                DescriptionError.IsVisible = true;
                return false;
            }

            DescriptionError.IsVisible = false;
            return true;
        }

        private bool ValidatePrixAchat()
        {
            if (string.IsNullOrWhiteSpace(PrixAchatEntry.Text) || !decimal.TryParse(PrixAchatEntry.Text, out decimal _))
            {
                PrixAchatError.Text = "Le prix d'achat doit être valide.";
                PrixAchatError.IsVisible = true;
                return false;
            }

            PrixAchatError.IsVisible = false;
            return true;
        }

        private bool ValidatePrixVente()
        {
            if (string.IsNullOrWhiteSpace(PrixVenteEntry.Text) || !decimal.TryParse(PrixVenteEntry.Text, out decimal _))
            {
                PrixVenteError.Text = "Le prix de vente doit être valide.";
                PrixVenteError.IsVisible = true;
                return false;
            }

            PrixVenteError.IsVisible = false;
            return true;
        }

        private bool ValidateQuantite()
        {
            if (string.IsNullOrWhiteSpace(QuantiteEntry.Text) || !int.TryParse(QuantiteEntry.Text, out int _))
            {
                QuantiteError.Text = "La quantité doit être valide.";
                QuantiteError.IsVisible = true;
                return false;
            }

            QuantiteError.IsVisible = false;
            return true;
        }

        private bool ValidateExpirationDate()
        {
            if (ExpirationDatePicker.Date < DateTime.Now.Date)
            {
                ExpirationDateError.Text = "La date d'expiration ne peut pas être dans le passé.";
                ExpirationDateError.IsVisible = true;
                return false;
            }

            ExpirationDateError.IsVisible = false;
            return true;
        }


        // Save the product (create or update)
        private async void OnSaveProductClicked(object sender, EventArgs e)
        {
            if (!ValidateInputs())
                return;


            try
            {
                await SaveProductAsync();
                await DisplayAlert("Success", "Product saved successfully.", "OK");
                await Navigation.PushAsync(new ProductsPage());
            }
            catch (Exception ex)
            {
                // Log exception (e.g., to a file or telemetry service)
                Console.WriteLine($"Error saving product: {ex.Message}");
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async Task SaveProductAsync()
        {
            using (var db = new AppDbContext())
            {
                if (_productToEdit == null)
                {
                    int productCount = db.Products.Count();
                    string productCode = GenerateProductCode(NomEntry.Text, productCount + 1);

                    var newProduct = new Product
                    {
                        Code = productCode,
                        Id = Guid.NewGuid(),
                        Nom = NomEntry.Text,
                        Description = DescriptionEntry.Text,
                        PrixAchat = decimal.Parse(PrixAchatEntry.Text),
                        PrixVente = decimal.Parse(PrixVenteEntry.Text),
                        Quantite = int.Parse(QuantiteEntry.Text),
                        Categorie = CategorieEntry.Text ?? string.Empty,
                        Taille = TailleEntry.Text ?? string.Empty,
                        UniteMesure = UniteMesureEntry.Text ?? string.Empty,
                        DateExpiration = ExpirationDatePicker.Date
                    };

                    db.Products.Add(newProduct);
                }
                else
                {
                    _productToEdit.Nom = NomEntry.Text;
                    _productToEdit.Description = DescriptionEntry.Text;
                    _productToEdit.PrixAchat = decimal.Parse(PrixAchatEntry.Text);
                    _productToEdit.PrixVente = decimal.Parse(PrixVenteEntry.Text);
                    _productToEdit.Quantite = int.Parse(QuantiteEntry.Text);
                    _productToEdit.Categorie = CategorieEntry.Text ?? string.Empty;
                    _productToEdit.Taille = TailleEntry.Text ?? string.Empty;
                    _productToEdit.UniteMesure = UniteMesureEntry.Text ?? string.Empty;
                    _productToEdit.DateExpiration = ExpirationDatePicker.Date;

                    db.Products.Update(_productToEdit);
                }

                await db.SaveChangesAsync();
            }
        }


        // Cancel and return to the product list
        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        // Show DatePicker and hide the placeholder when placeholder is tapped
        private void OnDatePlaceholderTapped(object sender, EventArgs e)
        {
            ExpirationDatePlaceholder.IsVisible = false;
            ExpirationDatePicker.IsVisible = true;
            ExpirationDatePicker.Focus();  // Open the DatePicker
        }

        // Handle DatePicker selection and update display
        private void OnDateSelected(object sender, DateChangedEventArgs e)
        {
            // Once a date is selected, keep the DatePicker visible
            ExpirationDatePlaceholder.IsVisible = false;
            ExpirationDatePicker.IsVisible = true;
        }

        private string GenerateProductCode(string productName, int productNumber)
        {
            // Get the first two uppercase letters of the product name
            string prefix = new string(productName.ToUpper().Take(2).ToArray());

            // Generate the code with 4 digits, padded with zeros
            string productCode = $"{prefix}_{productNumber:D4}";

            return productCode;
        }
    }
}

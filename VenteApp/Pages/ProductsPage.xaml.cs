using System.Globalization;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace VenteApp;

public partial class ProductsPage : ContentPage
{
    public ProductsPage()
    {
        InitializeComponent();

        this.Title = "Produits";

        try
        {
            // Pass the confirmation function to the ViewModel
            this.BindingContext = new ProductViewModel(ConfirmDeleteProduct);
        }
        catch (Exception ex)
        {
            // Log the inner exception to see what exactly is causing the issue
            Console.WriteLine($"Error: {ex.Message}, Inner Exception: {ex.InnerException?.Message}");
        }

    }

    private async Task<bool> ConfirmDeleteProduct(Product product)
    {
        return await DisplayAlert("Confirmation", $"Voulez-vous vraiment supprimer {product.Nom} ?", "Oui", "Non");
    }

    private async void OnAddProductClicked(object sender, EventArgs e)
    {
        // Rediriger vers la page de création de produit
        await Navigation.PushAsync(new CreateProductPage());
    }

    // Event handler for incrementing the quantity
    private async void OnEditProductClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var productDisplay = (ProductDisplay)((ViewCell)button.Parent.Parent).BindingContext;

        // Convert ProductDisplay back to Product
        var product = ((ProductViewModel)BindingContext).ConvertToProduct(productDisplay);

        // Rediriger vers la page de création de produit
        await Navigation.PushAsync(new CreateProductPage(product));
    }


    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (BindingContext is ProductViewModel viewModel)
        {
            viewModel.SearchCommand.Execute(e.NewTextValue);
        }
    }

    private async void OnShowProductClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var productDisplay = (ProductDisplay)((ViewCell)button.Parent.Parent).BindingContext;

        // Convert ProductDisplay back to Product
        var product = ((ProductViewModel)BindingContext).ConvertToProduct(productDisplay);

        // Navigate to a new page or show modal to display product details
        await Navigation.PushAsync(new ShowProductPage(product));
    }


    private async void OnDownloadProductsPdfClicked(object sender, EventArgs e)
    {
        try
        {
            var viewModel = (ProductViewModel)BindingContext;
            var products = viewModel.GetAllProductsForPdf();

            using (PdfDocument document = new PdfDocument())
            {
                document.Info.Title = "Liste des Produits";

                PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont titleFont = new XFont("Verdana", 20, XFontStyle.Bold);
                XFont headerFont = new XFont("Verdana", 12, XFontStyle.Bold);
                XFont contentFont = new XFont("Verdana", 10, XFontStyle.Regular);

                // Draw title
                gfx.DrawString("Liste des Produits", titleFont, XBrushes.Black, new XRect(0, 40, page.Width, 0), XStringFormats.TopCenter);

                // Set initial Y position for the content
                int yOffset = 80;

                // Define column widths and positions
                int[] columnWidths = { 60, 120, 100, 60, 80, 80, 60 };
                int[] columnPositions = { 20, 80, 200, 300, 360, 440, 520 };
                string[] headers = { "Code", "Nom", "Catégorie", "Taille", "Prix achat", "Prix vente", "Stock" };

                // Draw table header
                for (int i = 0; i < headers.Length; i++)
                {
                    gfx.DrawString(headers[i], headerFont, XBrushes.Black,
                        new XRect(columnPositions[i], yOffset, columnWidths[i], 20),
                        XStringFormats.TopLeft);
                }
                yOffset += 20;

                // Draw a line below the header
                gfx.DrawLine(XPens.Black, 20, yOffset, page.Width - 20, yOffset);
                yOffset += 10;

                // Add each product to the PDF
                foreach (var product in products)
                {
                    // Check if there's space for more content, otherwise add a new page
                    if (yOffset + 20 > page.Height - 40)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        yOffset = 40;
                    }

                    // Draw product details
                    gfx.DrawString(product.Code ?? "", contentFont, XBrushes.Black,
                        new XRect(columnPositions[0], yOffset, columnWidths[0], 20), XStringFormats.TopLeft);
                    gfx.DrawString(product.Nom ?? "", contentFont, XBrushes.Black,
                        new XRect(columnPositions[1], yOffset, columnWidths[1], 20), XStringFormats.TopLeft);
                    gfx.DrawString(product.Categorie ?? "", contentFont, XBrushes.Black,
                        new XRect(columnPositions[2], yOffset, columnWidths[2], 20), XStringFormats.TopLeft);
                    gfx.DrawString(product.Taille ?? "", contentFont, XBrushes.Black,
                        new XRect(columnPositions[3], yOffset, columnWidths[3], 20), XStringFormats.TopLeft);

                    // Handle currency formatting
                    string prixAchat = product.PrixAchat.ToString("C", CultureInfo.CurrentCulture);
                    string prixVente = product.PrixVente.ToString("C", CultureInfo.CurrentCulture);

                    gfx.DrawString(prixAchat, contentFont, XBrushes.Black,
                        new XRect(columnPositions[4], yOffset, columnWidths[4], 20), XStringFormats.TopLeft);
                    gfx.DrawString(prixVente, contentFont, XBrushes.Black,
                        new XRect(columnPositions[5], yOffset, columnWidths[5], 20), XStringFormats.TopLeft);
                    gfx.DrawString(product.Quantite.ToString(), contentFont, XBrushes.Black,
                        new XRect(columnPositions[6], yOffset, columnWidths[6], 20), XStringFormats.TopLeft);

                    yOffset += 20;
                }

                // Save the PDF file to the download folder
                string downloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Download");
                Directory.CreateDirectory(downloadFolder);
                string currentDate = DateTime.Now.ToString("yyyyMMdd_HHmm");
                string fileName = Path.Combine(downloadFolder, $"Liste_des_Produits_{currentDate}.pdf");

                document.Save(fileName);

                await Navigation.PushAsync(new PdfViewerPage(fileName));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating PDF: {ex.Message}");
            await DisplayAlert("Erreur", $"Une erreur s'est produite lors de la création du fichier PDF: {ex.Message}", "OK");
        }
    }


}

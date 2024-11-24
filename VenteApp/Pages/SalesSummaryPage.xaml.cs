using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.IO;

namespace VenteApp
{
    public partial class SalesSummaryPage : ContentPage
    {
        public SalesSummaryPage()
        {
            InitializeComponent();
            this.Title = "Total des ventes";
            BindingContext = new SalesSummaryViewModel();
        }

        // Event handler for search field text change
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (BindingContext is SalesSummaryViewModel viewModel)
            {
                viewModel.SearchTerm = e.NewTextValue; // Update the search term in the ViewModel
            }
        }

        private async void OnDownloadSalesSummaryPdfClicked(object sender, EventArgs e)
        {
            try
            {
                using (PdfDocument document = new PdfDocument())
                {
                    document.Info.Title = "Résumé des Ventes";

                    PdfPage page = document.AddPage();
                    XGraphics gfx = XGraphics.FromPdfPage(page);
                    XFont titleFont = new XFont("Verdana", 20, XFontStyle.Bold);
                    XFont labelFont = new XFont("Verdana", 12, XFontStyle.Regular);

                    // Draw title
                    gfx.DrawString("Résumé des Ventes", titleFont, XBrushes.Black, new XRect(0, 40, page.Width, 0), XStringFormats.TopCenter);

                    // Get all sales summary data (without pagination)
                    var allSalesSummary = GetAllSalesSummary();

                    // Set initial Y position for the content
                    int yOffset = 80;

                    // Draw table header
                    gfx.DrawString("Nom du Produit", labelFont, XBrushes.Black, new XRect(40, yOffset, 200, 0), XStringFormats.TopLeft);
                    gfx.DrawString("Quantité Totale Vendue", labelFont, XBrushes.Black, new XRect(240, yOffset, 200, 0), XStringFormats.TopLeft);
                    gfx.DrawString("Prix Total des Ventes", labelFont, XBrushes.Black, new XRect(440, yOffset, 100, 0), XStringFormats.TopLeft);
                    yOffset += 20;

                    // Draw a line below the header
                    gfx.DrawLine(XPens.Black, 40, yOffset, page.Width - 40, yOffset);
                    yOffset += 10;

                    // Add each product's sales summary to the PDF
                    foreach (var summary in allSalesSummary)
                    {
                        // Check if there's space for more content, otherwise add a new page
                        if (yOffset + 20 > page.Height - 40)
                        {
                            page = document.AddPage();
                            gfx = XGraphics.FromPdfPage(page);
                            yOffset = 40;
                        }

                        // Draw product details
                        gfx.DrawString(summary.Nom, labelFont, XBrushes.Black, new XRect(40, yOffset, 200, 0), XStringFormats.TopLeft);
                        gfx.DrawString(summary.TotalQuantitySold.ToString(), labelFont, XBrushes.Black, new XRect(240, yOffset, 200, 0), XStringFormats.TopLeft);
                        gfx.DrawString(summary.TotalSalesPrice.ToString("C"), labelFont, XBrushes.Black, new XRect(440, yOffset, 100, 0), XStringFormats.TopLeft);

                        // Move to the next line
                        yOffset += 20;
                    }

                    // Save the PDF file to the download folder
                    string downloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Download");
                    if (!Directory.Exists(downloadFolder))
                    {
                        Directory.CreateDirectory(downloadFolder);
                    }
                    string currentDate = DateTime.Now.ToString("yyyyMMdd_HHmm");
                    string fileName = Path.Combine(downloadFolder, $"Résumé_des_Ventes_{currentDate}.pdf");

                    using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                    {
                        document.Save(stream);
                    }

                    await Navigation.PushAsync(new PdfViewerPage(fileName));
                }

            }
            catch (Exception ex)
            {
                // Handle the exception (log it and display an error message)
                Console.WriteLine($"Error generating PDF: {ex.Message}");
                await DisplayAlert("Erreur", $"Une erreur s'est produite lors de la création du fichier PDF: {ex.Message}", "OK");
            }
        }

        // Method to get all sales summary data without pagination

        private List<ProductSalesSummary> GetAllSalesSummary()
        {
            using (var db = new AppDbContext())
            {
                // Load data into memory (LINQ-to-Objects), filtering by date and search term
                var totalSales = db.SaleTransactions
                                   .Include(st => st.Product) // Include product details
                                   .Where(st => st.DateDeVente >= ((SalesSummaryViewModel)BindingContext).DateDebut &&
                                                st.DateDeVente <= ((SalesSummaryViewModel)BindingContext).DateFin)
                                   .Where(st => string.IsNullOrEmpty(((SalesSummaryViewModel)BindingContext).SearchTerm) ||
                                                st.Product.Nom.ToLower().Contains(((SalesSummaryViewModel)BindingContext).SearchTerm.ToLower()))
                                   .ToList() // Load all matching records into memory
                                   .GroupBy(st => st.ProductId)
                                   .Select(group => new ProductSalesSummary
                                   {
                                       ProductId = group.Key,
                                       Nom = group.First().Product.Nom,
                                       TotalQuantitySold = group.Sum(st => st.Quantite),
                                       TotalSalesPrice = group.Sum(st => (decimal)(st.Quantite * st.Product.PrixVente)) // Sum in-memory
                                   })
                                   .ToList();

                return totalSales;
            }
        }


    }
}

using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.IO;

namespace VenteApp
{
    public partial class InventoryPage : ContentPage
    {
        public InventoryPage()
        {
            InitializeComponent();
            this.Title = "Inventaires";

            BindingContext = new InventoryViewModel();
        }

        // Search as the user types
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (BindingContext is InventoryViewModel viewModel)
            {
                viewModel.SearchCommand.Execute(e.NewTextValue);
            }
        }

        private async void OnDownloadInventoryPdfClicked(object sender, EventArgs e)
        {
            try
            {
                using (PdfDocument document = new PdfDocument())
                {
                    document.Info.Title = "Inventaire";

                    PdfPage page = document.AddPage();
                    XGraphics gfx = XGraphics.FromPdfPage(page);
                    XFont titleFont = new XFont("Verdana", 20, XFontStyle.Bold);
                    XFont labelFont = new XFont("Verdana", 10, XFontStyle.Regular);

                    // Draw title
                    gfx.DrawString("Inventaire", titleFont, XBrushes.Black, new XRect(0, 40, page.Width, 0), XStringFormats.TopCenter);

                    // Get all inventory data
                    var allInventory = GetAllInventory();

                    // Set initial Y position for the content
                    int yOffset = 80;

                    // Define column widths and positions
                    int[] columnWidths = { 150, 200, 100, 50, 50 };
                    int[] columnPositions = { 40, 190, 390, 490, 540 };

                    // Draw table header
                    string[] headers = { "Nom", "Description", "Catégorie", "Taille", "Stock" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        gfx.DrawString(headers[i], labelFont, XBrushes.Black,
                            new XRect(columnPositions[i], yOffset, columnWidths[i], 20),
                            XStringFormats.TopLeft);
                    }
                    yOffset += 20;

                    // Draw a line below the header
                    gfx.DrawLine(XPens.Black, 40, yOffset, page.Width - 40, yOffset);
                    yOffset += 10;

                    // Add each product to the PDF
                    foreach (var product in allInventory)
                    {
                        // Check if there's space for more content, otherwise add a new page
                        if (yOffset + 20 > page.Height - 40)
                        {
                            page = document.AddPage();
                            gfx = XGraphics.FromPdfPage(page);
                            yOffset = 40;
                        }

                        // Draw product details
                        gfx.DrawString(product.Nom ?? "", labelFont, XBrushes.Black,
                            new XRect(columnPositions[0], yOffset, columnWidths[0], 20), XStringFormats.TopLeft);
                        gfx.DrawString(product.Description ?? "", labelFont, XBrushes.Black,
                            new XRect(columnPositions[1], yOffset, columnWidths[1], 20), XStringFormats.TopLeft);
                        gfx.DrawString(product.Categorie ?? "", labelFont, XBrushes.Black,
                            new XRect(columnPositions[2], yOffset, columnWidths[2], 20), XStringFormats.TopLeft);
                        gfx.DrawString(product.Taille ?? "", labelFont, XBrushes.Black,
                            new XRect(columnPositions[3], yOffset, columnWidths[3], 20), XStringFormats.TopLeft);
                        gfx.DrawString(product.Quantite.ToString(), labelFont, XBrushes.Black,
                            new XRect(columnPositions[4], yOffset, columnWidths[4], 20), XStringFormats.TopLeft);

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
                    string fileName = Path.Combine(downloadFolder, $"Inventaire_{currentDate}.pdf");

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

        private List<Product> GetAllInventory()
        {
            using (var db = new AppDbContext())
            {
                return db.Products.ToList();
            }
        }
    }
}

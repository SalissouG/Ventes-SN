using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.Globalization;

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
                    gfx.DrawString("Inventaire", titleFont, XBrushes.Black,
                        new XRect(0, 40, page.Width, 0), XStringFormats.TopCenter);

                    var allInventory = GetAllInventory();
                    int yOffset = 80;

                    // Define column positions and widths to match HistoricalPage
                    int[] columnWidths = { 120, 100, 60, 100, 80 };
                    int[] columnPositions = { 20, 140, 240, 300, 400 };

                    string[] headers = { "Nom", "Cat�gorie", "Taille", "Stock", "Date d'Expiration" };

                    // Draw table header
                    for (int i = 0; i < headers.Length; i++)
                    {
                        gfx.DrawString(headers[i], labelFont, XBrushes.Black,
                            new XRect(columnPositions[i], yOffset, columnWidths[i], 20),
                            XStringFormats.TopLeft);
                    }
                    yOffset += 20;

                    // Draw header line
                    gfx.DrawLine(XPens.Black, 20, yOffset, page.Width - 20, yOffset);
                    yOffset += 10;

                    foreach (var product in allInventory)
                    {
                        if (yOffset + 20 > page.Height - 40)
                        {
                            page = document.AddPage();
                            gfx = XGraphics.FromPdfPage(page);
                            yOffset = 40;
                        }

                        // Draw product details
                        gfx.DrawString(TruncateText(product.Nom, columnWidths[0], labelFont, gfx), labelFont, XBrushes.Black,
                            new XRect(columnPositions[0], yOffset, columnWidths[0], 20), XStringFormats.TopLeft);
                        gfx.DrawString(TruncateText(product.Categorie, columnWidths[1], labelFont, gfx), labelFont, XBrushes.Black,
                            new XRect(columnPositions[1], yOffset, columnWidths[1], 20), XStringFormats.TopLeft);
                        gfx.DrawString(TruncateText(product.Taille, columnWidths[2], labelFont, gfx), labelFont, XBrushes.Black,
                            new XRect(columnPositions[2], yOffset, columnWidths[2], 20), XStringFormats.TopLeft);
                        gfx.DrawString(product.Quantite.ToString(), labelFont, XBrushes.Black,
                            new XRect(columnPositions[3], yOffset, columnWidths[3], 20), XStringFormats.TopLeft);
                        gfx.DrawString(product.DateExpiration?.ToString("dd/MM/yyyy", CultureInfo.CreateSpecificCulture("fr-FR")) ?? "N/A", labelFont, XBrushes.Black,
                            new XRect(columnPositions[4], yOffset, columnWidths[4], 20), XStringFormats.TopLeft);

                        yOffset += 20;
                    }

                    // Save PDF
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
                Console.WriteLine($"Error generating PDF: {ex.Message}");
                await DisplayAlert("Erreur", $"Une erreur s'est produite lors de la cr�ation du fichier PDF: {ex.Message}", "OK");
            }
        }

        private string TruncateText(string text, int maxWidth, XFont font, XGraphics gfx)
        {
            if (gfx.MeasureString(text, font).Width > maxWidth)
            {
                while (gfx.MeasureString(text + "...", font).Width > maxWidth && text.Length > 0)
                {
                    text = text.Substring(0, text.Length - 1);
                }
                return text + "...";
            }
            return text;
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

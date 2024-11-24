using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.IO;

namespace VenteApp;

public partial class HistoricalPage : ContentPage
{
    public HistoricalPage()
    {
        InitializeComponent();
        this.Title = "Historiques";
        BindingContext = new HistoricalViewModel();
    }

    // Event handler for dynamic search
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (BindingContext is HistoricalViewModel viewModel)
        {
            viewModel.OnSearch(e.NewTextValue); // Trigger the search when text changes
        }
    }
    private async void OnDownloadHistoricalPdfClicked(object sender, EventArgs e)
    {
        try
        {
            var viewModel = (HistoricalViewModel)BindingContext;
            var sales = viewModel.GetAllSalesForPdf();

            using (PdfDocument document = new PdfDocument())
            {
                document.Info.Title = "Historique des Ventes";

                PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont titleFont = new XFont("Verdana", 20, XFontStyle.Bold);
                XFont labelFont = new XFont("Verdana", 10, XFontStyle.Regular);

                // Draw title
                gfx.DrawString("Historique des Ventes", titleFont, XBrushes.Black, new XRect(0, 40, page.Width, 0), XStringFormats.TopCenter);

                // Set initial Y position for the content
                int yOffset = 80;

                // Define column widths and positions
                int[] columnWidths = { 120, 100, 60, 100, 80, 60 };
                int[] columnPositions = { 20, 140, 240, 300, 400, 480 };
                string[] headers = { "Nom", "Catégorie", "Taille", "Date Vente", "Prix", "Quantité" };

                // Draw table header
                for (int i = 0; i < headers.Length; i++)
                {
                    gfx.DrawString(headers[i], labelFont, XBrushes.Black,
                        new XRect(columnPositions[i], yOffset, columnWidths[i], 20),
                        XStringFormats.TopLeft);
                }
                yOffset += 20;

                // Draw a line below the header
                gfx.DrawLine(XPens.Black, 20, yOffset, page.Width - 20, yOffset);
                yOffset += 10;

                // Add each sale to the PDF
                foreach (var sale in sales)
                {
                    // Check if there's space for more content, otherwise add a new page
                    if (yOffset + 20 > page.Height - 40)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        yOffset = 40;
                    }

                    // Draw sale details
                    gfx.DrawString(sale.Product.Nom ?? "", labelFont, XBrushes.Black,
                        new XRect(columnPositions[0], yOffset, columnWidths[0], 20), XStringFormats.TopLeft);
                    gfx.DrawString(sale.Product.Categorie ?? "", labelFont, XBrushes.Black,
                        new XRect(columnPositions[1], yOffset, columnWidths[1], 20), XStringFormats.TopLeft);
                    gfx.DrawString(sale.Product.Taille ?? "", labelFont, XBrushes.Black,
                        new XRect(columnPositions[2], yOffset, columnWidths[2], 20), XStringFormats.TopLeft);
                    gfx.DrawString(sale.DateDeVente.ToString("dd/MM/yyyy HH:mm"), labelFont, XBrushes.Black,
                        new XRect(columnPositions[3], yOffset, columnWidths[3], 20), XStringFormats.TopLeft);
                    gfx.DrawString(sale.Product.PrixVente.ToString("C"), labelFont, XBrushes.Black,
                        new XRect(columnPositions[4], yOffset, columnWidths[4], 20), XStringFormats.TopLeft);
                    gfx.DrawString(sale.Quantite.ToString(), labelFont, XBrushes.Black,
                        new XRect(columnPositions[5], yOffset, columnWidths[5], 20), XStringFormats.TopLeft);

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
                string fileName = Path.Combine(downloadFolder, $"Historique_des_Ventes_{currentDate}.pdf");

                using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    document.Save(stream);
                }

                // Navigate to PdfViewerPage
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

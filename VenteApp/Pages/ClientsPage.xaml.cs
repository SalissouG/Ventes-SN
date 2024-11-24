using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.IO;

namespace VenteApp;

public partial class ClientsPage : ContentPage
{
    public ClientsPage()
    {
        InitializeComponent();
        this.Title = "Clients";

        try
        {
            this.BindingContext = new ClientViewModel(ConfirmDeleteClient);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}, Inner Exception: {ex.InnerException?.Message}");
        }
    }

    private async Task<bool> ConfirmDeleteClient(Client client)
    {
        return await DisplayAlert("Confirmation", $"Voulez-vous vraiment supprimer {client.Nom} ?", "Oui", "Non");
    }

    private async void OnAddClientClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CreateClientPage());
    }

    private async void OnEditClientClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var client = (Client)((ViewCell)button.Parent.Parent).BindingContext;
        await Navigation.PushAsync(new CreateClientPage(client));
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (BindingContext is ClientViewModel viewModel)
        {
            viewModel.SearchCommand.Execute(e.NewTextValue);
        }
    }

    private async void OnDownloadClientsPdfClicked(object sender, EventArgs e)
    {
        try
        {
            var viewModel = (ClientViewModel)BindingContext;
            var clients = viewModel.GetAllClientsForPdf();

            using (PdfDocument document = new PdfDocument())
            {
                document.Info.Title = "Liste des Clients";

                PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont titleFont = new XFont("Verdana", 20, XFontStyle.Bold);
                XFont labelFont = new XFont("Verdana", 10, XFontStyle.Regular);

                // Draw title
                gfx.DrawString("Liste des Clients", titleFont, XBrushes.Black, new XRect(0, 40, page.Width, 0), XStringFormats.TopCenter);

                // Set initial Y position for the content
                int yOffset = 80;

                // Define column widths and positions
                int[] columnWidths = { 100, 100, 150, 150 };
                int[] columnPositions = { 20, 120, 220, 370 };
                string[] headers = { "Nom", "Prénom", "Email", "Téléphone" };

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

                // Add each client to the PDF
                foreach (var client in clients)
                {
                    // Check if there's space for more content, otherwise add a new page
                    if (yOffset + 20 > page.Height - 40)
                    {
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        yOffset = 40;
                    }

                    // Draw client details
                    gfx.DrawString(client.Nom ?? "", labelFont, XBrushes.Black,
                        new XRect(columnPositions[0], yOffset, columnWidths[0], 20), XStringFormats.TopLeft);
                    gfx.DrawString(client.Prenom ?? "", labelFont, XBrushes.Black,
                        new XRect(columnPositions[1], yOffset, columnWidths[1], 20), XStringFormats.TopLeft);
                    gfx.DrawString(client.Email ?? "", labelFont, XBrushes.Black,
                        new XRect(columnPositions[2], yOffset, columnWidths[2], 20), XStringFormats.TopLeft);
                    gfx.DrawString(client.Numero ?? "", labelFont, XBrushes.Black,
                        new XRect(columnPositions[3], yOffset, columnWidths[3], 20), XStringFormats.TopLeft);

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
                string fileName = Path.Combine(downloadFolder, $"Liste_des_Clients_{currentDate}.pdf");

                using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    document.Save(stream);
                }

                // Navigate to PdfViewerPage
                await Navigation.PushAsync(new PdfViewerPage(fileName));
            }

            await DisplayAlert("Téléchargement", "Le fichier PDF de la liste des clients a été enregistré dans le dossier Téléchargements.", "OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating PDF: {ex.Message}");
            await DisplayAlert("Erreur", $"Une erreur s'est produite lors de la création du fichier PDF: {ex.Message}", "OK");
        }
    }
}

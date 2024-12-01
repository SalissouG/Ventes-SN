using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace VenteApp
{
    public partial class BillingPage : ContentPage
    {
        public ObservableCollection<BillingViewModel> BillingList { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }

        public BillingPage()
        {
            InitializeComponent();
            LoadBills();
            BindingContext = this;
        }

        // Load bills by grouping SaleTransactions by OrderId
        private void LoadBills(string searchQuery = "")
        {
            using (var db = new AppDbContext())
            {
                // Step 1: Query the database and load data into memory
                var transactionsQuery = db.SaleTransactions
                                          .Include(st => st.Client)
                                          .Include(st => st.Product)
                                          .AsQueryable();

                // Step 2: Apply filtering in the query
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    transactionsQuery = transactionsQuery.Where(st => (st.Client.Nom.ToLower().Contains(searchQuery.ToLower()) || st.Client.Prenom.ToLower().Contains(searchQuery.ToLower())) ||
                                                                      st.Product.Nom.ToLower().Contains(searchQuery.ToLower()));
                }

                // Fetch the data from the database
                var transactions = transactionsQuery.ToList();

                // Step 3: Group and aggregate in-memory
                var groupedBills = transactions
                                   .GroupBy(st => st.OrderId)
                                   .Select(g => new BillingViewModel
                                   {
                                       OrderId = g.Key,
                                       ClientName = g.FirstOrDefault()?.Client != null ? $"{g.FirstOrDefault().Client.Nom} {g.FirstOrDefault().Client.Prenom}" : "Client inconnu",
                                       TotalAmount = g.Sum(st => st.Product.PrixVente * st.Quantite), // Sum in-memory (LINQ to objects)
                                       NumberOfProducts = g.Sum(st => st.Quantite),
                                       SaleDate = g.Max(st => st.DateDeVente),
                                       PaymentMode = g.FirstOrDefault().PaymentMode // New property
                                   })
                                   .ToList();

                // Pagination logic
                int totalBills = groupedBills.Count();
                TotalPages = (int)Math.Ceiling(totalBills / 10.0);
                BillingList = new ObservableCollection<BillingViewModel>(
                    groupedBills.Skip((CurrentPage - 1) * 10).Take(10)
                );

                // Notify UI about updates
                OnPropertyChanged(nameof(BillingList));
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(TotalPages));
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            LoadBills(e.NewTextValue);
        }

        private async void OnViewBillClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var orderId = (Guid)button.CommandParameter;

            try
            {
                using (var db = new AppDbContext())
                {
                    // Fetch the billing details using the OrderId
                    var transactions = db.SaleTransactions
                                         .Include(st => st.Client)
                                         .Include(st => st.Product)
                                         .Where(st => st.OrderId == orderId)
                                         .ToList();

                    if (transactions == null || !transactions.Any())
                    {
                        await DisplayAlert("Erreur", "Aucune facture trouvée pour cet identifiant de commande.", "OK");
                        return;
                    }

                    var billing = new BillingViewModel
                    {
                        OrderId = orderId,
                        ClientName = transactions.FirstOrDefault()?.Client != null ? $"{transactions.FirstOrDefault().Client.Nom} {transactions.FirstOrDefault().Client.Prenom}" : "Client inconnu",
                        TotalAmount = transactions.Sum(st => st.Product.PrixVente * st.Quantite),
                        NumberOfProducts = transactions.Sum(st => st.Quantite),
                        SaleDate = transactions.Max(st => st.DateDeVente),
                        PaymentMode = transactions.FirstOrDefault().PaymentMode
                    };

                    // Create a new PDF document
                    PdfDocument document = new PdfDocument();
                    document.Info.Title = "Facture";

                    // Create an empty page
                    PdfPage page = document.AddPage();

                    // Get an XGraphics object for drawing
                    XGraphics gfx = XGraphics.FromPdfPage(page);

                    // Define fonts
                    XFont titleFont = new XFont("Verdana", 18, XFontStyle.Bold);
                    XFont labelFont = new XFont("Verdana", 12, XFontStyle.Regular);
                    XFont headerFont = new XFont("Verdana", 12, XFontStyle.Bold);
                    XFont totalFont = new XFont("Verdana", 12, XFontStyle.Bold);
                    XFont infoFont = new XFont("Verdana", 10, XFontStyle.Regular);

                    double logoHeight = 0; // Declare logoHeight outside the block

                    // Load and draw the logo at the top-right
                    string logoPath = Path.Combine("C:\\Ventes", "Images", "logowhite.jpg");
                    double yPosition = 40;  // Starting y position for elements
                    if (File.Exists(logoPath))
                    {
                        using (XImage logo = XImage.FromFile(logoPath))
                        {
                            double logoWidth = 120; // Adjust logo width as needed
                            logoHeight = logo.PixelHeight * (logoWidth / logo.PixelWidth);

                            // Calculate position to draw the logo on the right
                            double xPosition = page.Width - logoWidth - 40; // Right margin of 40
                            gfx.DrawImage(logo, xPosition, yPosition, logoWidth, logoHeight);

                            // Draw address and phone number below the logo
                            yPosition += logoHeight + 10; // Add space below the logo
                            gfx.DrawString("123 Rue de l'Entreprise", infoFont, XBrushes.Black,
                                new XRect(xPosition, yPosition, logoWidth, 20), XStringFormats.TopLeft);
                            gfx.DrawString("Téléphone: 01 23 45 67 89", infoFont, XBrushes.Black,
                                new XRect(xPosition, yPosition + 15, logoWidth, 20), XStringFormats.TopLeft);
                        }
                    }

                    // Draw title aligned to the left of the page
                    gfx.DrawString("Facture", titleFont, XBrushes.Black,
                        new XRect(40, yPosition - (logoHeight > 0 ? logoHeight + 30 : 70), page.Width - 80, 50), XStringFormats.TopLeft);

                    // Draw client information below the title
                    yPosition += 100;
                    gfx.DrawString($"Client: {billing.ClientName}", labelFont, XBrushes.Black,
                        new XRect(40, yPosition, page.Width - 80, 0), XStringFormats.Default);
                    gfx.DrawString($"Date: {billing.SaleDate:dd/MM/yyyy}", labelFont, XBrushes.Black,
                        new XRect(40, yPosition + 20, page.Width - 80, 0), XStringFormats.Default);
                    gfx.DrawString($"Mode de paiement: {billing.PaymentMode}", labelFont, XBrushes.Black,
                        new XRect(40, yPosition + 40, page.Width - 80, 0), XStringFormats.Default);

                    // Move down for the product table
                    yPosition += 80;

                    // Draw table headers with borders
                    gfx.DrawString("Produit", headerFont, XBrushes.Black, new XRect(40, yPosition, 200, 0), XStringFormats.Default);
                    gfx.DrawString("Quantité", headerFont, XBrushes.Black, new XRect(240, yPosition, 100, 0), XStringFormats.Default);
                    gfx.DrawString("Prix", headerFont, XBrushes.Black, new XRect(340, yPosition, 100, 0), XStringFormats.Default);

                    // Draw horizontal line under headers
                    gfx.DrawLine(XPens.Black, 40, yPosition + 15, page.Width - 40, yPosition + 15);

                    // Draw products with borders
                    yPosition += 30;
                    foreach (var transaction in transactions)
                    {
                        gfx.DrawString(transaction.Product.Nom, labelFont, XBrushes.Black, new XRect(40, yPosition, 200, 0), XStringFormats.Default);
                        gfx.DrawString(transaction.Quantite.ToString(), labelFont, XBrushes.Black, new XRect(240, yPosition, 100, 0), XStringFormats.Default);
                        gfx.DrawString((transaction.Product.PrixVente * transaction.Quantite).ToString("C"), labelFont, XBrushes.Black, new XRect(340, yPosition, 100, 0), XStringFormats.Default);
                        yPosition += 20;
                    }

                    // Draw total amount at the end of the table
                    yPosition += 30;
                    gfx.DrawString($"Total: {billing.TotalAmount:C}", totalFont, XBrushes.Black,
                        new XRect(340, yPosition, 100, 0), XStringFormats.TopRight);

                    // Save the PDF
                    string downloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Download");

                    if (!Directory.Exists(downloadFolder))
                    {
                        Directory.CreateDirectory(downloadFolder);
                    }

                    string currentDate = DateTime.Now.ToString("yyyyMMdd_HHmm");
                    string fileName = Path.Combine(downloadFolder, $"Facture_{billing.ClientName}_{currentDate}.pdf");

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
                await DisplayAlert("Erreur", $"Une erreur s'est produite lors de la création du fichier PDF: {ex.Message}", "OK");
            }
        }


        private async void OnDownloadAllBillingsPdfClicked(object sender, EventArgs e)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // Step 1: Query the database and load data into memory
                    var transactionsQuery = db.SaleTransactions
                                              .Include(st => st.Client)
                                              .Include(st => st.Product)
                                              .AsQueryable();

                    // Fetch the data from the database
                    var transactions = await transactionsQuery.ToListAsync();

                    // Step 2: Group and aggregate in-memory
                    var groupedBills = transactions
                        .GroupBy(st => st.OrderId)
                        .Select(g => new BillingViewModel
                        {
                            ClientName = g.FirstOrDefault()?.Client != null ? $"{g.FirstOrDefault().Client.Nom} {g.FirstOrDefault().Client.Prenom}" : "Client inconnu",
                            TotalAmount = g.Sum(st => st.Product.PrixVente * st.Quantite),
                            NumberOfProducts = g.Sum(st => st.Quantite),
                            SaleDate = g.Max(st => st.DateDeVente),
                            PaymentMode = g.FirstOrDefault().PaymentMode // New property
                        })
                        .OrderByDescending(b => b.SaleDate)
                        .ToList();

                    using (PdfDocument document = new PdfDocument())
                    {
                        PdfPage page = document.AddPage();
                        XGraphics gfx = XGraphics.FromPdfPage(page);
                        XFont titleFont = new XFont("Verdana", 20, XFontStyle.Bold);
                        XFont headerFont = new XFont("Verdana", 12, XFontStyle.Bold);
                        XFont contentFont = new XFont("Verdana", 10, XFontStyle.Regular);

                        // Draw title
                        gfx.DrawString("Liste des Facturations", titleFont, XBrushes.Black, new XRect(0, 40, page.Width, 0), XStringFormats.TopCenter);

                        // Set initial Y position for the content
                        int yOffset = 80;

                        // Define column widths and positions
                        int[] columnWidths = { 200, 100, 80, 120, 100 };
                        int[] columnPositions = { 20, 220, 320, 400, 520 };
                        string[] headers = { "Client", "Total", "Produits", "Date de vente", "Mode de paiement" };

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

                        // Add each billing to the PDF
                        foreach (var billing in groupedBills)
                        {
                            // Check if there's space for more content, otherwise add a new page
                            if (yOffset + 20 > page.Height - 40)
                            {
                                page = document.AddPage();
                                gfx = XGraphics.FromPdfPage(page);
                                yOffset = 40;
                            }

                            // Draw billing details
                            gfx.DrawString(billing.ClientName, contentFont, XBrushes.Black,
                                new XRect(columnPositions[0], yOffset, columnWidths[0], 20), XStringFormats.TopLeft);
                            gfx.DrawString(billing.TotalAmount.ToString(), contentFont, XBrushes.Black,
                                new XRect(columnPositions[1], yOffset, columnWidths[1], 20), XStringFormats.TopLeft);
                            gfx.DrawString(billing.NumberOfProducts.ToString(), contentFont, XBrushes.Black,
                                new XRect(columnPositions[2], yOffset, columnWidths[2], 20), XStringFormats.TopLeft);
                            gfx.DrawString(billing.SaleDate.ToString("dd/MM/yyyy"), contentFont, XBrushes.Black,
                                new XRect(columnPositions[3], yOffset, columnWidths[3], 20), XStringFormats.TopLeft);
                            gfx.DrawString(billing.PaymentMode.ToString(), contentFont, XBrushes.Black,
                                new XRect(columnPositions[4], yOffset, columnWidths[4], 20), XStringFormats.TopLeft);

                            yOffset += 20;
                        }

                        // Save the PDF file to the download folder
                        string downloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Download");
                        Directory.CreateDirectory(downloadFolder);
                        string currentDate = DateTime.Now.ToString("yyyyMMdd_HHmm");
                        string fileName = Path.Combine(downloadFolder, $"Liste_des_Facturations_{currentDate}.pdf");

                        document.Save(fileName);

                        // Navigate to PdfViewerPage
                        await Navigation.PushAsync(new PdfViewerPage(fileName));
                    }

                    await DisplayAlert("Téléchargement", "Le fichier PDF de la liste des facturations a été enregistré dans le dossier Téléchargements.", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating PDF: {ex.Message}");
                await DisplayAlert("Erreur", $"Une erreur s'est produite lors de la création du fichier PDF: {ex.Message}", "OK");
            }
        }


    }

    public class BillingViewModel
    {
        public Guid OrderId { get; set; }
        public string ClientName { get; set; }
        public decimal TotalAmount { get; set; }
        public int NumberOfProducts { get; set; }

        public DateTime SaleDate { get; set; }

        public PaymentMode PaymentMode { get; set; } // New property
    }
}

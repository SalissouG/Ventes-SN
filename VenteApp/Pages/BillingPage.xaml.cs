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
                                       SaleDate = g.Max(st => st.DateDeVente)
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

        private void OnViewBillClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var orderId = (Guid)button.CommandParameter;

            // Navigate to Bill Details Page
            Navigation.PushAsync(new BillDetailPage(orderId));
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
                            SaleDate = g.Max(st => st.DateDeVente)
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
                        int[] columnWidths = { 200, 100, 80, 120 };
                        int[] columnPositions = { 20, 220, 320, 400 };
                        string[] headers = { "Client", "Total", "Produits", "Date de vente" };

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
                            gfx.DrawString(billing.TotalAmount.ToString("C"), contentFont, XBrushes.Black,
                                new XRect(columnPositions[1], yOffset, columnWidths[1], 20), XStringFormats.TopLeft);
                            gfx.DrawString(billing.NumberOfProducts.ToString(), contentFont, XBrushes.Black,
                                new XRect(columnPositions[2], yOffset, columnWidths[2], 20), XStringFormats.TopLeft);
                            gfx.DrawString(billing.SaleDate.ToString("dd/MM/yyyy"), contentFont, XBrushes.Black,
                                new XRect(columnPositions[3], yOffset, columnWidths[3], 20), XStringFormats.TopLeft);

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
    }
}

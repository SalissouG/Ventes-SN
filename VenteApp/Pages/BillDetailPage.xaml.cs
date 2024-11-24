using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace VenteApp
{
    public partial class BillDetailPage : ContentPage
    {
        public BillDetailViewModel Bill { get; set; }

        public BillDetailPage(Guid orderId)
        {
            InitializeComponent();
            LoadBillDetails(orderId);
            BindingContext = this;
        }

        private void LoadBillDetails(Guid orderId)
        {
            using (var db = new AppDbContext())
            {
                var transactions = db.SaleTransactions
                                     .Include(st => st.Client)
                                     .Include(st => st.Product)
                                     .Where(st => st.OrderId == orderId)
                                     .ToList();

                if (transactions.Any())
                {
                    var firstTransaction = transactions.FirstOrDefault();
                    Bill = new BillDetailViewModel
                    {
                        ClientName = firstTransaction.Client != null ? $"{firstTransaction.Client.Nom} {firstTransaction.Client.Prenom}" : "Client inconnu",
                        TotalAmount = transactions.Sum(t => t.Product.PrixVente * t.Quantite),
                        SaleDate = firstTransaction.DateDeVente,
                        NumeroSiret = firstTransaction.Client.NumeroClient,
                        Products = transactions.Select(t => new ProductDetailViewModel
                        {
                            ProductName = t.Product.Nom,
                            Quantity = t.Quantite,
                            Price = t.Product.PrixVente
                        }).ToList()
                    };

                    OnPropertyChanged(nameof(Bill));
                }
            }
        }

        private async void OnDownloadPdfClicked(object sender, EventArgs e)
        {
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

            try
            {
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
                gfx.DrawString($"Client: {Bill.ClientName}", labelFont, XBrushes.Black,
                    new XRect(40, yPosition, page.Width - 80, 0), XStringFormats.Default);
                gfx.DrawString($"Date: {Bill.SaleDate:dd/MM/yyyy}", labelFont, XBrushes.Black,
                    new XRect(40, yPosition + 20, page.Width - 80, 0), XStringFormats.Default);
                gfx.DrawString($"Siret: {Bill.NumeroSiret}", labelFont, XBrushes.Black,
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
                foreach (var product in Bill.Products)
                {
                    gfx.DrawString(product.ProductName, labelFont, XBrushes.Black, new XRect(40, yPosition, 200, 0), XStringFormats.Default);
                    gfx.DrawString(product.Quantity.ToString(), labelFont, XBrushes.Black, new XRect(240, yPosition, 100, 0), XStringFormats.Default);
                    gfx.DrawString(product.Price.ToString("C"), labelFont, XBrushes.Black, new XRect(340, yPosition, 100, 0), XStringFormats.Default);
                    yPosition += 20;
                }

                // Draw total amount at the end of the table
                yPosition += 30;
                gfx.DrawString($"Total: {Bill.TotalAmount:C}", totalFont, XBrushes.Black,
                    new XRect(340, yPosition, 100, 0), XStringFormats.TopRight);

                // Save the PDF
                string downloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Download");

                if (!Directory.Exists(downloadFolder))
                {
                    Directory.CreateDirectory(downloadFolder);
                }

                string currentDate = DateTime.Now.ToString("yyyyMMdd_HHmm");
                string fileName = Path.Combine(downloadFolder, $"Facture_{Bill.ClientName}_{currentDate}.pdf");

                using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    document.Save(stream);
                }

                // Navigate to PdfViewerPage
                await Navigation.PushAsync(new PdfViewerPage(fileName));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", "Une erreur s'est produite lors de la création du fichier PDF.", "OK");
            }
        }
    }
    public class BillDetailViewModel
    {
        public string ClientName { get; set; }

        public string NumeroSiret { get; set; }
        public decimal TotalAmount { get; set; }

        public DateTime SaleDate { get; set; }

        public List<ProductDetailViewModel> Products { get; set; }
    }

    public class ProductDetailViewModel
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}

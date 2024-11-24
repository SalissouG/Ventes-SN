
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using SkiaSharp;
using Microcharts;

namespace VenteApp
{
    public partial class DashboardPage : ContentPage
    {
        public DashboardPage()
        {
            InitializeComponent();
            this.BindingContext = new DashboardViewModel();
        }

        private async void OnDownloadChartsPdfClicked(object sender, EventArgs e)
        {
            try
            {
                // Create a new PDF document
                PdfDocument document = new PdfDocument();
                document.Info.Title = "Tableau de bord des produits";

                // Add a page for most sold products chart
                PdfPage mostSoldPage = document.AddPage();
                XGraphics mostSoldGfx = XGraphics.FromPdfPage(mostSoldPage);
                mostSoldGfx.DrawString("Top 10 Produits les plus vendus", new XFont("Verdana", 20, XFontStyle.Bold), XBrushes.Black, new XRect(0, 0, mostSoldPage.Width, 40), XStringFormats.TopCenter);

                // Render MostSoldChart as an image
                SKBitmap mostSoldBitmap = RenderChartAsBitmap(((DashboardViewModel)BindingContext).MostSoldChart);
                using (MemoryStream ms = new MemoryStream())
                {
                    mostSoldBitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
                    XImage image = XImage.FromStream(() => new MemoryStream(ms.ToArray()));
                    mostSoldGfx.DrawImage(image, 50, 60, mostSoldPage.Width - 100, 300);
                }

                // Add a page for least sold products chart
                PdfPage leastSoldPage = document.AddPage();
                XGraphics leastSoldGfx = XGraphics.FromPdfPage(leastSoldPage);
                leastSoldGfx.DrawString("Top 10 Produits les moins vendus", new XFont("Verdana", 20, XFontStyle.Bold), XBrushes.Black, new XRect(0, 0, leastSoldPage.Width, 40), XStringFormats.TopCenter);

                // Render LeastSoldChart as an image
                SKBitmap leastSoldBitmap = RenderChartAsBitmap(((DashboardViewModel)BindingContext).LeastSoldChart);
                using (MemoryStream ms = new MemoryStream())
                {
                    leastSoldBitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
                    XImage image = XImage.FromStream(() => new MemoryStream(ms.ToArray()));
                    leastSoldGfx.DrawImage(image, 50, 60, leastSoldPage.Width - 100, 300);
                }

                // Save PDF file to the device's download folder
                string downloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Download");
                if (!Directory.Exists(downloadFolder))
                {
                    Directory.CreateDirectory(downloadFolder);
                }

                string currentDate = DateTime.Now.ToString("yyyyMMdd_HHmm");

                string fileName = Path.Combine(downloadFolder, $"Tableau_de_bord_{currentDate}.pdf");
                using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    document.Save(stream);
                }

                // Navigate to PdfViewerPage
                await Navigation.PushAsync(new PdfViewerPage(fileName));
            }
            catch (Exception ex)
            {
                // Handle the exception (log it or display an error message)
                await DisplayAlert("Erreur", "Une erreur s'est produite lors de la création du fichier PDF.", "OK");
            }
        }

        // Method to render a chart as a SkiaSharp bitmap
        private SKBitmap RenderChartAsBitmap(Chart chart)
        {
            // Define the size of the bitmap
            int width = 800;
            int height = 600;

            // Create a new bitmap
            SKBitmap bitmap = new SKBitmap(width, height);

            // Create a canvas from the bitmap
            using (SKCanvas canvas = new SKCanvas(bitmap))
            {
                // Clear the canvas
                canvas.Clear(SKColors.White);

                // Render the chart on the canvas
                chart.Draw(canvas, width, height);

                // Flush the canvas to apply the drawing
                canvas.Flush();
            }

            return bitmap;
        }

    }
}

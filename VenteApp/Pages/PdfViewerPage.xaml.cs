namespace VenteApp
{
    public partial class PdfViewerPage : ContentPage
    {
        public PdfViewerPage(string pdfPath)
        {
            InitializeComponent();

            // Load the PDF file in the WebView
            PdfWebView.Source = new UrlWebViewSource
            {
                Url = pdfPath
            };
        }
    }
}

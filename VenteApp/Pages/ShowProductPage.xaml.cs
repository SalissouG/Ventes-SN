namespace VenteApp
{
    public partial class ShowProductPage : TabbedPage
    {
        public ShowProductPage(Product product)
        {
            InitializeComponent();
            BindingContext = product;

           
        }
    }

}

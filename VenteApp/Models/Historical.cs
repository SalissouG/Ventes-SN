
namespace VenteApp
{
    public class Historical
    {
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public List<Sale> SalesData { get; set; }  // Sales data between start and end dates
    }

}

using Microcharts;
using SkiaSharp;

namespace VenteApp
{
    public class DashboardViewModel : BindableObject
    {
        private DateTime _dateDebut;
        private DateTime _dateFin;
        private Chart _mostSoldChart;
        private Chart _leastSoldChart;

        public DateTime DateDebut
        {
            get => _dateDebut;
            set
            {
                _dateDebut = value;
                OnPropertyChanged();
                LoadSalesData();
            }
        }

        public DateTime DateFin
        {
            get => _dateFin;
            set
            {
                _dateFin = value;
                OnPropertyChanged();
                LoadSalesData();
            }
        }

        public Chart MostSoldChart
        {
            get => _mostSoldChart;
            set
            {
                _mostSoldChart = value;
                OnPropertyChanged();
            }
        }

        public Chart LeastSoldChart
        {
            get => _leastSoldChart;
            set
            {
                _leastSoldChart = value;
                OnPropertyChanged();
            }
        }

        public DashboardViewModel()
        {
            // Set default date range (last month to today)
            DateDebut = DateTime.Now.AddMonths(-1);
            DateFin = DateTime.Now;

            // Load initial data
            LoadSalesData();
        }

        // Load sales data and update the charts
        private void LoadSalesData()
        {
            using (var db = new AppDbContext())
            {
                // Fetch sales between DateDebut and DateFin
                var sales = db.SaleTransactions
                    .Where(st => st.DateDeVente >= DateDebut && st.DateDeVente <= DateFin)
                    .GroupBy(st => st.ProductId)
                    .Select(group => new ProductSales
                    {
                        ProductId = group.Key,
                        TotalQuantitySold = group.Sum(st => st.Quantite),
                        ProductName = group.First().Product.Nom
                    })
                    .ToList();

                // Check if there are any sales
                if (!sales.Any())
                {
                    // No sales data, display empty charts or placeholder
                    MostSoldChart = CreateEmptyChart("No data available");
                    LeastSoldChart = CreateEmptyChart("No data available");
                    return;
                }

                // Top 10 most sold products
                var mostSoldProducts = sales
                    .OrderByDescending(s => s.TotalQuantitySold)
                    .Take(10)
                    .ToList();

                // Top 10 least sold products
                var leastSoldProducts = sales
                    .OrderBy(s => s.TotalQuantitySold)
                    .Take(10)
                    .ToList();

                // Generate the chart for the most sold products
                MostSoldChart = new BarChart
                {
                    Entries = mostSoldProducts.Select(s => new ChartEntry(s.TotalQuantitySold)
                    {
                        Label = s.ProductName,
                        ValueLabel = s.TotalQuantitySold.ToString(),
                        Color = SKColor.Parse("#2c3e50")
                    }).ToList(),
                    LabelTextSize = 35,
                    ValueLabelOrientation = Orientation.Horizontal,  // Orient the labels horizontally
                    LabelOrientation = Orientation.Horizontal  // Labels at the bottom
                };

                // Generate the chart for the least sold products
                LeastSoldChart = new BarChart
                {
                    Entries = leastSoldProducts.Select(s => new ChartEntry(s.TotalQuantitySold)
                    {
                        Label = s.ProductName,
                        ValueLabel = s.TotalQuantitySold.ToString(),
                        Color = SKColor.Parse("#e74c3c")
                    }).ToList(),
                    LabelTextSize = 35,
                    ValueLabelOrientation = Orientation.Horizontal,  // Orient the labels horizontally
                    LabelOrientation = Orientation.Horizontal  // Labels at the bottom
                };
            }
        }

        // Create an empty chart to show when no data is available
        private Chart CreateEmptyChart(string message)
        {
            return new BarChart
            {
                Entries = new List<ChartEntry>
                {
                    new ChartEntry(0)
                    {
                        Label = message,
                        ValueLabel = "",
                        Color = SKColor.Parse("#bdc3c7")  // Gray color for placeholder
                    }
                },
                LabelTextSize = 35,
                ValueLabelOrientation = Orientation.Horizontal,
                LabelOrientation = Orientation.Horizontal
            };
        }
    }

    public class ProductSales
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public int TotalQuantitySold { get; set; }
    }
}

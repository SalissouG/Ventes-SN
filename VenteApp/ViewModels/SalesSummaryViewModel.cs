using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace VenteApp
{
    public class SalesSummaryViewModel : BindableObject
    {
        private const int PageSize = 10; // Number of products per page
        private int _currentPage = 1; // Current page number
        private int _totalPages; // Total number of pages

        private DateTime _dateDebut;
        private DateTime _dateFin;
        private string _searchTerm = string.Empty; // Search term for filtering

        public ObservableCollection<ProductSalesSummary> ProductSalesSummary { get; set; }

        public DateTime DateDebut
        {
            get => _dateDebut;
            set
            {
                _dateDebut = value;
                OnPropertyChanged();
                LoadSalesSummary(); // Automatically load the summary when DateDebut changes
            }
        }

        public DateTime DateFin
        {
            get => _dateFin;
            set
            {
                _dateFin = value;
                OnPropertyChanged();
                LoadSalesSummary(); // Automatically load the summary when DateFin changes
            }
        }

        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value;
                OnPropertyChanged();
                LoadSalesSummary(); // Reload sales when search term changes
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set
            {
                _totalPages = value;
                OnPropertyChanged();
            }
        }

        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }

        public SalesSummaryViewModel()
        {
            ProductSalesSummary = new ObservableCollection<ProductSalesSummary>();

            // Set default date range to last month to today
            DateDebut = DateTime.Now.AddMonths(-1);
            DateFin = DateTime.Now;

            NextPageCommand = new Command(OnNextPage);
            PreviousPageCommand = new Command(OnPreviousPage);

            LoadSalesSummary(); // Load the sales summary for the first page
        }

        // Load total sales summary and the first page, filtered by date range and search term
        private void LoadSalesSummary()
        {
            using (var db = new AppDbContext())
            {
                // Load data into memory (LINQ-to-Objects), filtering by date and search term
                var totalSales = db.SaleTransactions
                                   .Include(st => st.Product) // Include product details
                                   .Where(st => st.DateDeVente >= DateDebut && st.DateDeVente <= DateFin)
                                   .Where(st => string.IsNullOrEmpty(SearchTerm) ||
                                                st.Product.Nom.ToLower().Contains(SearchTerm.ToLower()))
                                   .ToList() // Load all matching records into memory
                                   .GroupBy(st => st.ProductId)
                                   .Select(group => new ProductSalesSummary
                                   {
                                       ProductId = group.Key,
                                       Nom = group.First().Product.Nom,
                                       TotalQuantitySold = group.Sum(st => st.Quantite),
                                       TotalSalesPrice = group.Sum(st => st.Quantite * st.Product.PrixVente) // Sum in-memory
                                   })
                                   .ToList();

                // Calculate total pages based on PageSize
                TotalPages = (int)Math.Ceiling(totalSales.Count / (double)PageSize);

                // Load the first page
                LoadPage(CurrentPage, totalSales);
            }
        }

        // Load sales summary for the current page
        private void LoadPage(int pageNumber, List<ProductSalesSummary> allSalesSummary)
        {
            // Calculate the number of products to skip based on the current page
            int skip = (pageNumber - 1) * PageSize;

            // Get the paged sales summary for the current page
            var pagedSalesSummary = allSalesSummary
                                    .Skip(skip)
                                    .Take(PageSize)
                                    .ToList();

            ProductSalesSummary.Clear();
            foreach (var summary in pagedSalesSummary)
            {
                ProductSalesSummary.Add(summary);
            }
        }

        // Handle the next page navigation
        private void OnNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadSalesSummary(); // Load the next page
            }
        }

        // Handle the previous page navigation
        private void OnPreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadSalesSummary(); // Load the previous page
            }
        }
    }

    // Model class for displaying product sales summary
    public class ProductSalesSummary
    {
        public Guid ProductId { get; set; }
        public string Nom { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalSalesPrice { get; set; }
    }
}

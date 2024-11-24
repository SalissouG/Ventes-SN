using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace VenteApp
{
    public class HistoricalViewModel : BindableObject
    {
        private const int PageSize = 10; // Number of sales per page
        private int _currentPage = 1; // Current page number
        private int _totalPages; // Total number of pages

        private DateTime _dateDebut;
        private DateTime _dateFin;
        private ObservableCollection<SaleTransaction> _filteredSales;

        public DateTime DateDebut
        {
            get => _dateDebut;
            set
            {
                _dateDebut = value;
                OnPropertyChanged();
                FilterSales(); // Automatically filter when DateDebut changes
            }
        }

        public DateTime DateFin
        {
            get => _dateFin;
            set
            {
                _dateFin = value;
                OnPropertyChanged();
                FilterSales(); // Automatically filter when DateFin changes
            }
        }

        public ObservableCollection<SaleTransaction> FilteredSales
        {
            get => _filteredSales;
            set
            {
                _filteredSales = value;
                OnPropertyChanged();
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

        public ICommand SearchCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }

        public HistoricalViewModel()
        {
            DateDebut = DateTime.Now.AddMonths(-1);
            DateFin = DateTime.Now;

            FilteredSales = new ObservableCollection<SaleTransaction>();

            SearchCommand = new Command<string>(OnSearch);
            NextPageCommand = new Command(OnNextPage);
            PreviousPageCommand = new Command(OnPreviousPage);

            LoadSalesFromDatabase(); // Load the first page
        }

        // Load sales data from the database and calculate total pages
        private void LoadSalesFromDatabase()
        {
            using (var db = new AppDbContext())
            {
                var totalSales = db.SaleTransactions.Count();
                TotalPages = (int)Math.Ceiling(totalSales / (double)PageSize);

                //LoadPage(CurrentPage); // Load the current page

                int skip = (CurrentPage - 1) * PageSize;
                var pagedSales = db.SaleTransactions
                                   .Include(sale => sale.Product)
                                   .OrderBy(sale => sale.DateDeVente)
                                   .Where(sale => sale.DateDeVente >= DateDebut && sale.DateDeVente <= DateFin)
                                   .Skip(skip)
                                   .Take(PageSize)
                                   .ToList();

                FilteredSales.Clear();
                foreach (var sale in pagedSales)
                {
                    FilteredSales.Add(sale);
                }
            }
        }

        // Load sales for the current page
        private void LoadPage(int pageNumber)
        {
            using (var db = new AppDbContext())
            {
                int skip = (pageNumber - 1) * PageSize;
                var pagedSales = db.SaleTransactions
                                   .Include(sale => sale.Product)
                                   .OrderBy(sale => sale.DateDeVente)
                                   .Where(sale => sale.DateDeVente >= DateDebut && sale.DateDeVente <= DateFin)
                                   .Skip(skip)
                                   .Take(PageSize)
                                   .ToList();

                FilteredSales.Clear();
                foreach (var sale in pagedSales)
                {
                    FilteredSales.Add(sale);
                }
            }
        }

        // Handle the next page navigation
        private void OnNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                FilterSales(); // Load the next page
            }
        }

        // Handle the previous page navigation
        private void OnPreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                FilterSales(); // Load the previous page
            }
        }

        // Search sales based on the product name or description

        private void FilterSales()
        {
            using (var db = new AppDbContext())
            {
                // Fetch the total number of records in the specified date range
                var totalSales = db.SaleTransactions
                                   .Where(sale => sale.DateDeVente >= DateDebut && sale.DateDeVente <= DateFin)
                                   .Count();

                // Calculate total pages based on PageSize
                TotalPages = (int)Math.Ceiling(totalSales / (double)PageSize);

                // Ensure CurrentPage is valid
                if (CurrentPage > TotalPages)
                    CurrentPage = TotalPages > 0 ? TotalPages : 1;

                // Calculate the number of records to skip based on the current page
                int skip = (CurrentPage - 1) * PageSize;

                // Fetch sales data from the database for the current page
                var pagedSales = db.SaleTransactions
                                   .Include(sale => sale.Product) // Include the Product details
                                   .Where(sale => sale.DateDeVente >= DateDebut && sale.DateDeVente <= DateFin)
                                   .OrderBy(sale => sale.DateDeVente)
                                   .Skip(skip)
                                   .Take(PageSize)
                                   .ToList();

                // Clear the existing records and add the newly fetched records to FilteredSales
                if (FilteredSales != null)
                {
                    FilteredSales.Clear();
                    foreach (var sale in pagedSales)
                    {
                        FilteredSales.Add(sale);
                    }
                }
               
            }
        }

        public void OnSearch(string searchTerm)
        {
            using (var db = new AppDbContext())
            {
                var filteredSales = db.SaleTransactions
                                      .Include(sale => sale.Product)
                                      .Where(sale => sale.Product.Nom.ToLower().Contains(searchTerm.ToLower()) ||
                                                     sale.Product.Description.ToLower().Contains(searchTerm.ToLower()) ||
                                                     sale.Product.Categorie.ToLower().Contains(searchTerm.ToLower()))
                                      .Where(sale => sale.DateDeVente >= DateDebut && sale.DateDeVente <= DateFin)
                                      .OrderBy(sale => sale.DateDeVente)
                                      .Skip((CurrentPage - 1) * PageSize)
                                      .Take(PageSize)
                                      .ToList();

                FilteredSales.Clear();
                foreach (var sale in filteredSales)
                {
                    FilteredSales.Add(sale);
                }

                // Update total pages for the search results
                TotalPages = (int)Math.Ceiling(filteredSales.Count / (double)PageSize);
            }
        }

        public List<SaleTransaction> GetAllSalesForPdf()
        {
            using (var db = new AppDbContext())
            {
                return db.SaleTransactions
                         .Include(sale => sale.Product)
                         .Where(sale => sale.DateDeVente >= DateDebut && sale.DateDeVente <= DateFin)
                         .OrderBy(sale => sale.DateDeVente)
                         .ToList();
            }
        }
    }
}











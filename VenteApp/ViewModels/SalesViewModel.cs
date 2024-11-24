using System.Collections.ObjectModel;
using System.Windows.Input;

namespace VenteApp
{
    public class SalesViewModel : BindableObject
    {
        private const int PageSize = 10; // Number of sales per page
        private int _currentPage = 1; // Current page number
        private int _totalPages; // Total number of pages

        public ObservableCollection<Sale> Sales { get; set; }
        public List<Product> AllProducts { get; set; } // To store all products for search and pagination

        public ICommand SearchCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }

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

        public SalesViewModel()
        {
            Sales = new ObservableCollection<Sale>();

            // Initialize the search and pagination commands
            SearchCommand = new Command<string>(OnSearchSales);
            NextPageCommand = new Command(OnNextPage);
            PreviousPageCommand = new Command(OnPreviousPage);

            LoadSales(); // Load the first page of sales

        }

        // Load all products from the database and convert them to sales with pagination
        private void LoadSales()
        {
            using (var db = new AppDbContext())
            {
                // Load all products from the database
                AllProducts = db.Products.ToList();

                // Calculate total pages
                TotalPages = (int)Math.Ceiling(AllProducts.Count / (double)PageSize);

                // Load the first page of sales
                int skip = (CurrentPage - 1) * PageSize;

                // Fetch products for the current page
                var pagedProducts = db.Products
                                      .OrderBy(p => p.Nom)
                                      .Skip(skip)
                                      .Take(PageSize)
                                      .ToList();

                // Convert products to sales
                var pagedSales = ConvertProductsToSales(pagedProducts);

                // Clear and reload the sales collection for the current page
                Sales.Clear();
                foreach (var sale in pagedSales)
                {
                    Sales.Add(sale);
                }
            }
        }

        // Load sales for the current page
        private void LoadPage(int pageNumber)
        {
            using (var db = new AppDbContext())
            {
                int skip = (pageNumber - 1) * PageSize;

                // Fetch products for the current page
                var pagedProducts = db.Products
                                      .OrderBy(p => p.Nom)
                                      .Skip(skip)
                                      .Take(PageSize)
                                      .ToList();

                // Convert products to sales
                var pagedSales = ConvertProductsToSales(pagedProducts);

                // Clear and reload the sales collection for the current page
                Sales.Clear();
                foreach (var sale in pagedSales)
                {
                    Sales.Add(sale);
                }
            }
        }

        // Handle the next page navigation
        private void OnNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadPage(CurrentPage); // Load the next page of sales
            }
        }

        // Handle the previous page navigation
        private void OnPreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadPage(CurrentPage); // Load the previous page of sales
            }
        }

        // Search sales based on product name (case-insensitive) with pagination
        private void OnSearchSales(string query)
        {
            using (var db = new AppDbContext())
            {
                // Perform a case-insensitive search in the database
                var filteredProducts = db.Products
                                         .Where(p => p.Nom.ToLower().Contains(query.ToLower()) ||
                                                     p.Description.ToLower().Contains(query.ToLower()) ||
                                                     p.Categorie.ToLower().Contains(query.ToLower()))
                                         .ToList();

                // Update the pagination info
                TotalPages = (int)Math.Ceiling(filteredProducts.Count / (double)PageSize);
                CurrentPage = 1; // Reset to the first page of the search result

                // Load the first page of the filtered sales
                var filteredSales = ConvertProductsToSales(filteredProducts.Take(PageSize).ToList());

                // Clear and reload the sales collection for the search result
                Sales.Clear();
                foreach (var sale in filteredSales)
                {
                    Sales.Add(sale);
                }
            }
        }

        // Convert product list to sale list
        private List<Sale> ConvertProductsToSales(List<Product> products)
        {
            return products.Select(product => new Sale
            {
                ProductId = product.Id,
                Nom = product.Nom,
                Description = product.Description,
                Prix = product.PrixVente,
                Quantite = 0,  // Initialize sales quantity as 0
                Categorie = product.Categorie,
                Taille = product.Taille,
                DateDeVente = DateTime.Now
            }).ToList();
        }
    }
}

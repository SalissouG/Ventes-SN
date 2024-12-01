using System.Collections.ObjectModel;
using System.Windows.Input;

namespace VenteApp
{
    public class InventoryViewModel : BindableObject
    {
        private const int PageSize = 10; // Number of products per page
        private int _currentPage = 1; // Current page number
        private int _totalPages; // Total number of pages

        public ObservableCollection<Product> Products { get; set; }
        public ObservableCollection<Product> AllProducts { get; set; } // Used for search functionality

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

        public InventoryViewModel()
        {
            Products = new ObservableCollection<Product>();

            SearchCommand = new Command<string>(OnSearchProducts);
            NextPageCommand = new Command(OnNextPage);
            PreviousPageCommand = new Command(OnPreviousPage);

            LoadProducts(); // Load the first page
        }

        // Load products from the database with pagination
        private void LoadProducts()
        {
            using (var db = new AppDbContext())
            {
                var totalProducts = db.Products.Count();
                TotalPages = (int)Math.Ceiling(totalProducts / (double)PageSize);

                //LoadPage(CurrentPage); // Load the current page
                int skip = (CurrentPage - 1) * PageSize;
                var pagedProducts = db.Products
                                      .OrderBy(p => p.Nom)
                                      .Skip(skip)
                                      .Take(PageSize)
                                      .ToList();

                Products.Clear();
                foreach (var product in pagedProducts)
                {
                    Products.Add(product);
                }
            }
        }

        // Load products for the current page
        private void LoadPage(int pageNumber)
        {
            using (var db = new AppDbContext())
            {
                int skip = (pageNumber - 1) * PageSize;
                var pagedProducts = db.Products
                                      .OrderBy(p => p.Nom)
                                      .Skip(skip)
                                      .Take(PageSize)
                                      .ToList();

                Products.Clear();
                foreach (var product in pagedProducts)
                {
                    Products.Add(product);
                }
            }
        }

        // Handle the next page navigation
        private void OnNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadPage(CurrentPage); // Load the next page
            }
        }

        // Handle the previous page navigation
        private void OnPreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadPage(CurrentPage); // Load the previous page
            }
        }

        // Handle product search with pagination
        private void OnSearchProducts(string query)
        {
            using (var db = new AppDbContext())
            {
                var filteredProducts = db.Products
                                         .Where(p => p.Nom.ToLower().Contains(query.ToLower()) ||
                                                     p.Description.ToLower().Contains(query.ToLower()) ||
                                                     p.Categorie.ToLower().Contains(query.ToLower()) ||
                                                     p.DateExpiration.HasValue && p.DateExpiration.Value.ToString("dd/MM/yyyy").Contains(query))
                                         .ToList();

                // Update the pagination info
                TotalPages = (int)Math.Ceiling(filteredProducts.Count / (double)PageSize);
                CurrentPage = 1; // Reset to the first page of the search result

                // Load the first page of the filtered products
                var pagedProducts = filteredProducts.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();

                Products.Clear();
                foreach (var product in pagedProducts)
                {
                    Products.Add(product);
                }
            }
        }
    }
}

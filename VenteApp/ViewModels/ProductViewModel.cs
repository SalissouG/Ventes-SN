using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace VenteApp
{
    public class ProductViewModel : BindableObject
    {
        private const int PageSize = 10; // Number of products per page
        private int _currentPage = 1; // Current page number
        private int _totalPages; // Total number of pages

        public ObservableCollection<ProductDisplay> Products { get; set; } // Only products for the current page
        public ICommand DeleteCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }

        private readonly Func<Product, Task<bool>> _confirmDelete;

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

        public ProductViewModel(Func<Product, Task<bool>> confirmDelete)
        {
            Products = new ObservableCollection<ProductDisplay>();
            _confirmDelete = confirmDelete;

            DeleteCommand = new Command<ProductDisplay>(OnDeleteProduct);
            SearchCommand = new Command<string>(OnSearchProducts);
            NextPageCommand = new Command(OnNextPage);
            PreviousPageCommand = new Command(OnPreviousPage);

            LoadProducts(); // Load products for the first page
        }

        // Load total number of products and first page from the database
        private void LoadProducts()
        {
            using (var db = new AppDbContext())
            {
                try
                {
                    // Get the total number of products
                    int totalProducts = db.Products.Count();

                    // Calculate total pages
                    TotalPages = (int)Math.Ceiling(totalProducts / (double)PageSize);

                    if (totalProducts == 0)
                    {
                        Products.Clear();
                        return;
                    }

                    // Calculate the number of products to skip based on the current page
                    int skip = (CurrentPage - 1) * PageSize;

                    // Fetch the products for the current page
                    var pagedProducts = db.Products
                                          .OrderBy(p => p.Nom) // Optional: Order by name
                                          .Skip(skip)
                                          .Take(PageSize)
                                          .ToList();

                    // Convert products to ProductDisplay and load the products for the current page
                    Products.Clear();
                    foreach (var product in pagedProducts)
                    {
                        Products.Add(ConvertToProductDisplay(product));
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception
                    Console.WriteLine($"Error loading products: {ex.Message}");
                }
            }
        }

        // Load products by page, directly from the database
        private void LoadPage(int pageNumber)
        {
            using (var db = new AppDbContext())
            {
                try
                {
                    // Calculate the number of products to skip based on the current page
                    int skip = (pageNumber - 1) * PageSize;

                    // Fetch the products for the current page
                    var pagedProducts = db.Products
                                          .OrderBy(p => p.Nom) // Optional: Order by name
                                          .Skip(skip)
                                          .Take(PageSize)
                                          .ToList();

                    // Convert products to ProductDisplay and load the products for the current page
                    Products.Clear();
                    foreach (var product in pagedProducts)
                    {
                        Products.Add(ConvertToProductDisplay(product));
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception
                    Console.WriteLine($"Error loading page {pageNumber}: {ex.Message}");
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

        private async void OnDeleteProduct(ProductDisplay productDisplay)
        {
            if (productDisplay == null)
                return;

            // Convert ProductDisplay back to Product
            var product = ConvertToProduct(productDisplay);

            // Ask for confirmation before deleting
            bool confirm = await _confirmDelete(product);
            if (!confirm)
                return;

            using (var db = new AppDbContext())
            {
                try
                {
                    // Find the product to delete
                    var productToDelete = db.Products.Find(product.Id);
                    if (productToDelete != null)
                    {
                        // Find and delete all associated sale transactions
                        var saleTransactionsToDelete = db.SaleTransactions
                                                         .Where(st => st.ProductId == product.Id)
                                                         .ToList();
                        foreach (var saleTransaction in saleTransactionsToDelete)
                        {
                            db.SaleTransactions.Remove(saleTransaction);
                        }

                        // Remove the product
                        db.Products.Remove(productToDelete);

                        // Save changes
                        db.SaveChanges();
                    }

                    int totalProducts = db.Products.Count();
                    TotalPages = (int)Math.Ceiling(totalProducts / (double)PageSize);

                    // Ensure that the current page does not exceed the total pages
                    if (TotalPages == 0)
                    {
                        TotalPages = 1;
                        CurrentPage = 1;
                    }
                    else if (CurrentPage > TotalPages)
                    {
                        CurrentPage = TotalPages; // Go back to the last available page
                    }

                    // Calculate the number of products to skip based on the current page
                    int skip = (CurrentPage - 1) * PageSize;

                    // Fetch the products for the current page
                    var pagedProducts = db.Products
                                          .OrderBy(p => p.Nom) // Optional: Order by name
                                          .Skip(skip)
                                          .Take(PageSize)
                                          .ToList();

                    // Convert products to ProductDisplay and load the products for the current page
                    Products.Clear();
                    foreach (var pagedProduct in pagedProducts)
                    {
                        Products.Add(ConvertToProductDisplay(pagedProduct));
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception
                    Console.WriteLine($"Error deleting product: {ex.Message}");
                }
            }
        }

        // Handle searching products directly from the database
        private void OnSearchProducts(string query)
        {
            using (var db = new AppDbContext())
            {
                try
                {
                    // Fetch products matching the search query, applying pagination
                    var filteredProducts = db.Products
                                             .Where(p => p.Nom.ToLower().Contains(query.ToLower()) ||
                                                         p.Description.ToLower().Contains(query.ToLower()) ||
                                                         p.Categorie.ToLower().Contains(query.ToLower()) ||
                                                         p.Code.ToLower().Contains(query.ToLower()))
                                             .OrderBy(p => p.Nom) // Optional: Order by name
                                             .Skip((CurrentPage - 1) * PageSize)
                                             .Take(PageSize)
                                             .ToList();

                    // Convert products to ProductDisplay and update the ObservableCollection with the filtered products
                    Products.Clear();
                    foreach (var product in filteredProducts)
                    {
                        Products.Add(ConvertToProductDisplay(product));
                    }

                    // Update the total pages based on the filtered result count
                    int totalFilteredProducts = db.Products
                                                  .Count(p => p.Nom.ToLower().Contains(query.ToLower()) ||
                                                              p.Description.ToLower().Contains(query.ToLower()) ||
                                                              p.Categorie.ToLower().Contains(query.ToLower()));
                    TotalPages = (int)Math.Ceiling(totalFilteredProducts / (double)PageSize);
                }
                catch (Exception ex)
                {
                    // Log the exception
                    Console.WriteLine($"Error searching products: {ex.Message}");
                }
            }
        }

        public List<Product> GetAllProductsForPdf()
        {
            using (var db = new AppDbContext())
            {
                try
                {
                    return db.Products.OrderBy(p => p.Nom).ToList();
                }
                catch (Exception ex)
                {
                    // Log the exception
                    Console.WriteLine($"Error getting products for PDF: {ex.Message}");
                    return new List<Product>();
                }
            }
        }

        // Convert Product to ProductDisplay
        private ProductDisplay ConvertToProductDisplay(Product product)
        {
            return new ProductDisplay
            {
                Id = product.Id.ToString(),
                Nom = product.Nom,
                Description = product.Description,
                Categorie = product.Categorie,
                Taille = product.Taille,
                PrixVente = product.PrixVente.ToString("C"), // Format as currency
                Quantite = product.Quantite.ToString(),
                PrixAchat = product.PrixAchat.ToString("C"), // Format as currency
                Code = product.Code,
                DateExpiration = product.DateExpiration?.ToString("d"), // Format as short date
                UniteMesure = product.UniteMesure,

              
            };
        }

        public Product ConvertToProduct(ProductDisplay productDisplay)
        {
            return new Product
            {
                Id = Guid.Parse(productDisplay.Id),
                Nom = productDisplay.Nom,
                Description = productDisplay.Description,
                Categorie = productDisplay.Categorie,
                Taille = productDisplay.Taille,
                PrixVente = decimal.Parse(productDisplay.PrixVente, NumberStyles.Currency),
                Quantite = int.Parse(productDisplay.Quantite),
                PrixAchat = decimal.Parse(productDisplay.PrixAchat, NumberStyles.Currency),
                Code = productDisplay.Code,
                DateExpiration = string.IsNullOrEmpty(productDisplay.DateExpiration) ? (DateTime?)null : DateTime.Parse(productDisplay.DateExpiration),
                UniteMesure = productDisplay.UniteMesure,

                
            };
        }


    }
}

using System.Collections.ObjectModel;
using System.Windows.Input;

namespace VenteApp
{
    public class ClientViewModel : BindableObject
    {
        private const int PageSize = 5;  // Number of clients per page
        private int _currentPage = 1;     // Current page number
        private int _totalPages;          // Total number of pages

        public ObservableCollection<Client> Clients { get; set; }
        public ICommand DeleteCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }

        private readonly Func<Client, Task<bool>> _confirmDelete;

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

        public ClientViewModel(Func<Client, Task<bool>> confirmDelete)
        {
            Clients = new ObservableCollection<Client>();
            _confirmDelete = confirmDelete;

            DeleteCommand = new Command<Client>(OnDeleteClient);
            SearchCommand = new Command<string>(OnSearchClients);
            NextPageCommand = new Command(OnNextPage);
            PreviousPageCommand = new Command(OnPreviousPage);

            LoadClients(); // Load clients for the first page
        }

        // Load total number of clients and first page from the database
        private void LoadClients()
        {
            using (var db = new AppDbContext())
            {
                // Get the total number of clients
                int totalClients = db.Clients.Count();

                // Calculate total pages
                TotalPages = (int)Math.Ceiling(totalClients / (double)PageSize);

                // Load the first page
                int skip = (CurrentPage - 1) * PageSize;

                var pagedClients = db.Clients
                                     .OrderBy(c => c.Nom)
                                     .Skip(skip)
                                     .Take(PageSize)
                                     .ToList();

                Clients.Clear();
                foreach (var client in pagedClients)
                {
                    Clients.Add(client);
                }
            }
        }

        // Load clients by page, directly from the database
        private void LoadPage(int pageNumber)
        {
            using (var db = new AppDbContext())
            {
                int skip = (pageNumber - 1) * PageSize;

                var pagedClients = db.Clients
                                     .OrderBy(c => c.Nom)
                                     .Skip(skip)
                                     .Take(PageSize)
                                     .ToList();

                Clients.Clear();
                foreach (var client in pagedClients)
                {
                    Clients.Add(client);
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

        // Handle deleting a client from the database and UI
        private async void OnDeleteClient(Client client)
        {
            if (client == null)
                return;

            bool confirm = await _confirmDelete(client);
            if (!confirm)
                return;

            using (var db = new AppDbContext())
            {
                var clientToDelete = db.Clients.Find(client.Id);
                if (clientToDelete != null)
                {
                    // Find and delete all associated sale transactions
                    var saleTransactionsToDelete = db.SaleTransactions
                                                     .Where(st => st.ClientId == client.Id)
                                                     .ToList();
                    foreach (var saleTransaction in saleTransactionsToDelete)
                    {
                        db.SaleTransactions.Remove(saleTransaction);
                    }

                    db.Clients.Remove(clientToDelete);
                    db.SaveChanges();
                }

                int totalClients = db.Clients.Count();
                TotalPages = (int)Math.Ceiling(totalClients / (double)PageSize);

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
            }

            // Reload current page after deletion
            LoadPage(CurrentPage);
        }

        // Handle searching clients directly from the database
        private void OnSearchClients(string query)
        {
            using (var db = new AppDbContext())
            {
                var filteredClients = db.Clients
                                        .Where(c => c.Nom.ToLower().Contains(query.ToLower()) ||
                                                    c.Prenom.ToLower().Contains(query.ToLower()) ||
                                                    c.Email.ToLower().Contains(query.ToLower()))
                                        .OrderBy(c => c.Nom)
                                        .Skip((CurrentPage - 1) * PageSize)
                                        .Take(PageSize)
                                        .ToList();

                Clients.Clear();
                foreach (var client in filteredClients)
                {
                    Clients.Add(client);
                }

                int totalFilteredClients = db.Clients
                                             .Count(c => c.Nom.ToLower().Contains(query.ToLower()) ||
                                                         c.Prenom.ToLower().Contains(query.ToLower()) ||
                                                         c.Email.ToLower().Contains(query.ToLower()));
                TotalPages = (int)Math.Ceiling(totalFilteredClients / (double)PageSize);
            }
        }

        public List<Client> GetAllClientsForPdf()
        {
            using (var db = new AppDbContext())
            {
                return db.Clients.OrderBy(c => c.Nom).ToList();
            }
        }
    }
}

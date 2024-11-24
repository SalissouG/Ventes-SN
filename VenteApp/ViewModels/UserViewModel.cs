using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace VenteApp
{
    public class UserViewModel : BindableObject
    {
        private const int PageSize = 1; // Number of users per page
        private int _currentPage = 1;
        private int _totalPages;

        public ObservableCollection<User> Users { get; set; }
        public ICommand DeleteCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }

        private readonly Func<User, Task<bool>> _confirmDelete;

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

        public UserViewModel(Func<User, Task<bool>> confirmDelete)
        {
            Users = new ObservableCollection<User>();
            _confirmDelete = confirmDelete;

            DeleteCommand = new Command<User>(OnDeleteUser);
            SearchCommand = new Command<string>(OnSearchUsers);
            NextPageCommand = new Command(OnNextPage);
            PreviousPageCommand = new Command(OnPreviousPage);

            LoadUsers(); // Initial load
        }

        private void LoadUsers()
        {
            using (var db = new AppDbContext())
            {
                try
                {
                    int totalUsers = db.Users.Count();

                    // Guard against zero users
                    TotalPages = (PageSize > 0) ? (int)Math.Ceiling(totalUsers / (double)PageSize) : 1;
                    CurrentPage = Math.Min(CurrentPage, TotalPages);

                    var pagedUsers = db.Users
                                       .OrderBy(u => u.Nom ?? string.Empty)
                                       .Skip((CurrentPage - 1) * PageSize)
                                       .Take(PageSize)
                                       .ToList();

                    Users.Clear();
                    foreach (var user in pagedUsers)
                    {
                        Users.Add(user);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading users: {ex.Message}");
                }
            }
        }

        private void LoadPage(int pageNumber)
        {
            using (var db = new AppDbContext())
            {
                try
                {
                    var pagedUsers = db.Users
                                       .OrderBy(u => u.Nom ?? string.Empty)
                                       .Skip((pageNumber - 1) * PageSize)
                                       .Take(PageSize)
                                       .ToList();

                    Users.Clear();
                    foreach (var user in pagedUsers)
                    {
                        Users.Add(user);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading page {pageNumber}: {ex.Message}");
                }
            }
        }

        private void OnNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadPage(CurrentPage);
            }
        }

        private void OnPreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadPage(CurrentPage);
            }
        }

        private async void OnDeleteUser(User user)
        {
            if (user == null) return;

            bool confirm = await _confirmDelete(user);
            if (!confirm) return;

            using (var db = new AppDbContext())
            {
                try
                {
                    var userToDelete = db.Users.Find(user.Id);
                    if (userToDelete != null)
                    {
                        db.Users.Remove(userToDelete);
                        db.SaveChanges();
                    }

                    int totalUsers = db.Users.Count();
                    TotalPages = (PageSize > 0) ? (int)Math.Ceiling(totalUsers / (double)PageSize) : 1;
                    CurrentPage = Math.Min(CurrentPage, TotalPages);

                    var pagedUsers = db.Users
                                       .OrderBy(u => u.Nom ?? string.Empty)
                                       .Skip((CurrentPage - 1) * PageSize)
                                       .Take(PageSize)
                                       .ToList();

                    Users.Clear();
                    foreach (var pagedUser in pagedUsers)
                    {
                        Users.Add(pagedUser);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting user: {ex.Message}");
                }
            }
        }

        private void OnSearchUsers(string query)
        {
            using (var db = new AppDbContext())
            {
                try
                {
                    var filteredUsers = db.Users
                                          .Where(u => (u.Nom ?? "").ToLower().Contains(query.ToLower()) ||
                                                      (u.Prenom ?? "").ToLower().Contains(query.ToLower()) ||
                                                      (u.Email ?? "").ToLower().Contains(query.ToLower()))
                                          .OrderBy(u => u.Nom ?? string.Empty)
                                          .Skip((CurrentPage - 1) * PageSize)
                                          .Take(PageSize)
                                          .ToList();

                    Users.Clear();
                    foreach (var user in filteredUsers)
                    {
                        Users.Add(user);
                    }

                    TotalPages = (int)Math.Ceiling(db.Users.Count(u => (u.Nom ?? "").ToLower().Contains(query.ToLower()) ||
                                                                       (u.Prenom ?? "").ToLower().Contains(query.ToLower()) ||
                                                                       (u.Email ?? "").ToLower().Contains(query.ToLower())) / (double)PageSize);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error searching users: {ex.Message}");
                }
            }
        }
    }
}

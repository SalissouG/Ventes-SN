using Newtonsoft.Json;

namespace VenteApp
{
    public class UserService
    {
        private static UserService _instance;
        public static UserService Instance => _instance ??= new UserService();

        public User ConnectedUser { get; private set; }

        // Private constructor to prevent instantiation from outside
        private UserService() { }

        // Method to set the connected user
        public void SetConnectedUser(User user)
        {
            ConnectedUser = user ?? throw new ArgumentNullException(nameof(user), "User cannot be null");

            SaveUser();
        }

        // Method to check if the connected user is an admin
        public bool IsAdmin()
        {
            var userJson = Preferences.Get("ConnectedUser", string.Empty);
            if (!string.IsNullOrEmpty(userJson))
            {
                ConnectedUser = JsonConvert.DeserializeObject<User>(userJson);
            }

            return ConnectedUser?.Role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public void SaveUser()
        {
            if (ConnectedUser != null)
            {
                var userJson = JsonConvert.SerializeObject(ConnectedUser);
                Preferences.Set("ConnectedUser", userJson);
            }
        }

        public void LoadUser()
        {
            var userJson = Preferences.Get("ConnectedUser", string.Empty);
            if (!string.IsNullOrEmpty(userJson))
            {
                ConnectedUser = JsonConvert.DeserializeObject<User>(userJson);
            }
        }

        public bool IsConnectedUser()
        {
            var userJson = Preferences.Get("ConnectedUser", string.Empty);

            return !string.IsNullOrEmpty(userJson);
        }

        // Method to clear the connected user (for logout)
        public void ClearConnectedUser()
        {
            Preferences.Remove("ConnectedUser");
            ConnectedUser = null;
        }

        public string GetConnectedUserName()
        {
            if (ConnectedUser != null)
            {
                return $"{ConnectedUser.Prenom} {ConnectedUser.Nom}";
            }
            return "No user connected";
        }
    }
}

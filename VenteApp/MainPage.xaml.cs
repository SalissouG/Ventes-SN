namespace VenteApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string login = LoginEntry.Text;
            string password = PasswordEntry.Text;

            if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(password))
            {
                // Perform login validation
                bool isValid = await ValidateLoginAsync(login, password);

                if (isValid)
                {
                    // If login is successful, navigate to the MenuPage

                    await Navigation.PushAsync(new MenuPage());
                }
                else
                {
                    // Show error if login fails
                    await DisplayAlert("Error", "Invalid credentials", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Please enter valid credentials", "OK");
            }
        }

        private async Task<bool> ValidateLoginAsync(string login, string password)
        {
            bool result = false;
            using (var db = new AppDbContext())
            {
                // Find user by phone number or email
                var user = db.Users.FirstOrDefault(u => u.Login == login);
                if (user == null)
                {
                  return false; // User not found
                }

                // Decrypt the stored password
                string decryptedPassword = EncryptionService.Instance.Decrypt(user.Password);

                // Check if the entered password matches the decrypted password
                result = password == decryptedPassword;

                if (result)
                {
                    UserService.Instance.SetConnectedUser(user);

                }

                return result;

            }
        }

        private void OnForgotPasswordClicked(object sender, EventArgs e)
        {
            // Navigate to forgot password page
            DisplayAlert("Forgot Password", "Redirecting to forgot password page", "OK");
        }
    }
}

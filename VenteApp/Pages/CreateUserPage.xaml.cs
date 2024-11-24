namespace VenteApp
{
    public partial class CreateUserPage : ContentPage
    {
        private User _userToEdit;  // User being edited

        public CreateUserPage(User user = null)
        {
            InitializeComponent();

            this.Title = user == null ? "Créer un utilisateur" : "Modifier l'utilisateur";

            // Load roles in the RolePicker
            RolePicker.ItemsSource = new List<string> { "Admin", "Normal" };

            if (user != null)
            {
                _userToEdit = user; // If editing, load user data
                LoadUserDetails(_userToEdit);
            }
        }

        // Load user details into the form for editing
        private void LoadUserDetails(User user)
        {
            NomEntry.Text = user.Nom;
            PrenomEntry.Text = user.Prenom;
            NumeroEntry.Text = user.Numero;
            AdresseEntry.Text = user.Adresse;
            EmailEntry.Text = user.Email;
            LoginEntry.Text = user.Login;
            PasswordEntry.Text = EncryptionService.Instance.Decrypt(user.Password); // Be careful with handling passwords!
            RolePicker.SelectedItem = user.Role;
        }

        // Real-time validation for Nom field
        private void OnNomTextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateNom();
        }

        // Real-time validation for Prenom field
        private void OnPrenomTextChanged(object sender, TextChangedEventArgs e)
        {
            ValidatePrenom();
        }

        // Real-time validation for Numero field
        private void OnNumeroTextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateNumero();
        }

        // Real-time validation for Email field
        private void OnEmailTextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateEmail();
        }

        // Real-time validation for Login field
        private void OnLoginTextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateLogin();
        }

        // Real-time validation for Password field
        private void OnPasswordTextChanged(object sender, TextChangedEventArgs e)
        {
            ValidatePassword();
        }

        // Validate all fields before saving
        private bool ValidateInputs()
        {
            bool isValid = true;

            isValid = ValidateNom() && isValid;
            isValid = ValidatePrenom() && isValid;
            isValid = ValidateNumero() && isValid;
            isValid = ValidateEmail() && isValid;
            isValid = ValidateLogin() && isValid;
            isValid = ValidatePassword() && isValid;

            return isValid;
        }

        // Validate Nom
        private bool ValidateNom()
        {
            if (string.IsNullOrWhiteSpace(NomEntry.Text))
            {
                NomError.Text = "Le nom est obligatoire.";
                NomError.IsVisible = true;
                return false;
            }
            else
            {
                NomError.IsVisible = false;
                return true;
            }
        }

        // Validate Prenom
        private bool ValidatePrenom()
        {
            if (string.IsNullOrWhiteSpace(PrenomEntry.Text))
            {
                PrenomError.Text = "Le prénom est obligatoire.";
                PrenomError.IsVisible = true;
                return false;
            }
            else
            {
                PrenomError.IsVisible = false;
                return true;
            }
        }

        // Validate Numero de téléphone
        private bool ValidateNumero()
        {
            if (string.IsNullOrWhiteSpace(NumeroEntry.Text))
            {
                NumeroError.Text = "Le numéro de téléphone est obligatoire.";
                NumeroError.IsVisible = true;
                return false;
            }
            else if (!NumeroEntry.Text.All(char.IsDigit))
            {
                NumeroError.Text = "Le numéro de téléphone doit contenir uniquement des chiffres.";
                NumeroError.IsVisible = true;
                return false;
            }
            else if (NumeroEntry.Text.Length < 8)
            {
                NumeroError.Text = "Le numéro de téléphone doit comporter au moins 8 chiffres.";
                NumeroError.IsVisible = true;
                return false;
            }
            else
            {
                NumeroError.IsVisible = false;
                return true;
            }
        }

        // Validate Email
        private bool ValidateEmail()
        {
            if (string.IsNullOrWhiteSpace(EmailEntry.Text))
            {
                EmailError.Text = "L'email est obligatoire.";
                EmailError.IsVisible = true;
                return false;
            }
            else if (!IsValidEmail(EmailEntry.Text))
            {
                EmailError.Text = "L'email n'est pas valide.";
                EmailError.IsVisible = true;
                return false;
            }
            else
            {
                EmailError.IsVisible = false;
                return true;
            }
        }

        // Helper method to validate email format
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // Validate Login
        private bool ValidateLogin()
        {
            if (string.IsNullOrWhiteSpace(LoginEntry.Text))
            {
                LoginError.Text = "Le login est obligatoire.";
                LoginError.IsVisible = true;
                return false;
            }
            else
            {
                LoginError.IsVisible = false;
                return true;
            }
        }

        // Validate Password
        private bool ValidatePassword()
        {
            if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                PasswordError.Text = "Le mot de passe est obligatoire.";
                PasswordError.IsVisible = true;
                return false;
            }
            else if (PasswordEntry.Text.Length < 6)
            {
                PasswordError.Text = "Le mot de passe doit comporter au moins 6 caractères.";
                PasswordError.IsVisible = true;
                return false;
            }
            else
            {
                PasswordError.IsVisible = false;
                return true;
            }
        }

        // Save the user (create or update)
        private async void OnSaveUserClicked(object sender, EventArgs e)
        {
            if (!ValidateInputs())
                return; // Stop execution if inputs are invalid.

            var selectedRole = RolePicker.SelectedItem as string;

            try
            {
                await SaveUserAsync(selectedRole);
                await DisplayAlert("Succès", "Utilisateur enregistré avec succès.", "OK");
                await Navigation.PopAsync(); // Navigate back to the user list page.
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., to a file or telemetry service).
                Console.WriteLine($"Error saving user: {ex.Message}");
                await DisplayAlert("Erreur", $"Une erreur s'est produite : {ex.Message}", "OK");
            }
        }

        private async Task SaveUserAsync(string selectedRole)
        {
            using (var db = new AppDbContext())
            {
                if (_userToEdit == null)
                {
                    // Create a new user.
                    var newUser = new User
                    {
                        Nom = NomEntry.Text,
                        Prenom = PrenomEntry.Text,
                        Numero = NumeroEntry.Text,
                        Adresse = AdresseEntry.Text,
                        Email = EmailEntry.Text,
                        Login = LoginEntry.Text,
                        Password = EncryptionService.Instance.Encrypt(PasswordEntry.Text),
                        Role = selectedRole
                    };

                    db.Users.Add(newUser);
                }
                else
                {
                    // Update an existing user.
                    var user = db.Users.Find(_userToEdit.Id);

                    if (user == null)
                        throw new Exception("User not found.");

                    user.Nom = NomEntry.Text;
                    user.Prenom = PrenomEntry.Text;
                    user.Numero = NumeroEntry.Text;
                    user.Adresse = AdresseEntry.Text;
                    user.Email = EmailEntry.Text;
                    user.Login = LoginEntry.Text;
                    user.Password = EncryptionService.Instance.Encrypt(PasswordEntry.Text);
                    user.Role = selectedRole;

                    db.Users.Update(user);
                }

                await db.SaveChangesAsync(); // Commit changes to the database.
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync(); // Return to the previous page
        }
    }
}

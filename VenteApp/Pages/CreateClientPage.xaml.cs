namespace VenteApp
{
    public partial class CreateClientPage : ContentPage
    {
        private Client _clientToEdit;  // Client being edited

        public CreateClientPage(Client client = null)
        {
            InitializeComponent();

            this.Title = client == null ? "Créer un client" : "Modifier le client";

            if (client != null)
            {
                _clientToEdit = client; // If editing, load client data
                LoadClientDetails(_clientToEdit);
            }
        }

        // Load client details into the form for editing
        private void LoadClientDetails(Client client)
        {
            NomEntry.Text = client.Nom;
            PrenomEntry.Text = client.Prenom;
            NumeroEntry.Text = client.Numero;
            AdresseEntry.Text = client.Adresse;
            EmailEntry.Text = client.Email;
            NumeroClientEntry.Text = client.NumeroClient;
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

        // Validate all fields before saving
        private bool ValidateInputs()
        {
            bool isValid = true;

            isValid = ValidateNom() && isValid;
            isValid = ValidatePrenom() && isValid;
            isValid = ValidateNumero() && isValid;
            isValid = ValidateEmail() && isValid;

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

        // Save the client (create or update)
        private async void OnSaveClientClicked(object sender, EventArgs e)
        {
            if (!ValidateInputs())
                return; // Stop execution if inputs are invalid.

            try
            {
                await SaveClientAsync();
                await DisplayAlert("Succès", "Client enregistré avec succès.", "OK");
                await Navigation.PushAsync(new ClientsPage()); // Navigate back to the clients list.
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., to a file or telemetry service).
                Console.WriteLine($"Error saving client: {ex.Message}");
                await DisplayAlert("Erreur", $"Une erreur s'est produite : {ex.Message}", "OK");
            }
        }

        private async Task SaveClientAsync()
        {
            using (var db = new AppDbContext())
            {
                if (_clientToEdit == null)
                {
                    // Create a new client.
                    var newClient = new Client
                    {
                        Nom = NomEntry.Text,
                        Prenom = PrenomEntry.Text,
                        Numero = NumeroEntry.Text,
                        Adresse = AdresseEntry.Text,
                        Email = EmailEntry.Text,
                        NumeroClient = NumeroClientEntry.Text // New field.
                    };

                    db.Clients.Add(newClient);
                }
                else
                {
                    // Update an existing client.
                    var client = db.Clients.Find(_clientToEdit.Id);

                    if (client == null)
                        throw new Exception("Client not found.");

                    client.Nom = NomEntry.Text;
                    client.Prenom = PrenomEntry.Text;
                    client.Numero = NumeroEntry.Text;
                    client.Adresse = AdresseEntry.Text;
                    client.Email = EmailEntry.Text;
                    client.NumeroClient = NumeroClientEntry.Text;

                    db.Clients.Update(client);
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

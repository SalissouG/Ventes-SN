using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace VenteApp
{
    public partial class LicenseMenuPage : ContentPage
    {
        public LicenseMenuPage()
        {
            InitializeComponent();
            PreloadLicence();
        }

        // Précharger la licence si elle existe déjà
        private void PreloadLicence()
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, "licence.json");

            // Vérifier si le fichier de licence existe
            if (File.Exists(path))
            {
                // Lire la licence à partir du fichier JSON
                var json = File.ReadAllText(path);
                var licenceData = JsonConvert.DeserializeObject<dynamic>(json);
                var licenceKey = (string)licenceData.LicenceKey;

                // Charger la licence dans le champ d'entrée
                LicenceEntry.Text = licenceKey;

                // Valider la licence chargée automatiquement
                if (LicenseValidator.ValidateLicence(licenceKey, "MySuperSecretKey"))
                {
                    ValidationMessageLabel.Text = "Licence valide.";
                    ValidationMessageLabel.TextColor = Colors.Green;
                }
                else
                {
                    ValidationMessageLabel.Text = "Licence expirée ou invalide.";
                    ValidationMessageLabel.TextColor = Colors.Red;
                }
            }
        }


        // Event handler for the "Valider" button click
        public async void OnValidateLicence(object sender, EventArgs e)
        {
            var licenceKey = LicenceEntry.Text; // Get the inputted licence key

            if (string.IsNullOrEmpty(licenceKey))
            {
                // Display message if the licence key is empty
                ValidationMessageLabel.Text = "Veuillez entrer une clé de licence.";
                return;
            }

            // Validate the licence key
            if (LicenseValidator.ValidateLicence(licenceKey, "MySuperSecretKey"))
            {
                // If valid, save it and display success message
                SaveLicence(licenceKey);
                ValidationMessageLabel.Text = "Licence valide.";
                ValidationMessageLabel.TextColor = Colors.Green;

            }
            else
            {
                SaveLicence(licenceKey);
                // If invalid, display error message
                ValidationMessageLabel.Text = "Licence invalide ou expirée.";
                ValidationMessageLabel.TextColor = Colors.Red;
            }
        }

        // Save the valid licence in a JSON file
        private void SaveLicence(string licence)
        {
            var licenceData = new { LicenceKey = licence };
            var json = JsonConvert.SerializeObject(licenceData);

            var path = Path.Combine(FileSystem.AppDataDirectory, "licence.json");
            File.WriteAllText(path, json); // Save the licence in the app's data directory
        }

        // Sign data using HMAC SHA256 with a secret key
        private string SignData(string data, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(dataBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}

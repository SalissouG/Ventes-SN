using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.ComponentModel;

namespace VenteApp
{
    public class LicenseViewModel : INotifyPropertyChanged
    {
        private string _licenceKey;
        private string _validationMessage;

        // Property for the Licence Key input by the user
        public string LicenceKey
        {
            get => _licenceKey;
            set
            {
                _licenceKey = value;
                OnPropertyChanged(nameof(LicenceKey)); // Notify the UI when LicenceKey changes
            }
        }

        // Property for the Validation Message shown after validation
        public string ValidationMessage
        {
            get => _validationMessage;
            set
            {
                _validationMessage = value;
                OnPropertyChanged(nameof(ValidationMessage)); // Notify the UI when ValidationMessage changes
            }
        }

        // Command that triggers licence validation
        public Command ValidateLicenceCommand { get; }

        public LicenseViewModel()
        {
            ValidateLicenceCommand = new Command(async () => await ValidateLicenceAsync());
        }

        // Async method to validate the licence
        private async Task ValidateLicenceAsync()
        {
            // Check if LicenceKey is null or empty
            if (string.IsNullOrEmpty(LicenceKey))
            {
                ValidationMessage = "Veuillez entrer une clé de licence."; // Display message if key is empty
                return;
            }

            // Validate the licence key
            if (ValidateLicence(LicenceKey))
            {
                SaveLicence(LicenceKey);
                ValidationMessage = "Licence valide. (Fermer et relancer l'application)"; // Update message if valid

               
            }
            else
            {
                ValidationMessage = "Licence invalide ou expirée."; // Update message if invalid
            }
        }

        // Validate the licence using the secret key
        private bool ValidateLicence(string licence)
        {
            if (string.IsNullOrEmpty(licence)) return false; // Handle null or empty license

            try
            {
                var decodedLicence = Encoding.UTF8.GetString(Convert.FromBase64String(licence));
                var parts = decodedLicence.Split('.');
                if (parts.Length != 2) return false;

                var expirationString = parts[0];
                var signature = parts[1];

                if (signature != SignData(expirationString, "MySuperSecretKey")) return false;

                DateTime expirationDate;
                if (!DateTime.TryParse(expirationString, out expirationDate)) return false;

                return expirationDate >= DateTime.Now;
            }
            catch
            {
                return false;
            }
        }

        // Save the valid licence to a JSON file
        private void SaveLicence(string licence)
        {
            var licenceData = new { LicenceKey = licence };
            var json = JsonConvert.SerializeObject(licenceData);

            var path = Path.Combine(FileSystem.AppDataDirectory, "licence.json");
            File.WriteAllText(path, json);
        }

        // Sign data with the secret key using HMAC SHA256
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

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using Microsoft.Maui.Controls;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VenteApp
{
    public partial class CreateOwnerPage : ContentPage
    {
        private Owner _existingOwner; // Reference to store the existing owner if one exists
        private string _selectedLogoPath;

        public CreateOwnerPage()
        {
            InitializeComponent();
            this.Title = "Owner Information";

            LoadOwnerDetails(); // Load existing owner details if available
        }

        // Load the existing Owner details from the database if they exist
        private void LoadOwnerDetails()
        {
            using (var db = new AppDbContext())
            {
                _existingOwner = db.Owners.FirstOrDefault();

                if (_existingOwner != null)
                {
                    // Populate the form with existing owner details
                    AddressEntry.Text = _existingOwner.Address;
                    PhoneNumberEntry.Text = _existingOwner.PhoneNumber;
                    _selectedLogoPath = _existingOwner.LogoPath;

                    if (!string.IsNullOrEmpty(_existingOwner.LogoPath) && File.Exists(_existingOwner.LogoPath))
                    {
                        LogoImage.Source = ImageSource.FromFile(_existingOwner.LogoPath);
                        LogoImage.IsVisible = true;
                    }
                }
            }
        }

        // Event handler for selecting a logo
        private async void OnSelectLogoClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select a logo image",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    _selectedLogoPath = result.FullPath;
                    LogoImage.Source = ImageSource.FromFile(_selectedLogoPath);
                    LogoImage.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load logo: {ex.Message}", "OK");
            }
        }

        // Event handler for saving the owner details
        private async void OnSaveOwnerClicked(object sender, EventArgs e)
        {
            if (ValidateInputs())
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
                        if (_existingOwner == null)
                        {
                            // Create new Owner
                            var newOwner = new Owner
                            {
                                Id = Guid.NewGuid(),
                                Address = AddressEntry.Text,
                                PhoneNumber = PhoneNumberEntry.Text,
                                LogoPath = _selectedLogoPath
                            };
                            db.Owners.Add(newOwner);
                        }
                        else
                        {
                            // Update existing Owner
                            _existingOwner.Address = AddressEntry.Text;
                            _existingOwner.PhoneNumber = PhoneNumberEntry.Text;
                            _existingOwner.LogoPath = _selectedLogoPath;
                            db.Owners.Update(_existingOwner);
                        }

                        await db.SaveChangesAsync();
                    }

                    await DisplayAlert("Success", "Owner information saved successfully.", "OK");
                    await Navigation.PopAsync(); // Navigate back to the previous page
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"An error occurred while saving: {ex.Message}", "OK");
                }
            }
        }

        // Input validation
        private bool ValidateInputs()
        {
            bool isValid = true;

            if (string.IsNullOrWhiteSpace(AddressEntry.Text))
            {
                AddressError.Text = "Address is required.";
                AddressError.IsVisible = true;
                isValid = false;
            }
            else
            {
                AddressError.IsVisible = false;
            }

            if (string.IsNullOrWhiteSpace(PhoneNumberEntry.Text) || !PhoneNumberEntry.Text.All(char.IsDigit) || PhoneNumberEntry.Text.Length < 8)
            {
                PhoneError.Text = "Valid phone number is required.";
                PhoneError.IsVisible = true;
                isValid = false;
            }
            else
            {
                PhoneError.IsVisible = false;
            }

            return isValid;
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync(); // Navigate back to the previous page
        }
    }
}

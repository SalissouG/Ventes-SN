using Microsoft.Maui.Controls;
using System;
using System.IO;
using System.Linq;

namespace VenteApp
{
    public partial class OwnerPage : ContentPage
    {
        private Owner _existingOwner;

        public OwnerPage()
        {
            InitializeComponent();
            LoadOwnerDetails();
        }

        // Load existing owner details from the database
        private void LoadOwnerDetails()
        {
            using (var db = new AppDbContext())
            {
                _existingOwner = db.Owners.FirstOrDefault();

                if (_existingOwner == null)
                {
                    // Show message and "Add Owner" button if no owner exists
                    NoOwnerLabel.IsVisible = true;
                    AddOwnerButton.IsVisible = true;
                    EditOwnerButton.IsVisible = false;
                }
                else
                {
                    // Populate owner details and show "Edit Owner" button
                    NoOwnerLabel.IsVisible = false;
                    OwnerAddressLabel.Text = $"Address: {_existingOwner.Address}";
                    OwnerPhoneLabel.Text = $"Phone Number: {_existingOwner.PhoneNumber}";
                    OwnerAddressLabel.IsVisible = true;
                    OwnerPhoneLabel.IsVisible = true;

                    if (!string.IsNullOrEmpty(_existingOwner.LogoPath) && File.Exists(_existingOwner.LogoPath))
                    {
                        OwnerLogoImage.Source = ImageSource.FromFile(_existingOwner.LogoPath);
                        OwnerLogoImage.IsVisible = true;
                    }

                    AddOwnerButton.IsVisible = false;
                    EditOwnerButton.IsVisible = true;
                }
            }
        }

        // Navigate to CreateOwnerPage for adding a new owner
        private async void OnAddOwnerClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreateOwnerPage());
        }

        // Navigate to CreateOwnerPage for editing the existing owner
        private async void OnEditOwnerClicked(object sender, EventArgs e)
        {
            if (_existingOwner != null)
            {
                await Navigation.PushAsync(new CreateOwnerPage());
            }
        }
    }
}

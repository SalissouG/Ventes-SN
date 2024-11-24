namespace VenteApp
{
    public partial class UsersPage : ContentPage
    {
        public UsersPage()
        {
            InitializeComponent();
            this.Title = "Utilisateurs";

            try
            {
                this.BindingContext = new UserViewModel(ConfirmDeleteUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, Inner Exception: {ex.InnerException?.Message}");
            }
        }

        private async Task<bool> ConfirmDeleteUser(User user)
        {
            return await DisplayAlert("Confirmation", $"Voulez-vous vraiment supprimer {user.Nom} ?", "Oui", "Non");
        }

        private async void OnAddUserClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreateUserPage());
        }

        private async void OnEditUserClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var user = (User)((ViewCell)button.Parent.Parent).BindingContext;
            await Navigation.PushAsync(new CreateUserPage(user));
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (BindingContext is UserViewModel viewModel)
            {
                viewModel.SearchCommand.Execute(e.NewTextValue);
            }
        }
    }
}

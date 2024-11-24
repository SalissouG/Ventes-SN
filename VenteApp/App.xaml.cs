using System.Globalization;

namespace VenteApp;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

        Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr"); // French
        //Thread.CurrentThread.CurrentUICulture = new CultureInfo("es"); // Spanish
        //Thread.CurrentThread.CurrentUICulture = new CultureInfo("pt"); 
        //Thread.CurrentThread.CurrentUICulture = new CultureInfo("ha");

        using (var db = new AppDbContext())
        {
            db.Database.EnsureCreated();  // Create database if it doesn't exist
            db.InitializeDatabase();
        }

        // Vérifier si la licence est valide au démarrage
        if (LicenseValidator.IsLicenceValid("MySuperSecretKey"))
        {
            if (UserService.Instance.IsConnectedUser())
            {
                MainPage = new NavigationPage(new MenuPage());
            }
            else
            {
                MainPage = new NavigationPage(new MainPage());
            }
        }
        else
        {
            MainPage = new NavigationPage(new LicensePage()); // Rediriger vers la page de licence
        }

        NavigationPage.SetHasNavigationBar(MainPage, false);

        CartService.Instance.LoadCart();

    }

    protected override void OnStart()
    {
        base.OnStart();
        // Any additional startup logic can go here
    }

    protected override void OnSleep()
    {
        // Save the cart when the application goes to sleep
        CartService.Instance.SaveCart();
        base.OnSleep();
    }

    protected override void OnResume()
    {
        // Optionally, reload the cart when the application resumes
        CartService.Instance.LoadCart();
        base.OnResume();
    }

}

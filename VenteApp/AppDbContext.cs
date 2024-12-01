using Microsoft.EntityFrameworkCore;

namespace VenteApp
{
    public class AppDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<SaleTransaction> SaleTransactions { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<User> Users { get; set; }

        public DbSet<Owner> Owners { get; set; }
        public DbSet<Basket> Baskets { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Get the path to the local application folder (cross-platform)
            var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // Combine the folder path with the database file name
            var databasePath = Path.Combine(folderPath, "VenteApp18.db");

            // Log the database path (for debugging purposes)f
            Console.WriteLine($"Database path: {databasePath}");

            // Use SQLite with the full database path
            optionsBuilder.UseSqlite($"Filename={databasePath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // SaleTransaction to Product
            modelBuilder.Entity<SaleTransaction>()
                .HasOne(s => s.Product)
                .WithMany(p => p.Sales)
                .HasForeignKey(s => s.ProductId);

            // SaleTransaction to Client
            modelBuilder.Entity<SaleTransaction>()
                .HasOne(s => s.Client)
                .WithMany(c => c.Transactions)
                .HasForeignKey(s => s.ClientId);
        }

        public void InitializeDatabase()
        {
            //this.Database.Migrate(); // Apply any pending migrations

            if (!Users.Any()) // Check if there are any users in the database
            {
                var defaultAdmin = new User
                {
                    Id = Guid.NewGuid(),
                    Nom = "Admin",
                    Prenom = "Administrator",
                    Numero = "0000000000",
                    Adresse = "N/A",
                    Email = "admin@venteapp.com",
                    Login = "admin",
                    Password = EncryptionService.Instance.Encrypt("Admin123"), // Encrypt the default password
                    Role = "Admin",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                Users.Add(defaultAdmin);
                SaveChanges(); // Save the new user to the database
                Console.WriteLine("Default admin user added to the database.");
            }
        }
    }
}

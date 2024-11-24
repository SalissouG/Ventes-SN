namespace VenteApp
{
    public class Client
    {
        public Guid Id { get; set; } // Primary key
        public string Nom { get; set; } // Required: Last name of the client
        public string Prenom { get; set; } // Required: First name of the client
        public string Numero { get; set; } // Required: Phone number
        public string Adresse { get; set; } // Optional: Address of the client
        public string Email { get; set; } // Required: Email address

        public string NumeroClient { get; set; }

        public ICollection<SaleTransaction> Transactions { get; set; }
    }
}

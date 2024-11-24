namespace VenteApp
{
    public class Owner
    {
        public Guid Id { get; set; } // Primary key
        public string Address { get; set; } // Required: Owner's address
        public string PhoneNumber { get; set; } // Required: Owner's phone number
        public string LogoPath { get; set; } // Path to the logo image
    }
}


namespace VenteApp
{
    public class SaleTransaction
    {
        public Guid Id { get; set; } // Primary key
        public int Quantite { get; set; } // Quantity sold
        public DateTime DateDeVente { get; set; } // Date of sale

        public Guid ProductId { get; set; } // Foreign key to Product
        public Product Product { get; set; } // Navigation property to Product

        public Guid? ClientId { get; set; } // Foreign key to Client
        public Client Client { get; set; } // Navigation property for the client associated with this transaction

        public Guid OrderId { get; set; } 

    }
}

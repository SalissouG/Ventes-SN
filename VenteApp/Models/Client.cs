using System.ComponentModel.DataAnnotations;

namespace VenteApp
{
    public class Client
    {
        public Guid Id { get; set; } // Primary key

        [Required]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères.")]
        public string Nom { get; set; } // Required: Last name of the client

        [Required]
        [StringLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères.")]
        public string Prenom { get; set; } // Required: First name of the client

        [Required]
        [StringLength(15, ErrorMessage = "Le numéro de téléphone ne peut pas dépasser 15 caractères.")]
        public string Numero { get; set; } // Required: Phone number

        [StringLength(200, ErrorMessage = "L'adresse ne peut pas dépasser 200 caractères.")]
        public string Adresse { get; set; } // Optional: Address of the client

        [Required]
        [StringLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères.")]
        [EmailAddress(ErrorMessage = "L'email n'est pas valide.")]
        public string Email { get; set; } // Required: Email address

        [Required]
        [StringLength(50, ErrorMessage = "Le numéro client ne peut pas dépasser 50 caractères.")]
        public string NumeroClient { get; set; }

        public ICollection<SaleTransaction> Transactions { get; set; }
    }
}

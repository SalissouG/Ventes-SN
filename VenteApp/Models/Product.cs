using System.ComponentModel.DataAnnotations;

namespace VenteApp
{
    public class Product
    {
        public Guid Id { get; set; } // Primary key

        [Required]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères.")]
        public string Nom { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères.")]
        public string Description { get; set; }

        [StringLength(50, ErrorMessage = "La catégorie ne peut pas dépasser 50 caractères.")]
        public string Categorie { get; set; }

        [StringLength(50, ErrorMessage = "La taille ne peut pas dépasser 50 caractères.")]
        public string Taille { get; set; }

        public decimal PrixVente { get; set; } // Unit price
        public int Quantite { get; set; }
        public decimal PrixAchat { get; set; }
        public string Code { get; set; }
        public DateTime? DateExpiration { get; set; } // New property for expiration date

        [StringLength(50, ErrorMessage = "L'unité de mesure ne peut pas dépasser 50 caractères.")]
        public string UniteMesure { get; set; }

        // Optionally, a collection of sales linked to this product
        public ICollection<SaleTransaction> Sales { get; set; }
    }
}

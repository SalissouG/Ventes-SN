
namespace VenteApp
{
   public  class ProductDisplay
    {
        public string Id { get; set; }
        public string Nom { get; set; }
        public string Description { get; set; }
        public string Categorie { get; set; }
        public string Taille { get; set; }
        public string PrixVente { get; set; }
        public string Quantite { get; set; }
        public string PrixAchat { get; set; }
        public string Code { get; set; }
        public string DateExpiration { get; set; }
        public string UniteMesure { get; set; }

        // Provider properties
        public string ProviderNom { get; set; }
        public string ProviderPrenom { get; set; }
        public string ProviderNumero { get; set; }
        public string ProviderAdresse { get; set; }
        public string ProviderEmail { get; set; }
    }
}

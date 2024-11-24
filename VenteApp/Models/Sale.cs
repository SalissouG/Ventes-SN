using System;
using System.ComponentModel;

namespace VenteApp
{
    public class Sale : INotifyPropertyChanged
    {
        public Guid Id { get; set; }

        public Guid ProductId{ get; set; }
        public string Nom { get; set; }
        public string Description { get; set; }
        public decimal Prix { get; set; }
        private int quantite;
        public int Quantite
        {
            get => quantite;
            set
            {
                quantite = value;
                OnPropertyChanged(nameof(Quantite));
                OnPropertyChanged(nameof(TotalPrice)); // Recalculate total price when quantity changes
            }
        }
        public string Categorie { get; set; }
        public string Taille { get; set; }
        public DateTime DateLimite { get; set; }

        public DateTime DateDeVente { get; set; }

        // Total price calculated based on quantity and price
        public decimal TotalPrice => Quantite * Prix;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

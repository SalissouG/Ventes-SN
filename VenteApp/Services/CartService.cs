using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace VenteApp
{
    public class CartService
    {
        private static CartService _instance;
        public static CartService Instance => _instance ??= new CartService();

        public ObservableCollection<Sale> CartItems { get; }

        private CartService()
        {
            CartItems = new ObservableCollection<Sale>();
        }

        public void AddToCart(Sale sale)
        {
            var existingItem = CartItems.FirstOrDefault(s => s.Nom == sale.Nom);
            if (existingItem != null)
            {
                // If the item already exists in the cart, increase its quantity
                existingItem.Quantite = sale.Quantite;
            }
            else
            {
                // Add a new item to the cart
                CartItems.Add(new Sale
                {   
                    Id = sale.Id,
                    ProductId = sale.ProductId,
                    DateDeVente = sale.DateDeVente,
                    Nom = sale.Nom,
                    Description = sale.Description,
                    Prix = sale.Prix,
                    Quantite = sale.Quantite,
                    Categorie = sale.Categorie,
                    Taille = sale.Taille,
                    DateLimite = sale.DateLimite
                });
            }
        }

        public void RemoveFromCart(Sale sale)
        {
            CartItems.Remove(sale);
            SaveCart();
        }

        public decimal GetTotalPrice()
        {
            return CartItems.Sum(item => item.TotalPrice);
        }

        public void SaveCart()
        {
            var cartJson = JsonConvert.SerializeObject(CartItems);
            Preferences.Set("CartItems", cartJson);
        }

        public void LoadCart()
        {
            var cartJson = Preferences.Get("CartItems", string.Empty);
            if (!string.IsNullOrEmpty(cartJson))
            {
                var items = JsonConvert.DeserializeObject<ObservableCollection<Sale>>(cartJson);
                CartItems.Clear();
                foreach (var item in items)
                {
                    CartItems.Add(item);
                }
            }
        }
    }
}

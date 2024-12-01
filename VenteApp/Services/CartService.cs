using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace VenteApp
{
    public class CartService
    {
        private static readonly object _instanceLock = new object();
        private static CartService _instance;

        public static CartService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new CartService();
                        }
                    }
                }
                return _instance;
            }
        }

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

        private readonly object _lock = new object();

        public void RemoveFromCart(Sale sale)
        {
            lock (_lock)
            {
                if (CartItems.Count == 0) return;

                var itemToRemove = CartItems.FirstOrDefault(s => s.Id == sale.Id);
                if (itemToRemove != null)
                {
                    CartItems.Remove(itemToRemove);
                    SaveCart();
                }
            }
        }

        public decimal GetTotalPrice()
        {
            if (CartItems.Count == 0) return 0;

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

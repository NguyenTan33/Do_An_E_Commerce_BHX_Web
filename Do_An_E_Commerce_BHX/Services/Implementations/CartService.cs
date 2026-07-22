using Do_An_E_Commerce_BHX.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Do_An_E_Commerce_BHX.Services.Implementations
{
    public class CartService
    {
        public ApplicationDbContext appDBContext { get; set; }
        public CartService(ApplicationDbContext appDBContext)
        {
            this.appDBContext = appDBContext;
        }
        public void AddItemToCart(int id)
        {
            var product = appDBContext.Product.FirstOrDefault(p => p.Id == id);
            //var product = appDBContext.Products.FirstOrDefault(p => p.Id == id);
            //add to user table
        }
        public void RemoveItemFromCart(int id)
        {
            var product = appDBContext.Product.FirstOrDefault(p => p.Id == id);
            //delete  
        }
        public void ChangeQuantity(int userId, int productId, int amount)
        {
            var product = appDBContext.Product.FirstOrDefault(p => p.Id == productId);
            //add to user table
        }

    }
}

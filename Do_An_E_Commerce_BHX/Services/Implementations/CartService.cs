using Do_An_E_Commerce_BHX.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using Do_An_E_Commerce_BHX.Models.Entities;
namespace Do_An_E_Commerce_BHX.Services.Implementations
{
    public class CartService
    {
        public ApplicationDbContext appDBContext { get; set; }
        public CartService(ApplicationDbContext appDBContext)
        {
            this.appDBContext = appDBContext;
        }
        public void AddItemToCart(int id,string userId , int quantity = 1 )
        {
            var product = appDBContext.Product.FirstOrDefault(p => p.Id == id);
            var cart = appDBContext.Cart
                .Include(c => c.CartDetails) // Lôi luôn CartDetails đi kèm
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null) return;
            if (product == null) return;

            var exsistProduct = cart.CartDetails.FirstOrDefault(c => c.ProductId == id);

            if (exsistProduct != null)
            {
                exsistProduct.Quantity += quantity;
            }
            else
            {
                var newCartDetail = new CartDetail
                {
                    CartId = cart.Id,
                    ProductId = id,
                    Quantity = quantity,
                };
                cart.CartDetails.Add(newCartDetail);
            }

            appDBContext.SaveChanges();
            //add to user table
        }
        public void RemoveItemFromCart(int id,string userId)
        {
            //delete  
            var cart = appDBContext.Cart
                .Include(c => c.CartDetails) // Lôi luôn CartDetails đi kèm
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null) return;

            var existingProductInCart = cart.CartDetails.FirstOrDefault(p => p.ProductId == id);
            if(existingProductInCart != null)
            {
                cart.CartDetails.Remove(existingProductInCart);
            }
            appDBContext.SaveChanges();
        }
        public void ChangeQuantity(string userId, int productId, int amount)
        {
            if (amount < 0 || amount > 100) return;
            //add to user table
            var cart = appDBContext.Cart
                .Include(c => c.CartDetails) // Lôi luôn CartDetails đi kèm
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null) return;
            var existingProductInCart = cart.CartDetails.FirstOrDefault(p => p.ProductId == productId);
            if (existingProductInCart != null)
            {
                existingProductInCart.Quantity = amount;
            }
            appDBContext.SaveChanges();
        }


        // Lấy giỏ hàng ra cho Index
        public Cart GetCartByUserId(string userId)
        {
            return appDBContext.Cart
                .Include(c => c.CartDetails)
                .Include("CartDetails.Product") // Lôi Product ra để hiện Tên, Ảnh, Giá
                .FirstOrDefault(c => c.UserId == userId);
        }

    }
}

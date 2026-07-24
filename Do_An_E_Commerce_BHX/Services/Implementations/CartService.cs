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
            var cart = GetOrCreateCart(userId);
            if (product == null) return;

            var existProduct = cart.CartDetails.FirstOrDefault(c => c.ProductId == id);

            if (existProduct != null)
            {
                existProduct.Quantity += quantity;
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
            var cart = GetOrCreateCart(userId);

            if (cart == null) return;

            var existingProductInCart = cart.CartDetails.FirstOrDefault(p => p.ProductId == id);
            if(existingProductInCart != null)
            {
                appDBContext.CartDetail.Remove(existingProductInCart);
            }
            appDBContext.SaveChanges();
        }
        public void ChangeQuantity(string userId, int productId, int amount)
        {
            if (amount < 1 || amount > 100) return;
            //add to user table
            var cart = GetOrCreateCart(userId);

            if (cart == null) return;
            var existingProductInCart = cart.CartDetails.FirstOrDefault(p => p.ProductId == productId);
            if (existingProductInCart != null)
            {
                existingProductInCart.Quantity = amount;
            }
            appDBContext.SaveChanges();
        }

        public Cart GetOrCreateCart(string userId)
        {
            bool isRealUser = appDBContext.Users.Any(u => u.Id == userId);

            if (isRealUser)
            {

                var cart = appDBContext.Cart
                   .Include(c => c.CartDetails) // Lôi luôn CartDetails đi kèm
                   .FirstOrDefault(c => c.UserId == userId);

                // NẾU CHƯA CÓ CART THÌ TẠO MỚI LUÔN
                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = userId,
                        CartDetails = new List<CartDetail>()
                    };
                    appDBContext.Cart.Add(cart);
                    appDBContext.SaveChanges(); // Lưu để lấy Cart.Id
                }
                return cart;
            }
            else
            {
                var cart = appDBContext.Cart
                  .Include(c => c.CartDetails) // Lôi luôn CartDetails đi kèm
                  .FirstOrDefault(c => c.GuestId == userId);

                // NẾU CHƯA CÓ CART THÌ TẠO MỚI LUÔN
                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = null,    // Khách vãng lai -> Để NULL để không dính Foreign Key
                        GuestId = userId, // Lưu chuỗi "GUEST_..." vào GuestId
                        CartDetails = new List<CartDetail>()
                    };
                    appDBContext.Cart.Add(cart);
                    appDBContext.SaveChanges(); // Lưu để lấy Cart.Id
                }
                return cart;
            }


        }

        // Lấy giỏ hàng ra cho Index
        public Cart GetCartByUserId(string userId)
        {
            // 1. Lấy hoặc tạo mới Cart trước (đảm bảo không bao giờ null)
            var cart = GetOrCreateCart(userId);

            // 2. Load kèm danh sách Product cho CartDetails
            appDBContext.Entry(cart)
                .Collection(c => c.CartDetails)
                .Query()
                .Include(cd => cd.Product)
                .Load();

            return cart;
        }

    }
}

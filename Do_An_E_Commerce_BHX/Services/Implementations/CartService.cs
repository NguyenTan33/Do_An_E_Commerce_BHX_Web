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
        private static bool _schemaUpdated = false;
        private static readonly object _lock = new object();

        public ApplicationDbContext appDBContext { get; set; }
        public CartService(ApplicationDbContext appDBContext)
        {
            this.appDBContext = appDBContext;
            EnsureCartTableSchema(appDBContext);
        }

        private static void EnsureCartTableSchema(ApplicationDbContext db)
        {
            if (!_schemaUpdated)
            {
                lock (_lock)
                {
                    if (!_schemaUpdated)
                    {
                        try
                        {
                            db.Database.ExecuteSqlCommand(@"
                                BEGIN TRY
                                    ALTER TABLE dbo.Carts ALTER COLUMN UserId NVARCHAR(255) NULL;
                                END TRY BEGIN CATCH END CATCH;

                                BEGIN TRY
                                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CartDetails') AND name = 'Price')
                                    BEGIN
                                        ALTER TABLE dbo.CartDetails ADD Price FLOAT NOT NULL DEFAULT 0;
                                    END
                                END TRY BEGIN CATCH END CATCH;
                            ");
                            _schemaUpdated = true;
                        }
                        catch { }
                    }
                }
            }
        }

        public void AddItemToCart(int id, string userId, int quantity = 1)
        {
            var product = appDBContext.Product.FirstOrDefault(p => p.Id == id);
            if (product == null) throw new Exception("Không tìm thấy sản phẩm!");

            // Kiểm tra lượng tồn thực tế (nếu là sản phẩm dùng chung kho gốc ParentProduct)
            int effectiveStock = product.Quantity;
            if (product.ParentProductId.HasValue && product.ParentProductId.Value > 0)
            {
                var parent = appDBContext.Product.FirstOrDefault(p => p.Id == product.ParentProductId.Value);
                if (parent != null) effectiveStock = parent.Quantity;
            }

            int factor = product.UnitMultiplier > 0 ? product.UnitMultiplier : 1;
            int availableUnits = effectiveStock / factor;

            if (availableUnits <= 0)
            {
                throw new Exception($"Sản phẩm '{product.Name}' hiện đã hết hàng trong kho!");
            }

            if (quantity > availableUnits) quantity = availableUnits;
            if (quantity <= 0) quantity = 1;

            var cart = GetOrCreateCart(userId);
            if (cart == null) throw new Exception("Không tạo được giỏ hàng người dùng!");

            var existProduct = appDBContext.CartDetail.FirstOrDefault(c => c.CartId == cart.Id && c.ProductId == id);

            if (existProduct != null)
            {
                int targetQuantity = existProduct.Quantity + quantity;
                if (targetQuantity > availableUnits) targetQuantity = availableUnits;
                if (targetQuantity <= 0) targetQuantity = 1;

                existProduct.Quantity = targetQuantity;
                existProduct.Price = Convert.ToDouble(product.Price);
                appDBContext.Entry(existProduct).State = EntityState.Modified;
            }
            else
            {
                var newCartDetail = new CartDetail
                {
                    CartId = cart.Id,
                    ProductId = id,
                    Quantity = quantity,
                    Price = Convert.ToDouble(product.Price)
                };
                appDBContext.CartDetail.Add(newCartDetail);
            }

            appDBContext.SaveChanges();
        }
        public void RemoveItemFromCart(int id, string userId)
        {
            var cart = GetOrCreateCart(userId);
            if (cart == null) return;

            var existingProductInCart = appDBContext.CartDetail.FirstOrDefault(p => p.CartId == cart.Id && p.ProductId == id);
            if (existingProductInCart != null)
            {
                appDBContext.CartDetail.Remove(existingProductInCart);
                appDBContext.SaveChanges();
            }
        }
        public int ChangeQuantity(string userId, int productId, int amount)
        {
            bool z = false;
            if (amount < 1 || amount > 100) return 0;
            int realQuantity = appDBContext.Product.FirstOrDefault(Product => Product.Id == productId).Quantity;
            if (amount > realQuantity) z = true;

            var cart = GetOrCreateCart(userId);
            if (cart == null) return 0;

            var existingProductInCart = appDBContext.CartDetail.FirstOrDefault(p => p.CartId == cart.Id && p.ProductId == productId);
            if (existingProductInCart != null)
            {
                if (z == false)
                {
                    existingProductInCart.Quantity = amount;
                }
                else
                {
                    existingProductInCart.Quantity = realQuantity;
                }
                appDBContext.Entry(existingProductInCart).State = EntityState.Modified;
                appDBContext.SaveChanges();
                return existingProductInCart.Quantity;
            }
            return 0;
        }

        public Cart GetOrCreateCart(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;

            var cart = appDBContext.Cart
               .Include(c => c.CartDetails)
               .FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CartDetails = new List<CartDetail>()
                };
                appDBContext.Cart.Add(cart);
                appDBContext.SaveChanges();
            }
            return cart;
        }

        public void RemoveSelectedItemsFromCart(List<int> productIds, string userId)
        {
            if (productIds == null || !productIds.Any()) return;
            var cart = GetOrCreateCart(userId);
            if (cart == null) return;

            var itemsToRemove = appDBContext.CartDetail.Where(cd => cd.CartId == cart.Id && productIds.Contains(cd.ProductId)).ToList();
            if (itemsToRemove.Any())
            {
                appDBContext.CartDetail.RemoveRange(itemsToRemove);
                appDBContext.SaveChanges();
            }
        }

        public void ClearCart(string userId)
        {
            var cart = GetOrCreateCart(userId);
            if (cart == null) return;

            var itemsToRemove = appDBContext.CartDetail.Where(cd => cd.CartId == cart.Id).ToList();
            if (itemsToRemove.Any())
            {
                appDBContext.CartDetail.RemoveRange(itemsToRemove);
                appDBContext.SaveChanges();
            }
        }

        // Lấy giỏ hàng ra cho Index
        public Cart GetCartByUserId(string userId)
        {
            var cart = GetOrCreateCart(userId);
            if (cart == null) return null;

            cart.CartDetails = appDBContext.CartDetail
                .Include(cd => cd.Product)
                .Where(cd => cd.CartId == cart.Id)
                .ToList();

            return cart;
        }
    }
}

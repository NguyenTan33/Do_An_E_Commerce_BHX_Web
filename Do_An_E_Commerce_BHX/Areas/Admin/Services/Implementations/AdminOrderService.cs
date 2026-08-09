using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Areas.Admin.Services.Interfaces;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Implementations;

namespace Do_An_E_Commerce_BHX.Areas.Admin.Services.Implementations
{
    public class AdminOrderService : IAdminOrderService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly OrderService _orderService;

        public AdminOrderService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? new ApplicationDbContext();
            var calc = new Calculate();
            var cartSvc = new CartService(_dbContext);
            _orderService = new OrderService(_dbContext, calc, cartSvc);
        }

        public async Task<List<Order>> GetAdminOrdersAsync(string search, int? status)
        {
            return await _orderService.GetAdminOrdersAsync(search, status);
        }

        public async Task<Dictionary<string, int>> GetOrderCountsAsync()
        {
            return await _orderService.GetOrderCountsAsync();
        }

        public async Task<bool> ApproveOrderAsync(int id)
        {
            return await _orderService.ApproveOrderAsync(id);
        }

        public async Task<bool> CancelOrderAsync(int id)
        {
            return await _orderService.CancelOrderAsync(id);
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            return await _orderService.DeleteOrderAsync(id);
        }

        public async Task<int> BulkApproveAsync(int[] ids)
        {
            if (ids == null || !ids.Any()) return 0;

            var orders = await _dbContext.Order.Where(o => ids.Contains(o.Id) && o.OrderStatus == 0).ToListAsync();
            foreach (var o in orders)
            {
                o.OrderStatus = 1; // Đã duyệt
            }
            await _dbContext.SaveChangesAsync();
            return orders.Count;
        }

        public async Task<int> BulkCancelAsync(int[] ids)
        {
            if (ids == null || !ids.Any()) return 0;

            var orders = await _dbContext.Order.Include("OrderDetails").Where(o => ids.Contains(o.Id)).ToListAsync();
            int count = 0;
            foreach (var o in orders)
            {
                if (o.OrderStatus != 5)
                {
                    RestoreOrderStock(o);
                    o.OrderStatus = 5; // Hủy
                    count++;
                }
            }
            await _dbContext.SaveChangesAsync();
            return count;
        }

        public async Task<int> BulkDeleteAsync(int[] ids)
        {
            if (ids == null || !ids.Any()) return 0;

            var orders = await _dbContext.Order.Include("OrderDetails").Where(o => ids.Contains(o.Id)).ToListAsync();
            foreach (var o in orders)
            {
                if (o.OrderStatus != 5 && o.OrderStatus != 4)
                {
                    RestoreOrderStock(o);
                }
                _dbContext.OrderDetail.RemoveRange(o.OrderDetails);
                _dbContext.Order.Remove(o);
            }
            await _dbContext.SaveChangesAsync();
            return orders.Count;
        }

        public async Task<List<Order>> GetPackingListAsync(string search)
        {
            var query = _dbContext.Order
                .Include("OrderDetails.Product")
                .Where(o => o.OrderStatus == 1);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                int idSearch;
                bool isNum = int.TryParse(s, out idSearch);
                query = query.Where(o => o.ReceiverPhone.Contains(s) || (isNum && o.Id == idSearch));
            }

            return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        }

        public async Task<Order> GetOrderForPackingAsync(int id)
        {
            return await _dbContext.Order
                .Include("OrderDetails.Product")
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<bool> CompletePackingAsync(int id, string currentUserId)
        {
            var order = await _dbContext.Order.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return false;

            var currentUser = !string.IsNullOrEmpty(currentUserId)
                ? await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == currentUserId)
                : null;
            string staffInfo = currentUser != null ? (currentUser.FullName ?? currentUser.UserName) : "Nhân viên Admin";

            order.OrderStatus = 2; // Đã soạn xong / Chờ giao hàng
            order.Note = (order.Note ?? "") + $" [Soạn bởi: {staffInfo}]";
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<List<Order>> GetDeliveryListAsync(string search)
        {
            var query = _dbContext.Order
                .Include("OrderDetails.Product")
                .Where(o => o.OrderStatus == 2 || o.OrderStatus == 3);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                int idSearch;
                bool isNum = int.TryParse(s, out idSearch);
                query = query.Where(o => o.ReceiverPhone.Contains(s) || (isNum && o.Id == idSearch));
            }

            return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        }

        public async Task<(bool Success, string StaffInfo)> StartDeliveryAsync(int id, string currentUserId, string note)
        {
            var order = await _dbContext.Order.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return (false, null);

            var currentUser = !string.IsNullOrEmpty(currentUserId)
                ? await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == currentUserId)
                : null;
            string staffInfo = currentUser != null ? (currentUser.FullName ?? currentUser.UserName) : "Shipper";

            if (!string.IsNullOrEmpty(currentUserId))
            {
                order.UserId = currentUserId;
            }

            order.OrderStatus = 3; // Đang giao hàng
            order.Note = (order.Note ?? "") + $" [Giao bởi: {staffInfo}" + (!string.IsNullOrWhiteSpace(note) ? $" - {note.Trim()}" : "") + "]";
            await _dbContext.SaveChangesAsync();

            return (true, staffInfo);
        }

        public async Task<bool> CompleteDeliverySuccessAsync(int id, string currentUserId)
        {
            var order = await _dbContext.Order.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return false;

            var currentUser = !string.IsNullOrEmpty(currentUserId)
                ? await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == currentUserId)
                : null;
            string staffInfo = currentUser != null ? (!string.IsNullOrEmpty(currentUser.FullName) ? $"{currentUser.FullName} ({currentUser.UserName})" : currentUser.UserName) : "Shipper Bách Hóa Xanh";

            order.OrderStatus = 4;   // Thành công / Hoàn tất
            order.PaymentStatus = 1; // Đã thanh toán

            if (string.IsNullOrEmpty(order.Note) || !order.Note.Contains("Giao bởi:"))
            {
                order.Note = (order.Note ?? "") + $" [Giao bởi: {staffInfo}]";
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompleteDeliveryFailedAsync(int id)
        {
            var order = await _dbContext.Order.Include("OrderDetails").FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return false;

            if (order.OrderStatus != 5)
            {
                RestoreOrderStock(order);
                order.OrderStatus = 5; // Giao thất bại / Hủy đơn
                await _dbContext.SaveChangesAsync();
            }

            return true;
        }

        public async Task<(List<Order> Orders, double TotalSuccessRevenue, int TotalCount, int SuccessCount, int FailedCount)> GetOrderHistoryAsync(
            string search, int? status, decimal? minPrice, decimal? maxPrice, DateTime? fromDate, DateTime? toDate)
        {
            var query = _dbContext.Order
                .Include(o => o.User)
                .Include("OrderDetails.Product")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                int orderIdSearch;
                bool isNumeric = int.TryParse(s, out orderIdSearch);
                query = query.Where(o => o.ReceiverPhone.Contains(s) || 
                                         o.ReceiverName.Contains(s) || 
                                         (isNumeric && o.Id == orderIdSearch));
            }

            if (status.HasValue)
            {
                query = query.Where(o => o.OrderStatus == status.Value);
            }

            if (minPrice.HasValue)
            {
                double minD = (double)minPrice.Value;
                query = query.Where(o => o.TotalAmount >= minD);
            }
            if (maxPrice.HasValue)
            {
                double maxD = (double)maxPrice.Value;
                query = query.Where(o => o.TotalAmount <= maxD);
            }

            if (fromDate.HasValue)
            {
                DateTime start = fromDate.Value.Date;
                query = query.Where(o => o.OrderDate >= start);
            }
            if (toDate.HasValue)
            {
                DateTime end = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.OrderDate <= end);
            }

            var listOrders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            double totalSuccessRevenue = listOrders.Where(o => o.OrderStatus == 4).Sum(o => o.TotalAmount);
            int totalCount = listOrders.Count;
            int successCount = listOrders.Count(o => o.OrderStatus == 4);
            int failedCount = listOrders.Count(o => o.OrderStatus == 5);

            return (listOrders, totalSuccessRevenue, totalCount, successCount, failedCount);
        }

        public async Task<object> GetOrderDetailJsonDataAsync(int id)
        {
            var order = await _dbContext.Order
                .Include(o => o.User)
                .Include("OrderDetails.Product")
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return null;

            var items = order.OrderDetails.Select(od => new
            {
                productId = od.ProductId,
                productName = od.Product != null ? od.Product.Name : "Sản phẩm",
                barcode = od.Product != null ? od.Product.Barcode : "",
                image = od.Product != null ? od.Product.URLImage : "",
                quantity = od.Quantity,
                price = od.Price,
                total = od.Price * od.Quantity
            }).ToList();

            string noteStr = order.Note ?? "";
            string packedBy = ExtractNoteTag(noteStr, "Soạn bởi:") ?? ExtractNoteTag(noteStr, "Soạn hàng bởi:");
            string deliveredBy = ExtractNoteTag(noteStr, "Giao bởi:") ?? ExtractNoteTag(noteStr, "Giao hàng bởi:") ?? ExtractNoteTag(noteStr, "Shipper:");

            if (string.IsNullOrWhiteSpace(packedBy))
            {
                if (order.OrderStatus >= 2)
                {
                    packedBy = "Nhân viên Bách Hóa Xanh (Admin)";
                }
                else
                {
                    packedBy = "Chưa thực hiện soạn đơn";
                }
            }

            if (string.IsNullOrWhiteSpace(deliveredBy))
            {
                if (order.OrderStatus == 3 || order.OrderStatus == 4)
                {
                    if (order.User != null)
                    {
                        deliveredBy = !string.IsNullOrEmpty(order.User.FullName) ? $"{order.User.FullName} ({order.User.UserName})" : order.User.UserName;
                    }
                    else
                    {
                        deliveredBy = "Shipper Bách Hóa Xanh";
                    }
                }
                else
                {
                    deliveredBy = "Chưa thực hiện giao hàng";
                }
            }

            return new
            {
                id = order.Id,
                orderDate = order.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                receiverName = order.ReceiverName,
                receiverPhone = order.ReceiverPhone,
                shippingAddress = order.ShippingAddress,
                note = noteStr,
                packedBy = packedBy,
                deliveredBy = deliveredBy,
                paymentMethod = order.PaymentMethod == 1 ? "Ngân hàng (VietQR)" : (order.PaymentMethod == 2 ? "Ví MoMo" : "Tiền mặt (COD)"),
                paymentStatus = order.PaymentStatus == 1 ? "Đã thanh toán" : "Chưa thanh toán",
                orderStatus = order.OrderStatus,
                totalAmount = order.TotalAmount,
                discountAmount = order.DiscountAmount,
                shippingFee = order.ShippingFee,
                items = items
            };
        }

        private string ExtractNoteTag(string note, string tagPrefix)
        {
            if (string.IsNullOrEmpty(note)) return null;
            int idx = note.IndexOf(tagPrefix, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            int start = idx + tagPrefix.Length;
            int end = note.IndexOf("]", start);
            if (end > start)
            {
                return note.Substring(start, end - start).Trim(' ', ':');
            }
            return note.Substring(start).Trim(' ', ':');
        }

        private void RestoreOrderStock(Order order)
        {
            if (order == null || order.OrderDetails == null) return;

            foreach (var item in order.OrderDetails)
            {
                var product = _dbContext.Product.Find(item.ProductId);
                if (product != null)
                {
                    var targetStockProduct = (product.ParentProductId.HasValue && product.ParentProductId.Value > 0)
                        ? _dbContext.Product.Find(product.ParentProductId.Value) ?? product
                        : product;

                    int factor = product.UnitMultiplier > 0 ? product.UnitMultiplier : 1;
                    int quantityToRestore = item.Quantity * factor;

                    targetStockProduct.Quantity += quantityToRestore;
                }
            }
        }
    }
}

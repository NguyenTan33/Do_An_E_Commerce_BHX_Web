using Do_An_E_Commerce_BHX.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;

namespace Do_An_E_Commerce_BHX.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        [ChildActionOnly]
        public ActionResult CartSummary()
        {
            int cartCount = 0;
            decimal cartTotal = 0;

            if (Session["UserId"] != null)
            {
                int userId = (int)Session["UserId"];

                // Include chính xác tên property CartDetails trong model Cart.cs
                var cart = _db.Cart.Include(c => c.CartDetails)
                                   .FirstOrDefault(c => c.UserId == userId);

                if (cart != null && cart.CartDetails != null && cart.CartDetails.Any())
                {
                    cartCount = cart.CartDetails.Sum(d => d.Quantity);
                    // Ép kiểu tổng thành decimal để View dễ format bằng .ToString("N0")
                    cartTotal = (decimal)cart.CartDetails.Sum(d => d.Quantity * d.Price);
                }
            }

            ViewBag.CartCount = cartCount;
            ViewBag.CartTotal = cartTotal;

            return PartialView("_CartSummary");
        }

        // GET: Cart
        public ActionResult Index()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
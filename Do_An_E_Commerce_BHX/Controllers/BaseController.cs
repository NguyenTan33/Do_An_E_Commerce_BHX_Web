using System;
using System.Web;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Microsoft.AspNet.Identity;

namespace Do_An_E_Commerce_BHX.Controllers
{
    public class BaseController : Controller
    {
        protected ApplicationDbContext DbContext { get; set; }

        public BaseController()
        {
            DbContext = new ApplicationDbContext();
        }

        public BaseController(ApplicationDbContext dbContext)
        {
            DbContext = dbContext ?? new ApplicationDbContext();
        }

        // Hàm lấy UserId tự động có ở tất cả Controller con
        protected string GetCurrentUserId()
        {
            if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
            {
                return User.Identity.GetUserId();
            }

            string cookieName = "GuestId";

            if (Request != null && Request.Cookies != null && Request.Cookies[cookieName] != null && !string.IsNullOrEmpty(Request.Cookies[cookieName].Value))
            {
                return Request.Cookies[cookieName].Value;
            }

            string newGuestId = "GUEST_" + Guid.NewGuid().ToString();

            HttpCookie guestCookie = new HttpCookie(cookieName, newGuestId)
            {
                Expires = DateTime.Now.AddDays(30),
                HttpOnly = true
            };

            if (Response != null && Response.Cookies != null)
            {
                Response.Cookies.Add(guestCookie);
            }
            if (Request != null && Request.Cookies != null)
            {
                Request.Cookies.Add(guestCookie);
            }

            return newGuestId;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && DbContext != null)
            {
                DbContext.Dispose();
                DbContext = null;
            }
            base.Dispose(disposing);
        }
    }
}
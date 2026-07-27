using System;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;

namespace Do_An_E_Commerce_BHX.Controllers
{
    public class BaseController : Controller
    {
        // Hàm này sẽ tự động có ở tất cả Controller con
        protected string GetCurrentUserId()
        {
            if (User.Identity.IsAuthenticated)
            {
                return User.Identity.GetUserId(); // User đã đăng nhập
            }

            // Khách vãng lai: Lấy ID tạm từ Cookie
            string cookieName = "GuestId";

            // 1. Kiểm tra xem Cookie đã tồn tại trong Request chưa
            if (Request.Cookies[cookieName] != null && !string.IsNullOrEmpty(Request.Cookies[cookieName].Value))
            {
                return Request.Cookies[cookieName].Value;
            }

            // 2. Nếu chưa có -> Tạo GuestId mới (chuỗi string GUID)
            string newGuestId = "GUEST_" + Guid.NewGuid().ToString();

            // 3. Lưu vào Cookie và set thời gian sống (30 ngày)
            HttpCookie guestCookie = new HttpCookie(cookieName, newGuestId)
            {
                Expires = DateTime.Now.AddDays(30),
                HttpOnly = true
            };

            Response.Cookies.Add(guestCookie);
            Request.Cookies.Add(guestCookie);

            return newGuestId;
        }
    }
}
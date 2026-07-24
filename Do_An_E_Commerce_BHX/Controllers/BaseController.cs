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

            // 2. Nếu chưa có -> Tạo GuestId mới
            string newGuestId = "GUEST_" + Guid.NewGuid().ToString();

            // 3. Lưu vào Cookie và set thời gian sống (VD: 30 ngày)
            HttpCookie guestCookie = new HttpCookie(cookieName, newGuestId)
            {
                Expires = DateTime.Now.AddDays(30),
                HttpOnly = true // Tăng tính bảo mật
            };

            // Ghi cookie vào Response gửi về Client
            Response.Cookies.Add(guestCookie);

            // Gán tạm vào Request để các hàm gọi tiếp theo trong CÙNG 1 Request nhận được ngay
            Request.Cookies.Add(guestCookie);

            return newGuestId;
        }
    }
}
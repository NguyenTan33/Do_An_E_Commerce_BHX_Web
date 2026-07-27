using System;
using System.Web;

namespace Do_An_E_Commerce_BHX.Models
{
    public static class CustomerTierHelper
    {
        public static string GetTierName(int points)
        {
            if (points >= 20000) return "VIP";
            if (points >= 12000) return "Kim Cương";
            if (points >= 7000) return "Vàng";
            if (points >= 3000) return "Bạc";
            if (points >= 1000) return "Đồng";
            return "Thành Viên";
        }

        public static HtmlString GetTierBadgeHtml(int points)
        {
            string html = "";
            if (points >= 20000)
            {
                html = "<span class=\"badge bg-danger text-white px-2 py-1 shadow-sm fw-bold\"><i class=\"fa-solid fa-crown text-warning me-1\"></i>VIP</span>";
            }
            else if (points >= 12000)
            {
                html = "<span class=\"badge bg-info text-dark px-2 py-1 shadow-sm fw-bold\"><i class=\"fa-solid fa-gem text-primary me-1\"></i>Hạng Kim Cương</span>";
            }
            else if (points >= 7000)
            {
                html = "<span class=\"badge bg-warning text-dark px-2 py-1 shadow-sm fw-bold\"><i class=\"fa-solid fa-medal text-danger me-1\"></i>Hạng Vàng</span>";
            }
            else if (points >= 3000)
            {
                html = "<span class=\"badge bg-secondary text-white px-2 py-1 shadow-sm fw-bold\"><i class=\"fa-solid fa-award me-1\"></i>Hạng Bạc</span>";
            }
            else if (points >= 1000)
            {
                html = "<span class=\"badge text-white px-2 py-1 shadow-sm fw-bold\" style=\"background-color: #cd7f32;\"><i class=\"fa-solid fa-shield-halved me-1\"></i>Hạng Đồng</span>";
            }
            else
            {
                html = "<span class=\"badge bg-light text-dark border px-2 py-1 fw-bold\"><i class=\"fa-solid fa-user me-1\"></i>Thành Viên</span>";
            }

            return new HtmlString(html);
        }

        public static string GetTierColor(int points)
        {
            if (points >= 20000) return "#dc3545"; // Đỏ VIP
            if (points >= 12000) return "#0dcaf0"; // Xanh kim cương
            if (points >= 7000) return "#ffc107"; // Vàng
            if (points >= 3000) return "#6c757d"; // Bạc
            if (points >= 1000) return "#cd7f32"; // Đồng
            return "#6c757d";
        }
    }
}

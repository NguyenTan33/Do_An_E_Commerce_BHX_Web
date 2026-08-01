using System;

namespace Do_An_E_Commerce_BHX.Models
{
    /// <summary>
    /// OOP Value Object đại diện cho Trạng Thái Hoạt Động (Online / Offline) của người dùng
    /// </summary>
    public class UserPresence
    {
        public DateTime? LastActivityDate { get; }

        public UserPresence(DateTime? lastActivityDate)
        {
            LastActivityDate = lastActivityDate;
        }

        /// <summary>
        /// Người dùng được xem là Trực Tuyến (Online) nếu có tương tác trong 5 phút gần nhất
        /// </summary>
        public bool IsOnline
        {
            get
            {
                if (!LastActivityDate.HasValue) return false;
                return (DateTime.Now - LastActivityDate.Value).TotalMinutes <= 5;
            }
        }

        /// <summary>
        /// Chuỗi văn bản mô tả thời gian Offline linh hoạt kể từ lần đăng nhập / tương tác cuối cùng
        /// </summary>
        public string OfflineDurationText
        {
            get
            {
                if (IsOnline) return "Đang trực tuyến";
                if (!LastActivityDate.HasValue) return "Chưa từng đăng nhập";

                TimeSpan diff = DateTime.Now - LastActivityDate.Value;
                if (diff.TotalMinutes < 1)
                    return "Vừa mới thoát";
                if (diff.TotalMinutes < 60)
                    return $"{Math.Max(1, (int)diff.TotalMinutes)} phút trước";
                if (diff.TotalHours < 24)
                    return $"{(int)diff.TotalHours} giờ trước";
                if (diff.TotalDays < 30)
                    return $"{(int)diff.TotalDays} ngày trước";

                return LastActivityDate.Value.ToString("dd/MM/yyyy HH:mm");
            }
        }

        /// <summary>
        /// Badge HTML hiển thị trạng thái Trực tuyến / Ngoại tuyến chuẩn Dark Theme
        /// </summary>
        public string BadgeHtml
        {
            get
            {
                if (IsOnline)
                {
                    return "<span class='badge bg-success text-white border border-success px-2 py-1 shadow-sm'><i class='fa-solid fa-circle text-white me-1' style='font-size: 8px;'></i> 🟢 Trực tuyến</span>";
                }
                if (!LastActivityDate.HasValue)
                {
                    return "<span class='badge bg-dark text-muted border border-secondary px-2 py-1'><i class='fa-regular fa-clock me-1'></i> Chưa đăng nhập</span>";
                }
                return $"<span class='badge bg-dark text-warning border border-secondary px-2 py-1' title='Lần tương tác cuối: {LastActivityDate.Value:dd/MM/yyyy HH:mm}'><i class='fa-solid fa-moon text-warning me-1'></i> Offline ({OfflineDurationText})</span>";
            }
        }
    }
}

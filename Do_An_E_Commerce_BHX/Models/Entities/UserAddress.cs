using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Do_An_E_Commerce_BHX.Models.Entities
{
    public class UserAddress
    {
        [Key]
        public int Id { get; set; }

        // Khóa ngoại liên kết tới tài khoản User
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // Tên gợi nhớ cho địa chỉ (VD: "Nhà riêng", "Cơ quan", "Nhà bạn gái")
        [Required, StringLength(50)]
        public string AddressName { get; set; }

        // Tên người nhận hàng tại địa chỉ này
        [Required, StringLength(100)]
        public string ReceiverName { get; set; }

        // SĐT người nhận hàng
        [Required, StringLength(15)]
        public string ReceiverPhone { get; set; }

        // Địa chỉ chi tiết (Số nhà, tên đường, Phường/Xã, Quận/Huyện, Tỉnh/TP)
        [Required, StringLength(255)]
        public string DetailedAddress { get; set; }

        // Đánh dấu địa chỉ mặc định (true = Mặc định)
        public bool IsDefault { get; set; }
    }
}
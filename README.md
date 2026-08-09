# 🛒 Bách Hóa Xanh E-Commerce Platform

> **Hệ thống E-Commerce thương mại điện tử chuyên nghiệp mô phỏng Bách Hóa Xanh**  
> Được xây dựng trên nền tảng **ASP.NET MVC 5 (.NET Framework 4.7.2)** theo kiến trúc **Thin Controller / Fat Service (OOP)**, tích hợp thanh toán tự động **VietQR SePay Webhook**, ví MoMo, phát hành hóa đơn điện tử qua Email, hệ thống Voucher thông minh kiểu Shopee và theo dõi hành vi người dùng (User Behavior Analytics).

---

[![Live Demo](https://img.shields.io/badge/🌐_Live_Demo-webcuatan.click-00C853?style=for-the-badge&logo=googlechrome&logoColor=white)](https://webcuatan.click)
[![ASP.NET MVC](https://img.shields.io/badge/Framework-ASP.NET_MVC_5-blue.svg)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/Language-C%23_7.3-purple.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Entity Framework](https://img.shields.io/badge/ORM-Entity_Framework_6-red.svg)](https://docs.microsoft.com/en-us/ef/ef6/)
[![SQL Server](https://img.shields.io/badge/Database-SQL_Server-blue.svg)](https://www.microsoft.com/sql-server)
[![VietQR SePay](https://img.shields.io/badge/Payment-VietQR_SePay_Auto_Reconciliation-green.svg)](https://sepay.vn/)
[![License](https://img.shields.io/badge/License-MIT-brightgreen.svg)](#)

---

## 🌐 Trải nghiệm Live Demo
- **Website chính thức**: [https://webcuatan.click](https://webcuatan.click)
- **Trang Quản trị (Admin Area)**: [https://webcuatan.click/Admin](https://webcuatan.click/Admin)
- *Hệ thống đang hoạt động trực tuyến 24/7 với đầy đủ các tính năng đặt hàng, quét mã VietQR tự động gạch nợ real-time và gửi hóa đơn điện tử.*

---

## 📌 Mục lục
- [Giới thiệu Dự án](#-giới-thiệu-dự-án)
- [Điểm sáng Kỹ thuật & Architecture (CV Highlights)](#-điểm-sáng-kỹ-thuật--architecture-cv-highlights)
- [Tính năng nổi bật](#-tính-năng-nổi-bật)
  - [1. Khách hàng (Client Storefront)](#1-khách-hàng-client-storefront)
  - [2. Quản trị viên (Admin Portal)](#2-quản-trị-viên-admin-portal)
- [Công nghệ & Thư viện sử dụng](#-công-nghệ--thư-viện-sử-dụng)
- [Cấu trúc thư mục Dự án](#-cấu-trúc-thư-mục-dự-án)
- [Hướng dẫn Cài đặt & Chạy ứng dụng](#-hướng-dẫn-cài-đặt--chạy-ứng-dụng)
- [Liên hệ & Tác giả](#-liên-hệ--tác-giả)

---

## 🎯 Giới thiệu Dự án

**Bách Hóa Xanh E-Commerce Platform** là một dự án ứng dụng web thương mại điện tử hoàn chỉnh, giải quyết toàn bộ bài toán mua sắm nhu yếu phẩm trực tuyến: từ tìm kiếm sản phẩm, chọn đơn vị tính linh hoạt (Kg, Chai, Khay, Thùng), áp dụng mã giảm giá thông minh, đặt hàng, tự động xác thực thanh toán chuyển khoản ngân hàng qua QR Code theo thời gian thực (Real-time Payment Reconciliation) đến quy trình duyệt đơn & đóng gói của nhà bán hàng.

Dự án được tái cấu trúc hoàn toàn (Refactored) theo nguyên lý **Lập trình hướng đối tượng (OOP)**, tuân thủ mô hình **Thin Controller / Fat Service**, giúp mã nguồn đạt độ tách biệt cao (Separation of Concerns), dễ bảo trì, dễ viết Unit Test và mở rộng cho doanh nghiệp quy mô lớn.

---

## 🌟 Điểm sáng Kỹ thuật & Architecture

1. **Kiến trúc Thin Controller & Service Layer Pattern (OOP)**:
   - Toàn bộ Business Logic phức tạp (tính tiền, khấu trừ kho, thuật toán xét điều kiện Voucher, gạch nợ tự động) được đóng gói trong **Tầng Service (`Services/Implementations`)** thông qua các **Interface (`Services/Interfaces`)**.
   - Controllers chỉ đảm nhận nhiệm vụ điều hướng URL, validate dữ liệu đầu vào và trả về kết quả JSON/View.

2. **Thanh toán tự động VietQR SePay & Webhook Reconciliator**:
   - Tích hợp **VietQR (Ngân hàng VietinBank)** sinh mã QR động kèm nội dung chuyển khoản độc nhất (`BHX{OrderId}` hoặc Mã phiên `P{yymmdd...}`).
   - **Tự động Gạch nợ Real-time via Webhook**: Lắng nghe tín hiệu Webhook từ SePay khi khách chuyển tiền thành công, tự động cập nhật trạng thái đơn hàng và gửi Email hóa đơn điện tử mà không cần sự can thiệp của con người.
   - **Fallback Polling**: Tự động gọi **SePay REST API** từ phía máy chủ để kiểm tra trạng thái thanh toán ngay cả trong môi trường Localhost (ngăn chặn tình trạng sót đơn).
   - **Auto-Expiry 10-Minute Engine**: Tự động hủy đơn và hoàn trả số lượng tồn kho sản phẩm nếu đơn VietQR quá 10 phút không thanh toán.

3. **Shopee-style Smart Voucher Engine**:
   - Thuật toán đánh giá Voucher thời gian thực theo nhiều tiêu chí: *Giá trị đơn tối thiểu (MinOrderAmount)*, *Danh mục áp dụng (Category)*, *Mức giảm tối đa (MaxDiscountAmount)*, *Giới hạn lượt sử dụng (UsageLimit)* và *Hạn dùng (ExpiryDate)*.
   - Tự động gợi ý danh sách Voucher khả dụng tốt nhất cho giỏ hàng của người dùng.

4. **User Behavior Analytics Tracker (Async Concurrency)**:
   - Hệ thống ghi log hành vi người dùng (`SearchKeyword`, `ViewProduct`, `DurationSeconds`, `ScrollPercent`) bất đồng bộ bằng `Task.Run` / Beacon API mà **không gây nghẽn (Non-blocking) luồng xử lý chính của trang web**.

5. **Ví điểm thưởng & Đa đơn vị tính (Multi-unit Stock)**:
   - Quản lý quy đổi đơn vị tính (Cái -> Thùng -> Khay) dùng chung tồn kho sản phẩm gốc (`ParentProduct`).
   - Tích điểm thành viên (`LoyaltyPoints`) trừ tiền trực tiếp trên đơn hàng và Ví hoàn tiền Affiliate.

---

## 🚀 Tính năng nổi bật

### 1. Khách hàng (Client Storefront)
- **Trang chủ & Khung giờ vàng (Flash Sale Slot)**: Tự động phát hiện và đếm ngược theo 4 khung giờ vàng trong ngày (Sáng, Trưa, Tối, Đêm).
- **Tìm kiếm & Bộ lọc nâng cao**: Lọc theo từ khóa, danh mục nhu yếu phẩm, khoảng giá và sắp xếp linh hoạt.
- **Chi tiết sản phẩm & Đơn vị tính**: Cho phép chọn đơn vị tính (vd: Mua lẻ 1 Quả hoặc Mua 1 Túi 1kg) với giá và tồn kho quy đổi tương ứng.
- **Hỏi đáp & Đánh giá (Customer Reviews & Q&A)**: Gửi đánh giá 1-5 sao, bình luận và đặt câu hỏi cho QTV.
- **Giỏ hàng tương tác cao (AJAX Cart)**: Thêm/sửa/xóa sản phẩm, chọn nhiều mục để thanh toán, áp mã giảm giá không cần tải lại trang.
- **Thanh toán & Đặt hàng đa phương thức**: COD, Chuyển khoản VietQR, Ví MoMo. Tra cứu đơn hàng tức thì qua Số điện thoại / Mã đơn.
- **Sổ địa chỉ & Ví cá nhân**: Quản lý nhiều địa chỉ nhận hàng (đặt mặc định), theo dõi số dư ví và gửi yêu cầu rút tiền.

### 2. Quản trị viên (Admin Portal)
- **Dashboard phân tích tổng quan**: Thống kê doanh thu, tổng số đơn hàng, khách hàng mới và biểu đồ xu hướng.
- **Quản lý sản phẩm & Tồn kho**: Đổi đơn vị tính linh hoạt, xem cảnh báo hết hàng, khóa/mở bán sản phẩm.
- **Quản lý Đơn hàng & Quy trình Đóng gói**: Duyệt đơn, chuyển trạng thái (Chờ duyệt -> Đã duyệt -> Đã đóng gói -> Đang giao -> Thành công / Hủy). Tự động hoàn tồn kho khi hủy đơn.
- **Quản lý Mã giảm giá (Vouchers)**: Tạo mã khuyến mãi với bộ quy tắc điều kiện tùy chỉnh.
- **Hệ thống CSKH & Phản hồi QTV**: Trả lời câu hỏi của khách hàng trên trang sản phẩm, duyệt đánh giá.
- **Phân quyền & Quản lý người dùng (RBAC)**: Cấp quyền Admin/Staff/User, khóa tài khoản, reset mật khẩu trực tiếp.

---

## 🛠️ Công nghệ & Thư viện sử dụng

| Tầng | Công nghệ / Thư viện |
| :--- | :--- |
| **Backend Framework** | ASP.NET MVC 5 (.NET Framework 4.7.2) |
| **Programming Language** | C# 7.3 |
| **Database & ORM** | Microsoft SQL Server, Entity Framework 6 (Code First & DbContext) |
| **Authentication & Security** | ASP.NET Identity 2.0 (OWIN Cookie Authentication, RBAC) |
| **Architecture Pattern** | Thin Controller / Fat Service Layer / Repository Pattern |
| **Integrations** | VietQR SePay REST API & Webhook, MoMo Payment Gateway, SMTP Mail Invoice |
| **Frontend Layout** | HTML5, Modern Vanilla CSS3 Design System, Bootstrap 5 |
| **Frontend Scripting** | JavaScript (ES6+, Fetch API, AJAX, DOM Manipulation) |
| **Third-party Packages** | Newtonsoft.Json, PagedList.Mvc |

---

## 📁 Cấu trúc thư mục Dự án

```
Do_An_E_Commerce_BHX/
├── Areas/
│   └── Admin/                     # Phân vùng Quản trị (Admin Area)
│       ├── Controllers/           # Admin BaseController & Management Controllers
│       ├── Services/              # Admin Service Interfaces & Implementations
│       └── Views/                 # Admin Dashboard & Dark Glassmorphism Views
├── Controllers/                   # Client Thin Controllers (Order, Home, Product, Cart...)
├── Services/                      # Application Business Logic Layer
│   ├── Interfaces/                # IOrderCheckoutService, IHomeService, IStoreProductService...
│   └── Implementations/           # Service Implementations (OOP Logic)
├── Models/                        # Entity Framework Data Models & ViewModels
│   ├── Entities/                  # Product, Order, Category, Promotion, Cart, UserAddress...
│   └── ViewModels/                # HomeIndexViewModel, UserViewModel...
├── Helpers/                       # CurrencyHelper, StatusEnum, Utility Helpers
├── Views/                         # Razor Views (Storefront Client Views)
├── App_Data/                      # System Transaction Logs (sepay_transactions.log)
├── Web.config                     # Environment Configuration (ConnectionStrings, AppSettings)
└── Do_An_E_Commerce_BHX.csproj    # MSBuild Project Manifest
```

---

## 🚀 Hướng dẫn Cài đặt & Chạy ứng dụng

### Yêu cầu hệ thống
- **Visual Studio 2019 / 2022** (Đã cài đặt `.NET Desktop Development` và `ASP.NET and web development`).
- **Microsoft SQL Server 2016+** hoặc **SQL Server Express / LocalDB**.
- **IIS Express** (Tích hợp sẵn trong Visual Studio).

### Các bước khởi chạy

1. **Clone repository**:
   ```bash
   git clone https://github.com/NguyenTan33/Do_An_E_Commerce_BHX_Web.git
   cd Do_An_E_Commerce_BHX_Web
   ```

2. **Cấu hình Cơ sở dữ liệu (Database)**:
   - Mở SQL Server Management Studio (SSMS).
   - Tạo Database mới tên `Do_An_E_Commerce_BHX`.
   - Chạy file script `Database_Setup.sql` trong thư mục gốc để khởi tạo bảng và dữ liệu mẫu.

3. **Cấu hình chuỗi kết nối (Connection String)**:
   - Mở file `Web.config` trong thư mục gốc dự án.
   - Cập nhật chuỗi kết nối `DefaultConnection` phù hợp với SQL Server của bạn:
   ```xml
   <add name="DefaultConnection" 
        connectionString="Data Source=YOUR_SERVER_NAME;Initial Catalog=Do_An_E_Commerce_BHX;Integrated Security=True;MultipleActiveResultSets=True;" 
        providerName="System.Data.SqlClient" />
   ```

4. **Khởi chạy ứng dụng**:
   - Mở giải pháp `Do_An_E_Commerce_BHX.sln` bằng Visual Studio.
   - Nhấn **F5** hoặc bấm nút **IIS Express** để biên dịch và khởi chạy trên trình duyệt (`http://localhost:44331/`).

---

## 👤 Liên hệ & Tác giả

- **Họ và tên**: Nguyễn Minh Tân
- **Họ và tên tác giả 2**: Từ Quyết Thắng
- **GitHub**: [NguyenTan33](https://github.com/NguyenTan33)
- **Dự án Repository**: [Do_An_E_Commerce_BHX_Web](https://github.com/NguyenTan33/Do_An_E_Commerce_BHX_Web)

---
*Cảm ơn bạn đã xem qua dự án! Nếu thấy hữu ích, hãy tặng dự án 1 ⭐️ trên GitHub nhé!*

# 🎱 Billiard Management System (Phần mềm Quản lý Quán Billiard)

> Hệ thống phần mềm quản lý quán Billiard chuyên nghiệp, tối ưu cho hộ kinh doanh cá thể và câu lạc bộ Billiard vừa & nhỏ.

---

## 📌 Giới thiệu dự án

Dự án được xây dựng nhằm giải quyết bài toán vận hành hàng ngày của quán bida: từ quản lý bàn chơi theo thời gian thực, tự động tính tiền giờ theo loại bàn/khung giờ, quản lý dịch vụ F&B đi kèm, đến thanh toán và thống kê doanh thu rõ ràng, minh bạch.

---

## 🚀 Các tính năng chính (Dự kiến & Phát triển)

- **🎱 Quản lý Bàn & Trạng thái Thời gian thực:**
  - Phân loại bàn: *Carom (Libre, 3 Băng), Pool (Lỗ), Snooker,...*
  - Theo dõi trạng thái: *Bàn trống, Đang chơi, Đặt trước, Bảo trì.*
  - Chức năng: Mở bàn, tạm dừng, chuyển bàn, gộp bàn.

- **⏱️ Tự động tính tiền giờ:**
  - Bấm giờ chính xác theo từng phút.
  - Cấu hình bảng giá linh hoạt theo loại bàn, khung giờ vàng, ngày cuối tuần / ngày lễ.

- **🍹 Quản lý Dịch vụ & F&B:**
  - Gọi đồ uống, đồ ăn, thuốc lá, thuê phụ kiện tính trực tiếp vào hóa đơn bàn.
  - Quản lý danh mục hàng hóa và tồn kho F&B.

- **💳 Thanh toán & Hóa đơn:**
  - Tự động cộng tiền giờ + tiền dịch vụ.
  - Áp dụng mã giảm giá, chiết khấu khách hàng.
  - Hỗ trợ in hóa đơn và tạo mã QR chuyển khoản (VietQR).

- **📊 Báo cáo & Quản lý Ca:**
  - Báo cáo doanh thu theo ngày, tuần, tháng.
  - Quản lý giao ca nhân viên, đối soát dòng tiền mặt và chuyển khoản.

- **👥 Phân quyền & Khách hàng:**
  - Phân quyền: *Chủ quán (Admin)* và *Thu ngân / Nhân viên*.
  - Quản lý hội viên, tích điểm khách hàng thân thiết.

---

## 🛠️ Công nghệ sử dụng

- **Giao diện & Nền tảng:** WPF (.NET Framework 4.7.2, C#)
- **Kiến trúc:** MVVM (Model - View - ViewModel)
- **UI Toolkit:** XAML, Material Design / ModernWpf
- **Cơ sở dữ liệu:** MySQL 8.0+ (Lưu trữ Cloud trên Aiven Managed Database, SSL Mode Required)
- **Data Access:** ADO.NET với thư viện high-performance `MySqlConnector`

---

## 🗄️ Cơ sở Dữ liệu & Tài liệu (Database & Documentation)

- **Chi tiết thiết kế 17 bảng:** Xem toàn bộ sơ đồ ERD và Data Dictionary tại [docs/database.md](docs/database.md).
- **Mã nguồn khởi tạo DB:** File DDL schema và seed data tại [database/schema.sql](database/schema.sql).
- **Tài khoản đăng nhập mặc định (Seed Data):**
  - **Quản trị viên (Admin):** Tài khoản `admin` / Mật khẩu `Admin@123` (Được cấp toàn bộ 8 quyền hệ thống).
  - **Nhân viên thu ngân (Staff):** Tài khoản `staff01` / Mật khẩu `Admin@123`.

---

## 💻 Yêu cầu cài đặt & Khởi chạy

1. **Yêu cầu hệ thống:**
   - Windows 10 / 11
   - Visual Studio 2022 (hoặc 2019) có cài đặt workload **.NET Desktop Development**.
   - .NET Framework 4.7.2 trở lên.

2. **Cấu hình kết nối cơ sở dữ liệu (Bảo mật theo chuẩn .NET):**
   - Dự án áp dụng cơ chế tách riêng chuỗi kết nối vào file [billiard-management-system/connections.config](file:///d:/billiard-management-system/billiard-management-system/connections.config). File này đã được thêm vào `.gitignore` để không bao giờ bị lộ mật khẩu lên GitHub.
   - File [billiard-management-system/App.config](file:///d:/billiard-management-system/billiard-management-system/App.config) chỉ chứa thẻ nạp tham chiếu: `<connectionStrings configSource="connections.config" />`.
   - Khi clone dự án sang máy mới, chỉ cần copy file `connections.config.example` thành `connections.config` và điền thông tin đăng nhập:
     ```xml
     <connectionStrings>
         <add name="BilliardDb" 
              connectionString="Server=YOUR_HOST;Port=YOUR_PORT;Database=defaultdb;User ID=YOUR_USER;Password=YOUR_PASSWORD;SslMode=Required;CharSet=utf8mb4;" 
              providerName="MySqlConnector" />
     </connectionStrings>
     ```

3. **Khởi chạy dự án:**
   ```bash
   # Clone repository về máy
   git clone https://github.com/<tai-khoan>/billiard-management-system.git

   # Mở file solution trong Visual Studio
   billiard-management-system.slnx
   ```
   - Nhấn **F5** hoặc bấm nút **Start** trong Visual Studio để biên dịch và chạy phần mềm.

---

## 📝 Giấy phép (License)
Dự án được phát triển dưới giấy phép MIT License.

# 🎱 Billiard Management System (Hệ thống Quản lý Quán Billiard Chuyên Nghiệp)

> Hệ thống phần mềm Desktop **WPF (.NET Framework 4.7.2, C#)** theo chuẩn kiến trúc **MVVM**, kết nối cơ sở dữ liệu đám mây **MySQL 8.0+ (Aiven Managed Database)** và **Cloudinary CDN**, tối ưu hóa toàn diện quy trình vận hành phòng máy/câu lạc bộ Billiard từ quản lý sơ đồ bàn thời gian thực, tự động tách khung giờ tính tiền, gọi món F&B, lập hóa đơn thanh toán đến báo cáo doanh thu và nhật ký kiểm toán hệ thống.

---

## 📑 Mục lục
1. [Kiến trúc Hệ thống & Công nghệ](#-1-kiến-trúc-hệ-thống--công-nghệ)
2. [Mô tả Các Phân hệ Chức năng Chính](#-2-mô-tả-các-phân-hệ-chức-năng-chính)
3. [Sơ đồ Cơ sở Dữ liệu (Database ERD - 15 Bảng)](#-3-sơ-đồ-cơ-sở-dữ-liệu-database-erd---15-bảng)
4. [Hướng dẫn Cài đặt & Cấu hình Bảo mật (MySQL/Aiven.io & Cloudinary)](#-4-hướng-dẫn-cài-đặt--cấu-hình-bảo-mật-mysqlaivenio--cloudinary)
5. [Dữ liệu Khởi tạo & Tài khoản Demo (Seed Data)](#-5-dữ-liệu-khởi-tạo--tài-khoản-demo-seed-data)
6. [Kịch bản Trình bày & Demo Đánh giá Cuối kỳ](#-6-kịch-bản-trình-bày--demo-đánh-giá-cuối-kỳ)
7. [Phân công & Nắm vững Kiến trúc Giữa Hai Thành viên](#-7-phân-công--nắm-vững-kiến-trúc-giữa-hai-thành-viên)

---

## 🏗️ 1. Kiến trúc Hệ thống & Công nghệ

### Công nghệ sử dụng
- **Nền tảng & Giao diện:** WPF (`.NET Framework 4.7.2`, C#), XAML + `MaterialDesignThemes` (`MaterialDesignInXamlToolkit`).
- **Kiến trúc phần mềm:** **MVVM (Model – View – ViewModel)** kết hợp tầng **Service Layer** và **ADO.NET Asynchronous (`MySqlConnector`)**.
- **Cơ sở dữ liệu:** **MySQL 8.0+ trên Aiven Cloud** (`SslMode=Required`, `CharSet=utf8mb4`, tự động đồng bộ múi giờ Việt Nam `SET time_zone = '+07:00'`).
- **Lưu trữ Hình ảnh Đám mây:** **Cloudinary REST API** (Upload ảnh sản phẩm có chữ ký `SHA-1` hoặc `Unsigned Preset`, trả về `secure_url` HTTPS).
- **Bảo mật & Kiểm toán:** Băm mật khẩu một chiều bằng **BCrypt (`BCrypt.Net-Next`)**, phân quyền hai lớp **RBAC (UI + Service Layer)**, tự động lọc dữ liệu nhạy cảm (`[REDACTED]`) trong `AuditLogs`.

### Sơ đồ Kiến trúc Tổng thể (MVVM + Service Layer)
```mermaid
flowchart LR
    subgraph UI["Tầng Giao diện (WPF Views - XAML)"]
        V1["LoginWindow / MainWindow"]
        V2["TableManagementView"]
        V3["ProductManagementView"]
        V4["InvoiceManagementView"]
        V5["ReportManagementView"]
        V6["StaffManagementView"]
    end

    subgraph VM["Tầng Điều phối (ViewModels)"]
        VM1["LoginViewModel / MainViewModel"]
        VM2["TableManagementViewModel"]
        VM3["ProductManagementViewModel"]
        VM4["InvoiceManagementViewModel"]
        VM5["ReportManagementViewModel"]
        VM6["StaffManagementViewModel"]
    end

    subgraph SVC["Tầng Nghiệp vụ (Services & RBAC Guards)"]
        S1["AuthService & UserSession"]
        S2["TableService & PricingRuleService"]
        S3["ProductService & CloudinaryService"]
        S4["OrderService (Transactional Billing)"]
        S5["ReportService & AuditLogService"]
        S6["SystemIntegrationTestService"]
    end

    subgraph INFRA["Hạ tầng Dữ liệu & Đám mây"]
        DB[("Aiven Cloud MySQL 8.0\n(SSL + UTC+07:00)")]
        CDN[("Cloudinary CDN\n(HTTPS Product Images)")]
    end

    UI <-->|"Data Binding / ICommand"| VM
    VM -->|"Async Calls"| SVC
    SVC -->|"MySqlConnector (Transactions)"| DB
    S3 -->|"REST Upload / SecureUrl"| CDN
```

---

## 🚀 2. Mô tả Các Phân hệ Chức năng Chính

1. **🔐 Xác thực, Phiên làm việc & Phân quyền RBAC (`Issue #9, #10, #11`):**
   - Đăng nhập kiểm tra mật khẩu băm `BCrypt`, chặn tài khoản bị khóa (`IsActive = false`), tự động nạp danh sách quyền hạn (`Permissions`) vào `UserSession`.
   - Kiểm soát quyền hạn 2 lớp (ẩn/khóa nút trên UI **và** chặn trực tiếp tại đầu mỗi hàm `Service` qua `UserSession.CheckAccess`).
   - Quản lý tài khoản (`Users`) kết hợp hồ sơ nhân sự (`Employees`) trong một giao dịch (`Transaction`).

2. **🎱 Quản lý Sơ đồ Bàn, Loại bàn & Bảng giá Khung giờ (`Issue #12, #15, #16, #18`):**
   - Sơ đồ bàn trực quan theo thời gian thực với bộ lọc trạng thái (`Available`, `Occupied`, `Maintenance`) và đồng hồ đếm thời lượng chơi chính xác theo múi giờ `+07:00`.
   - Hỗ trợ mở bàn kèm tên/SĐT khách hàng, chuyển bàn (`SwitchTableAsync`), hủy phiên (`EndSessionAsync`) có xác nhận và khóa chống mở trùng bàn (`SELECT ... FOR UPDATE`).
   - Động cơ tính tiền bàn tự động tách khoảng thời gian (`PricingRuleService.CalculateTimeSlotSegments`) khi phiên chơi đi qua nhiều khung giờ (giờ thường, giờ cao điểm, qua ngày thường sang cuối tuần/ngày lễ) và áp dụng khuyến mãi (`Percentage` / `FixedAmount`), lưu chi tiết vào `SessionPricingDetails`.

3. **🍹 Quản lý Dịch vụ F&B, Gọi món & Ảnh Cloudinary (`Issue #13, #14, #17`):**
   - CRUD danh mục (`ProductCategories`) và sản phẩm (`Products`), bảo toàn giá lịch sử khi sản phẩm đổi giá hoặc ngừng kinh doanh.
   - Tích hợp `CloudinaryService` tải ảnh sản phẩm trực tiếp từ máy tính lên Cloudinary, lưu `ImageUrl` và hiển thị ảnh kèm ảnh mặc định dự phòng khi thiếu ảnh.
   - Gọi món vào bàn đang chơi, cộng dồn số lượng hoặc tăng/giảm/xóa món trong `SessionProducts` và cập nhật tạm tính tức thời.

4. **🧾 Lập Hóa đơn, Thanh toán & Tra cứu Lịch sử (`Issue #19, #20, #21`):**
   - Sinh mã hóa đơn duy nhất (`INV-yyyyMMdd-HHmmss-xxxx`), lưu đồng bộ `Invoices` và `InvoiceDetails` (`ItemType = 'Table'` và `'Product'`) trong cùng 1 transaction.
   - Hỗ trợ thanh toán tiền mặt (`Cash`) hoặc chuyển khoản (`BankTransfer`), nhập giảm giá trực tiếp, hiển thị hộp thoại xác nhận chi tiết và chống bấm đúp thanh toán trùng (`ActiveSessionCheckouts` / `ActiveInvoicePayments`).
   - Màn hình **Hóa đơn & Lịch sử Phiên** cho phép lọc theo khoảng ngày, trạng thái, từ khóa và thanh toán bổ sung các hóa đơn đang `Pending`.

5. **📊 Báo cáo Doanh thu, Nhật ký Hệ thống & Kiểm thử Tích hợp (`Issue #22, #23, #24, #25`):**
   - Màn hình **Báo cáo & Thống kê** tính doanh thu chuẩn xác từ các hóa đơn `Status = 'Paid'`, tách bạch doanh thu tiền bàn và dịch vụ, thống kê theo ngày, theo bàn, top sản phẩm bán chạy và bảng hóa đơn đối soát.
   - Nhật ký kiểm toán `AuditLogs` lưu người thực hiện, hành động, đối tượng, thời gian và dữ liệu JSON trước/sau thay đổi (`OldValues` / `NewValues`), tự động ẩn mật khẩu/hash (`[REDACTED]`).
   - Tích hợp sẵn bộ chạy **Kiểm thử Tích hợp Toàn hệ thống (`SystemIntegrationTestService`)** và cơ chế bắt ngoại lệ toàn cục (`DispatcherUnhandledException`).

---

## 🗄️ 3. Sơ đồ Cơ sở Dữ liệu (Database ERD - 15 Bảng)

Chi tiết từ điển dữ liệu của toàn bộ các bảng được mô tả tại [docs/database.md](docs/database.md) và kịch bản khởi tạo tại [database/schema.sql](database/schema.sql).

```mermaid
erDiagram
    Roles ||--o{ Users : "phan_loai"
    Roles ||--o{ RolePermissions : "cap_quyen"
    Permissions ||--o{ RolePermissions : "thuoc_ve"
    Users ||--o| Employees : "ho_so_nhan_su"
    Users ||--o{ Sessions : "mo_ban"
    Users ||--o{ Invoices : "thu_ngan"
    Users ||--o{ AuditLogs : "thuc_hien"

    TableTypes ||--o{ Tables : "phan_nhom"
    TableTypes ||--o{ TablePricingRules : "ap_dung_gia"
    Customers ||--o{ Sessions : "khach_choi"
    Tables ||--o{ Sessions : "phuc_vu"

    Sessions ||--o{ SessionPricingDetails : "tach_khung_gio"
    TablePricingRules ||--o{ SessionPricingDetails : "quy_tac_gia"
    Sessions ||--o{ SessionProducts : "goi_mon"
    Products ||--o{ SessionProducts : "san_pham_goi"
    ProductCategories ||--o{ Products : "danh_muc"

    Sessions ||--|| Invoices : "thanh_toan"
    Invoices ||--o{ InvoiceDetails : "chi_tiet_hd"
```

---

## 💻 4. Hướng dẫn Cài đặt & Cấu hình Bảo mật (MySQL/Aiven.io & Cloudinary)

### 4.1. Yêu cầu hệ thống
- **Hệ điều hành:** Windows 10 / Windows 11.
- **Công cụ phát triển:** Visual Studio 2022 (hoặc 2019) với workload **.NET Desktop Development** (`.NET Framework 4.7.2`).
- **Cơ sở dữ liệu:** MySQL 8.0+ (Local hoặc **Aiven Managed MySQL**).

### 4.2. Khởi tạo Cơ sở dữ liệu
Chạy toàn bộ nội dung file [database/schema.sql](database/schema.sql) trên MySQL Workbench, DBeaver hoặc qua script [database/migrate.py](database/migrate.py) để tạo 15 bảng cùng dữ liệu mẫu (Seed Data).

### 4.3. Cấu hình bảo mật MySQL / Aiven.io (`connections.config`)
Để bảo vệ thông tin nhạy cảm, dự án **không lưu mật khẩu trong `App.config`**. Thay vào đó, `App.config` nạp chuỗi kết nối từ file `billiard-management-system/connections.config` (đã được đưa vào `.gitignore`).
1. Sao chép file mẫu `billiard-management-system/connections.config.example` thành `billiard-management-system/connections.config`:
   ```powershell
   Copy-Item billiard-management-system\connections.config.example billiard-management-system\connections.config
   ```
2. Cập nhật thông tin host/user/password Aiven MySQL của bạn trong `billiard-management-system/connections.config`:
   ```xml
   <connectionStrings>
       <add name="BilliardDb"
            connectionString="Server=YOUR_AIVEN_HOST;Port=YOUR_PORT;Database=defaultdb;User ID=avnadmin;Password=YOUR_PASSWORD;SslMode=Required;CharSet=utf8mb4;"
            providerName="MySqlConnector" />
   </connectionStrings>
   ```
   > **Lưu ý đồng bộ múi giờ:** `DatabaseHelper.OpenConnectionAsync()` tự động thực thi `SET time_zone = '+07:00';` ngay khi mở kết nối để tránh lệch 7 giờ giữa giờ UTC của Aiven Cloud và giờ địa phương Việt Nam.

### 4.4. Cấu hình bảo mật Cloudinary (Upload ảnh sản phẩm)
`CloudinaryService` đọc cấu hình theo thứ tự ưu tiên từ biến môi trường của hệ điều hành hoặc `AppSettings` cục bộ (không commit secret lên Git):
- **Cách 1 (Khuyến nghị):** Đặt biến môi trường trên Windows:
  ```powershell
  [Environment]::SetEnvironmentVariable("CLOUDINARY_URL", "cloudinary://API_KEY:API_SECRET@CLOUD_NAME", "User")
  ```
- **Cách 2:** Đặt các biến môi trường rời: `CLOUDINARY_CLOUD_NAME`, `CLOUDINARY_API_KEY`, `CLOUDINARY_API_SECRET` (hoặc `CLOUDINARY_UPLOAD_PRESET` nếu dùng Unsigned Upload).
- Nếu chưa cấu hình khóa API, người dùng vẫn có thể dán trực tiếp đường dẫn ảnh HTTPS hợp lệ vào ô **URL Ảnh sản phẩm**.

### 4.5. Biên dịch và Chạy ứng dụng
- Mở `billiard-management-system.slnx` bằng **Visual Studio 2022**.
- Nhấn **F5** (hoặc `Ctrl + F5`) để biên dịch và khởi chạy ứng dụng.

---

## 🌱 5. Dữ liệu Khởi tạo & Tài khoản Demo (Seed Data)

File [database/schema.sql](database/schema.sql) đã chuẩn bị sẵn đầy đủ dữ liệu để trình bày ngay lập tức:
- **Tài khoản đăng nhập mặc định:**
  | Vai trò | Tên đăng nhập (`Username`) | Mật khẩu (`Password`) | Quyền hạn được cấp |
  | :--- | :--- | :--- | :--- |
  | **Chủ quán / Quản trị viên (`Admin`)** | `admin` | `Admin@123` | Toàn bộ 8 quyền quản trị & vận hành |
  | **Nhân viên Thu ngân (`Staff`)** | `staff01` | `Admin@123` | `TABLE_VIEW`, `SESSION_OPEN`, `SESSION_CLOSE`, `INVOICE_PAY` |
- **Danh mục bàn mẫu:** 6 bàn thuộc 3 loại (`P01`, `P02`, `P03` - Bàn Pool 9 feet; `C01`, `C02` - Bàn Carom Libre; `3B01` - Bàn 3 Băng).
- **Quy tắc giá & Khuyến mãi mẫu:** Giá ngày thường, giá giờ cao điểm tối (`18:00 - 23:59`), giá cuối tuần và Khuyến mãi Giờ Vàng buổi chiều (`-15%` từ `13:00 - 17:00`).
- **Thực đơn F&B mẫu:** 3 danh mục (`Nước giải khát`, `Đồ ăn nhanh`, `Thuốc lá & Khác`) với 7 sản phẩm phổ biến.
- **Lịch sử phiên & Hóa đơn mẫu:** Phiên chơi hoàn tất kèm hóa đơn đã thanh toán `INV-DEMO-0001` để đối soát ngay trên màn hình **Hóa đơn** và **Báo cáo Doanh thu**.

---

## 🎬 6. Kịch bản Trình bày & Demo Đánh giá Cuối kỳ

Kịch bản dưới đây giúp trình bày trọn vẹn toàn bộ luồng nghiệp vụ trong **5 – 7 phút**:

1. **Bước 1 — Đăng nhập & Kiểm chứng Phân quyền RBAC:**
   - Đăng nhập bằng tài khoản `staff01` / `Admin@123` -> Quan sát thanh điều hướng chỉ hiển thị các chức năng vận hành được cấp phép (`Sơ đồ & Vận hành Bàn`, `Hóa đơn & Lịch sử Phiên`).
   - Đăng xuất và đăng nhập lại bằng tài khoản `admin` / `Admin@123` -> Mở khóa toàn bộ 5 phân hệ.

2. **Bước 2 — Mở bàn & Quản lý Phiên chơi Thời gian thực:**
   - Tại màn hình **Sơ đồ & Vận hành Bàn**, chọn bàn trống `P02`.
   - Nhập tên khách hàng (*"Anh Minh"*), SĐT (*"0912345678"*) và bấm **BẮT ĐẦU TÍNH GIỜ**.
   - Quan sát bàn chuyển sang trạng thái **Đang chơi (Occupied)**, đồng hồ bắt đầu đếm từ `00:00:00` (không bị lệch múi giờ).
   - Thử tính năng **Chuyển bàn** từ `P02` sang `P03`.

3. **Bước 3 — Thêm Sản phẩm / Dịch vụ vào Phiên chơi:**
   - Trên panel chi tiết bàn `P03`, chọn sản phẩm **Bò Húc (RedBull)** với số lượng `2` và bấm **Thêm**, hoặc bấm trực tiếp vào thẻ món nhanh.
   - Dùng nút `+` / `-` để điều chỉnh số lượng hoặc bấm nút thùng rác để thử hộp thoại xác nhận xóa món.

4. **Bước 4 — Kiểm tra Bảng tách Khung giờ & Thanh toán Hóa đơn:**
   - Mở mục **Chi tiết tính tiền giờ bàn (Tách theo khung giờ)** trên panel phải để xem động cơ `PricingRuleService` tự động tách khoảng thời gian và áp dụng quy tắc giá.
   - Chọn phương thức thanh toán **Tiền mặt (`Cash`)** hoặc **Chuyển khoản (`BankTransfer`)**, nhập giảm giá `5000` VNĐ.
   - Bấm **THANH TOÁN & TRẢ BÀN** -> Kiểm tra hộp thoại xác nhận tổng kết hóa đơn -> Xác nhận thanh toán -> Bàn tự động trở về trạng thái **Bàn trống (`Available`)**.

5. **Bước 5 — Tra cứu Hóa đơn & Đối soát Báo cáo Doanh thu:**
   - Chuyển sang màn hình **Hóa đơn & Lịch sử Phiên**: Chọn hóa đơn vừa thanh toán để xem bảng `InvoiceDetails` (tách rõ dòng tiền bàn và từng dòng sản phẩm).
   - Chuyển sang màn hình **Báo cáo & Thống kê**: Bấm **Hôm nay** -> Đối soát Tổng doanh thu thực thu, tỷ trọng Tiền bàn / Dịch vụ, biểu đồ doanh thu theo ngày, hiệu suất bàn và bảng hóa đơn đối soát.

6. **Bước 6 — Kiểm toán Hệ thống (`AuditLogs`) & Chạy Kiểm thử Tích hợp Tự động:**
   - Chuyển sang màn hình **Nhân sự & Hệ thống**:
     - Mở Tab **🛡️ NHẬT KÝ HỆ THỐNG (AUDIT LOGS)** để xem toàn bộ lịch sử đăng nhập, mở bàn, thanh toán và thay đổi dữ liệu trước/sau (`OldValues` / `NewValues`).
     - Mở Tab **🧪 KIỂM THỬ TÍCH HỢP & THEO DÕI LỖI** và bấm **CHẠY KIỂM THỬ TÍCH HỢP TOÀN HỆ THỐNG** để trình diễn 5 bộ kịch bản kiểm thử tự động chạy trực tiếp và báo cáo `PASS 100%`.

---

## 🤝 7. Phân công & Nắm vững Kiến trúc Giữa Hai Thành viên

Cả hai thành viên cùng nắm vững kiến trúc **MVVM + Service Layer + MySQL Transactions** và có thể trình bày chéo mọi luồng nghiệp vụ:

| Hạng mục / Phân hệ | Thành viên 1 (Backend, Core Billing & Security) | Thành viên 2 (WPF UI/UX, Catalog, Reports & Testing) |
| :--- | :--- | :--- |
| **Kiến trúc & Cơ sở dữ liệu** | Thiết kế 15 bảng MySQL, cấu hình bảo mật `connections.config`, đồng bộ múi giờ `+07:00` trong `DatabaseHelper`. | Xây dựng cấu trúc MVVM (`BaseViewModel`, `RelayCommand`, `AsyncRelayCommand`), `Converters` và điều hướng `MainWindow`. |
| **Xác thực, Nhân sự & RBAC** | `AuthService`, `UserSession`, kiểm soát phân quyền 2 lớp (UI + Service), `UserService` & `AuditLogService`. | Giao diện `LoginWindow`, `StaffManagementView` (Quản lý nhân sự, Ma trận phân quyền RBAC, Xem `AuditLogs`). |
| **Vận hành Bàn & Tính tiền** | `TableService`, `PricingRuleService` (thuật toán tách khung giờ `CalculateTimeSlotSegments`), `OrderService` (`Invoices` & `PayInvoiceAsync`). | `TableManagementView` (Sơ đồ bàn thời gian thực, CRUD Bàn/Loại bàn/Bảng giá, Panel gọi món & Thanh toán). |
| **Sản phẩm, Báo cáo & Kiểm thử** | `ProductService` (bảo toàn giá lịch sử), `ReportService` (truy vấn đối soát doanh thu), `SystemIntegrationTestService`. | `ProductManagementView` + `CloudinaryService`, `InvoiceManagementView`, `ReportManagementView` & bắt lỗi toàn cục `App.xaml.cs`. |

---

## 📝 Giấy phép (License)
Dự án được phát triển phục vụ học tập và đánh giá học phần dưới giấy phép MIT License.

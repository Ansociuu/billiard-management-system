-- ====================================================================
-- BILLIARD MANAGEMENT SYSTEM - DATABASE SCHEMA (MySQL 8.0+)
-- Target: Aiven MySQL (Cloud Database)
-- Charset: utf8mb4, Collation: utf8mb4_unicode_ci
-- ====================================================================

SET FOREIGN_KEY_CHECKS = 0;

-- --------------------------------------------------------------------
-- NHÓM A — TÀI KHOẢN VÀ PHÂN QUYỀN (AUTHENTICATION & RBAC)
-- --------------------------------------------------------------------

-- 1. Roles (Vai trò người dùng)
DROP TABLE IF EXISTS RolePermissions;
DROP TABLE IF EXISTS Permissions;
DROP TABLE IF EXISTS Employees;
DROP TABLE IF EXISTS AuditLogs;
DROP TABLE IF EXISTS InvoiceDetails;
DROP TABLE IF EXISTS Invoices;
DROP TABLE IF EXISTS SessionProducts;
DROP TABLE IF EXISTS SessionPricingDetails;
DROP TABLE IF EXISTS Sessions;
DROP TABLE IF EXISTS TablePricingRules;
DROP TABLE IF EXISTS Tables;
DROP TABLE IF EXISTS TableTypes;
DROP TABLE IF EXISTS Products;
DROP TABLE IF EXISTS ProductCategories;
DROP TABLE IF EXISTS Customers;
DROP TABLE IF EXISTS Users;
DROP TABLE IF EXISTS Roles;

CREATE TABLE Roles (
    RoleId INT AUTO_INCREMENT PRIMARY KEY,
    RoleName VARCHAR(30) NOT NULL UNIQUE,
    Description VARCHAR(255) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. Users (Tài khoản đăng nhập)
CREATE TABLE Users (
    UserId BIGINT AUTO_INCREMENT PRIMARY KEY,
    RoleId INT NOT NULL,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    FullName VARCHAR(100) NOT NULL,
    Phone VARCHAR(20) NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Permissions (Danh mục quyền)
CREATE TABLE Permissions (
    PermissionId INT AUTO_INCREMENT PRIMARY KEY,
    PermissionCode VARCHAR(50) NOT NULL UNIQUE,
    PermissionName VARCHAR(100) NOT NULL,
    Description VARCHAR(255) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. RolePermissions (Phân quyền cho vai trò)
CREATE TABLE RolePermissions (
    RoleId INT NOT NULL,
    PermissionId INT NOT NULL,
    PRIMARY KEY (RoleId, PermissionId),
    CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId) ON DELETE CASCADE,
    CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES Permissions(PermissionId) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------------------
-- NHÓM B — QUẢN LÝ BÀN BILLIARD (TABLE MANAGEMENT)
-- --------------------------------------------------------------------

-- 5. TableTypes (Loại bàn chơi)
CREATE TABLE TableTypes (
    TableTypeId INT AUTO_INCREMENT PRIMARY KEY,
    TypeName VARCHAR(50) NOT NULL UNIQUE,
    Description VARCHAR(255) NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 6. Tables (Bàn chơi)
CREATE TABLE Tables (
    TableId INT AUTO_INCREMENT PRIMARY KEY,
    TableTypeId INT NOT NULL,
    TableNumber VARCHAR(20) NOT NULL UNIQUE,
    Status VARCHAR(20) NOT NULL DEFAULT 'Available',
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT FK_Tables_TableTypes FOREIGN KEY (TableTypeId) REFERENCES TableTypes(TableTypeId),
    CONSTRAINT CHK_Tables_Status CHECK (Status IN ('Available', 'Occupied', 'Maintenance'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------------------
-- NHÓM C — SẢN PHẨM VÀ DỊCH VỤ (PRODUCTS & F&B)
-- --------------------------------------------------------------------

-- 7. ProductCategories (Danh mục sản phẩm/dịch vụ)
CREATE TABLE ProductCategories (
    CategoryId INT AUTO_INCREMENT PRIMARY KEY,
    CategoryName VARCHAR(100) NOT NULL UNIQUE,
    Description VARCHAR(255) NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 8. Products (Sản phẩm kinh doanh F&B)
CREATE TABLE Products (
    ProductId INT AUTO_INCREMENT PRIMARY KEY,
    CategoryId INT NOT NULL,
    ProductName VARCHAR(150) NOT NULL,
    Unit VARCHAR(30) NOT NULL,
    Price DECIMAL(12,2) NOT NULL,
    ImageUrl VARCHAR(2048) NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT FK_Products_ProductCategories FOREIGN KEY (CategoryId) REFERENCES ProductCategories(CategoryId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------------------
-- NHÓM G — KHÁCH HÀNG VÀ NHÂN VIÊN (CUSTOMERS & EMPLOYEES)
-- (Tạo trước để Sessions có thể tham chiếu CustomerId)
-- --------------------------------------------------------------------

-- 15. Customers (Khách hàng)
CREATE TABLE Customers (
    CustomerId BIGINT AUTO_INCREMENT PRIMARY KEY,
    FullName VARCHAR(100) NOT NULL,
    Phone VARCHAR(20) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    INDEX IX_Customers_Phone (Phone)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 16. Employees (Hồ sơ nhân sự)
CREATE TABLE Employees (
    EmployeeId BIGINT AUTO_INCREMENT PRIMARY KEY,
    UserId BIGINT NOT NULL UNIQUE,
    Email VARCHAR(100) NULL,
    Address VARCHAR(255) NULL,
    HireDate DATE NULL,
    Position VARCHAR(50) NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT FK_Employees_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------------------
-- NHÓM D — PHIÊN CHƠI VÀ SẢN PHẨM KHÁCH GỌI (SESSIONS & ORDERING)
-- --------------------------------------------------------------------

-- 9. Sessions (Phiên chơi tại bàn)
CREATE TABLE Sessions (
    SessionId BIGINT AUTO_INCREMENT PRIMARY KEY,
    TableId INT NOT NULL,
    UserId BIGINT NOT NULL,
    CustomerId BIGINT NULL,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Active',
    TableAmount DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    CONSTRAINT FK_Sessions_Tables FOREIGN KEY (TableId) REFERENCES Tables(TableId),
    CONSTRAINT FK_Sessions_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_Sessions_Customers FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
    CONSTRAINT CHK_Sessions_Status CHECK (Status IN ('Active', 'Completed', 'Cancelled')),
    INDEX IX_Sessions_Status (Status),
    INDEX IX_Sessions_TableId (TableId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 10. SessionProducts (Sản phẩm gọi trong phiên)
CREATE TABLE SessionProducts (
    SessionProductId BIGINT AUTO_INCREMENT PRIMARY KEY,
    SessionId BIGINT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(12,2) NOT NULL,
    Amount DECIMAL(12,2) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_SessionProducts_Sessions FOREIGN KEY (SessionId) REFERENCES Sessions(SessionId) ON DELETE CASCADE,
    CONSTRAINT FK_SessionProducts_Products FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
    CONSTRAINT CHK_SessionProducts_Quantity CHECK (Quantity > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------------------
-- NHÓM E — HÓA ĐƠN VÀ THANH TOÁN (INVOICES & PAYMENTS)
-- --------------------------------------------------------------------

-- 11. Invoices (Hóa đơn thanh toán)
CREATE TABLE Invoices (
    InvoiceId BIGINT AUTO_INCREMENT PRIMARY KEY,
    InvoiceCode VARCHAR(30) NOT NULL UNIQUE,
    SessionId BIGINT NOT NULL UNIQUE,
    UserId BIGINT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    TableAmount DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    ServiceAmount DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    DiscountAmount DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    TotalAmount DECIMAL(12,2) NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Pending',
    PaymentMethod VARCHAR(20) NULL,
    PaidAt DATETIME NULL,
    CONSTRAINT FK_Invoices_Sessions FOREIGN KEY (SessionId) REFERENCES Sessions(SessionId),
    CONSTRAINT FK_Invoices_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT CHK_Invoices_Status CHECK (Status IN ('Pending', 'Paid', 'Cancelled')),
    CONSTRAINT CHK_Invoices_PaymentMethod CHECK (PaymentMethod IS NULL OR PaymentMethod IN ('Cash', 'BankTransfer')),
    INDEX IX_Invoices_CreatedAt (CreatedAt),
    INDEX IX_Invoices_Status (Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 12. InvoiceDetails (Dòng chi tiết hóa đơn: tiền bàn & sản phẩm)
CREATE TABLE InvoiceDetails (
    InvoiceDetailId BIGINT AUTO_INCREMENT PRIMARY KEY,
    InvoiceId BIGINT NOT NULL,
    ItemType VARCHAR(20) NOT NULL,
    ProductId INT NULL,
    Description VARCHAR(255) NOT NULL,
    Quantity DECIMAL(10,2) NOT NULL,
    UnitPrice DECIMAL(12,2) NOT NULL,
    Amount DECIMAL(12,2) NOT NULL,
    CONSTRAINT FK_InvoiceDetails_Invoices FOREIGN KEY (InvoiceId) REFERENCES Invoices(InvoiceId) ON DELETE CASCADE,
    CONSTRAINT FK_InvoiceDetails_Products FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
    CONSTRAINT CHK_InvoiceDetails_ItemType CHECK (ItemType IN ('Table', 'Product'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------------------
-- NHÓM F — QUY TẮC GIÁ VÀ TÍNH TIỀN BÀN (PRICING RULES & ENGINE)
-- --------------------------------------------------------------------

-- 13. TablePricingRules (Bảng quy tắc giá thuê bàn và khuyến mãi)
CREATE TABLE TablePricingRules (
    PricingRuleId BIGINT AUTO_INCREMENT PRIMARY KEY,
    TableTypeId INT NOT NULL,
    RuleName VARCHAR(100) NOT NULL,
    RuleType VARCHAR(20) NOT NULL,
    DayType VARCHAR(20) NOT NULL,
    StartTime TIME NULL,
    EndTime TIME NULL,
    HourlyRate DECIMAL(12,2) NULL,
    DiscountType VARCHAR(20) NULL,
    DiscountValue DECIMAL(12,2) NULL,
    EffectiveFrom DATE NOT NULL,
    EffectiveTo DATE NULL,
    Priority INT NOT NULL DEFAULT 1,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_PricingRules_TableTypes FOREIGN KEY (TableTypeId) REFERENCES TableTypes(TableTypeId),
    CONSTRAINT CHK_PricingRules_RuleType CHECK (RuleType IN ('BaseRate', 'Promotion')),
    CONSTRAINT CHK_PricingRules_DayType CHECK (DayType IN ('Weekday', 'Weekend', 'Holiday', 'Any')),
    CONSTRAINT CHK_PricingRules_DiscountType CHECK (DiscountType IS NULL OR DiscountType IN ('Percentage', 'FixedAmount'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 14. SessionPricingDetails (Chi tiết các khoảng thời gian tính tiền bàn)
CREATE TABLE SessionPricingDetails (
    SessionPricingDetailId BIGINT AUTO_INCREMENT PRIMARY KEY,
    SessionId BIGINT NOT NULL,
    TableId INT NOT NULL,
    BasePricingRuleId BIGINT NOT NULL,
    PromotionPricingRuleId BIGINT NULL,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NOT NULL,
    DurationMinutes DECIMAL(10,2) NOT NULL,
    BaseHourlyRate DECIMAL(12,2) NOT NULL,
    DiscountType VARCHAR(20) NULL,
    DiscountValue DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    FinalHourlyRate DECIMAL(12,2) NOT NULL,
    Amount DECIMAL(12,2) NOT NULL,
    CONSTRAINT FK_SessionPricing_Sessions FOREIGN KEY (SessionId) REFERENCES Sessions(SessionId) ON DELETE CASCADE,
    CONSTRAINT FK_SessionPricing_Tables FOREIGN KEY (TableId) REFERENCES Tables(TableId),
    CONSTRAINT FK_SessionPricing_BaseRule FOREIGN KEY (BasePricingRuleId) REFERENCES TablePricingRules(PricingRuleId),
    CONSTRAINT FK_SessionPricing_PromoRule FOREIGN KEY (PromotionPricingRuleId) REFERENCES TablePricingRules(PricingRuleId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------------------
-- NHÓM H — NHẬT KÝ THAO TÁC (AUDIT LOGS)
-- --------------------------------------------------------------------

-- 17. AuditLogs (Nhật ký truy vết thao tác hệ thống)
CREATE TABLE AuditLogs (
    AuditLogId BIGINT AUTO_INCREMENT PRIMARY KEY,
    UserId BIGINT NULL,
    Action VARCHAR(50) NOT NULL,
    EntityType VARCHAR(50) NOT NULL,
    EntityId BIGINT NULL,
    Description VARCHAR(500) NULL,
    OldValues JSON NULL,
    NewValues JSON NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
    INDEX IX_AuditLogs_CreatedAt (CreatedAt),
    INDEX IX_AuditLogs_Action (Action)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

-- ====================================================================
-- SEED DATA - DỮ LIỆU KHỞI TẠO BAN ĐẦU
-- ====================================================================

-- 1. Roles
INSERT INTO Roles (RoleId, RoleName, Description) VALUES
(1, 'Admin', 'Chủ quán - Toàn quyền quản trị hệ thống'),
(2, 'Staff', 'Nhân viên - Thực hiện nghiệp vụ mở bàn, bán hàng, thanh toán');

-- 2. Permissions
INSERT INTO Permissions (PermissionId, PermissionCode, PermissionName, Description) VALUES
(1, 'TABLE_VIEW', 'Xem danh sách bàn', 'Cho phép xem sơ đồ và trạng thái bàn'),
(2, 'SESSION_OPEN', 'Mở phiên chơi', 'Cho phép mở bàn cho khách'),
(3, 'SESSION_CLOSE', 'Kết thúc phiên chơi', 'Cho phép chốt giờ và dừng phiên'),
(4, 'PRODUCT_MANAGE', 'Quản lý sản phẩm', 'Thêm, sửa, cập nhật giá F&B'),
(5, 'INVOICE_PAY', 'Thanh toán hóa đơn', 'Thu tiền và xác nhận hóa đơn đã thanh toán'),
(6, 'REPORT_VIEW', 'Xem báo cáo doanh thu', 'Xem báo cáo tài chính, doanh thu theo ca/ngày'),
(7, 'USER_MANAGE', 'Quản lý người dùng', 'Thêm mới, khóa tài khoản nhân viên'),
(8, 'ROLE_MANAGE', 'Quản lý phân quyền', 'Cấu hình quyền cho các vai trò');

-- 3. RolePermissions
-- Admin: Toàn bộ 8 quyền
INSERT INTO RolePermissions (RoleId, PermissionId) VALUES
(1, 1), (1, 2), (1, 3), (1, 4), (1, 5), (1, 6), (1, 7), (1, 8);

-- Staff: Các quyền nghiệp vụ bàn, gọi món và thanh toán
INSERT INTO RolePermissions (RoleId, PermissionId) VALUES
(2, 1), (2, 2), (2, 3), (2, 5);

-- 4. Initial Users (Mật khẩu mặc định: 'Admin@123' băm bằng BCrypt)
-- Hash BCrypt cho 'Admin@123': $2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy
INSERT INTO Users (UserId, RoleId, Username, PasswordHash, FullName, Phone, IsActive) VALUES
(1, 1, 'admin', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'Quản Trị Viên (Admin)', '0901234567', TRUE),
(2, 2, 'staff01', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'Nhân Viên Ca 1', '0908765432', TRUE);

-- 5. TableTypes
INSERT INTO TableTypes (TableTypeId, TypeName, Description, IsActive) VALUES
(1, 'Bàn Pool (Lỗ)', 'Bàn bida lỗ tiêu chuẩn 9 feet', TRUE),
(2, 'Bàn Carom (Libre)', 'Bàn bida phăng / tự do tiêu chuẩn', TRUE),
(3, 'Bàn 3 Băng (3-Cushion)', 'Bàn bida 3 băng cao cấp có sưởi', TRUE);

-- 6. Tables
INSERT INTO Tables (TableId, TableTypeId, TableNumber, Status, IsActive) VALUES
(1, 1, 'P01', 'Available', TRUE),
(2, 1, 'P02', 'Available', TRUE),
(3, 1, 'P03', 'Available', TRUE),
(4, 2, 'C01', 'Available', TRUE),
(5, 2, 'C02', 'Available', TRUE),
(6, 3, '3B01', 'Available', TRUE);

-- 7. ProductCategories
INSERT INTO ProductCategories (CategoryId, CategoryName, Description, IsActive) VALUES
(1, 'Nước giải khát', 'Các loại nước ngọt đóng chai, lon', TRUE),
(2, 'Đồ ăn nhanh', 'Mì tôm, xúc xích, snack', TRUE),
(3, 'Thuốc lá & Khác', 'Thuốc lá, khăn lạnh, bật lửa', TRUE);

-- 8. Products
INSERT INTO Products (ProductId, CategoryId, ProductName, Unit, Price, ImageUrl, IsActive) VALUES
(1, 1, 'Bò Húc (RedBull)', 'Lon', 20000.00, NULL, TRUE),
(2, 1, 'Sting Dâu', 'Chai', 15000.00, NULL, TRUE),
(3, 1, 'Nước Suối Aquafina 500ml', 'Chai', 10000.00, NULL, TRUE),
(4, 1, 'Cà Phê Đen Đá', 'Ly', 18000.00, NULL, TRUE),
(5, 2, 'Mì Tôm Trứng', 'Tô', 25000.00, NULL, TRUE),
(6, 2, 'Xúc Xích Nướng', 'Cây', 15000.00, NULL, TRUE),
(7, 3, 'Khăn Lạnh', 'Cái', 3000.00, NULL, TRUE);

-- 9. TablePricingRules
-- Bàn Pool: Ngày thường 50k/h, Cuối tuần 70k/h
INSERT INTO TablePricingRules (TableTypeId, RuleName, RuleType, DayType, StartTime, EndTime, HourlyRate, DiscountType, DiscountValue, EffectiveFrom, EffectiveTo, Priority, IsActive) VALUES
(1, 'Giá ngày thường Bàn Pool', 'BaseRate', 'Weekday', NULL, NULL, 50000.00, NULL, NULL, '2026-01-01', NULL, 1, TRUE),
(1, 'Giá cuối tuần Bàn Pool', 'BaseRate', 'Weekend', NULL, NULL, 70000.00, NULL, NULL, '2026-01-01', NULL, 2, TRUE),
-- Bàn Carom Libre: Ngày thường 45k/h, Cuối tuần 60k/h
(2, 'Giá ngày thường Bàn Carom', 'BaseRate', 'Weekday', NULL, NULL, 45000.00, NULL, NULL, '2026-01-01', NULL, 1, TRUE),
(2, 'Giá cuối tuần Bàn Carom', 'BaseRate', 'Weekend', NULL, NULL, 60000.00, NULL, NULL, '2026-01-01', NULL, 2, TRUE),
-- Bàn 3 Băng: Giá cố định 80k/h
(3, 'Giá tiêu chuẩn Bàn 3 Băng', 'BaseRate', 'Any', NULL, NULL, 80000.00, NULL, NULL, '2026-01-01', NULL, 1, TRUE);

using System;

namespace billiard_management_system.Models
{
    public class User
    {
        public long UserId { get; set; }
        public int RoleId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        // Navigation / Role Display property
        public string RoleName { get; set; }

        // Thông tin mở rộng từ bảng Employees (Hồ sơ nhân viên)
        public long? EmployeeId { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public DateTime? HireDate { get; set; }
        public string Position { get; set; }

        // Computed Display Properties cho UI
        public string StatusDisplayName => IsActive ? "Đang hoạt động" : "Đã khóa";
        public string PositionDisplay => !string.IsNullOrWhiteSpace(Position)
            ? Position
            : (string.Equals(RoleName, "Admin", StringComparison.OrdinalIgnoreCase) ? "Quản lý / Chủ quán" : "Nhân viên Thu ngân");
        public string HireDateDisplay => HireDate.HasValue ? HireDate.Value.ToString("dd/MM/yyyy") : "Chưa cập nhật";
    }
}

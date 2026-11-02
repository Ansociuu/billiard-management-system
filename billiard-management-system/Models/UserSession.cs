using System;
using System.Collections.Generic;

namespace billiard_management_system.Models
{
    /// <summary>
    /// Thông tin phiên làm việc của người dùng hiện tại sau khi đăng nhập thành công
    /// </summary>
    public class UserSession
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public HashSet<string> Permissions { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool IsAdmin => string.Equals(RoleName, "Admin", StringComparison.OrdinalIgnoreCase);
        public bool IsStaff => string.Equals(RoleName, "Staff", StringComparison.OrdinalIgnoreCase);

        public bool HasPermission(string permissionCode)
        {
            if (IsAdmin) return true;
            return Permissions != null && Permissions.Contains(permissionCode);
        }
    }
}

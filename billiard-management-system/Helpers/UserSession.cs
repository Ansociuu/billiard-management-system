using System;
using System.Collections.Generic;
using billiard_management_system.Models;

namespace billiard_management_system.Helpers
{
    /// <summary>
    /// Quản lý trạng thái phiên đăng nhập toàn cục và phân quyền người dùng (Session & RBAC)
    /// </summary>
    public static class UserSession
    {
        public static User CurrentUser { get; private set; }
        public static DateTime LoginTime { get; private set; }
        public static HashSet<string> Permissions { get; private set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static bool IsLoggedIn => CurrentUser != null;
        public static bool IsAdmin => CurrentUser != null && (CurrentUser.RoleName == "Admin" || CurrentUser.RoleId == 1);
        public static bool IsStaff => CurrentUser != null && (CurrentUser.RoleName == "Staff" || CurrentUser.RoleId == 2);

        public static TimeSpan SessionDuration => IsLoggedIn ? (DateTime.Now - LoginTime) : TimeSpan.Zero;

        public static event Action<User> SessionStarted;
        public static event Action SessionEnded;

        /// <summary>
        /// Khởi tạo phiên làm việc khi đăng nhập thành công
        /// </summary>
        public static void SetSession(User user, IEnumerable<string> permissions)
        {
            CurrentUser = user ?? throw new ArgumentNullException(nameof(user));
            LoginTime = DateTime.Now;

            Permissions.Clear();
            if (permissions != null)
            {
                foreach (var p in permissions)
                {
                    Permissions.Add(p);
                }
            }

            SessionStarted?.Invoke(user);
        }

        /// <summary>
        /// Hủy phiên làm việc khi đăng xuất, giải phóng thông tin nhạy cảm trong bộ nhớ
        /// </summary>
        public static void ClearSession()
        {
            CurrentUser = null;
            LoginTime = default;
            Permissions.Clear();

            SessionEnded?.Invoke();
        }

        /// <summary>
        /// Kiểm tra người dùng hiện tại có quyền thực hiện thao tác hay không
        /// </summary>
        /// <param name="permissionCode">Mã quyền (vd: TABLE_VIEW, INVOICE_PAY, REPORT_VIEW,...)</param>
        public static bool HasPermission(string permissionCode)
        {
            if (CurrentUser == null) return false;
            // Admin có toàn bộ các quyền
            if (IsAdmin) return true;
            if (string.IsNullOrWhiteSpace(permissionCode)) return true;

            return Permissions.Contains(permissionCode);
        }
    }
}

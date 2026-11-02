using System;
using System.Collections.Generic;
using billiard_management_system.Models;

namespace billiard_management_system.Helpers
{
    /// <summary>
    /// Lưu trữ phiên đăng nhập và phân quyền của người dùng hiện tại
    /// </summary>
    public static class UserSession
    {
        public static User CurrentUser { get; private set; }
        public static HashSet<string> Permissions { get; private set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static bool IsLoggedIn => CurrentUser != null;

        public static void SetSession(User user, IEnumerable<string> permissions)
        {
            CurrentUser = user;
            Permissions.Clear();
            if (permissions != null)
            {
                foreach (var p in permissions)
                {
                    Permissions.Add(p);
                }
            }
        }

        public static void ClearSession()
        {
            CurrentUser = null;
            Permissions.Clear();
        }

        public static bool HasPermission(string permissionCode)
        {
            if (CurrentUser == null) return false;
            // Admin có toàn quyền
            if (CurrentUser.RoleName == "Admin" || CurrentUser.RoleId == 1) return true;
            return Permissions.Contains(permissionCode);
        }
    }
}


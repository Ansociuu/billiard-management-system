using System;
using System.Collections.Generic;
using billiard_management_system.Models;

namespace billiard_management_system.Helpers
{
    /// <summary>
    /// Danh mục hằng số mã quyền chuẩn trong hệ thống (RBAC Permission Codes)
    /// </summary>
    public static class PermissionCodes
    {
        public const string TableView = "TABLE_VIEW";
        public const string SessionOpen = "SESSION_OPEN";
        public const string SessionClose = "SESSION_CLOSE";
        public const string ProductManage = "PRODUCT_MANAGE";
        public const string InvoicePay = "INVOICE_PAY";
        public const string ReportView = "REPORT_VIEW";
        public const string UserManage = "USER_MANAGE";
        public const string RoleManage = "ROLE_MANAGE";

        public static string GetDisplayName(string code)
        {
            switch (code?.ToUpperInvariant())
            {
                case TableView: return "Xem danh sách bàn (TABLE_VIEW)";
                case SessionOpen: return "Mở phiên chơi (SESSION_OPEN)";
                case SessionClose: return "Kết thúc phiên chơi (SESSION_CLOSE)";
                case ProductManage: return "Quản lý dịch vụ/sản phẩm (PRODUCT_MANAGE)";
                case InvoicePay: return "Thanh toán hóa đơn (INVOICE_PAY)";
                case ReportView: return "Xem báo cáo doanh thu (REPORT_VIEW)";
                case UserManage: return "Quản lý nhân viên & tài khoản (USER_MANAGE)";
                case RoleManage: return "Quản lý phân quyền (ROLE_MANAGE)";
                default: return code ?? "Không xác định";
            }
        }
    }

    /// <summary>
    /// Quản lý trạng thái phiên đăng nhập toàn cục và phân quyền người dùng (Session & RBAC)
    /// </summary>
    public static class UserSession
    {
        public static User CurrentUser { get; private set; }
        public static DateTime LoginTime { get; private set; }
        public static HashSet<string> Permissions { get; private set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static bool IsLoggedIn => CurrentUser != null;
        public static bool IsAdmin => CurrentUser != null &&
            (string.Equals(CurrentUser.RoleName, "Admin", StringComparison.OrdinalIgnoreCase) || CurrentUser.RoleId == 1);
        public static bool IsStaff => CurrentUser != null &&
            (string.Equals(CurrentUser.RoleName, "Staff", StringComparison.OrdinalIgnoreCase) || CurrentUser.RoleId == 2);

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
                    if (!string.IsNullOrWhiteSpace(p))
                    {
                        Permissions.Add(p.Trim());
                    }
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
        /// Thay đổi tạm thời ngữ cảnh phiên làm việc (phục vụ kiểm thử tích hợp RBAC mà không kích hoạt sự kiện chuyển màn hình UI)
        /// </summary>
        public static void ReplaceSessionSilently(User user, IEnumerable<string> permissions)
        {
            CurrentUser = user;
            Permissions.Clear();
            if (permissions != null)
            {
                foreach (var p in permissions)
                {
                    if (!string.IsNullOrWhiteSpace(p))
                    {
                        Permissions.Add(p.Trim());
                    }
                }
            }
        }

        /// <summary>
        /// Kiểm tra người dùng hiện tại có quyền thực hiện thao tác hay không
        /// </summary>
        /// <param name="permissionCode">Mã quyền (vd: TABLE_VIEW, INVOICE_PAY, REPORT_VIEW,...)</param>
        public static bool HasPermission(string permissionCode)
        {
            if (CurrentUser == null || !CurrentUser.IsActive) return false;
            // Admin có toàn bộ các quyền quản trị và vận hành
            if (IsAdmin) return true;
            if (string.IsNullOrWhiteSpace(permissionCode)) return true;

            return Permissions.Contains(permissionCode.Trim());
        }

        /// <summary>
        /// Kiểm tra quyền truy cập và trả về thông báo lỗi chuẩn nếu không đủ quyền
        /// </summary>
        public static (bool Allowed, string Message) CheckAccess(string permissionCode, string actionDescription = null)
        {
            if (!IsLoggedIn)
            {
                return (false, "Từ chối truy cập: Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.");
            }

            if (!CurrentUser.IsActive)
            {
                return (false, "Từ chối truy cập: Tài khoản hiện tại đã bị vô hiệu hóa.");
            }

            if (HasPermission(permissionCode))
            {
                return (true, string.Empty);
            }

            string permDisplay = PermissionCodes.GetDisplayName(permissionCode);
            string targetAction = string.IsNullOrWhiteSpace(actionDescription)
                ? $"quyền [{permDisplay}]"
                : $"thao tác \"{actionDescription}\" (yêu cầu quyền {permDisplay})";

            return (false, $"Từ chối truy cập: Tài khoản [{CurrentUser.Username}] ({CurrentUser.RoleName}) không được cấp phép thực hiện {targetAction}.");
        }

        /// <summary>
        /// Bắt buộc người dùng hiện tại phải có quyền được chỉ định, ném ngoại lệ nếu bị từ chối
        /// </summary>
        public static void DemandPermission(string permissionCode, string actionDescription = null)
        {
            var (allowed, message) = CheckAccess(permissionCode, actionDescription);
            if (!allowed)
            {
                throw new UnauthorizedAccessException(message);
            }
        }
    }
}

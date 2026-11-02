using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using billiard_management_system.Helpers;
using billiard_management_system.Models;
using MySqlConnector;

namespace billiard_management_system.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, User User)> LoginAsync(string username, string password);
        Task LogoutAsync(long? userId = null);
        Task<List<string>> GetUserPermissionsAsync(int roleId);
    }

    public class AuthService : IAuthService
    {
        private readonly IAuditLogService _auditLogService;

        public AuthService(IAuditLogService auditLogService = null)
        {
            _auditLogService = auditLogService ?? new AuditLogService();
        }

        public async Task<(bool Success, string Message, User User)> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.", null);
            }

            string cleanUsername = username.Trim();

            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    // Truy vấn tài khoản (không giới hạn IsActive ở WHERE để có thông báo lỗi cụ thể cho người dùng)
                    const string query = @"
                        SELECT u.UserId, u.RoleId, u.Username, u.PasswordHash, u.FullName, u.Phone, u.IsActive, u.CreatedAt,
                               r.RoleName
                        FROM Users u
                        INNER JOIN Roles r ON u.RoleId = r.RoleId
                        WHERE u.Username = @Username
                        LIMIT 1;";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", cleanUsername);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                await _auditLogService.LogAsync(
                                    null, "LOGIN_FAILED", "User", null,
                                    $"Đăng nhập thất bại: Tài khoản '{cleanUsername}' không tồn tại trên hệ thống.");
                                return (false, "Tài khoản không tồn tại trên hệ thống.", null);
                            }

                            var user = new User
                            {
                                UserId = reader.GetInt64("UserId"),
                                RoleId = reader.GetInt32("RoleId"),
                                Username = reader.GetString("Username"),
                                PasswordHash = reader.GetString("PasswordHash"),
                                FullName = reader.GetString("FullName"),
                                Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString("Phone"),
                                IsActive = reader.GetBoolean("IsActive"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                RoleName = reader.GetString("RoleName")
                            };

                            // Kiểm tra trạng thái khóa tài khoản
                            if (!user.IsActive)
                            {
                                await _auditLogService.LogAsync(
                                    user.UserId, "LOGIN_LOCKED", "User", user.UserId,
                                    $"Đăng nhập bị từ chối: Tài khoản '{cleanUsername}' hiện đang bị vô hiệu hóa.");
                                return (false, "Tài khoản này đã bị tạm khóa. Vui lòng liên hệ Quản trị viên để được hỗ trợ.", null);
                            }

                            // Xác thực mật khẩu bằng thuật toán BCrypt
                            bool passwordValid = false;
                            try
                            {
                                passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[BCrypt Verify Error]: {ex.Message}");
                                passwordValid = false;
                            }

                            if (!passwordValid)
                            {
                                await _auditLogService.LogAsync(
                                    user.UserId, "LOGIN_WRONG_PASSWORD", "User", user.UserId,
                                    $"Đăng nhập thất bại: Sai mật khẩu cho tài khoản '{cleanUsername}'.");
                                return (false, "Mật khẩu không chính xác. Vui lòng kiểm tra lại phím CapsLock.", null);
                            }

                            reader.Close();

                            // Lấy danh sách quyền hạn thực tế của Role
                            var permissions = await GetUserPermissionsInternalAsync(conn, user.RoleId);

                            // Khởi tạo phiên đăng nhập toàn cục
                            UserSession.SetSession(user, permissions);

                            // Ghi vết đăng nhập thành công vào AuditLogs
                            await _auditLogService.LogAsync(
                                user.UserId, "LOGIN_SUCCESS", "User", user.UserId,
                                $"Đăng nhập thành công với vai trò '{user.RoleName}'. Máy trạm: {Environment.MachineName}");

                            return (true, "Đăng nhập thành công!", user);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối cơ sở dữ liệu: {ex.Message}", null);
            }
        }

        public async Task LogoutAsync(long? userId = null)
        {
            long? targetUserId = userId ?? UserSession.CurrentUser?.UserId;
            string username = UserSession.CurrentUser?.Username ?? "Unknown";

            if (targetUserId.HasValue)
            {
                await _auditLogService.LogAsync(
                    targetUserId.Value, "LOGOUT", "User", targetUserId.Value,
                    $"Người dùng '{username}' đã đăng xuất khỏi hệ thống.");
            }

            UserSession.ClearSession();
        }

        public async Task<List<string>> GetUserPermissionsAsync(int roleId)
        {
            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                return await GetUserPermissionsInternalAsync(conn, roleId);
            }
        }

        private async Task<List<string>> GetUserPermissionsInternalAsync(MySqlConnection conn, int roleId)
        {
            var permissions = new List<string>();
            const string query = @"
                SELECT p.PermissionCode
                FROM RolePermissions rp
                INNER JOIN Permissions p ON rp.PermissionId = p.PermissionId
                WHERE rp.RoleId = @RoleId;";

            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@RoleId", roleId);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        permissions.Add(reader.GetString("PermissionCode"));
                    }
                }
            }
            return permissions;
        }
    }
}

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
        Task<List<string>> GetUserPermissionsAsync(int roleId);
    }

    public class AuthService : IAuthService
    {
        public async Task<(bool Success, string Message, User User)> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "Vui lòng nhập tên đăng nhập và mật khẩu.", null);
            }

            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    const string query = @"
                        SELECT u.UserId, u.RoleId, u.Username, u.PasswordHash, u.FullName, u.Phone, u.IsActive, u.CreatedAt,
                               r.RoleName
                        FROM Users u
                        INNER JOIN Roles r ON u.RoleId = r.RoleId
                        WHERE u.Username = @Username AND u.IsActive = TRUE
                        LIMIT 1;";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username.Trim());

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                return (false, "Tài khoản không tồn tại hoặc đã bị khóa.", null);
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

                            // So khớp mật khẩu bằng BCrypt
                            bool passwordValid = false;
                            try
                            {
                                passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                            }
                            catch
                            {
                                // Fallback cho trường hợp mật khẩu test dạng plain-text
                                passwordValid = (password == user.PasswordHash);
                            }

                            if (!passwordValid)
                            {
                                return (false, "Mật khẩu không chính xác.", null);
                            }

                            reader.Close();

                            // Lấy danh sách quyền của Role
                            var permissions = await GetUserPermissionsInternalAsync(conn, user.RoleId);

                            // Lưu thông tin phiên đăng nhập
                            UserSession.SetSession(user, permissions);

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


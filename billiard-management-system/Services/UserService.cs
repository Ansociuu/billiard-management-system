using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using billiard_management_system.Helpers;
using billiard_management_system.Models;
using MySqlConnector;

namespace billiard_management_system.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersWithProfileAsync();
        Task<bool> IsUsernameExistsAsync(string username, long? excludeUserId = null);
        Task<(bool HasData, int SessionCount, int InvoiceCount)> CheckBusinessDataAsync(long userId);
        Task<(bool Success, string Message, long UserId)> CreateUserWithProfileAsync(User user, string plainPassword);
        Task<(bool Success, string Message)> UpdateUserWithProfileAsync(User user, string newPlainPassword = null);
        Task<(bool Success, string Message)> ToggleUserActiveStatusAsync(long userId, bool isActive);
        Task<(bool Success, string Message)> DeleteOrDeactivateUserAsync(long userId);
    }

    public class UserService : IUserService
    {
        private static readonly Regex UsernameRegex = new Regex(@"^[a-zA-Z0-9_.-]{3,50}$", RegexOptions.Compiled);
        private static readonly Regex PhoneRegex = new Regex(@"^[0-9+\-\s]{8,20}$", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private readonly IAuditLogService _auditLogService;

        public UserService(IAuditLogService auditLogService = null)
        {
            _auditLogService = auditLogService ?? new AuditLogService();
        }

        public async Task<List<User>> GetAllUsersWithProfileAsync()
        {
            if (UserSession.IsLoggedIn)
            {
                UserSession.DemandPermission(PermissionCodes.UserManage, "Xem danh sách tài khoản và hồ sơ nhân viên");
            }

            var users = new List<User>();

            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                const string sql = @"
                    SELECT 
                        u.UserId, u.RoleId, u.Username, u.FullName, u.Phone, u.IsActive, u.CreatedAt,
                        r.RoleName,
                        e.EmployeeId, e.Email, e.Address, e.HireDate, e.Position
                    FROM Users u
                    INNER JOIN Roles r ON u.RoleId = r.RoleId
                    LEFT JOIN Employees e ON u.UserId = e.UserId
                    ORDER BY u.IsActive DESC, u.RoleId ASC, u.UserId ASC;";

                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        users.Add(new User
                        {
                            UserId = reader.GetInt64("UserId"),
                            RoleId = reader.GetInt32("RoleId"),
                            Username = reader.GetString("Username"),
                            FullName = reader.GetString("FullName"),
                            Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString("Phone"),
                            IsActive = reader.GetBoolean("IsActive"),
                            CreatedAt = reader.GetDateTime("CreatedAt"),
                            RoleName = reader.GetString("RoleName"),
                            EmployeeId = reader.IsDBNull(reader.GetOrdinal("EmployeeId")) ? (long?)null : reader.GetInt64("EmployeeId"),
                            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString("Email"),
                            Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString("Address"),
                            HireDate = reader.IsDBNull(reader.GetOrdinal("HireDate")) ? (DateTime?)null : reader.GetDateTime("HireDate"),
                            Position = reader.IsDBNull(reader.GetOrdinal("Position")) ? null : reader.GetString("Position")
                        });
                    }
                }
            }

            return users;
        }

        public async Task<bool> IsUsernameExistsAsync(string username, long? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;

            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                string sql = "SELECT COUNT(1) FROM Users WHERE LOWER(Username) = LOWER(@Username)";
                if (excludeUserId.HasValue)
                {
                    sql += " AND UserId <> @ExcludeId";
                }

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username.Trim());
                    if (excludeUserId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@ExcludeId", excludeUserId.Value);
                    }

                    var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    return count > 0;
                }
            }
        }

        public async Task<(bool HasData, int SessionCount, int InvoiceCount)> CheckBusinessDataAsync(long userId)
        {
            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                const string sessionSql = "SELECT COUNT(1) FROM Sessions WHERE UserId = @UserId;";
                int sessionCount = 0;
                using (var cmd = new MySqlCommand(sessionSql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    sessionCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                const string invoiceSql = "SELECT COUNT(1) FROM Invoices WHERE UserId = @UserId;";
                int invoiceCount = 0;
                using (var cmd = new MySqlCommand(invoiceSql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    invoiceCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                return (sessionCount > 0 || invoiceCount > 0, sessionCount, invoiceCount);
            }
        }

        public async Task<(bool Success, string Message, long UserId)> CreateUserWithProfileAsync(User user, string plainPassword)
        {
            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.UserManage, "Tạo tài khoản và hồ sơ nhân viên mới");
            if (!allowed)
            {
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "User", null, deniedMsg);
                return (false, deniedMsg, 0);
            }

            if (user == null)
            {
                return (false, "Dữ liệu nhân viên không hợp lệ.", 0);
            }

            // 1. Kiểm tra dữ liệu đầu vào
            string cleanUsername = (user.Username ?? string.Empty).Trim();
            string cleanFullName = (user.FullName ?? string.Empty).Trim();
            string cleanPhone = string.IsNullOrWhiteSpace(user.Phone) ? null : user.Phone.Trim();
            string cleanEmail = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email.Trim();
            string cleanAddress = string.IsNullOrWhiteSpace(user.Address) ? null : user.Address.Trim();
            string cleanPosition = string.IsNullOrWhiteSpace(user.Position) ? null : user.Position.Trim();

            if (string.IsNullOrWhiteSpace(cleanUsername))
            {
                return (false, "Vui lòng nhập tên đăng nhập (Username).", 0);
            }

            if (!UsernameRegex.IsMatch(cleanUsername))
            {
                return (false, "Tên đăng nhập phải từ 3–50 ký tự và chỉ chứa chữ cái, chữ số, dấu chấm (.), gạch dưới (_) hoặc gạch ngang (-).", 0);
            }

            if (string.IsNullOrWhiteSpace(plainPassword) || plainPassword.Length < 6)
            {
                return (false, "Mật khẩu khởi tạo phải có ít nhất 6 ký tự.", 0);
            }

            if (string.IsNullOrWhiteSpace(cleanFullName))
            {
                return (false, "Vui lòng nhập họ và tên nhân viên.", 0);
            }

            if (user.RoleId <= 0)
            {
                return (false, "Vui lòng chọn vai trò cho tài khoản.", 0);
            }

            if (!string.IsNullOrEmpty(cleanPhone) && !PhoneRegex.IsMatch(cleanPhone))
            {
                return (false, "Số điện thoại không hợp lệ (chỉ gồm 8–20 chữ số).", 0);
            }

            if (!string.IsNullOrEmpty(cleanEmail) && !EmailRegex.IsMatch(cleanEmail))
            {
                return (false, "Địa chỉ Email không đúng định dạng.", 0);
            }

            try
            {
                // 2. Kiểm tra trùng Username
                if (await IsUsernameExistsAsync(cleanUsername))
                {
                    return (false, $"Tên đăng nhập '{cleanUsername}' đã tồn tại trên hệ thống. Vui lòng chọn tên khác.", 0);
                }

                // 3. Băm mật khẩu bằng BCrypt
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                using (var trans = await conn.BeginTransactionAsync())
                {
                    // Insert vào bảng Users
                    const string insertUserSql = @"
                        INSERT INTO Users (RoleId, Username, PasswordHash, FullName, Phone, IsActive, CreatedAt)
                        VALUES (@RoleId, @Username, @PasswordHash, @FullName, @Phone, @IsActive, NOW());
                        SELECT LAST_INSERT_ID();";

                    long newUserId;
                    using (var cmdUser = new MySqlCommand(insertUserSql, conn, trans))
                    {
                        cmdUser.Parameters.AddWithValue("@RoleId", user.RoleId);
                        cmdUser.Parameters.AddWithValue("@Username", cleanUsername);
                        cmdUser.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        cmdUser.Parameters.AddWithValue("@FullName", cleanFullName);
                        cmdUser.Parameters.AddWithValue("@Phone", (object)cleanPhone ?? DBNull.Value);
                        cmdUser.Parameters.AddWithValue("@IsActive", user.IsActive);

                        newUserId = Convert.ToInt64(await cmdUser.ExecuteScalarAsync());
                    }

                    // Insert vào bảng Employees
                    const string insertEmpSql = @"
                        INSERT INTO Employees (UserId, Email, Address, HireDate, Position, IsActive)
                        VALUES (@UserId, @Email, @Address, @HireDate, @Position, @IsActive);";

                    using (var cmdEmp = new MySqlCommand(insertEmpSql, conn, trans))
                    {
                        cmdEmp.Parameters.AddWithValue("@UserId", newUserId);
                        cmdEmp.Parameters.AddWithValue("@Email", (object)cleanEmail ?? DBNull.Value);
                        cmdEmp.Parameters.AddWithValue("@Address", (object)cleanAddress ?? DBNull.Value);
                        cmdEmp.Parameters.AddWithValue("@HireDate", user.HireDate.HasValue ? (object)user.HireDate.Value.Date : DBNull.Value);
                        cmdEmp.Parameters.AddWithValue("@Position", (object)cleanPosition ?? DBNull.Value);
                        cmdEmp.Parameters.AddWithValue("@IsActive", user.IsActive);

                        await cmdEmp.ExecuteNonQueryAsync();
                    }

                    await trans.CommitAsync();

                    // Ghi nhật ký AuditLog (không bao giờ lưu mật khẩu hoặc PasswordHash)
                    await _auditLogService.LogAsync(
                        UserSession.CurrentUser?.UserId,
                        "CREATE_USER",
                        "User",
                        newUserId,
                        $"Tạo mới tài khoản '{cleanUsername}' ({cleanFullName}), RoleId={user.RoleId}, Chức vụ='{cleanPosition ?? "Mặc định"}'");

                    return (true, $"Tạo tài khoản và hồ sơ nhân viên [{cleanFullName}] thành công!", newUserId);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi tạo tài khoản nhân viên: {ex.Message}", 0);
            }
        }

        public async Task<(bool Success, string Message)> UpdateUserWithProfileAsync(User user, string newPlainPassword = null)
        {
            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.UserManage, "Cập nhật thông tin tài khoản và hồ sơ nhân viên");
            if (!allowed)
            {
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "User", user?.UserId, deniedMsg);
                return (false, deniedMsg);
            }

            if (user == null || user.UserId <= 0)
            {
                return (false, "Không xác định được tài khoản cần cập nhật.");
            }

            string cleanFullName = (user.FullName ?? string.Empty).Trim();
            string cleanPhone = string.IsNullOrWhiteSpace(user.Phone) ? null : user.Phone.Trim();
            string cleanEmail = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email.Trim();
            string cleanAddress = string.IsNullOrWhiteSpace(user.Address) ? null : user.Address.Trim();
            string cleanPosition = string.IsNullOrWhiteSpace(user.Position) ? null : user.Position.Trim();

            if (string.IsNullOrWhiteSpace(cleanFullName))
            {
                return (false, "Họ và tên nhân viên không được để trống.");
            }

            if (user.RoleId <= 0)
            {
                return (false, "Vai trò người dùng không hợp lệ.");
            }

            if (!string.IsNullOrEmpty(cleanPhone) && !PhoneRegex.IsMatch(cleanPhone))
            {
                return (false, "Số điện thoại không hợp lệ (chỉ gồm 8–20 chữ số).");
            }

            if (!string.IsNullOrEmpty(cleanEmail) && !EmailRegex.IsMatch(cleanEmail))
            {
                return (false, "Địa chỉ Email không đúng định dạng.");
            }

            // Bảo vệ tài khoản đang đăng nhập hoặc Admin gốc (UserId = 1)
            if (UserSession.CurrentUser != null && user.UserId == UserSession.CurrentUser.UserId)
            {
                if (!user.IsActive)
                {
                    return (false, "Bạn không thể tự khóa tài khoản đang đăng nhập hiện tại.");
                }
                if (UserSession.IsAdmin && user.RoleId != 1)
                {
                    return (false, "Không thể tự hạ quyền Quản trị viên của tài khoản đang đăng nhập.");
                }
            }

            if (user.UserId == 1 && (!user.IsActive || user.RoleId != 1))
            {
                return (false, "Tài khoản Quản trị viên gốc (admin) không thể bị khóa hoặc thay đổi vai trò.");
            }

            bool changePassword = !string.IsNullOrWhiteSpace(newPlainPassword);
            if (changePassword && newPlainPassword.Length < 6)
            {
                return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.");
            }

            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                using (var trans = await conn.BeginTransactionAsync())
                {
                    string oldValuesJson = null;
                    const string selectOldUserSql = "SELECT Username, FullName, RoleId, Phone, IsActive FROM Users WHERE UserId = @UserId LIMIT 1;";
                    using (var oldCmd = new MySqlCommand(selectOldUserSql, conn, trans))
                    {
                        oldCmd.Parameters.AddWithValue("@UserId", user.UserId);
                        using (var r = await oldCmd.ExecuteReaderAsync())
                        {
                            if (await r.ReadAsync())
                            {
                                string oldU = (r.GetString("Username") ?? string.Empty).Replace("\"", "\\\"");
                                string oldF = (r.GetString("FullName") ?? string.Empty).Replace("\"", "\\\"");
                                int oldR = r.GetInt32("RoleId");
                                string oldP = r.IsDBNull(r.GetOrdinal("Phone")) ? string.Empty : r.GetString("Phone").Replace("\"", "\\\"");
                                bool oldAct = r.GetBoolean("IsActive");
                                oldValuesJson = $"{{\"Username\":\"{oldU}\",\"FullName\":\"{oldF}\",\"RoleId\":{oldR},\"Phone\":\"{oldP}\",\"IsActive\":{(oldAct ? "true" : "false")}}}";
                            }
                        }
                    }

                    // 1. Cập nhật bảng Users
                    string updateUserSql = changePassword
                        ? @"UPDATE Users 
                            SET RoleId = @RoleId, FullName = @FullName, Phone = @Phone, IsActive = @IsActive, PasswordHash = @PasswordHash
                            WHERE UserId = @UserId;"
                        : @"UPDATE Users 
                            SET RoleId = @RoleId, FullName = @FullName, Phone = @Phone, IsActive = @IsActive
                            WHERE UserId = @UserId;";

                    using (var cmdUser = new MySqlCommand(updateUserSql, conn, trans))
                    {
                        cmdUser.Parameters.AddWithValue("@RoleId", user.RoleId);
                        cmdUser.Parameters.AddWithValue("@FullName", cleanFullName);
                        cmdUser.Parameters.AddWithValue("@Phone", (object)cleanPhone ?? DBNull.Value);
                        cmdUser.Parameters.AddWithValue("@IsActive", user.IsActive);
                        cmdUser.Parameters.AddWithValue("@UserId", user.UserId);

                        if (changePassword)
                        {
                            string passwordHash = BCrypt.Net.BCrypt.HashPassword(newPlainPassword);
                            cmdUser.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        }

                        await cmdUser.ExecuteNonQueryAsync();
                    }

                    // 2. Upsert vào bảng Employees
                    const string upsertEmpSql = @"
                        INSERT INTO Employees (UserId, Email, Address, HireDate, Position, IsActive)
                        VALUES (@UserId, @Email, @Address, @HireDate, @Position, @IsActive)
                        ON DUPLICATE KEY UPDATE
                            Email = VALUES(Email),
                            Address = VALUES(Address),
                            HireDate = VALUES(HireDate),
                            Position = VALUES(Position),
                            IsActive = VALUES(IsActive);";

                    using (var cmdEmp = new MySqlCommand(upsertEmpSql, conn, trans))
                    {
                        cmdEmp.Parameters.AddWithValue("@UserId", user.UserId);
                        cmdEmp.Parameters.AddWithValue("@Email", (object)cleanEmail ?? DBNull.Value);
                        cmdEmp.Parameters.AddWithValue("@Address", (object)cleanAddress ?? DBNull.Value);
                        cmdEmp.Parameters.AddWithValue("@HireDate", user.HireDate.HasValue ? (object)user.HireDate.Value.Date : DBNull.Value);
                        cmdEmp.Parameters.AddWithValue("@Position", (object)cleanPosition ?? DBNull.Value);
                        cmdEmp.Parameters.AddWithValue("@IsActive", user.IsActive);

                        await cmdEmp.ExecuteNonQueryAsync();
                    }

                    await trans.CommitAsync();

                    // Tuyệt đối không ghi mật khẩu hoặc PasswordHash vào NewValues
                    string newValuesJson = $"{{\"Username\":\"{(user.Username ?? string.Empty).Replace("\"", "\\\"")}\",\"FullName\":\"{cleanFullName.Replace("\"", "\\\"")}\",\"RoleId\":{user.RoleId},\"Phone\":\"{(cleanPhone ?? string.Empty).Replace("\"", "\\\"")}\",\"Position\":\"{(cleanPosition ?? string.Empty).Replace("\"", "\\\"")}\",\"IsActive\":{(user.IsActive ? "true" : "false")},\"PasswordReset\":{(changePassword ? "true" : "false")}}}";

                    string pwdNote = changePassword ? " (có đặt lại mật khẩu)" : string.Empty;
                    await _auditLogService.LogAsync(
                        UserSession.CurrentUser?.UserId,
                        "UPDATE_USER",
                        "User",
                        user.UserId,
                        $"Cập nhật hồ sơ nhân viên '{user.Username}' ({cleanFullName}), RoleId={user.RoleId}, IsActive={user.IsActive}{pwdNote}",
                        oldValuesJson,
                        newValuesJson);

                    return (true, $"Cập nhật thông tin nhân viên [{cleanFullName}] thành công!");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi cập nhật thông tin nhân viên: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ToggleUserActiveStatusAsync(long userId, bool isActive)
        {
            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.UserManage, "Kích hoạt hoặc vô hiệu hóa tài khoản");
            if (!allowed)
            {
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "User", userId, deniedMsg);
                return (false, deniedMsg);
            }

            if (userId == 1 && !isActive)
            {
                return (false, "Không thể vô hiệu hóa tài khoản Quản trị viên gốc (admin).");
            }

            if (UserSession.CurrentUser != null && userId == UserSession.CurrentUser.UserId && !isActive)
            {
                return (false, "Bạn không thể tự khóa tài khoản đang đăng nhập hiện tại.");
            }

            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                using (var trans = await conn.BeginTransactionAsync())
                {
                    const string sqlUser = "UPDATE Users SET IsActive = @IsActive WHERE UserId = @UserId;";
                    using (var cmd = new MySqlCommand(sqlUser, conn, trans))
                    {
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    const string sqlEmp = "UPDATE Employees SET IsActive = @IsActive WHERE UserId = @UserId;";
                    using (var cmd = new MySqlCommand(sqlEmp, conn, trans))
                    {
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await trans.CommitAsync();
                }

                string action = isActive ? "UNLOCK_USER" : "LOCK_USER";
                string statusText = isActive ? "kích hoạt lại" : "vô hiệu hóa (tạm khóa)";
                await _auditLogService.LogAsync(
                    UserSession.CurrentUser?.UserId, action, "User", userId,
                    $"Đã {statusText} tài khoản UserId={userId}.");

                return (true, $"Đã {statusText} tài khoản thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi thay đổi trạng thái tài khoản: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteOrDeactivateUserAsync(long userId)
        {
            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.UserManage, "Xóa hoặc ngừng sử dụng tài khoản");
            if (!allowed)
            {
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "User", userId, deniedMsg);
                return (false, deniedMsg);
            }

            if (userId == 1)
            {
                return (false, "Không thể xóa tài khoản Quản trị viên gốc (admin).");
            }

            if (UserSession.CurrentUser != null && userId == UserSession.CurrentUser.UserId)
            {
                return (false, "Bạn không thể xóa tài khoản đang đăng nhập hiện tại.");
            }

            try
            {
                // Kiểm tra xem tài khoản đã phát sinh dữ liệu nghiệp vụ (Sessions, Invoices) chưa
                var (hasData, sessionCount, invoiceCount) = await CheckBusinessDataAsync(userId);
                if (hasData)
                {
                    // Không xóa cứng tài khoản đã phát sinh dữ liệu nghiệp vụ quan trọng -> Chuyển sang vô hiệu hóa mềm
                    var (deactivated, _) = await ToggleUserActiveStatusAsync(userId, false);
                    if (deactivated)
                    {
                        return (true, $"Tài khoản này đã phát sinh dữ liệu nghiệp vụ ({sessionCount} phiên chơi, {invoiceCount} hóa đơn). Hệ thống không xóa cứng mà đã chuyển tài khoản sang trạng thái ĐÃ KHÓA (Ngừng hoạt động) để bảo toàn lịch sử.");
                    }
                    return (false, "Không thể xóa cứng tài khoản đã phát sinh dữ liệu nghiệp vụ.");
                }

                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                using (var trans = await conn.BeginTransactionAsync())
                {
                    // Giữ lại AuditLogs bằng cách đặt UserId = NULL cho các log cũ của user này
                    const string unlinkAuditSql = "UPDATE AuditLogs SET UserId = NULL WHERE UserId = @UserId;";
                    using (var cmdAudit = new MySqlCommand(unlinkAuditSql, conn, trans))
                    {
                        cmdAudit.Parameters.AddWithValue("@UserId", userId);
                        await cmdAudit.ExecuteNonQueryAsync();
                    }

                    const string deleteEmpSql = "DELETE FROM Employees WHERE UserId = @UserId;";
                    using (var cmdEmp = new MySqlCommand(deleteEmpSql, conn, trans))
                    {
                        cmdEmp.Parameters.AddWithValue("@UserId", userId);
                        await cmdEmp.ExecuteNonQueryAsync();
                    }

                    const string deleteUserSql = "DELETE FROM Users WHERE UserId = @UserId;";
                    using (var cmdUser = new MySqlCommand(deleteUserSql, conn, trans))
                    {
                        cmdUser.Parameters.AddWithValue("@UserId", userId);
                        await cmdUser.ExecuteNonQueryAsync();
                    }

                    await trans.CommitAsync();
                }

                await _auditLogService.LogAsync(
                    UserSession.CurrentUser?.UserId, "DELETE_USER", "User", userId,
                    $"Xóa vĩnh viễn tài khoản chưa phát sinh dữ liệu nghiệp vụ (UserId={userId}).");

                return (true, "Đã xóa tài khoản và hồ sơ nhân viên (chưa phát sinh dữ liệu nghiệp vụ) thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi xóa tài khoản: {ex.Message}");
            }
        }
    }
}

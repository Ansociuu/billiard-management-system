using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySqlConnector;
using billiard_management_system.Helpers;
using billiard_management_system.Models;

namespace billiard_management_system.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(long? userId, string action, string entityType, long? entityId, string description);
        Task LogAsync(long? userId, string action, string entityType, long? entityId, string description, string oldValuesJson, string newValuesJson);
        Task<List<AuditLog>> GetAuditLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, string actionCategory = null, string keyword = null, int limit = 300);
    }

    /// <summary>
    /// Dịch vụ ghi nhật ký truy vết thao tác quan trọng của hệ thống (Audit Logs) vào MySQL.
    /// Chính sách xử lý lỗi:
    /// 1. Không bao giờ ghi mật khẩu, PasswordHash, ApiSecret hoặc chuỗi kết nối vào nhật ký.
    /// 2. Hỗ trợ lưu dữ liệu trước/sau khi thay đổi (OldValues / NewValues dạng JSON).
    /// 3. Không làm gián đoạn hoặc mất dữ liệu nghiệp vụ chính nếu thao tác ghi log gặp sự cố.
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        private static readonly Regex SensitiveJsonFieldRegex = new Regex(
            @"""(password|passwordHash|plainPassword|newPassword|apiSecret|secret|token|connectionString|pwd)""\s*:\s*""[^""]*""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SensitiveKeyValueRegex = new Regex(
            @"\b(password|passwordHash|plainPassword|newPassword|apiSecret|secret|token|pwd)\s*=\s*([^\s,;)]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex BcryptHashRegex = new Regex(
            @"\$2[aby]\$\d{2}\$[./A-Za-z0-9]{53}",
            RegexOptions.Compiled);

        /// <summary>
        /// Loại bỏ mọi thông tin nhạy cảm (mật khẩu, BCrypt hash, secret, token) trước khi lưu vào AuditLogs
        /// </summary>
        public static string SanitizeSensitiveData(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            string sanitized = SensitiveJsonFieldRegex.Replace(input, "\"$1\":\"[REDACTED]\"");
            sanitized = SensitiveKeyValueRegex.Replace(sanitized, "$1=[REDACTED]");
            sanitized = BcryptHashRegex.Replace(sanitized, "[REDACTED_HASH]");
            return sanitized;
        }

        /// <summary>
        /// Chuẩn hóa dữ liệu trước/sau khi thay đổi để tương thích với cột JSON của MySQL
        /// </summary>
        public static string NormalizeJsonColumn(string rawOrJson)
        {
            if (string.IsNullOrWhiteSpace(rawOrJson))
            {
                return null;
            }

            string clean = SanitizeSensitiveData(rawOrJson.Trim());
            if ((clean.StartsWith("{") && clean.EndsWith("}")) || (clean.StartsWith("[") && clean.EndsWith("]")))
            {
                return clean;
            }

            string escaped = clean
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", " ")
                .Replace("\n", " ");
            return $"{{\"summary\":\"{escaped}\"}}";
        }

        public Task LogAsync(long? userId, string action, string entityType, long? entityId, string description)
        {
            return LogAsync(userId, action, entityType, entityId, description, null, null);
        }

        public async Task LogAsync(long? userId, string action, string entityType, long? entityId, string description, string oldValuesJson, string newValuesJson)
        {
            try
            {
                string safeAction = string.IsNullOrWhiteSpace(action) ? "UNKNOWN" : action.Trim();
                if (safeAction.Length > 50) safeAction = safeAction.Substring(0, 50);

                string safeEntity = string.IsNullOrWhiteSpace(entityType) ? "System" : entityType.Trim();
                if (safeEntity.Length > 50) safeEntity = safeEntity.Substring(0, 50);

                string safeDesc = SanitizeSensitiveData(description ?? string.Empty);
                if (safeDesc.Length > 500) safeDesc = safeDesc.Substring(0, 497) + "...";

                string safeOldJson = NormalizeJsonColumn(oldValuesJson);
                string safeNewJson = NormalizeJsonColumn(newValuesJson);

                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    const string sql = @"
                        INSERT INTO AuditLogs (UserId, Action, EntityType, EntityId, Description, OldValues, NewValues, CreatedAt)
                        VALUES (@UserId, @Action, @EntityType, @EntityId, @Description, @OldValues, @NewValues, @CreatedAt);";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Action", safeAction);
                        cmd.Parameters.AddWithValue("@EntityType", safeEntity);
                        cmd.Parameters.AddWithValue("@EntityId", (object)entityId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Description", safeDesc);
                        cmd.Parameters.AddWithValue("@OldValues", (object)safeOldJson ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NewValues", (object)safeNewJson ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Chính sách thống nhất: Ghi log thất bại không bao giờ làm mất dữ liệu nghiệp vụ chính
                System.Diagnostics.Debug.WriteLine($"[AuditLog Non-Blocking Warning]: {ex.Message}");
            }
        }

        /// <summary>
        /// Truy vấn danh sách nhật ký hệ thống dành cho người dùng có quyền quản trị (Admin / USER_MANAGE / ROLE_MANAGE)
        /// </summary>
        public async Task<List<AuditLog>> GetAuditLogsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string actionCategory = null,
            string keyword = null,
            int limit = 300)
        {
            if (UserSession.IsLoggedIn
                && !UserSession.IsAdmin
                && !UserSession.HasPermission(PermissionCodes.UserManage)
                && !UserSession.HasPermission(PermissionCodes.RoleManage))
            {
                const string denied = "Từ chối truy cập: Bạn không có quyền xem nhật ký hệ thống (Audit Logs).";
                await LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "AuditLog", null, denied);
                throw new UnauthorizedAccessException(denied);
            }

            var logs = new List<AuditLog>();
            int safeLimit = Math.Max(10, Math.Min(limit, 1000));

            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                string sql = $@"
                    SELECT a.AuditLogId,
                           a.UserId,
                           u.Username,
                           u.FullName,
                           r.RoleName,
                           a.Action,
                           a.EntityType,
                           a.EntityId,
                           a.Description,
                           CAST(a.OldValues AS CHAR) AS OldValuesText,
                           CAST(a.NewValues AS CHAR) AS NewValuesText,
                           a.CreatedAt
                    FROM AuditLogs a
                    LEFT JOIN Users u ON a.UserId = u.UserId
                    LEFT JOIN Roles r ON u.RoleId = r.RoleId
                    WHERE (@FromDate IS NULL OR a.CreatedAt >= @FromDate)
                      AND (@ToDate IS NULL OR a.CreatedAt < @ToDate)
                      AND (@Keyword IS NULL
                           OR a.Action LIKE @Keyword
                           OR a.EntityType LIKE @Keyword
                           OR a.Description LIKE @Keyword
                           OR u.Username LIKE @Keyword
                           OR u.FullName LIKE @Keyword)
                    ORDER BY a.CreatedAt DESC, a.AuditLogId DESC
                    LIMIT {safeLimit};";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    DateTime? start = fromDate?.Date;
                    DateTime? endExclusive = toDate?.Date.AddDays(1);
                    string kw = string.IsNullOrWhiteSpace(keyword) ? null : $"%{keyword.Trim()}%";

                    cmd.Parameters.AddWithValue("@FromDate", (object)start ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ToDate", (object)endExclusive ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Keyword", (object)kw ?? DBNull.Value);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new AuditLog
                            {
                                AuditLogId = reader.GetInt64("AuditLogId"),
                                UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? (long?)null : reader.GetInt64("UserId"),
                                Username = reader.IsDBNull(reader.GetOrdinal("Username")) ? null : reader.GetString("Username"),
                                FullName = reader.IsDBNull(reader.GetOrdinal("FullName")) ? null : reader.GetString("FullName"),
                                RoleName = reader.IsDBNull(reader.GetOrdinal("RoleName")) ? null : reader.GetString("RoleName"),
                                Action = reader.IsDBNull(reader.GetOrdinal("Action")) ? string.Empty : reader.GetString("Action"),
                                EntityType = reader.IsDBNull(reader.GetOrdinal("EntityType")) ? string.Empty : reader.GetString("EntityType"),
                                EntityId = reader.IsDBNull(reader.GetOrdinal("EntityId")) ? (long?)null : reader.GetInt64("EntityId"),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString("Description"),
                                OldValues = reader.IsDBNull(reader.GetOrdinal("OldValuesText")) ? null : reader.GetString("OldValuesText"),
                                NewValues = reader.IsDBNull(reader.GetOrdinal("NewValuesText")) ? null : reader.GetString("NewValuesText"),
                                CreatedAt = reader.GetDateTime("CreatedAt")
                            };

                            if (MatchesCategoryFilter(item, actionCategory))
                            {
                                logs.Add(item);
                            }
                        }
                    }
                }
            }

            return logs;
        }

        private static bool MatchesCategoryFilter(AuditLog log, string category)
        {
            if (string.IsNullOrWhiteSpace(category) || string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string act = (log.Action ?? string.Empty).ToUpperInvariant();
            string ent = (log.EntityType ?? string.Empty).ToUpperInvariant();

            switch (category.ToUpperInvariant())
            {
                case "AUTH":
                    return act.Contains("LOGIN") || act.Contains("LOGOUT") || ent == "AUTH";
                case "SESSION":
                    return act.Contains("SESSION") || act.Contains("TABLE") || ent == "SESSION" || ent == "TABLE";
                case "INVOICE":
                    return act.Contains("INVOICE") || act.Contains("PAY") || act.Contains("CHECKOUT") || ent == "INVOICE";
                case "PRICE_PRODUCT":
                    return act.Contains("PRICING") || act.Contains("PRODUCT") || act.Contains("CATEGORY")
                        || ent.Contains("PRICING") || ent.Contains("PRODUCT") || ent.Contains("CATEGORY");
                case "USER_RBAC":
                    return act.Contains("USER") || act.Contains("ROLE") || act.Contains("PERMISSION")
                        || ent == "USER" || ent == "ROLE";
                case "SECURITY":
                    return act.Contains("DENIED") || act.Contains("FAILED") || act.Contains("LOCK");
                default:
                    return true;
            }
        }
    }
}

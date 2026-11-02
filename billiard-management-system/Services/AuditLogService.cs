using System;
using System.Threading.Tasks;
using MySqlConnector;

namespace billiard_management_system.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(long? userId, string action, string entityType, long? entityId, string description);
    }

    /// <summary>
    /// Dịch vụ ghi nhật ký truy vết thao tác hệ thống (Audit Logs) vào MySQL
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        public async Task LogAsync(long? userId, string action, string entityType, long? entityId, string description)
        {
            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    const string sql = @"
                        INSERT INTO AuditLogs (UserId, Action, EntityType, EntityId, Description, CreatedAt)
                        VALUES (@UserId, @Action, @EntityType, @EntityId, @Description, NOW());";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Action", action ?? "UNKNOWN");
                        cmd.Parameters.AddWithValue("@EntityType", entityType ?? "System");
                        cmd.Parameters.AddWithValue("@EntityId", (object)entityId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Description", description ?? string.Empty);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Ghi log thất bại không được làm gián đoạn luồng nghiệp vụ chính của người dùng
                System.Diagnostics.Debug.WriteLine($"[AuditLog Error]: {ex.Message}");
            }
        }
    }
}


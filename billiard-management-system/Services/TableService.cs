using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using billiard_management_system.Helpers;
using billiard_management_system.Models;
using MySqlConnector;

namespace billiard_management_system.Services
{
    public interface ITableService
    {
        Task<List<BilliardTable>> GetAllTablesAsync(bool includeInactive = false);
        Task<List<TableType>> GetTableTypesAsync(bool includeInactive = false);
        Task<bool> IsTableNumberExistsAsync(string tableNumber, int? excludeTableId = null);
        Task<bool> IsTableTypeNameExistsAsync(string typeName, int? excludeTableTypeId = null);
        Task<(bool Success, string Message, int TableTypeId)> CreateTableTypeAsync(TableType tableType);
        Task<(bool Success, string Message)> UpdateTableTypeAsync(TableType tableType);
        Task<(bool Success, string Message, int TableId)> CreateTableAsync(BilliardTable table);
        Task<(bool Success, string Message)> UpdateTableAsync(BilliardTable table);
        Task<(bool Success, string Message)> ToggleTableActiveAsync(int tableId, bool isActive);
        Task<(bool Success, string Message, long SessionId)> StartSessionAsync(int tableId, long userId, long? customerId = null);
        Task<(bool Success, string Message)> SwitchTableAsync(long sessionId, int currentTableId, int newTableId);
        Task<(bool Success, string Message)> UpdateTableStatusAsync(int tableId, string newStatus);
    }

    public class TableService : ITableService
    {
        private readonly IAuditLogService _auditLogService;

        public TableService(IAuditLogService auditLogService = null)
        {
            _auditLogService = auditLogService ?? new AuditLogService();
        }

        public async Task<List<BilliardTable>> GetAllTablesAsync(bool includeInactive = false)
        {
            if (UserSession.IsLoggedIn)
            {
                UserSession.DemandPermission(PermissionCodes.TableView, "Xem danh sách bàn");
            }

            var tables = new List<BilliardTable>();

            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                string query = @"
                    SELECT 
                        t.TableId, t.TableTypeId, t.TableNumber, t.Status, t.IsActive,
                        tt.TypeName,
                        COALESCE(
                            (SELECT HourlyRate FROM TablePricingRules tpr 
                             WHERE tpr.TableTypeId = t.TableTypeId AND tpr.IsActive = TRUE AND tpr.RuleType = 'BaseRate'
                             ORDER BY tpr.Priority DESC LIMIT 1), 
                            50000.00
                        ) AS HourlyRate,
                        s.SessionId, s.StartTime, s.UserId, s.CustomerId, u.FullName AS StaffName, c.FullName AS CustomerName
                    FROM Tables t
                    INNER JOIN TableTypes tt ON t.TableTypeId = tt.TableTypeId
                    LEFT JOIN Sessions s ON t.TableId = s.TableId AND s.Status = 'Active'
                    LEFT JOIN Users u ON s.UserId = u.UserId
                    LEFT JOIN Customers c ON s.CustomerId = c.CustomerId";

                if (!includeInactive)
                {
                    query += " WHERE t.IsActive = TRUE";
                }

                query += " ORDER BY t.IsActive DESC, t.TableTypeId ASC, t.TableNumber ASC;";

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var table = new BilliardTable
                        {
                            TableId = reader.GetInt32("TableId"),
                            TableTypeId = reader.GetInt32("TableTypeId"),
                            TableNumber = reader.GetString("TableNumber"),
                            Status = reader.GetString("Status"),
                            IsActive = reader.GetBoolean("IsActive"),
                            TypeName = reader.GetString("TypeName"),
                            HourlyRate = reader.GetDecimal("HourlyRate")
                        };

                        if (!reader.IsDBNull(reader.GetOrdinal("SessionId")))
                        {
                            table.CurrentSession = new BilliardSession
                            {
                                SessionId = reader.GetInt64("SessionId"),
                                TableId = table.TableId,
                                TableNumber = table.TableNumber,
                                StartTime = reader.GetDateTime("StartTime"),
                                UserId = reader.GetInt64("UserId"),
                                CustomerId = reader.IsDBNull(reader.GetOrdinal("CustomerId")) ? (long?)null : reader.GetInt64("CustomerId"),
                                StaffName = reader.IsDBNull(reader.GetOrdinal("StaffName")) ? null : reader.GetString("StaffName"),
                                CustomerName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? null : reader.GetString("CustomerName"),
                                Status = "Active"
                            };
                            table.UpdateElapsed();
                        }

                        tables.Add(table);
                    }
                }
            }

            return tables;
        }

        public async Task<List<TableType>> GetTableTypesAsync(bool includeInactive = false)
        {
            var types = new List<TableType>();

            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                string query = "SELECT TableTypeId, TypeName, Description, IsActive FROM TableTypes";
                if (!includeInactive)
                {
                    query += " WHERE IsActive = TRUE";
                }
                query += " ORDER BY IsActive DESC, TableTypeId ASC;";

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        types.Add(new TableType
                        {
                            TableTypeId = reader.GetInt32("TableTypeId"),
                            TypeName = reader.GetString("TypeName"),
                            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description"),
                            IsActive = reader.GetBoolean("IsActive")
                        });
                    }
                }
            }

            return types;
        }

        public async Task<bool> IsTableNumberExistsAsync(string tableNumber, int? excludeTableId = null)
        {
            if (string.IsNullOrWhiteSpace(tableNumber)) return false;

            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                string sql = "SELECT COUNT(1) FROM Tables WHERE LOWER(TableNumber) = LOWER(@TableNumber)";
                if (excludeTableId.HasValue)
                {
                    sql += " AND TableId <> @ExcludeId";
                }

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TableNumber", tableNumber.Trim());
                    if (excludeTableId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@ExcludeId", excludeTableId.Value);
                    }
                    return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                }
            }
        }

        public async Task<bool> IsTableTypeNameExistsAsync(string typeName, int? excludeTableTypeId = null)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return false;

            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                string sql = "SELECT COUNT(1) FROM TableTypes WHERE LOWER(TypeName) = LOWER(@TypeName)";
                if (excludeTableTypeId.HasValue)
                {
                    sql += " AND TableTypeId <> @ExcludeId";
                }

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TypeName", typeName.Trim());
                    if (excludeTableTypeId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@ExcludeId", excludeTableTypeId.Value);
                    }
                    return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                }
            }
        }

        public async Task<(bool Success, string Message, int TableTypeId)> CreateTableTypeAsync(TableType tableType)
        {
            if (UserSession.IsLoggedIn && !UserSession.IsAdmin)
            {
                return (false, "Từ chối truy cập: Chỉ Quản trị viên (Admin) mới được phép thêm loại bàn mới.", 0);
            }

            if (tableType == null || string.IsNullOrWhiteSpace(tableType.TypeName))
            {
                return (false, "Tên loại bàn không được để trống.", 0);
            }

            string cleanName = tableType.TypeName.Trim();
            string cleanDesc = string.IsNullOrWhiteSpace(tableType.Description) ? null : tableType.Description.Trim();

            try
            {
                if (await IsTableTypeNameExistsAsync(cleanName))
                {
                    return (false, $"Loại bàn '{cleanName}' đã tồn tại trên hệ thống.", 0);
                }

                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    const string sql = @"
                        INSERT INTO TableTypes (TypeName, Description, IsActive)
                        VALUES (@TypeName, @Description, @IsActive);
                        SELECT LAST_INSERT_ID();";

                    int newId;
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TypeName", cleanName);
                        cmd.Parameters.AddWithValue("@Description", (object)cleanDesc ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", tableType.IsActive);
                        newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    await _auditLogService.LogAsync(
                        UserSession.CurrentUser?.UserId, "CREATE_TABLE_TYPE", "TableType", newId,
                        $"Thêm mới loại bàn '{cleanName}'.");

                    return (true, $"Thêm loại bàn [{cleanName}] thành công!", newId);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi thêm loại bàn: {ex.Message}", 0);
            }
        }

        public async Task<(bool Success, string Message)> UpdateTableTypeAsync(TableType tableType)
        {
            if (UserSession.IsLoggedIn && !UserSession.IsAdmin)
            {
                return (false, "Từ chối truy cập: Chỉ Quản trị viên (Admin) mới được phép chỉnh sửa loại bàn.");
            }

            if (tableType == null || tableType.TableTypeId <= 0 || string.IsNullOrWhiteSpace(tableType.TypeName))
            {
                return (false, "Thông tin loại bàn không hợp lệ.");
            }

            string cleanName = tableType.TypeName.Trim();
            string cleanDesc = string.IsNullOrWhiteSpace(tableType.Description) ? null : tableType.Description.Trim();

            try
            {
                if (await IsTableTypeNameExistsAsync(cleanName, tableType.TableTypeId))
                {
                    return (false, $"Tên loại bàn '{cleanName}' đã bị trùng với loại bàn khác.");
                }

                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    // Nếu ngừng sử dụng loại bàn, kiểm tra xem có bàn nào thuộc loại này đang có khách chơi không
                    if (!tableType.IsActive)
                    {
                        const string checkActiveSql = @"
                            SELECT COUNT(1) FROM Tables 
                            WHERE TableTypeId = @TypeId AND Status = 'Occupied';";
                        using (var cmdCheck = new MySqlCommand(checkActiveSql, conn))
                        {
                            cmdCheck.Parameters.AddWithValue("@TypeId", tableType.TableTypeId);
                            int occupiedCount = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());
                            if (occupiedCount > 0)
                            {
                                return (false, $"Không thể ngừng sử dụng loại bàn này vì hiện đang có {occupiedCount} bàn thuộc loại này đang có khách chơi.");
                            }
                        }
                    }

                    const string sql = @"
                        UPDATE TableTypes
                        SET TypeName = @TypeName, Description = @Description, IsActive = @IsActive
                        WHERE TableTypeId = @Id;";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TypeName", cleanName);
                        cmd.Parameters.AddWithValue("@Description", (object)cleanDesc ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", tableType.IsActive);
                        cmd.Parameters.AddWithValue("@Id", tableType.TableTypeId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await _auditLogService.LogAsync(
                        UserSession.CurrentUser?.UserId, "UPDATE_TABLE_TYPE", "TableType", tableType.TableTypeId,
                        $"Cập nhật loại bàn '{cleanName}' (IsActive={tableType.IsActive}).");

                    return (true, $"Cập nhật loại bàn [{cleanName}] thành công!");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi cập nhật loại bàn: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, int TableId)> CreateTableAsync(BilliardTable table)
        {
            if (UserSession.IsLoggedIn && !UserSession.IsAdmin)
            {
                return (false, "Từ chối truy cập: Chỉ Quản trị viên (Admin) mới được phép thêm bàn mới.", 0);
            }

            if (table == null || string.IsNullOrWhiteSpace(table.TableNumber))
            {
                return (false, "Vui lòng nhập số/mã bàn (vd: P04, C03).", 0);
            }

            if (table.TableTypeId <= 0)
            {
                return (false, "Vui lòng chọn loại bàn hợp lệ.", 0);
            }

            string cleanTableNo = table.TableNumber.Trim().ToUpperInvariant();
            string status = string.IsNullOrWhiteSpace(table.Status) ? "Available" : table.Status.Trim();

            if (status != "Available" && status != "Maintenance")
            {
                return (false, "Bàn mới tạo chỉ có thể ở trạng thái Bàn trống (Available) hoặc Bảo trì (Maintenance).", 0);
            }

            try
            {
                // Kiểm tra trùng số bàn
                if (await IsTableNumberExistsAsync(cleanTableNo))
                {
                    return (false, $"Số bàn '{cleanTableNo}' đã tồn tại trên hệ thống! Không được phép trùng số bàn.", 0);
                }

                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    const string sql = @"
                        INSERT INTO Tables (TableTypeId, TableNumber, Status, IsActive)
                        VALUES (@TableTypeId, @TableNumber, @Status, @IsActive);
                        SELECT LAST_INSERT_ID();";

                    int newTableId;
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@TableTypeId", table.TableTypeId);
                        cmd.Parameters.AddWithValue("@TableNumber", cleanTableNo);
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@IsActive", table.IsActive);
                        newTableId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    await _auditLogService.LogAsync(
                        UserSession.CurrentUser?.UserId, "CREATE_TABLE", "Table", newTableId,
                        $"Thêm mới bàn '{cleanTableNo}' (TableTypeId={table.TableTypeId}, Status={status}).");

                    return (true, $"Thêm bàn [{cleanTableNo}] thành công!", newTableId);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi thêm bàn: {ex.Message}", 0);
            }
        }

        public async Task<(bool Success, string Message)> UpdateTableAsync(BilliardTable table)
        {
            if (UserSession.IsLoggedIn && !UserSession.IsAdmin)
            {
                return (false, "Từ chối truy cập: Chỉ Quản trị viên (Admin) mới được phép chỉnh sửa thông tin bàn.");
            }

            if (table == null || table.TableId <= 0 || string.IsNullOrWhiteSpace(table.TableNumber))
            {
                return (false, "Thông tin bàn cần cập nhật không hợp lệ.");
            }

            if (table.TableTypeId <= 0)
            {
                return (false, "Vui lòng chọn loại bàn hợp lệ.");
            }

            string cleanTableNo = table.TableNumber.Trim().ToUpperInvariant();
            string targetStatus = string.IsNullOrWhiteSpace(table.Status) ? "Available" : table.Status.Trim();

            if (targetStatus != "Available" && targetStatus != "Occupied" && targetStatus != "Maintenance")
            {
                return (false, "Trạng thái bàn không hợp lệ (chỉ chấp nhận Available, Occupied, Maintenance).");
            }

            try
            {
                // 1. Kiểm tra trùng số bàn (loại trừ chính bàn đang sửa)
                if (await IsTableNumberExistsAsync(cleanTableNo, table.TableId))
                {
                    return (false, $"Số bàn '{cleanTableNo}' đã tồn tại ở một bàn khác! Không cho phép trùng số bàn.");
                }

                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    // 2. Kiểm tra trạng thái hiện tại và phiên chơi đang hoạt động (Active Session)
                    string currentStatus = "Available";
                    const string getTableSql = "SELECT Status FROM Tables WHERE TableId = @TableId;";
                    using (var cmdGet = new MySqlCommand(getTableSql, conn))
                    {
                        cmdGet.Parameters.AddWithValue("@TableId", table.TableId);
                        var res = await cmdGet.ExecuteScalarAsync();
                        if (res == null)
                        {
                            return (false, "Không tìm thấy bàn cần cập nhật.");
                        }
                        currentStatus = res.ToString();
                    }

                    const string activeSessionSql = "SELECT COUNT(1) FROM Sessions WHERE TableId = @TableId AND Status = 'Active';";
                    int activeSessions;
                    using (var cmdSess = new MySqlCommand(activeSessionSql, conn))
                    {
                        cmdSess.Parameters.AddWithValue("@TableId", table.TableId);
                        activeSessions = Convert.ToInt32(await cmdSess.ExecuteScalarAsync());
                    }

                    bool isCurrentlyPlaying = activeSessions > 0 || string.Equals(currentStatus, "Occupied", StringComparison.OrdinalIgnoreCase);

                    // Tiêu chí 5: Không cho phép chuyển bàn đang có phiên chơi sang trạng thái không hợp lệ hoặc ngừng sử dụng
                    if (isCurrentlyPlaying)
                    {
                        if (!string.Equals(targetStatus, "Occupied", StringComparison.OrdinalIgnoreCase))
                        {
                            return (false, $"Bàn [{cleanTableNo}] đang có phiên chơi hoạt động! Không thể chuyển trực tiếp sang trạng thái '{targetStatus}'. Vui lòng kết thúc/thanh toán phiên chơi hoặc chuyển bàn trước.");
                        }

                        if (!table.IsActive)
                        {
                            return (false, $"Bàn [{cleanTableNo}] đang có khách chơi! Không thể ngừng sử dụng bàn khi phiên chơi chưa kết thúc.");
                        }
                    }
                    else
                    {
                        // Bàn đang không có phiên chơi Active thì không được gán tay thành Occupied
                        if (string.Equals(targetStatus, "Occupied", StringComparison.OrdinalIgnoreCase))
                        {
                            return (false, "Không thể đặt thủ công trạng thái 'Đang chơi (Occupied)' khi chưa mở phiên chơi. Vui lòng dùng chức năng Mở Bàn.");
                        }
                    }

                    const string updateSql = @"
                        UPDATE Tables
                        SET TableTypeId = @TableTypeId,
                            TableNumber = @TableNumber,
                            Status = @Status,
                            IsActive = @IsActive
                        WHERE TableId = @TableId;";

                    using (var cmdUpdate = new MySqlCommand(updateSql, conn))
                    {
                        cmdUpdate.Parameters.AddWithValue("@TableTypeId", table.TableTypeId);
                        cmdUpdate.Parameters.AddWithValue("@TableNumber", cleanTableNo);
                        cmdUpdate.Parameters.AddWithValue("@Status", targetStatus);
                        cmdUpdate.Parameters.AddWithValue("@IsActive", table.IsActive);
                        cmdUpdate.Parameters.AddWithValue("@TableId", table.TableId);
                        await cmdUpdate.ExecuteNonQueryAsync();
                    }

                    await _auditLogService.LogAsync(
                        UserSession.CurrentUser?.UserId, "UPDATE_TABLE", "Table", table.TableId,
                        $"Cập nhật bàn '{cleanTableNo}' (TableTypeId={table.TableTypeId}, Status={targetStatus}, IsActive={table.IsActive}).");

                    return (true, $"Cập nhật thông tin bàn [{cleanTableNo}] thành công!");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi cập nhật bàn: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ToggleTableActiveAsync(int tableId, bool isActive)
        {
            if (UserSession.IsLoggedIn && !UserSession.IsAdmin)
            {
                return (false, "Từ chối truy cập: Chỉ Quản trị viên (Admin) mới được phép ngừng sử dụng hoặc kích hoạt lại bàn.");
            }

            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    const string checkSql = @"
                        SELECT t.Status, t.TableNumber,
                               (SELECT COUNT(1) FROM Sessions s WHERE s.TableId = t.TableId AND s.Status = 'Active') AS ActiveSessions
                        FROM Tables t
                        WHERE t.TableId = @TableId;";

                    string currentStatus = null;
                    string tableNumber = null;
                    int activeSessions = 0;

                    using (var cmdCheck = new MySqlCommand(checkSql, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@TableId", tableId);
                        using (var reader = await cmdCheck.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                return (false, "Không tìm thấy bàn cần thao tác.");
                            }
                            currentStatus = reader.GetString("Status");
                            tableNumber = reader.GetString("TableNumber");
                            activeSessions = Convert.ToInt32(reader["ActiveSessions"]);
                        }
                    }

                    if (!isActive && (currentStatus == "Occupied" || activeSessions > 0))
                    {
                        return (false, $"Bàn [{tableNumber}] đang có phiên chơi hoạt động! Không được phép ngừng sử dụng bàn đang có khách.");
                    }

                    const string updateSql = "UPDATE Tables SET IsActive = @IsActive WHERE TableId = @TableId;";
                    using (var cmd = new MySqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@TableId", tableId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    string actionText = isActive ? "kích hoạt sử dụng lại" : "ngừng sử dụng";
                    await _auditLogService.LogAsync(
                        UserSession.CurrentUser?.UserId, isActive ? "ACTIVATE_TABLE" : "DEACTIVATE_TABLE", "Table", tableId,
                        $"Đã {actionText} bàn '{tableNumber}' (TableId={tableId}).");

                    return (true, $"Đã {actionText} bàn [{tableNumber}] thành công!");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi thay đổi trạng thái sử dụng bàn: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, long SessionId)> StartSessionAsync(int tableId, long userId, long? customerId = null)
        {
            // Kiểm tra quyền mở phiên chơi (SESSION_OPEN) ở tầng nghiệp vụ
            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.SessionOpen, "Mở phiên chơi");
            if (!allowed)
            {
                await _auditLogService.LogAsync(userId, "ACCESS_DENIED", "Table", tableId, deniedMsg);
                return (false, deniedMsg, 0);
            }

            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                using (var trans = await conn.BeginTransactionAsync())
                {
                    // 1. Kiểm tra trạng thái bàn và trạng thái kinh doanh
                    const string checkSql = "SELECT Status, IsActive FROM Tables WHERE TableId = @TableId FOR UPDATE;";
                    using (var cmdCheck = new MySqlCommand(checkSql, conn, trans))
                    {
                        cmdCheck.Parameters.AddWithValue("@TableId", tableId);
                        using (var reader = await cmdCheck.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                await trans.RollbackAsync();
                                return (false, "Không tìm thấy bàn cần mở!", 0);
                            }

                            string status = reader.GetString("Status");
                            bool isActive = reader.GetBoolean("IsActive");
                            reader.Close();

                            if (!isActive)
                            {
                                await trans.RollbackAsync();
                                return (false, "Bàn này đã ngừng sử dụng, không thể mở phiên chơi!", 0);
                            }

                            if (status != "Available")
                            {
                                await trans.RollbackAsync();
                                return (false, "Bàn đang có khách hoặc đang bảo trì, không thể mở!", 0);
                            }
                        }
                    }

                    // Kiểm tra chống trùng phiên Active
                    const string checkActiveSessSql = "SELECT COUNT(1) FROM Sessions WHERE TableId = @TableId AND Status = 'Active';";
                    using (var cmdSess = new MySqlCommand(checkActiveSessSql, conn, trans))
                    {
                        cmdSess.Parameters.AddWithValue("@TableId", tableId);
                        int activeCount = Convert.ToInt32(await cmdSess.ExecuteScalarAsync());
                        if (activeCount > 0)
                        {
                            await trans.RollbackAsync();
                            return (false, "Bàn hiện đã có phiên chơi đang hoạt động!", 0);
                        }
                    }

                    // 2. Cập nhật trạng thái bàn thành Occupied
                    const string updateTableSql = "UPDATE Tables SET Status = 'Occupied' WHERE TableId = @TableId;";
                    using (var cmdUpdate = new MySqlCommand(updateTableSql, conn, trans))
                    {
                        cmdUpdate.Parameters.AddWithValue("@TableId", tableId);
                        await cmdUpdate.ExecuteNonQueryAsync();
                    }

                    // 3. Tạo phiên chơi mới (Sessions)
                    const string insertSessionSql = @"
                        INSERT INTO Sessions (TableId, UserId, CustomerId, StartTime, Status, TableAmount)
                        VALUES (@TableId, @UserId, @CustomerId, NOW(), 'Active', 0.00);
                        SELECT LAST_INSERT_ID();";

                    long sessionId = 0;
                    using (var cmdInsert = new MySqlCommand(insertSessionSql, conn, trans))
                    {
                        cmdInsert.Parameters.AddWithValue("@TableId", tableId);
                        cmdInsert.Parameters.AddWithValue("@UserId", userId);
                        cmdInsert.Parameters.AddWithValue("@CustomerId", (object)customerId ?? DBNull.Value);
                        sessionId = Convert.ToInt64(await cmdInsert.ExecuteScalarAsync());
                    }

                    await trans.CommitAsync();
                    return (true, "Mở bàn thành công!", sessionId);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi mở bàn: {ex.Message}", 0);
            }
        }

        public async Task<(bool Success, string Message)> SwitchTableAsync(long sessionId, int currentTableId, int newTableId)
        {
            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.SessionOpen, "Chuyển bàn chơi");
            if (!allowed)
            {
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "Session", sessionId, deniedMsg);
                return (false, deniedMsg);
            }

            if (currentTableId == newTableId)
            {
                return (false, "Bàn chuyển đến trùng với bàn hiện tại!");
            }

            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                using (var trans = await conn.BeginTransactionAsync())
                {
                    // Kiểm tra bàn mới có trống và đang hoạt động không
                    const string checkSql = "SELECT Status, IsActive FROM Tables WHERE TableId = @NewTableId FOR UPDATE;";
                    using (var cmdCheck = new MySqlCommand(checkSql, conn, trans))
                    {
                        cmdCheck.Parameters.AddWithValue("@NewTableId", newTableId);
                        using (var reader = await cmdCheck.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                await trans.RollbackAsync();
                                return (false, "Không tìm thấy bàn đích!");
                            }
                            string status = reader.GetString("Status");
                            bool isActive = reader.GetBoolean("IsActive");
                            reader.Close();

                            if (!isActive || status != "Available")
                            {
                                await trans.RollbackAsync();
                                return (false, "Bàn đích đang có khách, đang bảo trì hoặc đã ngừng sử dụng!");
                            }
                        }
                    }

                    // Cập nhật bàn cũ về Available
                    const string updateOldSql = "UPDATE Tables SET Status = 'Available' WHERE TableId = @OldTableId;";
                    using (var cmdOld = new MySqlCommand(updateOldSql, conn, trans))
                    {
                        cmdOld.Parameters.AddWithValue("@OldTableId", currentTableId);
                        await cmdOld.ExecuteNonQueryAsync();
                    }

                    // Cập nhật bàn mới thành Occupied
                    const string updateNewSql = "UPDATE Tables SET Status = 'Occupied' WHERE TableId = @NewTableId;";
                    using (var cmdNew = new MySqlCommand(updateNewSql, conn, trans))
                    {
                        cmdNew.Parameters.AddWithValue("@NewTableId", newTableId);
                        await cmdNew.ExecuteNonQueryAsync();
                    }

                    // Cập nhật phiên chơi sang bàn mới
                    const string updateSessionSql = "UPDATE Sessions SET TableId = @NewTableId WHERE SessionId = @SessionId;";
                    using (var cmdSession = new MySqlCommand(updateSessionSql, conn, trans))
                    {
                        cmdSession.Parameters.AddWithValue("@NewTableId", newTableId);
                        cmdSession.Parameters.AddWithValue("@SessionId", sessionId);
                        await cmdSession.ExecuteNonQueryAsync();
                    }

                    await trans.CommitAsync();
                    return (true, "Chuyển bàn thành công!");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi chuyển bàn: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateTableStatusAsync(int tableId, string newStatus)
        {
            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.SessionOpen, "Cập nhật trạng thái bàn");
            if (!allowed)
            {
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "Table", tableId, deniedMsg);
                return (false, deniedMsg);
            }

            if (newStatus != "Available" && newStatus != "Maintenance")
            {
                return (false, "Chỉ cho phép chuyển đổi trạng thái giữa Bàn trống (Available) và Bảo trì (Maintenance).");
            }

            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    // Kiểm tra xem bàn có đang có phiên chơi Active hay không
                    const string checkSql = @"
                        SELECT t.Status, t.TableNumber,
                               (SELECT COUNT(1) FROM Sessions s WHERE s.TableId = t.TableId AND s.Status = 'Active') AS ActiveSessions
                        FROM Tables t
                        WHERE t.TableId = @TableId;";

                    string currentStatus = null;
                    string tableNumber = null;
                    int activeSessions = 0;

                    using (var cmdCheck = new MySqlCommand(checkSql, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@TableId", tableId);
                        using (var reader = await cmdCheck.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                return (false, "Không tìm thấy bàn cần cập nhật.");
                            }
                            currentStatus = reader.GetString("Status");
                            tableNumber = reader.GetString("TableNumber");
                            activeSessions = Convert.ToInt32(reader["ActiveSessions"]);
                        }
                    }

                    if (currentStatus == "Occupied" || activeSessions > 0)
                    {
                        return (false, $"Bàn [{tableNumber}] đang có khách chơi! Không cho phép chuyển bàn đang có phiên chơi sang trạng thái '{newStatus}'.");
                    }

                    const string sql = "UPDATE Tables SET Status = @Status WHERE TableId = @TableId;";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Status", newStatus);
                        cmd.Parameters.AddWithValue("@TableId", tableId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await _auditLogService.LogAsync(
                        UserSession.CurrentUser?.UserId, "CHANGE_TABLE_STATUS", "Table", tableId,
                        $"Đổi trạng thái bàn '{tableNumber}' từ '{currentStatus}' sang '{newStatus}'.");

                    string msg = newStatus == "Maintenance"
                        ? $"Đã chuyển bàn [{tableNumber}] sang trạng thái Bảo trì!"
                        : $"Đã hoàn tất bảo trì, bàn [{tableNumber}] sẵn sàng đón khách!";
                    return (true, msg);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi cập nhật trạng thái: {ex.Message}");
            }
        }
    }
}

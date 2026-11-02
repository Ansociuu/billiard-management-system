using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using billiard_management_system.Models;
using MySqlConnector;

namespace billiard_management_system.Services
{
    public interface ITableService
    {
        Task<List<BilliardTable>> GetAllTablesAsync();
        Task<List<TableType>> GetTableTypesAsync();
        Task<(bool Success, string Message, long SessionId)> StartSessionAsync(int tableId, long userId, long? customerId = null);
        Task<(bool Success, string Message)> SwitchTableAsync(long sessionId, int currentTableId, int newTableId);
        Task<(bool Success, string Message)> UpdateTableStatusAsync(int tableId, string newStatus);
    }

    public class TableService : ITableService
    {
        public async Task<List<BilliardTable>> GetAllTablesAsync()
        {
            var tables = new List<BilliardTable>();

            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                // Truy vấn lấy danh sách bàn kèm loại bàn, giá giờ cơ sở và phiên chơi đang chạy (nếu có)
                const string query = @"
                    SELECT 
                        t.TableId, t.TableTypeId, t.TableNumber, t.Status, t.IsActive,
                        tt.TypeName,
                        COALESCE(
                            (SELECT HourlyRate FROM TablePricingRules tpr 
                             WHERE tpr.TableTypeId = t.TableTypeId AND tpr.IsActive = TRUE 
                             ORDER BY tpr.Priority DESC LIMIT 1), 
                            50000.00
                        ) AS HourlyRate,
                        s.SessionId, s.StartTime, s.UserId, s.CustomerId, u.FullName AS StaffName, c.FullName AS CustomerName
                    FROM Tables t
                    INNER JOIN TableTypes tt ON t.TableTypeId = tt.TableTypeId
                    LEFT JOIN Sessions s ON t.TableId = s.TableId AND s.Status = 'Active'
                    LEFT JOIN Users u ON s.UserId = u.UserId
                    LEFT JOIN Customers c ON s.CustomerId = c.CustomerId
                    WHERE t.IsActive = TRUE
                    ORDER BY t.TableNumber ASC;";

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

        public async Task<List<TableType>> GetTableTypesAsync()
        {
            var types = new List<TableType>();

            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                const string query = "SELECT TableTypeId, TypeName, Description, IsActive FROM TableTypes WHERE IsActive = TRUE;";
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

        public async Task<(bool Success, string Message, long SessionId)> StartSessionAsync(int tableId, long userId, long? customerId = null)
        {
            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                using (var trans = await conn.BeginTransactionAsync())
                {
                    // 1. Kiểm tra trạng thái bàn
                    const string checkSql = "SELECT Status FROM Tables WHERE TableId = @TableId FOR UPDATE;";
                    using (var cmdCheck = new MySqlCommand(checkSql, conn, trans))
                    {
                        cmdCheck.Parameters.AddWithValue("@TableId", tableId);
                        var status = (string)await cmdCheck.ExecuteScalarAsync();
                        if (status != "Available")
                        {
                            await trans.RollbackAsync();
                            return (false, "Bàn đang có khách hoặc đang bảo trì, không thể mở!", 0);
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
            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                using (var trans = await conn.BeginTransactionAsync())
                {
                    // Kiểm tra bàn mới có trống không
                    const string checkSql = "SELECT Status FROM Tables WHERE TableId = @NewTableId FOR UPDATE;";
                    using (var cmdCheck = new MySqlCommand(checkSql, conn, trans))
                    {
                        cmdCheck.Parameters.AddWithValue("@NewTableId", newTableId);
                        var status = (string)await cmdCheck.ExecuteScalarAsync();
                        if (status != "Available")
                        {
                            await trans.RollbackAsync();
                            return (false, "Bàn đích đang có khách hoặc bảo trì!");
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
            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    const string sql = "UPDATE Tables SET Status = @Status WHERE TableId = @TableId;";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Status", newStatus);
                        cmd.Parameters.AddWithValue("@TableId", tableId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return (true, "Cập nhật trạng thái thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi cập nhật: {ex.Message}");
            }
        }
    }
}


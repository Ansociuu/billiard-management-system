using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using billiard_management_system.Helpers;
using billiard_management_system.Models;
using MySqlConnector;

namespace billiard_management_system.Services
{
    public interface IOrderService
    {
        Task<(decimal TableAmount, decimal ServiceAmount, decimal TotalAmount, TimeSpan Duration)> CalculateSessionBillingAsync(long sessionId);
        Task<(decimal TableAmount, decimal ServiceAmount, decimal TotalAmount, TimeSpan Duration, List<SessionPricingDetail> PricingSegments)> CalculateSessionBillingDetailedAsync(long sessionId, DateTime? customEndTime = null);
        Task<(bool Success, string InvoiceCode, string Message)> CheckoutAsync(long sessionId, int tableId, long userId, decimal discountAmount, string paymentMethod);
    }

    public class OrderService : IOrderService
    {
        private readonly IAuditLogService _auditLogService;
        private readonly IPricingRuleService _pricingRuleService;

        public OrderService(IAuditLogService auditLogService = null, IPricingRuleService pricingRuleService = null)
        {
            _auditLogService = auditLogService ?? new AuditLogService();
            _pricingRuleService = pricingRuleService ?? new PricingRuleService(_auditLogService);
        }

        public async Task<(decimal TableAmount, decimal ServiceAmount, decimal TotalAmount, TimeSpan Duration)> CalculateSessionBillingAsync(long sessionId)
        {
            var (tableAmount, serviceAmount, totalAmount, duration, _) = await CalculateSessionBillingDetailedAsync(sessionId);
            return (tableAmount, serviceAmount, totalAmount, duration);
        }

        public async Task<(decimal TableAmount, decimal ServiceAmount, decimal TotalAmount, TimeSpan Duration, List<SessionPricingDetail> PricingSegments)> CalculateSessionBillingDetailedAsync(long sessionId, DateTime? customEndTime = null)
        {
            decimal tableAmount = 0;
            decimal serviceAmount = 0;
            TimeSpan duration = TimeSpan.Zero;
            var segments = new List<SessionPricingDetail>();

            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                // 1. Lấy thông tin thời gian bắt đầu, kết thúc (nếu có) và loại bàn của phiên chơi
                const string sessionSql = @"
                    SELECT s.StartTime, s.EndTime, s.TableId, t.TableTypeId
                    FROM Sessions s
                    INNER JOIN Tables t ON s.TableId = t.TableId
                    WHERE s.SessionId = @SessionId;";

                DateTime? startTime = null;
                DateTime? dbEndTime = null;
                int tableId = 0;
                int tableTypeId = 1;

                using (var cmd = new MySqlCommand(sessionSql, conn))
                {
                    cmd.Parameters.AddWithValue("@SessionId", sessionId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int endOrd = reader.GetOrdinal("EndTime");
                            startTime = reader.GetDateTime("StartTime");
                            dbEndTime = reader.IsDBNull(endOrd) ? (DateTime?)null : reader.GetDateTime(endOrd);
                            tableId = reader.GetInt32("TableId");
                            tableTypeId = reader.GetInt32("TableTypeId");
                        }
                    }
                }

                if (startTime.HasValue)
                {
                    DateTime effectiveEnd = customEndTime ?? dbEndTime ?? DateTime.Now;
                    var pricingResult = await _pricingRuleService.CalculateSessionPricingAsync(
                        sessionId, tableId, tableTypeId, startTime.Value, effectiveEnd);

                    segments = pricingResult.Segments;
                    tableAmount = pricingResult.TotalTableAmount;
                    duration = pricingResult.Duration;
                }

                // 2. Tính tổng tiền dịch vụ (F&B)
                const string serviceSql = @"
                    SELECT COALESCE(SUM(Amount), 0) AS TotalService 
                    FROM SessionProducts 
                    WHERE SessionId = @SessionId;";

                using (var cmd = new MySqlCommand(serviceSql, conn))
                {
                    cmd.Parameters.AddWithValue("@SessionId", sessionId);
                    serviceAmount = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
                }
            }

            decimal totalAmount = tableAmount + serviceAmount;
            return (tableAmount, serviceAmount, totalAmount, duration, segments);
        }

        public async Task<(bool Success, string InvoiceCode, string Message)> CheckoutAsync(
            long sessionId, int tableId, long userId, decimal discountAmount, string paymentMethod)
        {
            // Kiểm tra quyền kết thúc phiên (SESSION_CLOSE) và thanh toán hóa đơn (INVOICE_PAY)
            var (canClose, closeDeniedMsg) = UserSession.CheckAccess(PermissionCodes.SessionClose, "Kết thúc phiên chơi");
            if (!canClose)
            {
                await _auditLogService.LogAsync(userId, "ACCESS_DENIED", "Session", sessionId, closeDeniedMsg);
                return (false, string.Empty, closeDeniedMsg);
            }

            var (canPay, payDeniedMsg) = UserSession.CheckAccess(PermissionCodes.InvoicePay, "Thanh toán hóa đơn");
            if (!canPay)
            {
                await _auditLogService.LogAsync(userId, "ACCESS_DENIED", "Invoice", sessionId, payDeniedMsg);
                return (false, string.Empty, payDeniedMsg);
            }

            try
            {
                DateTime now = DateTime.Now;
                // Tính toán số tiền theo từng khoảng thời gian (SessionPricingDetails)
                var (tableAmount, serviceAmount, totalAmountBeforeDiscount, duration, pricingSegments) =
                    await CalculateSessionBillingDetailedAsync(sessionId, now);
                decimal finalAmount = Math.Max(0, totalAmountBeforeDiscount - discountAmount);
                string invoiceCode = $"HD{now:yyyyMMddHHmmss}";

                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                using (var trans = await conn.BeginTransactionAsync())
                {
                    // Kiểm tra trạng thái phiên chơi hiện tại (không cho phép kết thúc phiên đã hoàn tất hoặc đã hủy)
                    const string checkSessionSql = @"
                        SELECT s.Status, s.TableId, t.TableTypeId
                        FROM Sessions s
                        INNER JOIN Tables t ON s.TableId = t.TableId
                        WHERE s.SessionId = @SessionId
                        FOR UPDATE;";
                    string currentStatus = null;
                    int dbTableId = tableId;
                    int dbTableTypeId = 1;

                    using (var cmdCheck = new MySqlCommand(checkSessionSql, conn, trans))
                    {
                        cmdCheck.Parameters.AddWithValue("@SessionId", sessionId);
                        using (var reader = await cmdCheck.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                await trans.RollbackAsync();
                                return (false, string.Empty, $"Không tìm thấy phiên chơi #{sessionId}!");
                            }
                            currentStatus = reader.GetString("Status");
                            dbTableId = reader.GetInt32("TableId");
                            dbTableTypeId = reader.GetInt32("TableTypeId");
                        }
                    }

                    if (!string.Equals(currentStatus, "Active", StringComparison.OrdinalIgnoreCase))
                    {
                        await trans.RollbackAsync();
                        return (false, string.Empty, $"Không thể kết thúc/thanh toán phiên #{sessionId} vì phiên này đã ở trạng thái '{currentStatus}'!");
                    }

                    // 0. Lưu chi tiết các khoảng thời gian tính tiền bàn vào SessionPricingDetails
                    await _pricingRuleService.SaveSessionPricingDetailsAsync(
                        conn, trans, sessionId, dbTableId, dbTableTypeId, pricingSegments);

                    // 1. Thêm bản ghi Hóa đơn (Invoices)
                    const string insertInvoiceSql = @"
                        INSERT INTO Invoices (InvoiceCode, SessionId, UserId, CreatedAt, TableAmount, ServiceAmount, DiscountAmount, TotalAmount, Status, PaymentMethod, PaidAt)
                        VALUES (@InvoiceCode, @SessionId, @UserId, @Now, @TableAmount, @ServiceAmount, @DiscountAmount, @TotalAmount, 'Paid', @PaymentMethod, @Now);
                        SELECT LAST_INSERT_ID();";

                    long invoiceId = 0;
                    using (var cmd = new MySqlCommand(insertInvoiceSql, conn, trans))
                    {
                        cmd.Parameters.AddWithValue("@InvoiceCode", invoiceCode);
                        cmd.Parameters.AddWithValue("@SessionId", sessionId);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@Now", now);
                        cmd.Parameters.AddWithValue("@TableAmount", tableAmount);
                        cmd.Parameters.AddWithValue("@ServiceAmount", serviceAmount);
                        cmd.Parameters.AddWithValue("@DiscountAmount", discountAmount);
                        cmd.Parameters.AddWithValue("@TotalAmount", finalAmount);
                        cmd.Parameters.AddWithValue("@PaymentMethod", paymentMethod);
                        invoiceId = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                    }

                    // 2. Thêm chi tiết dòng tiền giờ vào InvoiceDetails
                    const string insertTableDetailSql = @"
                        INSERT INTO InvoiceDetails (InvoiceId, ItemType, ProductId, Description, Quantity, UnitPrice, Amount)
                        VALUES (@InvoiceId, 'Table', NULL, @Description, @Hours, @HourlyRate, @TableAmount);";

                    using (var cmd = new MySqlCommand(insertTableDetailSql, conn, trans))
                    {
                        cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                        cmd.Parameters.AddWithValue("@Description", $"Tiền giờ chơi ({(int)duration.TotalHours}h {duration.Minutes}m)");
                        cmd.Parameters.AddWithValue("@Hours", Math.Round((decimal)duration.TotalHours, 2));
                        cmd.Parameters.AddWithValue("@HourlyRate", duration.TotalHours > 0 ? Math.Round(tableAmount / (decimal)duration.TotalHours, 0) : tableAmount);
                        cmd.Parameters.AddWithValue("@TableAmount", tableAmount);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 3. Sao chép chi tiết F&B từ SessionProducts sang InvoiceDetails
                    const string copyProductsSql = @"
                        INSERT INTO InvoiceDetails (InvoiceId, ItemType, ProductId, Description, Quantity, UnitPrice, Amount)
                        SELECT @InvoiceId, 'Product', sp.ProductId, p.ProductName, sp.Quantity, sp.UnitPrice, sp.Amount
                        FROM SessionProducts sp
                        INNER JOIN Products p ON sp.ProductId = p.ProductId
                        WHERE sp.SessionId = @SessionId;";

                    using (var cmd = new MySqlCommand(copyProductsSql, conn, trans))
                    {
                        cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                        cmd.Parameters.AddWithValue("@SessionId", sessionId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 4. Đóng phiên chơi (Sessions) với EndTime theo giờ địa phương
                    const string closeSessionSql = @"
                        UPDATE Sessions 
                        SET Status = 'Completed', EndTime = @Now, TableAmount = @TableAmount 
                        WHERE SessionId = @SessionId;";

                    using (var cmd = new MySqlCommand(closeSessionSql, conn, trans))
                    {
                        cmd.Parameters.AddWithValue("@Now", now);
                        cmd.Parameters.AddWithValue("@TableAmount", tableAmount);
                        cmd.Parameters.AddWithValue("@SessionId", sessionId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 5. Trả bàn về trạng thái Available
                    const string resetTableSql = "UPDATE Tables SET Status = 'Available' WHERE TableId = @TableId;";
                    using (var cmd = new MySqlCommand(resetTableSql, conn, trans))
                    {
                        cmd.Parameters.AddWithValue("@TableId", dbTableId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await trans.CommitAsync();
                    return (true, invoiceCode, $"Thanh toán thành công! Hóa đơn: {invoiceCode}");
                }
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Lỗi thanh toán: {ex.Message}");
            }
        }
    }
}

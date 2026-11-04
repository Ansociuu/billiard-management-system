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
            decimal tableAmount = 0;
            decimal serviceAmount = 0;
            TimeSpan duration = TimeSpan.Zero;

            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                // 1. Lấy thông tin thời gian bắt đầu và loại bàn của phiên chơi
                const string sessionSql = @"
                    SELECT s.StartTime, t.TableTypeId
                    FROM Sessions s
                    INNER JOIN Tables t ON s.TableId = t.TableId
                    WHERE s.SessionId = @SessionId;";

                DateTime? startTime = null;
                int tableTypeId = 1;

                using (var cmd = new MySqlCommand(sessionSql, conn))
                {
                    cmd.Parameters.AddWithValue("@SessionId", sessionId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            startTime = reader.GetDateTime("StartTime");
                            tableTypeId = reader.GetInt32("TableTypeId");
                        }
                    }
                }

                if (startTime.HasValue)
                {
                    // Chỉ áp dụng quy tắc giá đang hoạt động (IsActive = TRUE) và còn trong hạn hiệu lực EffectiveFrom - EffectiveTo
                    decimal hourlyRate = await _pricingRuleService.GetEffectiveHourlyRateAsync(tableTypeId, DateTime.Now);
                    duration = DateTime.Now - startTime.Value;
                    if (duration.TotalMinutes < 1)
                    {
                        duration = TimeSpan.FromMinutes(1);
                    }

                    // Tính tiền bàn theo phút
                    tableAmount = Math.Round((decimal)duration.TotalHours * hourlyRate, 0);
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
            return (tableAmount, serviceAmount, totalAmount, duration);
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
                // Tính toán số tiền
                var (tableAmount, serviceAmount, totalAmountBeforeDiscount, duration) = await CalculateSessionBillingAsync(sessionId);
                decimal finalAmount = Math.Max(0, totalAmountBeforeDiscount - discountAmount);
                string invoiceCode = $"HD{DateTime.Now:yyyyMMddHHmmss}";

                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                using (var trans = await conn.BeginTransactionAsync())
                {
                    // 1. Thêm bản ghi Hóa đơn (Invoices)
                    const string insertInvoiceSql = @"
                        INSERT INTO Invoices (InvoiceCode, SessionId, UserId, CreatedAt, TableAmount, ServiceAmount, DiscountAmount, TotalAmount, Status, PaymentMethod, PaidAt)
                        VALUES (@InvoiceCode, @SessionId, @UserId, NOW(), @TableAmount, @ServiceAmount, @DiscountAmount, @TotalAmount, 'Paid', @PaymentMethod, NOW());
                        SELECT LAST_INSERT_ID();";

                    long invoiceId = 0;
                    using (var cmd = new MySqlCommand(insertInvoiceSql, conn, trans))
                    {
                        cmd.Parameters.AddWithValue("@InvoiceCode", invoiceCode);
                        cmd.Parameters.AddWithValue("@SessionId", sessionId);
                        cmd.Parameters.AddWithValue("@UserId", userId);
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

                    // 4. Đóng phiên chơi (Sessions)
                    const string closeSessionSql = @"
                        UPDATE Sessions 
                        SET Status = 'Completed', EndTime = NOW(), TableAmount = @TableAmount 
                        WHERE SessionId = @SessionId;";

                    using (var cmd = new MySqlCommand(closeSessionSql, conn, trans))
                    {
                        cmd.Parameters.AddWithValue("@TableAmount", tableAmount);
                        cmd.Parameters.AddWithValue("@SessionId", sessionId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 5. Trả bàn về trạng thái Available
                    const string resetTableSql = "UPDATE Tables SET Status = 'Available' WHERE TableId = @TableId;";
                    using (var cmd = new MySqlCommand(resetTableSql, conn, trans))
                    {
                        cmd.Parameters.AddWithValue("@TableId", tableId);
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

using System;
using System.Collections.Concurrent;
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
        (bool Valid, string Message) ValidateInvoiceDetail(InvoiceDetail detail);
        Task<(bool Success, Invoice Invoice, string Message)> CreateOrRefreshInvoiceForSessionAsync(long sessionId, int tableId, long userId, decimal discountAmount = 0m);
        Task<Invoice> GetInvoiceBySessionIdAsync(long sessionId);
        Task<List<Invoice>> GetInvoicesAsync(DateTime? fromDate = null, DateTime? toDate = null, string statusFilter = null, string keyword = null);
        Task<List<InvoiceDetail>> GetInvoiceDetailsAsync(long invoiceId);
        Task<List<BilliardSession>> GetSessionsHistoryAsync(DateTime? fromDate = null, DateTime? toDate = null, string statusFilter = null, string keyword = null);
        Task<(bool Success, string InvoiceCode, string Message)> PayInvoiceAsync(long invoiceId, long userId, decimal discountAmount, string paymentMethod);
        Task<(bool Success, string InvoiceCode, string Message)> CheckoutAsync(long sessionId, int tableId, long userId, decimal discountAmount, string paymentMethod);
    }

    public class OrderService : IOrderService
    {
        private static readonly ConcurrentDictionary<long, byte> ActiveSessionCheckouts = new ConcurrentDictionary<long, byte>();
        private static readonly ConcurrentDictionary<long, byte> ActiveInvoicePayments = new ConcurrentDictionary<long, byte>();

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

        /// <summary>
        /// Kiểm tra tính hợp lệ của dòng chi tiết hóa đơn (ItemType = 'Table' hoặc 'Product', và ProductId tương ứng).
        /// </summary>
        public (bool Valid, string Message) ValidateInvoiceDetail(InvoiceDetail detail)
        {
            if (detail == null)
            {
                return (false, "Dòng chi tiết hóa đơn không được rỗng.");
            }

            if (string.IsNullOrWhiteSpace(detail.ItemType))
            {
                return (false, "Loại dòng chi tiết hóa đơn (ItemType) không được để trống.");
            }

            if (string.Equals(detail.ItemType, "Table", StringComparison.OrdinalIgnoreCase))
            {
                if (detail.ProductId.HasValue)
                {
                    return (false, "Dòng chi tiết tiền bàn (ItemType = 'Table') không được gắn mã sản phẩm (ProductId phải là NULL).");
                }
            }
            else if (string.Equals(detail.ItemType, "Product", StringComparison.OrdinalIgnoreCase))
            {
                if (!detail.ProductId.HasValue || detail.ProductId.Value <= 0)
                {
                    return (false, "Dòng chi tiết sản phẩm (ItemType = 'Product') bắt buộc phải có mã sản phẩm (ProductId > 0) hợp lệ.");
                }
            }
            else
            {
                return (false, $"ItemType '{detail.ItemType}' không hợp lệ (Chỉ chấp nhận 'Table' hoặc 'Product').");
            }

            if (string.IsNullOrWhiteSpace(detail.Description))
            {
                return (false, "Mô tả dòng chi tiết hóa đơn (Description) không được để trống.");
            }

            if (detail.Quantity <= 0)
            {
                return (false, "Số lượng/thời lượng trên dòng hóa đơn phải lớn hơn 0.");
            }

            if (detail.UnitPrice < 0 || detail.Amount < 0)
            {
                return (false, "Đơn giá và thành tiền trên dòng hóa đơn không được âm.");
            }

            return (true, string.Empty);
        }

        private static async Task<string> GenerateUniqueInvoiceCodeAsync(
            MySqlConnection conn, MySqlTransaction trans, DateTime now, long sessionId)
        {
            string baseCode = $"HD{now:yyyyMMddHHmmss}-{sessionId}";
            if (baseCode.Length > 26)
            {
                baseCode = baseCode.Substring(0, 26);
            }

            string candidate = baseCode;
            int suffix = 1;

            const string checkCodeSql = "SELECT COUNT(1) FROM Invoices WHERE InvoiceCode = @Code;";
            while (true)
            {
                using (var cmd = new MySqlCommand(checkCodeSql, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@Code", candidate);
                    long count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                    if (count == 0)
                    {
                        return candidate;
                    }
                }

                candidate = $"{baseCode}-{suffix}";
                suffix++;
            }
        }

        /// <summary>
        /// Tạo hoặc cập nhật hóa đơn ở trạng thái Pending từ phiên chơi hợp lệ (lưu Invoices + InvoiceDetails trong 1 transaction).
        /// Không tạo nhiều hóa đơn cho cùng một phiên (SessionId là UNIQUE).
        /// </summary>
        public async Task<(bool Success, Invoice Invoice, string Message)> CreateOrRefreshInvoiceForSessionAsync(
            long sessionId, int tableId, long userId, decimal discountAmount = 0m)
        {
            if (sessionId <= 0)
            {
                return (false, null, "Mã phiên chơi không hợp lệ!");
            }

            if (discountAmount < 0)
            {
                return (false, null, "Số tiền giảm giá không được âm!");
            }

            try
            {
                DateTime now = DateTime.Now;
                var (tableAmount, serviceAmount, totalBeforeDiscount, duration, pricingSegments) =
                    await CalculateSessionBillingDetailedAsync(sessionId, now);
                decimal clampedDiscount = Math.Min(totalBeforeDiscount, Math.Max(0m, discountAmount));
                decimal finalAmount = Math.Max(0m, totalBeforeDiscount - clampedDiscount);

                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                using (var trans = await conn.BeginTransactionAsync())
                {
                    const string checkSessionSql = @"
                        SELECT s.Status, s.TableId, t.TableTypeId, t.TableNumber
                        FROM Sessions s
                        INNER JOIN Tables t ON s.TableId = t.TableId
                        WHERE s.SessionId = @SessionId
                        FOR UPDATE;";

                    string sessionStatus;
                    int dbTableId;
                    int dbTableTypeId;
                    string tableNumber;

                    using (var cmdSession = new MySqlCommand(checkSessionSql, conn, trans))
                    {
                        cmdSession.Parameters.AddWithValue("@SessionId", sessionId);
                        using (var reader = await cmdSession.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                await trans.RollbackAsync();
                                return (false, null, $"Không tìm thấy phiên chơi #{sessionId}!");
                            }

                            sessionStatus = reader.GetString("Status");
                            dbTableId = reader.GetInt32("TableId");
                            dbTableTypeId = reader.GetInt32("TableTypeId");
                            tableNumber = reader.GetString("TableNumber");
                        }
                    }

                    if (string.Equals(sessionStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    {
                        await trans.RollbackAsync();
                        return (false, null, $"Không thể lập hóa đơn cho phiên #{sessionId} đã bị hủy!");
                    }

                    // Kiểm tra xem phiên đã có hóa đơn hay chưa (đảm bảo mỗi phiên chỉ có tối đa 1 hóa đơn)
                    const string checkInvoiceSql = @"
                        SELECT InvoiceId, InvoiceCode, Status
                        FROM Invoices
                        WHERE SessionId = @SessionId
                        FOR UPDATE;";

                    long existingInvoiceId = 0;
                    string existingInvoiceCode = null;
                    string existingInvoiceStatus = null;

                    using (var cmdInv = new MySqlCommand(checkInvoiceSql, conn, trans))
                    {
                        cmdInv.Parameters.AddWithValue("@SessionId", sessionId);
                        using (var reader = await cmdInv.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                existingInvoiceId = reader.GetInt64("InvoiceId");
                                existingInvoiceCode = reader.GetString("InvoiceCode");
                                existingInvoiceStatus = reader.GetString("Status");
                            }
                        }
                    }

                    if (existingInvoiceId > 0 && string.Equals(existingInvoiceStatus, "Paid", StringComparison.OrdinalIgnoreCase))
                    {
                        await trans.RollbackAsync();
                        return (false, null, $"Phiên chơi #{sessionId} đã được lập hóa đơn và thanh toán trước đó (Mã HĐ: {existingInvoiceCode})!");
                    }

                    await _pricingRuleService.SaveSessionPricingDetailsAsync(
                        conn, trans, sessionId, dbTableId, dbTableTypeId, pricingSegments);

                    long invoiceId;
                    string invoiceCode;

                    if (existingInvoiceId > 0)
                    {
                        invoiceId = existingInvoiceId;
                        invoiceCode = existingInvoiceCode;

                        const string updateInvSql = @"
                            UPDATE Invoices
                            SET UserId = @UserId,
                                TableAmount = @TableAmount,
                                ServiceAmount = @ServiceAmount,
                                DiscountAmount = @DiscountAmount,
                                TotalAmount = @TotalAmount,
                                Status = 'Pending'
                            WHERE InvoiceId = @InvoiceId;";

                        using (var cmdUpd = new MySqlCommand(updateInvSql, conn, trans))
                        {
                            cmdUpd.Parameters.AddWithValue("@UserId", userId);
                            cmdUpd.Parameters.AddWithValue("@TableAmount", tableAmount);
                            cmdUpd.Parameters.AddWithValue("@ServiceAmount", serviceAmount);
                            cmdUpd.Parameters.AddWithValue("@DiscountAmount", clampedDiscount);
                            cmdUpd.Parameters.AddWithValue("@TotalAmount", finalAmount);
                            cmdUpd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                            await cmdUpd.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        invoiceCode = await GenerateUniqueInvoiceCodeAsync(conn, trans, now, sessionId);

                        const string insertInvSql = @"
                            INSERT INTO Invoices
                                (InvoiceCode, SessionId, UserId, CreatedAt, TableAmount, ServiceAmount, DiscountAmount, TotalAmount, Status, PaymentMethod, PaidAt)
                            VALUES
                                (@InvoiceCode, @SessionId, @UserId, @Now, @TableAmount, @ServiceAmount, @DiscountAmount, @TotalAmount, 'Pending', NULL, NULL);
                            SELECT LAST_INSERT_ID();";

                        using (var cmdIns = new MySqlCommand(insertInvSql, conn, trans))
                        {
                            cmdIns.Parameters.AddWithValue("@InvoiceCode", invoiceCode);
                            cmdIns.Parameters.AddWithValue("@SessionId", sessionId);
                            cmdIns.Parameters.AddWithValue("@UserId", userId);
                            cmdIns.Parameters.AddWithValue("@Now", now);
                            cmdIns.Parameters.AddWithValue("@TableAmount", tableAmount);
                            cmdIns.Parameters.AddWithValue("@ServiceAmount", serviceAmount);
                            cmdIns.Parameters.AddWithValue("@DiscountAmount", clampedDiscount);
                            cmdIns.Parameters.AddWithValue("@TotalAmount", finalAmount);
                            invoiceId = Convert.ToInt64(await cmdIns.ExecuteScalarAsync());
                        }
                    }

                    var (detailsOk, detailsErr, detailsList) = await RebuildInvoiceDetailsInTransactionAsync(
                        conn, trans, invoiceId, sessionId, duration, tableAmount, pricingSegments);
                    if (!detailsOk)
                    {
                        await trans.RollbackAsync();
                        return (false, null, detailsErr);
                    }

                    await trans.CommitAsync();

                    var invoice = new Invoice
                    {
                        InvoiceId = invoiceId,
                        InvoiceCode = invoiceCode,
                        SessionId = sessionId,
                        UserId = userId,
                        CreatedAt = now,
                        TableAmount = tableAmount,
                        ServiceAmount = serviceAmount,
                        DiscountAmount = clampedDiscount,
                        TotalAmount = finalAmount,
                        Status = "Pending",
                        TableNumber = tableNumber,
                        Details = new System.Collections.ObjectModel.ObservableCollection<InvoiceDetail>(detailsList)
                    };

                    return (true, invoice, $"Đã lập hóa đơn tạm tính {invoiceCode} thành công!");
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Lỗi lập hóa đơn: {ex.Message}");
            }
        }

        private async Task<(bool Success, string ErrorMessage, List<InvoiceDetail> Details)> RebuildInvoiceDetailsInTransactionAsync(
            MySqlConnection conn, MySqlTransaction trans,
            long invoiceId, long sessionId, TimeSpan duration, decimal tableAmount,
            List<SessionPricingDetail> pricingSegments)
        {
            var detailsToInsert = new List<InvoiceDetail>();

            // 1. Tạo các dòng chi tiết tiền bàn (ItemType = 'Table', ProductId = null)
            if (pricingSegments != null && pricingSegments.Count > 0)
            {
                foreach (var seg in pricingSegments)
                {
                    decimal hours = Math.Max(0.01m, Math.Round(seg.DurationMinutes / 60m, 2));
                    detailsToInsert.Add(new InvoiceDetail
                    {
                        InvoiceId = invoiceId,
                        ItemType = "Table",
                        ProductId = null,
                        Description = $"Tiền giờ chơi ({seg.StartTime:HH:mm} - {seg.EndTime:HH:mm}) • {seg.RuleSummaryDisplay}",
                        Quantity = hours,
                        UnitPrice = seg.FinalHourlyRate,
                        Amount = seg.Amount
                    });
                }
            }
            else
            {
                decimal hours = Math.Max(0.01m, Math.Round((decimal)duration.TotalHours, 2));
                decimal unitRate = duration.TotalHours > 0 ? Math.Round(tableAmount / (decimal)duration.TotalHours, 0) : tableAmount;
                detailsToInsert.Add(new InvoiceDetail
                {
                    InvoiceId = invoiceId,
                    ItemType = "Table",
                    ProductId = null,
                    Description = $"Tiền giờ chơi ({(int)duration.TotalHours}h {duration.Minutes}m)",
                    Quantity = hours,
                    UnitPrice = unitRate,
                    Amount = tableAmount
                });
            }

            // 2. Lấy các dòng sản phẩm đã gọi từ SessionProducts (ItemType = 'Product', ProductId > 0)
            const string selectProductsSql = @"
                SELECT sp.ProductId, p.ProductName, sp.Quantity, sp.UnitPrice, sp.Amount
                FROM SessionProducts sp
                INNER JOIN Products p ON sp.ProductId = p.ProductId
                WHERE sp.SessionId = @SessionId
                ORDER BY sp.SessionProductId ASC;";

            using (var cmdProd = new MySqlCommand(selectProductsSql, conn, trans))
            {
                cmdProd.Parameters.AddWithValue("@SessionId", sessionId);
                using (var reader = await cmdProd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        detailsToInsert.Add(new InvoiceDetail
                        {
                            InvoiceId = invoiceId,
                            ItemType = "Product",
                            ProductId = reader.GetInt32("ProductId"),
                            Description = reader.GetString("ProductName"),
                            Quantity = reader.GetInt32("Quantity"),
                            UnitPrice = reader.GetDecimal("UnitPrice"),
                            Amount = reader.GetDecimal("Amount")
                        });
                    }
                }
            }

            // 3. Kiểm tra tính hợp lệ của từng dòng InvoiceDetail trước khi ghi DB
            foreach (var detail in detailsToInsert)
            {
                var (valid, msg) = ValidateInvoiceDetail(detail);
                if (!valid)
                {
                    return (false, msg, null);
                }
            }

            // 4. Xóa chi tiết cũ của hóa đơn (nếu có) và thêm mới toàn bộ trong transaction
            const string deleteDetailsSql = "DELETE FROM InvoiceDetails WHERE InvoiceId = @InvoiceId;";
            using (var cmdDel = new MySqlCommand(deleteDetailsSql, conn, trans))
            {
                cmdDel.Parameters.AddWithValue("@InvoiceId", invoiceId);
                await cmdDel.ExecuteNonQueryAsync();
            }

            const string insertDetailSql = @"
                INSERT INTO InvoiceDetails
                    (InvoiceId, ItemType, ProductId, Description, Quantity, UnitPrice, Amount)
                VALUES
                    (@InvoiceId, @ItemType, @ProductId, @Description, @Quantity, @UnitPrice, @Amount);
                SELECT LAST_INSERT_ID();";

            foreach (var detail in detailsToInsert)
            {
                using (var cmdIns = new MySqlCommand(insertDetailSql, conn, trans))
                {
                    cmdIns.Parameters.AddWithValue("@InvoiceId", detail.InvoiceId);
                    cmdIns.Parameters.AddWithValue("@ItemType", detail.ItemType);
                    cmdIns.Parameters.AddWithValue("@ProductId", detail.ProductId.HasValue ? (object)detail.ProductId.Value : DBNull.Value);
                    cmdIns.Parameters.AddWithValue("@Description", detail.Description);
                    cmdIns.Parameters.AddWithValue("@Quantity", detail.Quantity);
                    cmdIns.Parameters.AddWithValue("@UnitPrice", detail.UnitPrice);
                    cmdIns.Parameters.AddWithValue("@Amount", detail.Amount);
                    detail.InvoiceDetailId = Convert.ToInt64(await cmdIns.ExecuteScalarAsync());
                }
            }

            return (true, string.Empty, detailsToInsert);
        }

        public async Task<Invoice> GetInvoiceBySessionIdAsync(long sessionId)
        {
            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                const string sql = @"
                    SELECT i.InvoiceId, i.InvoiceCode, i.SessionId, i.UserId, i.CreatedAt,
                           i.TableAmount, i.ServiceAmount, i.DiscountAmount, i.TotalAmount,
                           i.Status, i.PaymentMethod, i.PaidAt,
                           u.FullName AS CashierName, t.TableNumber
                    FROM Invoices i
                    INNER JOIN Sessions s ON i.SessionId = s.SessionId
                    INNER JOIN Tables t ON s.TableId = t.TableId
                    INNER JOIN Users u ON i.UserId = u.UserId
                    WHERE i.SessionId = @SessionId
                    LIMIT 1;";

                Invoice invoice = null;
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@SessionId", sessionId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int pmOrd = reader.GetOrdinal("PaymentMethod");
                            int paidOrd = reader.GetOrdinal("PaidAt");

                            invoice = new Invoice
                            {
                                InvoiceId = reader.GetInt64("InvoiceId"),
                                InvoiceCode = reader.GetString("InvoiceCode"),
                                SessionId = reader.GetInt64("SessionId"),
                                UserId = reader.GetInt64("UserId"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                TableAmount = reader.GetDecimal("TableAmount"),
                                ServiceAmount = reader.GetDecimal("ServiceAmount"),
                                DiscountAmount = reader.GetDecimal("DiscountAmount"),
                                TotalAmount = reader.GetDecimal("TotalAmount"),
                                Status = reader.GetString("Status"),
                                PaymentMethod = reader.IsDBNull(pmOrd) ? null : reader.GetString(pmOrd),
                                PaidAt = reader.IsDBNull(paidOrd) ? (DateTime?)null : reader.GetDateTime(paidOrd),
                                CashierName = reader.GetString("CashierName"),
                                TableNumber = reader.GetString("TableNumber")
                            };
                        }
                    }
                }

                if (invoice == null)
                {
                    return null;
                }

                const string detailSql = @"
                    SELECT InvoiceDetailId, InvoiceId, ItemType, ProductId, Description, Quantity, UnitPrice, Amount
                    FROM InvoiceDetails
                    WHERE InvoiceId = @InvoiceId
                    ORDER BY InvoiceDetailId ASC;";

                using (var cmdDet = new MySqlCommand(detailSql, conn))
                {
                    cmdDet.Parameters.AddWithValue("@InvoiceId", invoice.InvoiceId);
                    using (var reader = await cmdDet.ExecuteReaderAsync())
                    {
                        int prodOrd = reader.GetOrdinal("ProductId");
                        while (await reader.ReadAsync())
                        {
                            invoice.Details.Add(new InvoiceDetail
                            {
                                InvoiceDetailId = reader.GetInt64("InvoiceDetailId"),
                                InvoiceId = reader.GetInt64("InvoiceId"),
                                ItemType = reader.GetString("ItemType"),
                                ProductId = reader.IsDBNull(prodOrd) ? (int?)null : reader.GetInt32(prodOrd),
                                Description = reader.GetString("Description"),
                                Quantity = reader.GetDecimal("Quantity"),
                                UnitPrice = reader.GetDecimal("UnitPrice"),
                                Amount = reader.GetDecimal("Amount")
                            });
                        }
                    }
                }

                return invoice;
            }
        }

        public async Task<List<Invoice>> GetInvoicesAsync(
            DateTime? fromDate = null, DateTime? toDate = null, string statusFilter = null, string keyword = null)
        {
            var list = new List<Invoice>();

            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                string sql = @"
                    SELECT i.InvoiceId, i.InvoiceCode, i.SessionId, i.UserId, i.CreatedAt,
                           i.TableAmount, i.ServiceAmount, i.DiscountAmount, i.TotalAmount,
                           i.Status, i.PaymentMethod, i.PaidAt,
                           u.FullName AS CashierName, t.TableNumber,
                           COALESCE(c.FullName, 'Khách vãng lai') AS CustomerName,
                           s.StartTime AS SessionStartTime, s.EndTime AS SessionEndTime
                    FROM Invoices i
                    INNER JOIN Sessions s ON i.SessionId = s.SessionId
                    INNER JOIN Tables t ON s.TableId = t.TableId
                    INNER JOIN Users u ON i.UserId = u.UserId
                    LEFT JOIN Customers c ON s.CustomerId = c.CustomerId
                    WHERE 1 = 1";

                if (fromDate.HasValue)
                {
                    sql += " AND i.CreatedAt >= @FromDate";
                }

                if (toDate.HasValue)
                {
                    sql += " AND i.CreatedAt < @ToDateExclusive";
                }

                if (!string.IsNullOrWhiteSpace(statusFilter) && !string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
                {
                    sql += " AND i.Status = @Status";
                }

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    sql += @" AND (i.InvoiceCode LIKE @Keyword
                                   OR t.TableNumber LIKE @Keyword
                                   OR u.FullName LIKE @Keyword
                                   OR COALESCE(c.FullName, '') LIKE @Keyword
                                   OR CAST(i.SessionId AS CHAR) LIKE @Keyword)";
                }

                sql += " ORDER BY i.CreatedAt DESC, i.InvoiceId DESC;";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (fromDate.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@FromDate", fromDate.Value.Date);
                    }

                    if (toDate.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@ToDateExclusive", toDate.Value.Date.AddDays(1));
                    }

                    if (!string.IsNullOrWhiteSpace(statusFilter) && !string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
                    {
                        cmd.Parameters.AddWithValue("@Status", statusFilter.Trim());
                    }

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        cmd.Parameters.AddWithValue("@Keyword", $"%{keyword.Trim()}%");
                    }

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        int pmOrd = reader.GetOrdinal("PaymentMethod");
                        int paidOrd = reader.GetOrdinal("PaidAt");
                        int endOrd = reader.GetOrdinal("SessionEndTime");

                        while (await reader.ReadAsync())
                        {
                            list.Add(new Invoice
                            {
                                InvoiceId = reader.GetInt64("InvoiceId"),
                                InvoiceCode = reader.GetString("InvoiceCode"),
                                SessionId = reader.GetInt64("SessionId"),
                                UserId = reader.GetInt64("UserId"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                TableAmount = reader.GetDecimal("TableAmount"),
                                ServiceAmount = reader.GetDecimal("ServiceAmount"),
                                DiscountAmount = reader.GetDecimal("DiscountAmount"),
                                TotalAmount = reader.GetDecimal("TotalAmount"),
                                Status = reader.GetString("Status"),
                                PaymentMethod = reader.IsDBNull(pmOrd) ? null : reader.GetString(pmOrd),
                                PaidAt = reader.IsDBNull(paidOrd) ? (DateTime?)null : reader.GetDateTime(paidOrd),
                                CashierName = reader.GetString("CashierName"),
                                TableNumber = reader.GetString("TableNumber"),
                                CustomerName = reader.GetString("CustomerName"),
                                SessionStartTime = reader.GetDateTime("SessionStartTime"),
                                SessionEndTime = reader.IsDBNull(endOrd) ? (DateTime?)null : reader.GetDateTime(endOrd)
                            });
                        }
                    }
                }
            }

            return list;
        }

        public async Task<List<InvoiceDetail>> GetInvoiceDetailsAsync(long invoiceId)
        {
            var list = new List<InvoiceDetail>();
            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                const string sql = @"
                    SELECT InvoiceDetailId, InvoiceId, ItemType, ProductId, Description, Quantity, UnitPrice, Amount
                    FROM InvoiceDetails
                    WHERE InvoiceId = @InvoiceId
                    ORDER BY CASE WHEN ItemType = 'Table' THEN 0 ELSE 1 END, InvoiceDetailId ASC;";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        int prodOrd = reader.GetOrdinal("ProductId");
                        while (await reader.ReadAsync())
                        {
                            list.Add(new InvoiceDetail
                            {
                                InvoiceDetailId = reader.GetInt64("InvoiceDetailId"),
                                InvoiceId = reader.GetInt64("InvoiceId"),
                                ItemType = reader.GetString("ItemType"),
                                ProductId = reader.IsDBNull(prodOrd) ? (int?)null : reader.GetInt32(prodOrd),
                                Description = reader.GetString("Description"),
                                Quantity = reader.GetDecimal("Quantity"),
                                UnitPrice = reader.GetDecimal("UnitPrice"),
                                Amount = reader.GetDecimal("Amount")
                            });
                        }
                    }
                }
            }
            return list;
        }

        public async Task<List<BilliardSession>> GetSessionsHistoryAsync(
            DateTime? fromDate = null, DateTime? toDate = null, string statusFilter = null, string keyword = null)
        {
            var list = new List<BilliardSession>();

            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                string sql = @"
                    SELECT s.SessionId, s.TableId, s.UserId, s.CustomerId, s.StartTime, s.EndTime, s.Status, s.TableAmount,
                           t.TableNumber, tt.TypeName, u.FullName AS StaffName,
                           COALESCE(c.FullName, 'Khách vãng lai') AS CustomerName,
                           i.InvoiceCode,
                           COALESCE((SELECT SUM(sp.Amount) FROM SessionProducts sp WHERE sp.SessionId = s.SessionId), 0) AS ServiceAmount
                    FROM Sessions s
                    INNER JOIN Tables t ON s.TableId = t.TableId
                    INNER JOIN TableTypes tt ON t.TableTypeId = tt.TableTypeId
                    INNER JOIN Users u ON s.UserId = u.UserId
                    LEFT JOIN Customers c ON s.CustomerId = c.CustomerId
                    LEFT JOIN Invoices i ON s.SessionId = i.SessionId
                    WHERE 1 = 1";

                if (fromDate.HasValue)
                {
                    sql += " AND s.StartTime >= @FromDate";
                }

                if (toDate.HasValue)
                {
                    sql += " AND s.StartTime < @ToDateExclusive";
                }

                if (!string.IsNullOrWhiteSpace(statusFilter) && !string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
                {
                    sql += " AND s.Status = @Status";
                }

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    sql += @" AND (t.TableNumber LIKE @Keyword
                                   OR tt.TypeName LIKE @Keyword
                                   OR u.FullName LIKE @Keyword
                                   OR COALESCE(c.FullName, '') LIKE @Keyword
                                   OR COALESCE(i.InvoiceCode, '') LIKE @Keyword
                                   OR CAST(s.SessionId AS CHAR) LIKE @Keyword)";
                }

                sql += " ORDER BY s.StartTime DESC, s.SessionId DESC;";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (fromDate.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@FromDate", fromDate.Value.Date);
                    }

                    if (toDate.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@ToDateExclusive", toDate.Value.Date.AddDays(1));
                    }

                    if (!string.IsNullOrWhiteSpace(statusFilter) && !string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
                    {
                        cmd.Parameters.AddWithValue("@Status", statusFilter.Trim());
                    }

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        cmd.Parameters.AddWithValue("@Keyword", $"%{keyword.Trim()}%");
                    }

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        int custIdOrd = reader.GetOrdinal("CustomerId");
                        int endOrd = reader.GetOrdinal("EndTime");
                        int invCodeOrd = reader.GetOrdinal("InvoiceCode");

                        while (await reader.ReadAsync())
                        {
                            list.Add(new BilliardSession
                            {
                                SessionId = reader.GetInt64("SessionId"),
                                TableId = reader.GetInt32("TableId"),
                                UserId = reader.GetInt64("UserId"),
                                CustomerId = reader.IsDBNull(custIdOrd) ? (long?)null : reader.GetInt64(custIdOrd),
                                StartTime = reader.GetDateTime("StartTime"),
                                EndTime = reader.IsDBNull(endOrd) ? (DateTime?)null : reader.GetDateTime(endOrd),
                                Status = reader.GetString("Status"),
                                TableAmount = reader.GetDecimal("TableAmount"),
                                TableNumber = reader.GetString("TableNumber"),
                                TypeName = reader.GetString("TypeName"),
                                StaffName = reader.GetString("StaffName"),
                                CustomerName = reader.GetString("CustomerName"),
                                InvoiceCode = reader.IsDBNull(invCodeOrd) ? "Chưa lập HĐ" : reader.GetString(invCodeOrd),
                                ServiceAmount = reader.GetDecimal("ServiceAmount")
                            });
                        }
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Thanh toán một hóa đơn đang ở trạng thái Pending (hỗ trợ Cash / BankTransfer),
        /// cập nhật PaymentMethod, PaidAt, Status = 'Paid' và đồng bộ đóng phiên chơi + trả bàn trong 1 transaction.
        /// Chặn thanh toán trùng khi người dùng bấm nhiều lần.
        /// </summary>
        public async Task<(bool Success, string InvoiceCode, string Message)> PayInvoiceAsync(
            long invoiceId, long userId, decimal discountAmount, string paymentMethod)
        {
            if (invoiceId <= 0)
            {
                return (false, string.Empty, "Mã hóa đơn không hợp lệ!");
            }

            if (!ActiveInvoicePayments.TryAdd(invoiceId, 1))
            {
                return (false, string.Empty, $"Hóa đơn #{invoiceId} đang được xử lý thanh toán, vui lòng không bấm lặp lại!");
            }

            try
            {
                var (canPay, payDeniedMsg) = UserSession.CheckAccess(PermissionCodes.InvoicePay, "Thanh toán hóa đơn");
                if (!canPay)
                {
                    await _auditLogService.LogAsync(userId, "ACCESS_DENIED", "Invoice", invoiceId, payDeniedMsg);
                    return (false, string.Empty, payDeniedMsg);
                }

                if (discountAmount < 0)
                {
                    return (false, string.Empty, "Số tiền giảm giá không được âm!");
                }

                string normalizedMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Cash" : paymentMethod.Trim();
                if (!string.Equals(normalizedMethod, "Cash", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(normalizedMethod, "BankTransfer", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, string.Empty, "Phương thức thanh toán không hợp lệ (Chỉ chấp nhận Cash hoặc BankTransfer)!");
                }
                normalizedMethod = string.Equals(normalizedMethod, "BankTransfer", StringComparison.OrdinalIgnoreCase)
                    ? "BankTransfer"
                    : "Cash";

                DateTime now = DateTime.Now;

                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                using (var trans = await conn.BeginTransactionAsync())
                {
                    const string lockInvSql = @"
                        SELECT i.InvoiceCode, i.SessionId, i.TableAmount, i.ServiceAmount, i.Status,
                               s.TableId, s.Status AS SessionStatus
                        FROM Invoices i
                        INNER JOIN Sessions s ON i.SessionId = s.SessionId
                        WHERE i.InvoiceId = @InvoiceId
                        FOR UPDATE;";

                    string invoiceCode;
                    long sessionId;
                    decimal tableAmount;
                    decimal serviceAmount;
                    string invStatus;
                    int tableId;
                    string sessionStatus;

                    using (var cmd = new MySqlCommand(lockInvSql, conn, trans))
                    {
                        cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                await trans.RollbackAsync();
                                return (false, string.Empty, $"Không tìm thấy hóa đơn #{invoiceId}!");
                            }

                            invoiceCode = reader.GetString("InvoiceCode");
                            sessionId = reader.GetInt64("SessionId");
                            tableAmount = reader.GetDecimal("TableAmount");
                            serviceAmount = reader.GetDecimal("ServiceAmount");
                            invStatus = reader.GetString("Status");
                            tableId = reader.GetInt32("TableId");
                            sessionStatus = reader.GetString("SessionStatus");
                        }
                    }

                    if (string.Equals(invStatus, "Paid", StringComparison.OrdinalIgnoreCase))
                    {
                        await trans.RollbackAsync();
                        return (false, invoiceCode, $"Hóa đơn {invoiceCode} đã được thanh toán trước đó!");
                    }

                    if (string.Equals(invStatus, "Cancelled", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(sessionStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    {
                        await trans.RollbackAsync();
                        return (false, invoiceCode, $"Không thể thanh toán hóa đơn {invoiceCode} vì hóa đơn hoặc phiên chơi đã bị hủy!");
                    }

                    decimal totalBeforeDiscount = tableAmount + serviceAmount;
                    decimal clampedDiscount = Math.Min(totalBeforeDiscount, Math.Max(0m, discountAmount));
                    decimal finalAmount = Math.Max(0m, totalBeforeDiscount - clampedDiscount);

                    const string paySql = @"
                        UPDATE Invoices
                        SET UserId = @UserId,
                            DiscountAmount = @DiscountAmount,
                            TotalAmount = @TotalAmount,
                            Status = 'Paid',
                            PaymentMethod = @PaymentMethod,
                            PaidAt = @Now
                        WHERE InvoiceId = @InvoiceId;";

                    using (var cmdPay = new MySqlCommand(paySql, conn, trans))
                    {
                        cmdPay.Parameters.AddWithValue("@UserId", userId);
                        cmdPay.Parameters.AddWithValue("@DiscountAmount", clampedDiscount);
                        cmdPay.Parameters.AddWithValue("@TotalAmount", finalAmount);
                        cmdPay.Parameters.AddWithValue("@PaymentMethod", normalizedMethod);
                        cmdPay.Parameters.AddWithValue("@Now", now);
                        cmdPay.Parameters.AddWithValue("@InvoiceId", invoiceId);
                        await cmdPay.ExecuteNonQueryAsync();
                    }

                    if (string.Equals(sessionStatus, "Active", StringComparison.OrdinalIgnoreCase))
                    {
                        const string closeSessionSql = @"
                            UPDATE Sessions
                            SET Status = 'Completed', EndTime = @Now, TableAmount = @TableAmount
                            WHERE SessionId = @SessionId;";

                        using (var cmdClose = new MySqlCommand(closeSessionSql, conn, trans))
                        {
                            cmdClose.Parameters.AddWithValue("@Now", now);
                            cmdClose.Parameters.AddWithValue("@TableAmount", tableAmount);
                            cmdClose.Parameters.AddWithValue("@SessionId", sessionId);
                            await cmdClose.ExecuteNonQueryAsync();
                        }
                    }

                    const string resetTableSql = "UPDATE Tables SET Status = 'Available' WHERE TableId = @TableId;";
                    using (var cmdTable = new MySqlCommand(resetTableSql, conn, trans))
                    {
                        cmdTable.Parameters.AddWithValue("@TableId", tableId);
                        await cmdTable.ExecuteNonQueryAsync();
                    }

                    await trans.CommitAsync();

                    await _auditLogService.LogAsync(
                        userId, "INVOICE_PAID", "Invoice", invoiceId,
                        $"Hoàn tất thanh toán hóa đơn {invoiceCode} (PT: {normalizedMethod}, Tổng: {finalAmount:N0}đ).");

                    return (true, invoiceCode, $"Thanh toán thành công hóa đơn {invoiceCode} ({finalAmount:N0} đ)!");
                }
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Lỗi thanh toán hóa đơn: {ex.Message}");
            }
            finally
            {
                ActiveInvoicePayments.TryRemove(invoiceId, out _);
            }
        }

        public async Task<(bool Success, string InvoiceCode, string Message)> CheckoutAsync(
            long sessionId, int tableId, long userId, decimal discountAmount, string paymentMethod)
        {
            if (sessionId <= 0)
            {
                return (false, string.Empty, "Mã phiên chơi không hợp lệ!");
            }

            // Chặn bấm đúp / gọi trùng song song trên cùng 1 phiên chơi
            if (!ActiveSessionCheckouts.TryAdd(sessionId, 1))
            {
                return (false, string.Empty, $"Phiên chơi #{sessionId} đang được xử lý thanh toán, vui lòng không bấm nhiều lần!");
            }

            // Kiểm tra quyền kết thúc phiên (SESSION_CLOSE) và thanh toán hóa đơn (INVOICE_PAY)
            var (canClose, closeDeniedMsg) = UserSession.CheckAccess(PermissionCodes.SessionClose, "Kết thúc phiên chơi");
            if (!canClose)
            {
                ActiveSessionCheckouts.TryRemove(sessionId, out _);
                await _auditLogService.LogAsync(userId, "ACCESS_DENIED", "Session", sessionId, closeDeniedMsg);
                return (false, string.Empty, closeDeniedMsg);
            }

            var (canPay, payDeniedMsg) = UserSession.CheckAccess(PermissionCodes.InvoicePay, "Thanh toán hóa đơn");
            if (!canPay)
            {
                ActiveSessionCheckouts.TryRemove(sessionId, out _);
                await _auditLogService.LogAsync(userId, "ACCESS_DENIED", "Invoice", sessionId, payDeniedMsg);
                return (false, string.Empty, payDeniedMsg);
            }

            if (discountAmount < 0)
            {
                ActiveSessionCheckouts.TryRemove(sessionId, out _);
                return (false, string.Empty, "Số tiền giảm giá không được âm!");
            }

            string normalizedMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Cash" : paymentMethod.Trim();
            if (!string.Equals(normalizedMethod, "Cash", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(normalizedMethod, "BankTransfer", StringComparison.OrdinalIgnoreCase))
            {
                ActiveSessionCheckouts.TryRemove(sessionId, out _);
                return (false, string.Empty, "Phương thức thanh toán không hợp lệ (Chỉ chấp nhận Cash hoặc BankTransfer)!");
            }
            normalizedMethod = string.Equals(normalizedMethod, "BankTransfer", StringComparison.OrdinalIgnoreCase)
                ? "BankTransfer"
                : "Cash";

            try
            {
                DateTime now = DateTime.Now;
                // Tính toán số tiền theo từng khoảng thời gian (SessionPricingDetails)
                var (tableAmount, serviceAmount, totalAmountBeforeDiscount, duration, pricingSegments) =
                    await CalculateSessionBillingDetailedAsync(sessionId, now);
                decimal clampedDiscount = Math.Min(totalAmountBeforeDiscount, Math.Max(0m, discountAmount));
                decimal finalAmount = Math.Max(0m, totalAmountBeforeDiscount - clampedDiscount);

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

                    // Kiểm tra hóa đơn hiện có của phiên (chặn tạo nhiều hóa đơn hoặc thanh toán trùng cho cùng 1 phiên)
                    const string checkExistingInvoiceSql = @"
                        SELECT InvoiceId, InvoiceCode, Status
                        FROM Invoices
                        WHERE SessionId = @SessionId
                        FOR UPDATE;";

                    long existingInvoiceId = 0;
                    string existingInvoiceCode = null;
                    string existingInvoiceStatus = null;

                    using (var cmdCheckInv = new MySqlCommand(checkExistingInvoiceSql, conn, trans))
                    {
                        cmdCheckInv.Parameters.AddWithValue("@SessionId", sessionId);
                        using (var reader = await cmdCheckInv.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                existingInvoiceId = reader.GetInt64("InvoiceId");
                                existingInvoiceCode = reader.GetString("InvoiceCode");
                                existingInvoiceStatus = reader.GetString("Status");
                            }
                        }
                    }

                    if (existingInvoiceId > 0 && string.Equals(existingInvoiceStatus, "Paid", StringComparison.OrdinalIgnoreCase))
                    {
                        await trans.RollbackAsync();
                        return (false, existingInvoiceCode, $"Phiên chơi #{sessionId} đã được thanh toán trước đó (Mã HĐ: {existingInvoiceCode})!");
                    }

                    // 0. Lưu chi tiết các khoảng thời gian tính tiền bàn vào SessionPricingDetails
                    await _pricingRuleService.SaveSessionPricingDetailsAsync(
                        conn, trans, sessionId, dbTableId, dbTableTypeId, pricingSegments);

                    // 1. Tạo mới hoặc cập nhật bản ghi Hóa đơn (Invoices)
                    long invoiceId;
                    string invoiceCode;

                    if (existingInvoiceId > 0)
                    {
                        invoiceId = existingInvoiceId;
                        invoiceCode = existingInvoiceCode;

                        const string updateInvoiceSql = @"
                            UPDATE Invoices
                            SET UserId = @UserId,
                                TableAmount = @TableAmount,
                                ServiceAmount = @ServiceAmount,
                                DiscountAmount = @DiscountAmount,
                                TotalAmount = @TotalAmount,
                                Status = 'Paid',
                                PaymentMethod = @PaymentMethod,
                                PaidAt = @Now
                            WHERE InvoiceId = @InvoiceId;";

                        using (var cmd = new MySqlCommand(updateInvoiceSql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            cmd.Parameters.AddWithValue("@TableAmount", tableAmount);
                            cmd.Parameters.AddWithValue("@ServiceAmount", serviceAmount);
                            cmd.Parameters.AddWithValue("@DiscountAmount", clampedDiscount);
                            cmd.Parameters.AddWithValue("@TotalAmount", finalAmount);
                            cmd.Parameters.AddWithValue("@PaymentMethod", normalizedMethod);
                            cmd.Parameters.AddWithValue("@Now", now);
                            cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        invoiceCode = await GenerateUniqueInvoiceCodeAsync(conn, trans, now, sessionId);

                        const string insertInvoiceSql = @"
                            INSERT INTO Invoices (InvoiceCode, SessionId, UserId, CreatedAt, TableAmount, ServiceAmount, DiscountAmount, TotalAmount, Status, PaymentMethod, PaidAt)
                            VALUES (@InvoiceCode, @SessionId, @UserId, @Now, @TableAmount, @ServiceAmount, @DiscountAmount, @TotalAmount, 'Paid', @PaymentMethod, @Now);
                            SELECT LAST_INSERT_ID();";

                        using (var cmd = new MySqlCommand(insertInvoiceSql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@InvoiceCode", invoiceCode);
                            cmd.Parameters.AddWithValue("@SessionId", sessionId);
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            cmd.Parameters.AddWithValue("@Now", now);
                            cmd.Parameters.AddWithValue("@TableAmount", tableAmount);
                            cmd.Parameters.AddWithValue("@ServiceAmount", serviceAmount);
                            cmd.Parameters.AddWithValue("@DiscountAmount", clampedDiscount);
                            cmd.Parameters.AddWithValue("@TotalAmount", finalAmount);
                            cmd.Parameters.AddWithValue("@PaymentMethod", normalizedMethod);
                            invoiceId = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                        }
                    }

                    // 2 & 3. Tạo và kiểm tra toàn bộ dòng chi tiết hóa đơn (Table + Product) trong cùng transaction
                    var (detailsOk, detailsErr, _) = await RebuildInvoiceDetailsInTransactionAsync(
                        conn, trans, invoiceId, sessionId, duration, tableAmount, pricingSegments);
                    if (!detailsOk)
                    {
                        await trans.RollbackAsync();
                        return (false, string.Empty, detailsErr);
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

                    await _auditLogService.LogAsync(
                        userId, "INVOICE_PAID", "Invoice", invoiceId,
                        $"Thanh toán hóa đơn {invoiceCode} cho phiên #{sessionId} (Bàn #{dbTableId}, PT: {normalizedMethod}, Tổng: {finalAmount:N0}đ).");

                    return (true, invoiceCode, $"Thanh toán thành công! Hóa đơn: {invoiceCode} ({finalAmount:N0} đ)");
                }
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Lỗi thanh toán: {ex.Message}");
            }
            finally
            {
                ActiveSessionCheckouts.TryRemove(sessionId, out _);
            }
        }
    }
}

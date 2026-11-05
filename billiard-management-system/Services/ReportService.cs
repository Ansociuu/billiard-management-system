using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using billiard_management_system.Helpers;
using billiard_management_system.Models;
using MySqlConnector;

namespace billiard_management_system.Services
{
    public interface IReportService
    {
        Task<(bool Success, string Message, RevenueSummaryReport Report)> GetRevenueReportAsync(DateTime fromDate, DateTime toDate);
    }

    public class ReportService : IReportService
    {
        private readonly IAuditLogService _auditLogService;

        public ReportService(IAuditLogService auditLogService = null)
        {
            _auditLogService = auditLogService ?? new AuditLogService();
        }

        /// <summary>
        /// Tổng hợp báo cáo doanh thu và thống kê hoạt động từ fromDate đến toDate.
        /// Chỉ tính doanh thu dựa trên hóa đơn đã thanh toán (Status = 'Paid'), không cộng hóa đơn đang chờ hoặc đã hủy.
        /// Giới hạn theo quyền truy cập REPORT_VIEW.
        /// </summary>
        public async Task<(bool Success, string Message, RevenueSummaryReport Report)> GetRevenueReportAsync(DateTime fromDate, DateTime toDate)
        {
            var (allowed, deniedMsg) = UserSession.CheckAccess(PermissionCodes.ReportView, "Xem báo cáo doanh thu và thống kê");
            if (!allowed)
            {
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "Report", null, deniedMsg);
                return (false, deniedMsg, null);
            }

            if (fromDate.Date > toDate.Date)
            {
                return (false, "Khoảng thời gian báo cáo không hợp lệ: Ngày bắt đầu không được lớn hơn ngày kết thúc!", null);
            }

            DateTime startInclusive = fromDate.Date;
            DateTime endExclusive = toDate.Date.AddDays(1);

            var report = new RevenueSummaryReport
            {
                FromDate = startInclusive,
                ToDate = toDate.Date
            };

            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    // 1. Tổng hợp hóa đơn và doanh thu (chỉ cộng tiền cho Status = 'Paid' theo thời điểm thanh toán/lập)
                    const string summaryInvoiceSql = @"
                        SELECT
                            COALESCE(SUM(CASE WHEN Status = 'Paid' THEN 1 ELSE 0 END), 0) AS PaidCount,
                            COALESCE(SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END), 0) AS PendingCount,
                            COALESCE(SUM(CASE WHEN Status = 'Cancelled' THEN 1 ELSE 0 END), 0) AS CancelledCount,
                            COALESCE(SUM(CASE WHEN Status = 'Paid' THEN TableAmount ELSE 0 END), 0) AS TableRevenue,
                            COALESCE(SUM(CASE WHEN Status = 'Paid' THEN ServiceAmount ELSE 0 END), 0) AS ProductRevenue,
                            COALESCE(SUM(CASE WHEN Status = 'Paid' THEN DiscountAmount ELSE 0 END), 0) AS TotalDiscount,
                            COALESCE(SUM(CASE WHEN Status = 'Paid' THEN TotalAmount ELSE 0 END), 0) AS NetRevenue,
                            COALESCE(SUM(CASE WHEN Status = 'Paid' AND PaymentMethod = 'Cash' THEN TotalAmount ELSE 0 END), 0) AS CashRevenue,
                            COALESCE(SUM(CASE WHEN Status = 'Paid' AND PaymentMethod = 'BankTransfer' THEN TotalAmount ELSE 0 END), 0) AS BankRevenue
                        FROM Invoices
                        WHERE COALESCE(PaidAt, CreatedAt) >= @FromDate
                          AND COALESCE(PaidAt, CreatedAt) < @ToDateExclusive;";

                    using (var cmd = new MySqlCommand(summaryInvoiceSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@FromDate", startInclusive);
                        cmd.Parameters.AddWithValue("@ToDateExclusive", endExclusive);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                report.PaidInvoiceCount = Convert.ToInt32(reader["PaidCount"]);
                                report.PendingInvoiceCount = Convert.ToInt32(reader["PendingCount"]);
                                report.CancelledInvoiceCount = Convert.ToInt32(reader["CancelledCount"]);
                                report.TableRevenue = Convert.ToDecimal(reader["TableRevenue"]);
                                report.ProductRevenue = Convert.ToDecimal(reader["ProductRevenue"]);
                                report.TotalDiscount = Convert.ToDecimal(reader["TotalDiscount"]);
                                report.NetRevenue = Convert.ToDecimal(reader["NetRevenue"]);
                                report.CashRevenue = Convert.ToDecimal(reader["CashRevenue"]);
                                report.BankTransferRevenue = Convert.ToDecimal(reader["BankRevenue"]);
                            }
                        }
                    }

                    // 2. Thống kê số lượng phiên chơi trong khoảng thời gian
                    const string summarySessionSql = @"
                        SELECT
                            COUNT(*) AS TotalSessions,
                            COALESCE(SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END), 0) AS CompletedSessions,
                            COALESCE(SUM(CASE WHEN Status = 'Active' THEN 1 ELSE 0 END), 0) AS ActiveSessions,
                            COALESCE(SUM(CASE WHEN Status = 'Cancelled' THEN 1 ELSE 0 END), 0) AS CancelledSessions
                        FROM Sessions
                        WHERE StartTime >= @FromDate AND StartTime < @ToDateExclusive;";

                    using (var cmd = new MySqlCommand(summarySessionSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@FromDate", startInclusive);
                        cmd.Parameters.AddWithValue("@ToDateExclusive", endExclusive);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                report.TotalSessionCount = Convert.ToInt32(reader["TotalSessions"]);
                                report.CompletedSessionCount = Convert.ToInt32(reader["CompletedSessions"]);
                                report.ActiveSessionCount = Convert.ToInt32(reader["ActiveSessions"]);
                                report.CancelledSessionCount = Convert.ToInt32(reader["CancelledSessions"]);
                            }
                        }
                    }

                    // 3. Doanh thu chi tiết theo từng ngày (chỉ tính hóa đơn Paid)
                    const string dailySql = @"
                        SELECT
                            DATE(COALESCE(PaidAt, CreatedAt)) AS ReportDate,
                            COUNT(*) AS PaidCount,
                            COALESCE(SUM(TableAmount), 0) AS TableRev,
                            COALESCE(SUM(ServiceAmount), 0) AS ProductRev,
                            COALESCE(SUM(DiscountAmount), 0) AS DiscountAmt,
                            COALESCE(SUM(TotalAmount), 0) AS NetRev
                        FROM Invoices
                        WHERE Status = 'Paid'
                          AND COALESCE(PaidAt, CreatedAt) >= @FromDate
                          AND COALESCE(PaidAt, CreatedAt) < @ToDateExclusive
                        GROUP BY DATE(COALESCE(PaidAt, CreatedAt))
                        ORDER BY ReportDate DESC;";

                    using (var cmd = new MySqlCommand(dailySql, conn))
                    {
                        cmd.Parameters.AddWithValue("@FromDate", startInclusive);
                        cmd.Parameters.AddWithValue("@ToDateExclusive", endExclusive);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int paidCnt = Convert.ToInt32(reader["PaidCount"]);
                                report.DailyBreakdown.Add(new DailyRevenueItem
                                {
                                    ReportDate = Convert.ToDateTime(reader["ReportDate"]),
                                    PaidInvoiceCount = paidCnt,
                                    CompletedSessionCount = paidCnt,
                                    TableRevenue = Convert.ToDecimal(reader["TableRev"]),
                                    ProductRevenue = Convert.ToDecimal(reader["ProductRev"]),
                                    DiscountAmount = Convert.ToDecimal(reader["DiscountAmt"]),
                                    NetRevenue = Convert.ToDecimal(reader["NetRev"])
                                });
                            }
                        }
                    }

                    // 4. Hiệu suất & Doanh thu theo từng bàn (chỉ cộng doanh thu từ hóa đơn Paid trong kỳ)
                    const string tableSql = @"
                        SELECT
                            t.TableId,
                            t.TableNumber,
                            tt.TypeName,
                            COUNT(p.InvoiceId) AS PaidSessions,
                            COALESCE(SUM(GREATEST(1, TIMESTAMPDIFF(MINUTE, p.StartTime, COALESCE(p.EndTime, p.PaidAt, p.CreatedAt)))), 0) AS TotalMinutes,
                            COALESCE(SUM(p.TableAmount), 0) AS TableRev,
                            COALESCE(SUM(p.ServiceAmount), 0) AS ProductRev,
                            COALESCE(SUM(p.TotalAmount), 0) AS NetRev
                        FROM Tables t
                        INNER JOIN TableTypes tt ON t.TableTypeId = tt.TableTypeId
                        LEFT JOIN (
                            SELECT i.InvoiceId, i.TableAmount, i.ServiceAmount, i.TotalAmount, i.PaidAt, i.CreatedAt,
                                   s.TableId, s.StartTime, s.EndTime
                            FROM Invoices i
                            INNER JOIN Sessions s ON i.SessionId = s.SessionId
                            WHERE i.Status = 'Paid'
                              AND COALESCE(i.PaidAt, i.CreatedAt) >= @FromDate
                              AND COALESCE(i.PaidAt, i.CreatedAt) < @ToDateExclusive
                        ) p ON t.TableId = p.TableId
                        GROUP BY t.TableId, t.TableNumber, tt.TypeName, t.IsActive
                        HAVING COUNT(p.InvoiceId) > 0 OR t.IsActive = TRUE
                        ORDER BY NetRev DESC, t.TableNumber ASC;";

                    using (var cmd = new MySqlCommand(tableSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@FromDate", startInclusive);
                        cmd.Parameters.AddWithValue("@ToDateExclusive", endExclusive);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                report.TableBreakdown.Add(new TableRevenueItem
                                {
                                    TableId = Convert.ToInt32(reader["TableId"]),
                                    TableNumber = reader.GetString("TableNumber"),
                                    TypeName = reader.GetString("TypeName"),
                                    SessionCount = Convert.ToInt32(reader["PaidSessions"]),
                                    TotalMinutesPlayed = Convert.ToDecimal(reader["TotalMinutes"]),
                                    TableRevenue = Convert.ToDecimal(reader["TableRev"]),
                                    ProductRevenue = Convert.ToDecimal(reader["ProductRev"]),
                                    NetRevenue = Convert.ToDecimal(reader["NetRev"])
                                });
                            }
                        }
                    }

                    // 5. Top sản phẩm F&B bán chạy từ các hóa đơn đã thanh toán (Paid)
                    const string topProductsSql = @"
                        SELECT
                            p.ProductId,
                            p.ProductName,
                            c.CategoryName,
                            COALESCE(SUM(id.Quantity), 0) AS TotalQty,
                            COALESCE(SUM(id.Amount), 0) AS TotalRev
                        FROM InvoiceDetails id
                        INNER JOIN Invoices i ON id.InvoiceId = i.InvoiceId
                        INNER JOIN Products p ON id.ProductId = p.ProductId
                        INNER JOIN ProductCategories c ON p.CategoryId = c.CategoryId
                        WHERE i.Status = 'Paid'
                          AND id.ItemType = 'Product'
                          AND COALESCE(i.PaidAt, i.CreatedAt) >= @FromDate
                          AND COALESCE(i.PaidAt, i.CreatedAt) < @ToDateExclusive
                        GROUP BY p.ProductId, p.ProductName, c.CategoryName
                        ORDER BY TotalRev DESC, TotalQty DESC;";

                    using (var cmd = new MySqlCommand(topProductsSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@FromDate", startInclusive);
                        cmd.Parameters.AddWithValue("@ToDateExclusive", endExclusive);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                report.TopProducts.Add(new ProductRevenueItem
                                {
                                    ProductId = Convert.ToInt32(reader["ProductId"]),
                                    ProductName = reader.GetString("ProductName"),
                                    CategoryName = reader.GetString("CategoryName"),
                                    TotalQuantitySold = Convert.ToDecimal(reader["TotalQty"]),
                                    TotalRevenue = Convert.ToDecimal(reader["TotalRev"])
                                });
                            }
                        }
                    }

                    // 6. Danh sách hóa đơn Paid để đối chiếu trực tiếp với tổng số liệu báo cáo
                    const string paidInvoicesSql = @"
                        SELECT i.InvoiceId, i.InvoiceCode, i.SessionId, i.UserId, i.CreatedAt,
                               i.TableAmount, i.ServiceAmount, i.DiscountAmount, i.TotalAmount,
                               i.Status, i.PaymentMethod, i.PaidAt,
                               u.FullName AS CashierName, t.TableNumber,
                               COALESCE(c.FullName, 'Khách vãng lai') AS CustomerName
                        FROM Invoices i
                        INNER JOIN Sessions s ON i.SessionId = s.SessionId
                        INNER JOIN Tables t ON s.TableId = t.TableId
                        INNER JOIN Users u ON i.UserId = u.UserId
                        LEFT JOIN Customers c ON s.CustomerId = c.CustomerId
                        WHERE i.Status = 'Paid'
                          AND COALESCE(i.PaidAt, i.CreatedAt) >= @FromDate
                          AND COALESCE(i.PaidAt, i.CreatedAt) < @ToDateExclusive
                        ORDER BY COALESCE(i.PaidAt, i.CreatedAt) DESC, i.InvoiceId DESC;";

                    using (var cmd = new MySqlCommand(paidInvoicesSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@FromDate", startInclusive);
                        cmd.Parameters.AddWithValue("@ToDateExclusive", endExclusive);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            int pmOrd = reader.GetOrdinal("PaymentMethod");
                            int paidOrd = reader.GetOrdinal("PaidAt");
                            while (await reader.ReadAsync())
                            {
                                report.PaidInvoicesForReconciliation.Add(new Invoice
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
                                    CustomerName = reader.GetString("CustomerName")
                                });
                            }
                        }
                    }
                }

                return (true, "Đã tổng hợp dữ liệu báo cáo doanh thu thành công!", report);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi tổng hợp báo cáo doanh thu: {ex.Message}", null);
            }
        }
    }
}

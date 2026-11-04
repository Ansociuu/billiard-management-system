using System;
using System.Collections.Generic;

namespace billiard_management_system.Models
{
    public class RevenueSummaryReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Thống kê Hóa đơn
        public int PaidInvoiceCount { get; set; }
        public int PendingInvoiceCount { get; set; }
        public int CancelledInvoiceCount { get; set; }
        public int TotalInvoiceCount => PaidInvoiceCount + PendingInvoiceCount + CancelledInvoiceCount;

        // Thống kê Doanh thu (Chỉ tính trên các hóa đơn Status = 'Paid')
        public decimal TableRevenue { get; set; }
        public decimal ProductRevenue { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal CashRevenue { get; set; }
        public decimal BankTransferRevenue { get; set; }

        // Thống kê Phiên chơi trong kỳ
        public int TotalSessionCount { get; set; }
        public int CompletedSessionCount { get; set; }
        public int ActiveSessionCount { get; set; }
        public int CancelledSessionCount { get; set; }

        public List<DailyRevenueItem> DailyBreakdown { get; set; } = new List<DailyRevenueItem>();
        public List<TableRevenueItem> TableBreakdown { get; set; } = new List<TableRevenueItem>();
        public List<ProductRevenueItem> TopProducts { get; set; } = new List<ProductRevenueItem>();
        public List<Invoice> PaidInvoicesForReconciliation { get; set; } = new List<Invoice>();
    }

    public class DailyRevenueItem
    {
        public DateTime ReportDate { get; set; }
        public int PaidInvoiceCount { get; set; }
        public int CompletedSessionCount { get; set; }
        public decimal TableRevenue { get; set; }
        public decimal ProductRevenue { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetRevenue { get; set; }
    }

    public class TableRevenueItem
    {
        public int TableId { get; set; }
        public string TableNumber { get; set; }
        public string TypeName { get; set; }
        public int SessionCount { get; set; }
        public decimal TotalMinutesPlayed { get; set; }
        public decimal TableRevenue { get; set; }
        public decimal ProductRevenue { get; set; }
        public decimal NetRevenue { get; set; }

        public string TotalHoursDisplay => $"{Math.Round(TotalMinutesPlayed / 60m, 1):0.0} giờ";
    }

    public class ProductRevenueItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public decimal TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}

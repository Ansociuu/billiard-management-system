using System;
using System.Collections.ObjectModel;

namespace billiard_management_system.Models
{
    public class Invoice
    {
        public long InvoiceId { get; set; }
        public string InvoiceCode { get; set; }
        public long SessionId { get; set; }
        public long UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TableAmount { get; set; }
        public decimal ServiceAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } // Pending, Paid, Cancelled
        public string PaymentMethod { get; set; } // Cash, BankTransfer
        public DateTime? PaidAt { get; set; }

        // Navigation / Display
        public string CashierName { get; set; }
        public string TableNumber { get; set; }
        public string CustomerName { get; set; }
        public DateTime? SessionStartTime { get; set; }
        public DateTime? SessionEndTime { get; set; }
        public ObservableCollection<InvoiceDetail> Details { get; set; } = new ObservableCollection<InvoiceDetail>();

        public string StatusDisplay
        {
            get
            {
                if (string.Equals(Status, "Paid", StringComparison.OrdinalIgnoreCase)) return "Đã thanh toán";
                if (string.Equals(Status, "Pending", StringComparison.OrdinalIgnoreCase)) return "Chờ thanh toán";
                if (string.Equals(Status, "Cancelled", StringComparison.OrdinalIgnoreCase)) return "Đã hủy";
                return Status;
            }
        }

        public string PaymentMethodDisplay
        {
            get
            {
                if (string.Equals(PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase)) return "Tiền mặt (Cash)";
                if (string.Equals(PaymentMethod, "BankTransfer", StringComparison.OrdinalIgnoreCase)) return "Chuyển khoản (BankTransfer)";
                return "Chưa chọn";
            }
        }
    }
}


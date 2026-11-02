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

        // Navigation
        public string CashierName { get; set; }
        public string TableNumber { get; set; }
        public ObservableCollection<InvoiceDetail> Details { get; set; } = new ObservableCollection<InvoiceDetail>();
    }
}


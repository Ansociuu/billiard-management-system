namespace billiard_management_system.Models
{
    public class InvoiceDetail
    {
        public long InvoiceDetailId { get; set; }
        public long InvoiceId { get; set; }
        public string ItemType { get; set; } // Table, Product
        public int? ProductId { get; set; }
        public string Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }

        public string ItemTypeDisplay
        {
            get => string.Equals(ItemType, "Table", System.StringComparison.OrdinalIgnoreCase)
                ? "Tiền giờ bàn"
                : "Sản phẩm / Dịch vụ";
            set { }
        }
    }
}

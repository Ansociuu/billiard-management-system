namespace billiard_management_system.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        public string ProductName { get; set; }
        public string Unit { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;

        // Extra display properties
        public string CategoryName { get; set; }
        public string StatusDisplayName => IsActive ? "Đang bán" : "Ngừng bán";
        public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
    }
}

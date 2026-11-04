namespace billiard_management_system.Models
{
    public class TableType
    {
        public int TableTypeId { get; set; }
        public string TypeName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;

        public string StatusDisplayName => IsActive ? "Đang áp dụng" : "Ngừng sử dụng";
    }
}

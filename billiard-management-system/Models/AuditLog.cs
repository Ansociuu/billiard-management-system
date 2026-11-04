using System;

namespace billiard_management_system.Models
{
    /// <summary>
    /// Mô hình bản ghi nhật ký truy vết thao tác quan trọng của hệ thống (Bảng AuditLogs)
    /// </summary>
    public class AuditLog
    {
        public long AuditLogId { get; set; }
        public long? UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string RoleName { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public long? EntityId { get; set; }
        public string Description { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        public DateTime CreatedAt { get; set; }

        public string ActorDisplay
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(FullName) && !string.IsNullOrWhiteSpace(Username))
                {
                    return $"{FullName} (@{Username})";
                }
                if (!string.IsNullOrWhiteSpace(Username))
                {
                    return $"@{Username}";
                }
                if (UserId.HasValue)
                {
                    return $"User #{UserId.Value}";
                }
                return "Hệ thống / Chưa xác thực";
            }
        }

        public string EntityDisplay => EntityId.HasValue
            ? $"{EntityType} #{EntityId.Value}"
            : (string.IsNullOrWhiteSpace(EntityType) ? "System" : EntityType);

        public bool HasOldValues => !string.IsNullOrWhiteSpace(OldValues);
        public bool HasNewValues => !string.IsNullOrWhiteSpace(NewValues);
        public bool HasBeforeAfterChanges => HasOldValues || HasNewValues;

        public string OldValuesDisplay => string.IsNullOrWhiteSpace(OldValues)
            ? "(Không có dữ liệu trước thay đổi)"
            : OldValues;

        public string NewValuesDisplay => string.IsNullOrWhiteSpace(NewValues)
            ? "(Không có dữ liệu sau thay đổi)"
            : NewValues;

        public string ActionBadgeBackground
        {
            get
            {
                string act = (Action ?? string.Empty).ToUpperInvariant();
                if (act.Contains("DENIED") || act.Contains("FAILED") || act.Contains("CANCEL") || act.Contains("DELETE") || act.Contains("DEACTIVATE") || act.Contains("LOCK"))
                {
                    return "#FEE2E2";
                }
                if (act.Contains("PAID") || act.Contains("OPEN") || act.Contains("CREATE") || act.Contains("SUCCESS") || act.Contains("ACTIVATE"))
                {
                    return "#DCFCE7";
                }
                if (act.Contains("UPDATE") || act.Contains("SWITCH"))
                {
                    return "#E0F2FE";
                }
                return "#F1F5F9";
            }
        }

        public string ActionBadgeForeground
        {
            get
            {
                string act = (Action ?? string.Empty).ToUpperInvariant();
                if (act.Contains("DENIED") || act.Contains("FAILED") || act.Contains("CANCEL") || act.Contains("DELETE") || act.Contains("DEACTIVATE") || act.Contains("LOCK"))
                {
                    return "#B91C1C";
                }
                if (act.Contains("PAID") || act.Contains("OPEN") || act.Contains("CREATE") || act.Contains("SUCCESS") || act.Contains("ACTIVATE"))
                {
                    return "#15803D";
                }
                if (act.Contains("UPDATE") || act.Contains("SWITCH"))
                {
                    return "#0369A1";
                }
                return "#475569";
            }
        }
    }
}

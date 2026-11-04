using System;

namespace billiard_management_system.Models
{
    /// <summary>
    /// Ánh xạ bảng SessionPricingDetails (Bảng 14 trong schema.sql).
    /// Lưu chi tiết từng khoảng thời gian tính tiền bàn và quy tắc giá/khuyến mãi đã áp dụng.
    /// </summary>
    public class SessionPricingDetail
    {
        public long SessionPricingDetailId { get; set; }
        public long SessionId { get; set; }
        public int TableId { get; set; }
        public long BasePricingRuleId { get; set; }
        public long? PromotionPricingRuleId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal DurationMinutes { get; set; }
        public decimal BaseHourlyRate { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal FinalHourlyRate { get; set; }
        public decimal Amount { get; set; }

        // Thuộc tính hiển thị bổ sung trên UI / Hóa đơn
        public string BaseRuleName { get; set; }
        public string PromotionRuleName { get; set; }

        public string TimeRangeDisplay => $"{StartTime:HH:mm dd/MM} - {EndTime:HH:mm dd/MM}";

        public string DurationDisplay
        {
            get
            {
                int totalMins = (int)Math.Round(DurationMinutes);
                int hours = totalMins / 60;
                int mins = totalMins % 60;
                return hours > 0 ? $"{hours}h {mins:D2}p" : $"{mins}p";
            }
        }

        public string RuleSummaryDisplay
        {
            get
            {
                string baseText = string.IsNullOrWhiteSpace(BaseRuleName) ? "Giá cơ bản" : BaseRuleName;
                if (string.IsNullOrWhiteSpace(PromotionRuleName) || !PromotionPricingRuleId.HasValue)
                {
                    return baseText;
                }

                string discountDesc = string.Equals(DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase)
                    ? $"-{DiscountValue:0.#}%"
                    : $"-{DiscountValue:N0}đ/h";
                return $"{baseText} ({PromotionRuleName}: {discountDesc})";
            }
        }
    }
}

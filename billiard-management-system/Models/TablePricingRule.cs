using System;
using billiard_management_system.Core;

namespace billiard_management_system.Models
{
    /// <summary>
    /// Quy tắc định giá bàn theo khung giờ, loại ngày và chương trình khuyến mãi (Bảng TablePricingRules)
    /// </summary>
    public class TablePricingRule : ObservableObject
    {
        private long _pricingRuleId;
        private int _tableTypeId;
        private string _typeName;
        private string _ruleName;
        private string _ruleType = "BaseRate";
        private string _dayType = "Weekday";
        private TimeSpan? _startTime;
        private TimeSpan? _endTime;
        private decimal? _hourlyRate;
        private string _discountType;
        private decimal? _discountValue;
        private DateTime _effectiveFrom = DateTime.Today;
        private DateTime? _effectiveTo;
        private int _priority = 1;
        private bool _isActive = true;
        private DateTime _createdAt = DateTime.Now;

        public long PricingRuleId
        {
            get => _pricingRuleId;
            set => SetProperty(ref _pricingRuleId, value);
        }

        public int TableTypeId
        {
            get => _tableTypeId;
            set => SetProperty(ref _tableTypeId, value);
        }

        public string TypeName
        {
            get => _typeName;
            set => SetProperty(ref _typeName, value);
        }

        public string RuleName
        {
            get => _ruleName;
            set => SetProperty(ref _ruleName, value);
        }

        /// <summary>
        /// 'BaseRate' hoặc 'Promotion'
        /// </summary>
        public string RuleType
        {
            get => _ruleType;
            set
            {
                if (SetProperty(ref _ruleType, value))
                {
                    OnPropertyChanged(nameof(IsPromotion));
                    OnPropertyChanged(nameof(RuleTypeDisplay));
                    OnPropertyChanged(nameof(ValueDisplay));
                }
            }
        }

        /// <summary>
        /// 'Weekday', 'Weekend', 'Holiday', 'Any'
        /// </summary>
        public string DayType
        {
            get => _dayType;
            set
            {
                if (SetProperty(ref _dayType, value))
                {
                    OnPropertyChanged(nameof(DayTypeDisplay));
                }
            }
        }

        public TimeSpan? StartTime
        {
            get => _startTime;
            set
            {
                if (SetProperty(ref _startTime, value))
                {
                    OnPropertyChanged(nameof(TimeSlotDisplay));
                }
            }
        }

        public TimeSpan? EndTime
        {
            get => _endTime;
            set
            {
                if (SetProperty(ref _endTime, value))
                {
                    OnPropertyChanged(nameof(TimeSlotDisplay));
                }
            }
        }

        public decimal? HourlyRate
        {
            get => _hourlyRate;
            set
            {
                if (SetProperty(ref _hourlyRate, value))
                {
                    OnPropertyChanged(nameof(ValueDisplay));
                }
            }
        }

        /// <summary>
        /// NULL, 'Percentage', 'FixedAmount'
        /// </summary>
        public string DiscountType
        {
            get => _discountType;
            set
            {
                if (SetProperty(ref _discountType, value))
                {
                    OnPropertyChanged(nameof(ValueDisplay));
                }
            }
        }

        public decimal? DiscountValue
        {
            get => _discountValue;
            set
            {
                if (SetProperty(ref _discountValue, value))
                {
                    OnPropertyChanged(nameof(ValueDisplay));
                }
            }
        }

        public DateTime EffectiveFrom
        {
            get => _effectiveFrom;
            set
            {
                if (SetProperty(ref _effectiveFrom, value))
                {
                    OnPropertyChanged(nameof(EffectiveDateDisplay));
                }
            }
        }

        public DateTime? EffectiveTo
        {
            get => _effectiveTo;
            set
            {
                if (SetProperty(ref _effectiveTo, value))
                {
                    OnPropertyChanged(nameof(EffectiveDateDisplay));
                }
            }
        }

        public int Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (SetProperty(ref _isActive, value))
                {
                    OnPropertyChanged(nameof(StatusDisplayName));
                }
            }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public bool IsPromotion => string.Equals(RuleType, "Promotion", StringComparison.OrdinalIgnoreCase);

        public string RuleTypeDisplay => IsPromotion ? "🎁 Khuyến mãi" : "💵 Giá cơ bản";

        public string DayTypeDisplay
        {
            get
            {
                switch (DayType)
                {
                    case "Weekday": return "Ngày thường (T2-T6)";
                    case "Weekend": return "Cuối tuần (T7-CN)";
                    case "Holiday": return "Ngày lễ (Holiday)";
                    case "Any": return "Mọi ngày (T2-CN)";
                    default: return DayType;
                }
            }
        }

        public string TimeSlotDisplay
        {
            get
            {
                if (StartTime.HasValue && EndTime.HasValue)
                {
                    return $"{StartTime.Value:hh\\:mm} - {EndTime.Value:hh\\:mm}";
                }
                return "Cả ngày (00:00 - 24:00)";
            }
        }

        public string ValueDisplay
        {
            get
            {
                if (IsPromotion)
                {
                    if (string.Equals(DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
                    {
                        return $"Giảm {(DiscountValue ?? 0):0.##}%";
                    }
                    return $"Giảm {(DiscountValue ?? 0):N0} đ/h";
                }
                return $"{(HourlyRate ?? 0):N0} đ/h";
            }
        }

        public string EffectiveDateDisplay
        {
            get
            {
                if (EffectiveTo.HasValue)
                {
                    return $"{EffectiveFrom:dd/MM/yyyy} → {EffectiveTo.Value:dd/MM/yyyy}";
                }
                return $"Từ {EffectiveFrom:dd/MM/yyyy} (Vô thời hạn)";
            }
        }

        public string StatusDisplayName => IsActive ? "Đang áp dụng" : "Ngừng áp dụng";
    }
}

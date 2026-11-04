using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using billiard_management_system.Helpers;
using billiard_management_system.Models;
using MySqlConnector;

namespace billiard_management_system.Services
{
    public interface IPricingRuleService
    {
        Task<List<TablePricingRule>> GetPricingRulesAsync(int? tableTypeId = null, string ruleType = null, bool includeInactive = true);
        Task<decimal> GetEffectiveHourlyRateAsync(int tableTypeId, DateTime atTime);
        Task<(bool Success, string Message, long PricingRuleId)> CreatePricingRuleAsync(TablePricingRule rule);
        Task<(bool Success, string Message)> UpdatePricingRuleAsync(TablePricingRule rule);
        Task<(bool Success, string Message)> TogglePricingRuleActiveAsync(long pricingRuleId, bool isActive);
        (bool Valid, string Message) ValidatePricingRule(TablePricingRule rule);
    }

    public class PricingRuleService : IPricingRuleService
    {
        private static readonly HashSet<string> ValidRuleTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BaseRate", "Promotion"
        };

        private static readonly HashSet<string> ValidDayTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Weekday", "Weekend", "Holiday", "Any"
        };

        private static readonly HashSet<string> ValidDiscountTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Percentage", "FixedAmount"
        };

        // Danh sách ngày lễ cố định hàng năm (dd-MM) để nhận diện DayType = 'Holiday'
        private static readonly HashSet<string> FixedHolidays = new HashSet<string>
        {
            "01-01", // Tết Dương lịch
            "30-04", // Giải phóng miền Nam
            "01-05", // Quốc tế Lao động
            "02-09"  // Quốc khánh
        };

        private readonly IAuditLogService _auditLogService;

        public PricingRuleService(IAuditLogService auditLogService = null)
        {
            _auditLogService = auditLogService ?? new AuditLogService();
        }

        public async Task<List<TablePricingRule>> GetPricingRulesAsync(int? tableTypeId = null, string ruleType = null, bool includeInactive = true)
        {
            var list = new List<TablePricingRule>();

            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                string sql = @"
                    SELECT r.PricingRuleId, r.TableTypeId, r.RuleName, r.RuleType, r.DayType,
                           r.StartTime, r.EndTime, r.HourlyRate, r.DiscountType, r.DiscountValue,
                           r.EffectiveFrom, r.EffectiveTo, r.Priority, r.IsActive, r.CreatedAt,
                           tt.TypeName
                    FROM TablePricingRules r
                    INNER JOIN TableTypes tt ON r.TableTypeId = tt.TableTypeId
                    WHERE 1 = 1";

                if (!includeInactive)
                {
                    sql += " AND r.IsActive = TRUE";
                }

                if (tableTypeId.HasValue && tableTypeId.Value > 0)
                {
                    sql += " AND r.TableTypeId = @TableTypeId";
                }

                if (!string.IsNullOrWhiteSpace(ruleType) && !string.Equals(ruleType, "All", StringComparison.OrdinalIgnoreCase))
                {
                    sql += " AND r.RuleType = @RuleType";
                }

                sql += " ORDER BY r.IsActive DESC, r.TableTypeId ASC, r.RuleType ASC, r.Priority DESC, r.PricingRuleId DESC;";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (tableTypeId.HasValue && tableTypeId.Value > 0)
                    {
                        cmd.Parameters.AddWithValue("@TableTypeId", tableTypeId.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(ruleType) && !string.Equals(ruleType, "All", StringComparison.OrdinalIgnoreCase))
                    {
                        cmd.Parameters.AddWithValue("@RuleType", ruleType);
                    }

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(MapPricingRule(reader));
                        }
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Tính đơn giá giờ hiệu lực tại thời điểm atTime.
        /// Chỉ sử dụng các quy tắc đang hoạt động (IsActive = TRUE) và nằm trong khoảng EffectiveFrom - EffectiveTo.
        /// </summary>
        public async Task<decimal> GetEffectiveHourlyRateAsync(int tableTypeId, DateTime atTime)
        {
            // Chỉ lấy các quy tắc IsActive = TRUE
            var activeRules = await GetPricingRulesAsync(tableTypeId, null, includeInactive: false);

            DateTime targetDate = atTime.Date;
            TimeSpan targetTime = atTime.TimeOfDay;

            // Lọc các quy tắc còn trong hạn hiệu lực và khớp loại ngày + khung giờ
            var applicableRules = activeRules
                .Where(r => r.IsActive
                            && r.EffectiveFrom.Date <= targetDate
                            && (!r.EffectiveTo.HasValue || r.EffectiveTo.Value.Date >= targetDate)
                            && IsDayTypeMatch(r.DayType, targetDate)
                            && IsTimeSlotMatch(r.StartTime, r.EndTime, targetTime))
                .ToList();

            var baseRule = applicableRules
                .Where(r => string.Equals(r.RuleType, "BaseRate", StringComparison.OrdinalIgnoreCase) && r.HourlyRate.HasValue)
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.PricingRuleId)
                .FirstOrDefault();

            decimal baseRate = baseRule?.HourlyRate ?? 50000m;

            var promoRule = applicableRules
                .Where(r => string.Equals(r.RuleType, "Promotion", StringComparison.OrdinalIgnoreCase) && r.DiscountValue.HasValue)
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.PricingRuleId)
                .FirstOrDefault();

            if (promoRule == null)
            {
                return baseRate;
            }

            decimal finalRate = baseRate;
            if (string.Equals(promoRule.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
            {
                decimal pct = Math.Min(100m, Math.Max(0m, promoRule.DiscountValue.Value));
                finalRate = baseRate * (100m - pct) / 100m;
            }
            else if (string.Equals(promoRule.DiscountType, "FixedAmount", StringComparison.OrdinalIgnoreCase))
            {
                finalRate = Math.Max(0m, baseRate - promoRule.DiscountValue.Value);
            }

            return Math.Round(finalRate, 0);
        }

        public async Task<(bool Success, string Message, long PricingRuleId)> CreatePricingRuleAsync(TablePricingRule rule)
        {
            if (UserSession.IsLoggedIn && !UserSession.IsAdmin)
            {
                const string denied = "Từ chối truy cập: Chỉ Quản trị viên (Admin) mới được phép tạo quy tắc giá bàn và khuyến mãi.";
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "TablePricingRule", null, denied);
                return (false, denied, 0);
            }

            var (valid, validationMsg) = ValidatePricingRule(rule);
            if (!valid)
            {
                return (false, validationMsg, 0);
            }

            NormalizePricingRule(rule);

            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    const string sql = @"
                        INSERT INTO TablePricingRules
                            (TableTypeId, RuleName, RuleType, DayType, StartTime, EndTime,
                             HourlyRate, DiscountType, DiscountValue, EffectiveFrom, EffectiveTo, Priority, IsActive, CreatedAt)
                        VALUES
                            (@TableTypeId, @RuleName, @RuleType, @DayType, @StartTime, @EndTime,
                             @HourlyRate, @DiscountType, @DiscountValue, @EffectiveFrom, @EffectiveTo, @Priority, @IsActive, NOW());
                        SELECT LAST_INSERT_ID();";

                    long newId;
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        BindRuleParameters(cmd, rule);
                        newId = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                    }

                    await _auditLogService.LogAsync(
                        UserSession.CurrentUser?.UserId, "CREATE_PRICING_RULE", "TablePricingRule", newId,
                        $"Tạo quy tắc giá '{rule.RuleName}' ({rule.RuleType} - {rule.DayType}, IsActive={rule.IsActive}).");

                    return (true, $"Thêm quy tắc giá [{rule.RuleName}] thành công!", newId);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi thêm quy tắc giá: {ex.Message}", 0);
            }
        }

        public async Task<(bool Success, string Message)> UpdatePricingRuleAsync(TablePricingRule rule)
        {
            if (UserSession.IsLoggedIn && !UserSession.IsAdmin)
            {
                const string denied = "Từ chối truy cập: Chỉ Quản trị viên (Admin) mới được phép cập nhật quy tắc giá bàn và khuyến mãi.";
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "TablePricingRule", rule?.PricingRuleId, denied);
                return (false, denied);
            }

            if (rule == null || rule.PricingRuleId <= 0)
            {
                return (false, "Không xác định được quy tắc giá cần cập nhật.");
            }

            var (valid, validationMsg) = ValidatePricingRule(rule);
            if (!valid)
            {
                return (false, validationMsg);
            }

            NormalizePricingRule(rule);

            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    const string sql = @"
                        UPDATE TablePricingRules
                        SET TableTypeId = @TableTypeId,
                            RuleName = @RuleName,
                            RuleType = @RuleType,
                            DayType = @DayType,
                            StartTime = @StartTime,
                            EndTime = @EndTime,
                            HourlyRate = @HourlyRate,
                            DiscountType = @DiscountType,
                            DiscountValue = @DiscountValue,
                            EffectiveFrom = @EffectiveFrom,
                            EffectiveTo = @EffectiveTo,
                            Priority = @Priority,
                            IsActive = @IsActive
                        WHERE PricingRuleId = @PricingRuleId;";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        BindRuleParameters(cmd, rule);
                        cmd.Parameters.AddWithValue("@PricingRuleId", rule.PricingRuleId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await _auditLogService.LogAsync(
                        UserSession.CurrentUser?.UserId, "UPDATE_PRICING_RULE", "TablePricingRule", rule.PricingRuleId,
                        $"Cập nhật quy tắc giá '{rule.RuleName}' ({rule.RuleType} - {rule.DayType}, IsActive={rule.IsActive}).");

                    return (true, $"Cập nhật quy tắc [{rule.RuleName}] thành công!");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi cập nhật quy tắc giá: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> TogglePricingRuleActiveAsync(long pricingRuleId, bool isActive)
        {
            if (UserSession.IsLoggedIn && !UserSession.IsAdmin)
            {
                const string denied = "Từ chối truy cập: Chỉ Quản trị viên (Admin) mới được phép bật/tắt quy tắc giá.";
                await _auditLogService.LogAsync(UserSession.CurrentUser?.UserId, "ACCESS_DENIED", "TablePricingRule", pricingRuleId, denied);
                return (false, denied);
            }

            try
            {
                using (var conn = await DatabaseHelper.OpenConnectionAsync())
                {
                    const string sql = "UPDATE TablePricingRules SET IsActive = @IsActive WHERE PricingRuleId = @Id;";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@IsActive", isActive);
                        cmd.Parameters.AddWithValue("@Id", pricingRuleId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                string actionText = isActive ? "kích hoạt áp dụng" : "tạm ngưng áp dụng";
                await _auditLogService.LogAsync(
                    UserSession.CurrentUser?.UserId,
                    isActive ? "ACTIVATE_PRICING_RULE" : "DEACTIVATE_PRICING_RULE",
                    "TablePricingRule", pricingRuleId,
                    $"Đã {actionText} quy tắc giá ID={pricingRuleId}.");

                return (true, isActive
                    ? "Đã kích hoạt quy tắc giá thành công!"
                    : "Đã ngưng áp dụng quy tắc giá. Quy tắc này sẽ không được dùng để tính tiền.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi thay đổi trạng thái quy tắc giá: {ex.Message}");
            }
        }

        public (bool Valid, string Message) ValidatePricingRule(TablePricingRule rule)
        {
            if (rule == null)
            {
                return (false, "Dữ liệu quy tắc giá không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(rule.RuleName))
            {
                return (false, "Vui lòng nhập tên quy tắc giá hoặc chương trình khuyến mãi.");
            }

            if (rule.TableTypeId <= 0)
            {
                return (false, "Vui lòng chọn loại bàn áp dụng.");
            }

            if (string.IsNullOrWhiteSpace(rule.RuleType) || !ValidRuleTypes.Contains(rule.RuleType))
            {
                return (false, "Loại quy tắc không hợp lệ (Chỉ chấp nhận BaseRate hoặc Promotion).");
            }

            if (string.IsNullOrWhiteSpace(rule.DayType) || !ValidDayTypes.Contains(rule.DayType))
            {
                return (false, "Loại ngày áp dụng không hợp lệ (Weekday, Weekend, Holiday, Any).");
            }

            if (rule.Priority < 1)
            {
                return (false, "Độ ưu tiên (Priority) phải từ 1 trở lên.");
            }

            // Kiểm tra khoảng ngày hiệu lực EffectiveFrom - EffectiveTo
            if (rule.EffectiveFrom == default)
            {
                return (false, "Vui lòng chọn ngày bắt đầu hiệu lực (EffectiveFrom).");
            }

            if (rule.EffectiveTo.HasValue && rule.EffectiveTo.Value.Date < rule.EffectiveFrom.Date)
            {
                return (false, "Khoảng ngày hiệu lực không hợp lệ: Ngày kết thúc (EffectiveTo) phải lớn hơn hoặc bằng ngày bắt đầu (EffectiveFrom)!");
            }

            // Kiểm tra khung giờ StartTime - EndTime
            if (rule.StartTime.HasValue != rule.EndTime.HasValue)
            {
                return (false, "Khung giờ không hợp lệ: Vui lòng nhập đầy đủ cả Giờ bắt đầu và Giờ kết thúc (hoặc chọn Áp dụng cả ngày).");
            }

            if (rule.StartTime.HasValue && rule.EndTime.HasValue)
            {
                if (rule.StartTime.Value < TimeSpan.Zero || rule.StartTime.Value >= TimeSpan.FromHours(24)
                    || rule.EndTime.Value < TimeSpan.Zero || rule.EndTime.Value >= TimeSpan.FromHours(24))
                {
                    return (false, "Khung giờ phải nằm trong khoảng 00:00 đến 23:59.");
                }

                if (rule.StartTime.Value == rule.EndTime.Value)
                {
                    return (false, "Giờ bắt đầu và Giờ kết thúc không được trùng nhau.");
                }
            }

            // Kiểm tra giá cơ bản hoặc giá trị giảm giá
            if (string.Equals(rule.RuleType, "BaseRate", StringComparison.OrdinalIgnoreCase))
            {
                if (!rule.HourlyRate.HasValue || rule.HourlyRate.Value <= 0)
                {
                    return (false, "Quy tắc Giá cơ bản (BaseRate) bắt buộc phải có Đơn giá giờ lớn hơn 0 VNĐ!");
                }
            }
            else if (string.Equals(rule.RuleType, "Promotion", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(rule.DiscountType) || !ValidDiscountTypes.Contains(rule.DiscountType))
                {
                    return (false, "Vui lòng chọn hình thức giảm giá (Phần trăm 'Percentage' hoặc Số tiền cố định 'FixedAmount').");
                }

                if (!rule.DiscountValue.HasValue || rule.DiscountValue.Value <= 0)
                {
                    return (false, "Giá trị khuyến mãi (DiscountValue) phải lớn hơn 0!");
                }

                if (string.Equals(rule.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase) && rule.DiscountValue.Value > 100)
                {
                    return (false, "Mức giảm giá theo phần trăm không được vượt quá 100%!");
                }
            }

            return (true, string.Empty);
        }

        private static void NormalizePricingRule(TablePricingRule rule)
        {
            rule.RuleName = rule.RuleName.Trim();
            if (string.Equals(rule.RuleType, "BaseRate", StringComparison.OrdinalIgnoreCase))
            {
                rule.RuleType = "BaseRate";
                rule.DiscountType = null;
                rule.DiscountValue = null;
            }
            else
            {
                rule.RuleType = "Promotion";
                rule.HourlyRate = null;
            }
        }

        private static void BindRuleParameters(MySqlCommand cmd, TablePricingRule rule)
        {
            cmd.Parameters.AddWithValue("@TableTypeId", rule.TableTypeId);
            cmd.Parameters.AddWithValue("@RuleName", rule.RuleName);
            cmd.Parameters.AddWithValue("@RuleType", rule.RuleType);
            cmd.Parameters.AddWithValue("@DayType", rule.DayType);
            cmd.Parameters.AddWithValue("@StartTime", rule.StartTime.HasValue ? (object)rule.StartTime.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@EndTime", rule.EndTime.HasValue ? (object)rule.EndTime.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@HourlyRate", rule.HourlyRate.HasValue ? (object)rule.HourlyRate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@DiscountType", !string.IsNullOrWhiteSpace(rule.DiscountType) ? (object)rule.DiscountType : DBNull.Value);
            cmd.Parameters.AddWithValue("@DiscountValue", rule.DiscountValue.HasValue ? (object)rule.DiscountValue.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@EffectiveFrom", rule.EffectiveFrom.Date);
            cmd.Parameters.AddWithValue("@EffectiveTo", rule.EffectiveTo.HasValue ? (object)rule.EffectiveTo.Value.Date : DBNull.Value);
            cmd.Parameters.AddWithValue("@Priority", rule.Priority);
            cmd.Parameters.AddWithValue("@IsActive", rule.IsActive);
        }

        private static TablePricingRule MapPricingRule(MySqlDataReader reader)
        {
            int startOrd = reader.GetOrdinal("StartTime");
            int endOrd = reader.GetOrdinal("EndTime");
            int rateOrd = reader.GetOrdinal("HourlyRate");
            int discTypeOrd = reader.GetOrdinal("DiscountType");
            int discValOrd = reader.GetOrdinal("DiscountValue");
            int effToOrd = reader.GetOrdinal("EffectiveTo");

            return new TablePricingRule
            {
                PricingRuleId = reader.GetInt64("PricingRuleId"),
                TableTypeId = reader.GetInt32("TableTypeId"),
                TypeName = reader.GetString("TypeName"),
                RuleName = reader.GetString("RuleName"),
                RuleType = reader.GetString("RuleType"),
                DayType = reader.GetString("DayType"),
                StartTime = reader.IsDBNull(startOrd) ? (TimeSpan?)null : reader.GetTimeSpan(startOrd),
                EndTime = reader.IsDBNull(endOrd) ? (TimeSpan?)null : reader.GetTimeSpan(endOrd),
                HourlyRate = reader.IsDBNull(rateOrd) ? (decimal?)null : reader.GetDecimal(rateOrd),
                DiscountType = reader.IsDBNull(discTypeOrd) ? null : reader.GetString(discTypeOrd),
                DiscountValue = reader.IsDBNull(discValOrd) ? (decimal?)null : reader.GetDecimal(discValOrd),
                EffectiveFrom = reader.GetDateTime("EffectiveFrom"),
                EffectiveTo = reader.IsDBNull(effToOrd) ? (DateTime?)null : reader.GetDateTime(effToOrd),
                Priority = reader.GetInt32("Priority"),
                IsActive = reader.GetBoolean("IsActive"),
                CreatedAt = reader.GetDateTime("CreatedAt")
            };
        }

        private static bool IsDayTypeMatch(string ruleDayType, DateTime date)
        {
            if (string.Equals(ruleDayType, "Any", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            bool isHoliday = FixedHolidays.Contains(date.ToString("dd-MM"));
            if (string.Equals(ruleDayType, "Holiday", StringComparison.OrdinalIgnoreCase))
            {
                return isHoliday;
            }

            bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
            if (string.Equals(ruleDayType, "Weekend", StringComparison.OrdinalIgnoreCase))
            {
                return isWeekend;
            }

            if (string.Equals(ruleDayType, "Weekday", StringComparison.OrdinalIgnoreCase))
            {
                return !isWeekend && !isHoliday;
            }

            return false;
        }

        private static bool IsTimeSlotMatch(TimeSpan? start, TimeSpan? end, TimeSpan current)
        {
            if (!start.HasValue || !end.HasValue)
            {
                return true;
            }

            // Khung giờ trong cùng ngày (vd: 08:00 - 17:00)
            if (start.Value < end.Value)
            {
                return current >= start.Value && current <= end.Value;
            }

            // Khung giờ qua nửa đêm (vd: 22:00 - 06:00)
            return current >= start.Value || current <= end.Value;
        }
    }
}

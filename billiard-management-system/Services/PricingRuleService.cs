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
        List<SessionPricingDetail> CalculateTimeSlotSegments(long sessionId, int tableId, int tableTypeId, DateTime startTime, DateTime endTime, IEnumerable<TablePricingRule> rules);
        Task<(List<SessionPricingDetail> Segments, decimal TotalTableAmount, TimeSpan Duration)> CalculateSessionPricingAsync(long sessionId, int tableId, int tableTypeId, DateTime startTime, DateTime endTime);
        Task SaveSessionPricingDetailsAsync(MySqlConnection conn, MySqlTransaction trans, long sessionId, int tableId, int tableTypeId, IEnumerable<SessionPricingDetail> segments);
        Task<List<SessionPricingDetail>> GetSessionPricingDetailsAsync(long sessionId);
        (bool AllPassed, string Report) VerifyTimeSlotEngineScenarios();
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

            // Khung giờ trong cùng ngày (vd: 08:00 - 17:00), dùng khoảng nửa mở [start, end) để không trùng ở ranh giới
            if (start.Value < end.Value)
            {
                return current >= start.Value && current < end.Value;
            }

            // Khung giờ qua nửa đêm (vd: 22:00 - 06:00)
            return current >= start.Value || current < end.Value;
        }

        /// <summary>
        /// Chia phiên chơi từ startTime đến endTime thành các khoảng thời gian không trùng lặp, không bỏ sót ở ranh giới.
        /// Tại mỗi khoảng, chọn tối đa 1 quy tắc BaseRate có Priority cao nhất và tối đa 1 quy tắc Promotion có Priority cao nhất.
        /// Áp dụng khuyến mãi trực tiếp lên BaseHourlyRate (không cộng khuyến mãi như mức giá mới).
        /// </summary>
        public List<SessionPricingDetail> CalculateTimeSlotSegments(
            long sessionId, int tableId, int tableTypeId,
            DateTime startTime, DateTime endTime,
            IEnumerable<TablePricingRule> rules)
        {
            var segments = new List<SessionPricingDetail>();
            DateTime effectiveStart = startTime;
            DateTime effectiveEnd = endTime < startTime ? startTime : endTime;

            // Tối thiểu tính 1 phút nếu thời lượng dưới 1 phút
            if ((effectiveEnd - effectiveStart).TotalMinutes < 1.0)
            {
                effectiveEnd = effectiveStart.AddMinutes(1);
            }

            var activeRules = (rules ?? Enumerable.Empty<TablePricingRule>())
                .Where(r => r != null && r.IsActive && r.TableTypeId == tableTypeId)
                .ToList();

            // Bước 1: Tách theo ranh giới nửa đêm (00:00:00) khi phiên kéo dài qua ngày (ngày thường -> cuối tuần/ngày lễ)
            DateTime cursorDayStart = effectiveStart;
            while (cursorDayStart < effectiveEnd)
            {
                DateTime nextMidnight = cursorDayStart.Date.AddDays(1);
                DateTime cursorDayEnd = nextMidnight < effectiveEnd ? nextMidnight : effectiveEnd;
                DateTime currentDate = cursorDayStart.Date;

                // Các quy tắc có hiệu lực trong ngày currentDate
                var dayRules = activeRules
                    .Where(r => r.EffectiveFrom.Date <= currentDate
                                && (!r.EffectiveTo.HasValue || r.EffectiveTo.Value.Date >= currentDate)
                                && IsDayTypeMatch(r.DayType, currentDate))
                    .ToList();

                // Bước 2: Thu thập các mốc tách khung giờ (StartTime, EndTime) nằm trong khoảng (cursorDayStart, cursorDayEnd)
                var splitPoints = new SortedSet<DateTime> { cursorDayStart, cursorDayEnd };
                foreach (var rule in dayRules)
                {
                    if (rule.StartTime.HasValue)
                    {
                        DateTime ptStart = currentDate.Add(rule.StartTime.Value);
                        if (ptStart > cursorDayStart && ptStart < cursorDayEnd)
                        {
                            splitPoints.Add(ptStart);
                        }
                    }

                    if (rule.EndTime.HasValue)
                    {
                        DateTime ptEnd = currentDate.Add(rule.EndTime.Value);
                        if (ptEnd > cursorDayStart && ptEnd < cursorDayEnd)
                        {
                            splitPoints.Add(ptEnd);
                        }
                    }
                }

                var orderedPoints = splitPoints.ToList();
                for (int i = 0; i < orderedPoints.Count - 1; i++)
                {
                    DateTime sliceStart = orderedPoints[i];
                    DateTime sliceEnd = orderedPoints[i + 1];
                    if (sliceEnd <= sliceStart)
                    {
                        continue;
                    }

                    // Lấy điểm giữa của khoảng (sliceStart, sliceEnd) để đối chiếu chính xác quy tắc mà không bị nhập nhằng tại biên
                    DateTime midPoint = sliceStart.AddTicks((sliceEnd - sliceStart).Ticks / 2);
                    TimeSpan midTime = midPoint.TimeOfDay;

                    var matchingSliceRules = dayRules
                        .Where(r => IsTimeSlotMatch(r.StartTime, r.EndTime, midTime))
                        .ToList();

                    var baseRule = matchingSliceRules
                        .Where(r => string.Equals(r.RuleType, "BaseRate", StringComparison.OrdinalIgnoreCase) && r.HourlyRate.HasValue)
                        .OrderByDescending(r => r.Priority)
                        .ThenByDescending(r => r.PricingRuleId)
                        .FirstOrDefault();

                    // Chỉ chọn tối đa 1 khuyến mãi có độ ưu tiên cao nhất cho cùng một khoảng thời gian
                    var promoRule = matchingSliceRules
                        .Where(r => string.Equals(r.RuleType, "Promotion", StringComparison.OrdinalIgnoreCase) && r.DiscountValue.HasValue)
                        .OrderByDescending(r => r.Priority)
                        .ThenByDescending(r => r.PricingRuleId)
                        .FirstOrDefault();

                    long baseRuleId = baseRule?.PricingRuleId ?? 0;
                    decimal baseRate = baseRule?.HourlyRate ?? 50000m;
                    string baseRuleName = baseRule?.RuleName ?? "Giá cơ bản mặc định";

                    long? promoRuleId = promoRule?.PricingRuleId;
                    string discountType = promoRule?.DiscountType;
                    decimal discountVal = promoRule?.DiscountValue ?? 0m;
                    string promoRuleName = promoRule?.RuleName;

                    decimal finalRate = baseRate;
                    if (promoRule != null)
                    {
                        if (string.Equals(discountType, "Percentage", StringComparison.OrdinalIgnoreCase))
                        {
                            decimal pct = Math.Min(100m, Math.Max(0m, discountVal));
                            finalRate = Math.Round(baseRate * (100m - pct) / 100m, 0);
                        }
                        else if (string.Equals(discountType, "FixedAmount", StringComparison.OrdinalIgnoreCase))
                        {
                            finalRate = Math.Max(0m, baseRate - discountVal);
                        }
                    }

                    // Gộp với khoảng liền trước nếu cùng ngày và hoàn toàn trùng quy tắc giá + khuyến mãi + đơn giá
                    var lastSeg = segments.Count > 0 ? segments[segments.Count - 1] : null;
                    if (lastSeg != null
                        && lastSeg.EndTime == sliceStart
                        && lastSeg.BasePricingRuleId == baseRuleId
                        && lastSeg.PromotionPricingRuleId == promoRuleId
                        && lastSeg.BaseHourlyRate == baseRate
                        && string.Equals(lastSeg.DiscountType, discountType, StringComparison.OrdinalIgnoreCase)
                        && lastSeg.DiscountValue == discountVal
                        && lastSeg.FinalHourlyRate == finalRate)
                    {
                        lastSeg.EndTime = sliceEnd;
                        double mergedMinutes = (lastSeg.EndTime - lastSeg.StartTime).TotalMinutes;
                        lastSeg.DurationMinutes = Math.Round((decimal)mergedMinutes, 2);
                        lastSeg.Amount = Math.Round((decimal)(lastSeg.EndTime - lastSeg.StartTime).TotalHours * lastSeg.FinalHourlyRate, 0);
                    }
                    else
                    {
                        double sliceMinutes = (sliceEnd - sliceStart).TotalMinutes;
                        segments.Add(new SessionPricingDetail
                        {
                            SessionId = sessionId,
                            TableId = tableId,
                            BasePricingRuleId = baseRuleId,
                            PromotionPricingRuleId = promoRuleId,
                            StartTime = sliceStart,
                            EndTime = sliceEnd,
                            DurationMinutes = Math.Round((decimal)sliceMinutes, 2),
                            BaseHourlyRate = baseRate,
                            DiscountType = discountType,
                            DiscountValue = discountVal,
                            FinalHourlyRate = finalRate,
                            Amount = Math.Round((decimal)(sliceEnd - sliceStart).TotalHours * finalRate, 0),
                            BaseRuleName = baseRuleName,
                            PromotionRuleName = promoRuleName
                        });
                    }
                }

                cursorDayStart = cursorDayEnd;
            }

            return segments;
        }

        public async Task<(List<SessionPricingDetail> Segments, decimal TotalTableAmount, TimeSpan Duration)> CalculateSessionPricingAsync(
            long sessionId, int tableId, int tableTypeId, DateTime startTime, DateTime endTime)
        {
            var activeRules = await GetPricingRulesAsync(tableTypeId, null, includeInactive: false);
            var segments = CalculateTimeSlotSegments(sessionId, tableId, tableTypeId, startTime, endTime, activeRules);

            TimeSpan duration = endTime - startTime;
            if (duration < TimeSpan.Zero)
            {
                duration = TimeSpan.Zero;
            }
            if (duration.TotalMinutes < 1.0)
            {
                duration = TimeSpan.FromMinutes(1);
            }

            decimal totalTableAmount = segments.Sum(s => s.Amount);
            return (segments, totalTableAmount, duration);
        }

        /// <summary>
        /// Lưu danh sách các khoảng thời gian tính tiền bàn vào bảng SessionPricingDetails trong cùng transaction.
        /// Bảo toàn lịch sử giá và khuyến mãi tại thời điểm tính.
        /// </summary>
        public async Task SaveSessionPricingDetailsAsync(
            MySqlConnection conn, MySqlTransaction trans,
            long sessionId, int tableId, int tableTypeId,
            IEnumerable<SessionPricingDetail> segments)
        {
            if (segments == null)
            {
                return;
            }

            // Xóa chi tiết cũ của phiên (nếu có tính lại trước khi chốt) để tránh trùng bản ghi
            const string deleteSql = "DELETE FROM SessionPricingDetails WHERE SessionId = @SessionId;";
            using (var cmdDel = new MySqlCommand(deleteSql, conn, trans))
            {
                cmdDel.Parameters.AddWithValue("@SessionId", sessionId);
                await cmdDel.ExecuteNonQueryAsync();
            }

            long? fallbackBaseRuleId = null;

            const string insertSql = @"
                INSERT INTO SessionPricingDetails
                    (SessionId, TableId, BasePricingRuleId, PromotionPricingRuleId,
                     StartTime, EndTime, DurationMinutes, BaseHourlyRate,
                     DiscountType, DiscountValue, FinalHourlyRate, Amount)
                VALUES
                    (@SessionId, @TableId, @BasePricingRuleId, @PromotionPricingRuleId,
                     @StartTime, @EndTime, @DurationMinutes, @BaseHourlyRate,
                     @DiscountType, @DiscountValue, @FinalHourlyRate, @Amount);";

            foreach (var seg in segments)
            {
                long baseRuleId = seg.BasePricingRuleId;
                if (baseRuleId <= 0)
                {
                    if (!fallbackBaseRuleId.HasValue)
                    {
                        fallbackBaseRuleId = await ResolveOrCreateFallbackBaseRuleIdAsync(conn, trans, tableTypeId, seg.BaseHourlyRate);
                    }
                    baseRuleId = fallbackBaseRuleId.Value;
                    seg.BasePricingRuleId = baseRuleId;
                }

                using (var cmdIns = new MySqlCommand(insertSql, conn, trans))
                {
                    cmdIns.Parameters.AddWithValue("@SessionId", sessionId);
                    cmdIns.Parameters.AddWithValue("@TableId", tableId);
                    cmdIns.Parameters.AddWithValue("@BasePricingRuleId", baseRuleId);
                    cmdIns.Parameters.AddWithValue("@PromotionPricingRuleId", seg.PromotionPricingRuleId.HasValue && seg.PromotionPricingRuleId.Value > 0
                        ? (object)seg.PromotionPricingRuleId.Value
                        : DBNull.Value);
                    cmdIns.Parameters.AddWithValue("@StartTime", seg.StartTime);
                    cmdIns.Parameters.AddWithValue("@EndTime", seg.EndTime);
                    cmdIns.Parameters.AddWithValue("@DurationMinutes", seg.DurationMinutes);
                    cmdIns.Parameters.AddWithValue("@BaseHourlyRate", seg.BaseHourlyRate);
                    cmdIns.Parameters.AddWithValue("@DiscountType", !string.IsNullOrWhiteSpace(seg.DiscountType)
                        ? (object)seg.DiscountType
                        : DBNull.Value);
                    cmdIns.Parameters.AddWithValue("@DiscountValue", seg.DiscountValue);
                    cmdIns.Parameters.AddWithValue("@FinalHourlyRate", seg.FinalHourlyRate);
                    cmdIns.Parameters.AddWithValue("@Amount", seg.Amount);
                    await cmdIns.ExecuteNonQueryAsync();
                }
            }
        }

        private static async Task<long> ResolveOrCreateFallbackBaseRuleIdAsync(
            MySqlConnection conn, MySqlTransaction trans, int tableTypeId, decimal baseHourlyRate)
        {
            const string findRuleSql = @"
                SELECT PricingRuleId
                FROM TablePricingRules
                WHERE TableTypeId = @TableTypeId AND RuleType = 'BaseRate'
                ORDER BY IsActive DESC, Priority DESC, PricingRuleId ASC
                LIMIT 1;";

            using (var cmdFind = new MySqlCommand(findRuleSql, conn, trans))
            {
                cmdFind.Parameters.AddWithValue("@TableTypeId", tableTypeId);
                object existing = await cmdFind.ExecuteScalarAsync();
                if (existing != null && existing != DBNull.Value)
                {
                    return Convert.ToInt64(existing);
                }
            }

            const string insertFallbackSql = @"
                INSERT INTO TablePricingRules
                    (TableTypeId, RuleName, RuleType, DayType, StartTime, EndTime,
                     HourlyRate, DiscountType, DiscountValue, EffectiveFrom, EffectiveTo, Priority, IsActive, CreatedAt)
                VALUES
                    (@TableTypeId, 'Giá cơ bản mặc định', 'BaseRate', 'Any', NULL, NULL,
                     @HourlyRate, NULL, NULL, '2026-01-01', NULL, 1, TRUE, @Now);
                SELECT LAST_INSERT_ID();";

            using (var cmdCreate = new MySqlCommand(insertFallbackSql, conn, trans))
            {
                cmdCreate.Parameters.AddWithValue("@TableTypeId", tableTypeId);
                cmdCreate.Parameters.AddWithValue("@HourlyRate", baseHourlyRate > 0 ? baseHourlyRate : 50000m);
                cmdCreate.Parameters.AddWithValue("@Now", DateTime.Now);
                return Convert.ToInt64(await cmdCreate.ExecuteScalarAsync());
            }
        }

        public async Task<List<SessionPricingDetail>> GetSessionPricingDetailsAsync(long sessionId)
        {
            var list = new List<SessionPricingDetail>();
            using (var conn = await DatabaseHelper.OpenConnectionAsync())
            {
                const string sql = @"
                    SELECT spd.SessionPricingDetailId, spd.SessionId, spd.TableId,
                           spd.BasePricingRuleId, spd.PromotionPricingRuleId,
                           spd.StartTime, spd.EndTime, spd.DurationMinutes,
                           spd.BaseHourlyRate, spd.DiscountType, spd.DiscountValue,
                           spd.FinalHourlyRate, spd.Amount,
                           br.RuleName AS BaseRuleName,
                           pr.RuleName AS PromotionRuleName
                    FROM SessionPricingDetails spd
                    LEFT JOIN TablePricingRules br ON spd.BasePricingRuleId = br.PricingRuleId
                    LEFT JOIN TablePricingRules pr ON spd.PromotionPricingRuleId = pr.PricingRuleId
                    WHERE spd.SessionId = @SessionId
                    ORDER BY spd.StartTime ASC;";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@SessionId", sessionId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        int promoIdOrd = reader.GetOrdinal("PromotionPricingRuleId");
                        int discTypeOrd = reader.GetOrdinal("DiscountType");
                        int baseNameOrd = reader.GetOrdinal("BaseRuleName");
                        int promoNameOrd = reader.GetOrdinal("PromotionRuleName");

                        while (await reader.ReadAsync())
                        {
                            list.Add(new SessionPricingDetail
                            {
                                SessionPricingDetailId = reader.GetInt64("SessionPricingDetailId"),
                                SessionId = reader.GetInt64("SessionId"),
                                TableId = reader.GetInt32("TableId"),
                                BasePricingRuleId = reader.GetInt64("BasePricingRuleId"),
                                PromotionPricingRuleId = reader.IsDBNull(promoIdOrd) ? (long?)null : reader.GetInt64(promoIdOrd),
                                StartTime = reader.GetDateTime("StartTime"),
                                EndTime = reader.GetDateTime("EndTime"),
                                DurationMinutes = reader.GetDecimal("DurationMinutes"),
                                BaseHourlyRate = reader.GetDecimal("BaseHourlyRate"),
                                DiscountType = reader.IsDBNull(discTypeOrd) ? null : reader.GetString(discTypeOrd),
                                DiscountValue = reader.GetDecimal("DiscountValue"),
                                FinalHourlyRate = reader.GetDecimal("FinalHourlyRate"),
                                Amount = reader.GetDecimal("Amount"),
                                BaseRuleName = reader.IsDBNull(baseNameOrd) ? "Giá cơ bản" : reader.GetString(baseNameOrd),
                                PromotionRuleName = reader.IsDBNull(promoNameOrd) ? null : reader.GetString(promoNameOrd)
                            });
                        }
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Bộ kiểm thử tự động cho 3 kịch bản cốt lõi:
        /// 1) Phiên nằm trọn trong một khung giờ (kèm chọn 1 khuyến mãi ưu tiên cao nhất).
        /// 2) Phiên đi qua nhiều khung giờ (giờ thường -> giờ cao điểm).
        /// 3) Phiên kéo dài qua ngày (ngày thường Thứ Sáu -> cuối tuần Thứ Bảy), kiểm tra không trùng/không sót ranh giới.
        /// </summary>
        public (bool AllPassed, string Report) VerifyTimeSlotEngineScenarios()
        {
            var testRules = new List<TablePricingRule>
            {
                new TablePricingRule
                {
                    PricingRuleId = 1, TableTypeId = 1, RuleName = "Giá ngày thường",
                    RuleType = "BaseRate", DayType = "Weekday",
                    StartTime = null, EndTime = null, HourlyRate = 50000m,
                    EffectiveFrom = new DateTime(2026, 1, 1), Priority = 1, IsActive = true
                },
                new TablePricingRule
                {
                    PricingRuleId = 2, TableTypeId = 1, RuleName = "Giờ cao điểm tối ngày thường",
                    RuleType = "BaseRate", DayType = "Weekday",
                    StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(22, 0, 0), HourlyRate = 80000m,
                    EffectiveFrom = new DateTime(2026, 1, 1), Priority = 3, IsActive = true
                },
                new TablePricingRule
                {
                    PricingRuleId = 3, TableTypeId = 1, RuleName = "Giá cuối tuần",
                    RuleType = "BaseRate", DayType = "Weekend",
                    StartTime = null, EndTime = null, HourlyRate = 70000m,
                    EffectiveFrom = new DateTime(2026, 1, 1), Priority = 2, IsActive = true
                },
                new TablePricingRule
                {
                    PricingRuleId = 4, TableTypeId = 1, RuleName = "KM Happy Hour -20%",
                    RuleType = "Promotion", DayType = "Weekday",
                    StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(12, 0, 0),
                    DiscountType = "Percentage", DiscountValue = 20m,
                    EffectiveFrom = new DateTime(2026, 1, 1), Priority = 5, IsActive = true
                },
                new TablePricingRule
                {
                    PricingRuleId = 5, TableTypeId = 1, RuleName = "KM Phụ -5,000đ (Ưu tiên thấp hơn)",
                    RuleType = "Promotion", DayType = "Weekday",
                    StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(12, 0, 0),
                    DiscountType = "FixedAmount", DiscountValue = 5000m,
                    EffectiveFrom = new DateTime(2026, 1, 1), Priority = 2, IsActive = true
                }
            };

            // Kịch bản 1: Nằm trọn trong 1 khung giờ (Thứ Năm 05/11/2026 09:30 -> 11:30 = 2 giờ, áp dụng 1 KM Priority=5 giảm 20% của 50k = 40k/h -> 80,000đ)
            var s1 = CalculateTimeSlotSegments(101, 1, 1,
                new DateTime(2026, 11, 5, 9, 30, 0),
                new DateTime(2026, 11, 5, 11, 30, 0),
                testRules);
            bool pass1 = s1.Count == 1
                         && s1[0].DurationMinutes == 120m
                         && s1[0].PromotionPricingRuleId == 4
                         && s1[0].FinalHourlyRate == 40000m
                         && s1[0].Amount == 80000m;

            // Kịch bản 2: Qua nhiều khung giờ (Thứ Năm 05/11/2026 17:00 -> 19:30: 1h giá thường 50k + 1.5h giờ cao điểm 80k = 50k + 120k = 170,000đ)
            var s2 = CalculateTimeSlotSegments(102, 1, 1,
                new DateTime(2026, 11, 5, 17, 0, 0),
                new DateTime(2026, 11, 5, 19, 30, 0),
                testRules);
            bool pass2 = s2.Count == 2
                         && s2[0].EndTime == s2[1].StartTime
                         && s2[0].DurationMinutes == 60m && s2[0].Amount == 50000m
                         && s2[1].DurationMinutes == 90m && s2[1].Amount == 120000m;

            // Kịch bản 3: Qua ngày Thứ Sáu 06/11/2026 23:00 -> Thứ Bảy 07/11/2026 01:00 (1h ngày thường 50k + 1h cuối tuần 70k = 120,000đ)
            var s3 = CalculateTimeSlotSegments(103, 1, 1,
                new DateTime(2026, 11, 6, 23, 0, 0),
                new DateTime(2026, 11, 7, 1, 0, 0),
                testRules);
            bool pass3 = s3.Count == 2
                         && s3[0].EndTime == new DateTime(2026, 11, 7, 0, 0, 0)
                         && s3[1].StartTime == new DateTime(2026, 11, 7, 0, 0, 0)
                         && s3[0].Amount == 50000m
                         && s3[1].Amount == 70000m;

            bool allPassed = pass1 && pass2 && pass3;
            string report = $"Scenario1(SingleSlot+SinglePromo)={pass1}; Scenario2(MultiSlotPeak)={pass2}; Scenario3(CrossDayWeekdayToWeekend)={pass3}";
            return (allPassed, report);
        }
    }
}

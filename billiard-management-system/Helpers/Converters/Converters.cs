using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace billiard_management_system.Helpers.Converters
{
    /// <summary>
    /// Chuyển trạng thái bàn thành màu sắc đặc trưng
    /// </summary>
    public class TableStatusToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush AvailableBrush = new SolidColorBrush(Color.FromRgb(46, 125, 50));   // Xanh lá (Green 800)
        private static readonly SolidColorBrush OccupiedBrush = new SolidColorBrush(Color.FromRgb(198, 40, 40));     // Đỏ đậm (Red 800)
        private static readonly SolidColorBrush MaintenanceBrush = new SolidColorBrush(Color.FromRgb(117, 117, 117)); // Xám (Grey 600)

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as string;
            switch (status)
            {
                case "Available":
                    return AvailableBrush;
                case "Occupied":
                    return OccupiedBrush;
                case "Maintenance":
                    return MaintenanceBrush;
                default:
                    return AvailableBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Chuyển đổi số tiền thành chuỗi định dạng tiền tệ Việt Nam (VNĐ)
    /// </summary>
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "0 đ";

            if (decimal.TryParse(value.ToString(), out decimal amount))
            {
                return string.Format(new CultureInfo("vi-VN"), "{0:N0} đ", amount);
            }
            return "0 đ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Chuyển đổi TimeSpan sang định dạng thời gian chơi (vd: 01:25:30)
    /// </summary>
    public class DurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan ts)
            {
                return string.Format("{0:00}:{1:00}:{2:00}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
            }
            return "00:00:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

    /// <summary>
    /// Chuyển đổi mã menu đang chọn thành màu nền / màu chữ tương ứng
    /// </summary>
    public class MenuActiveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string currentMenu = value as string;
            string targetMenu = parameter as string;

            bool isActive = string.Equals(currentMenu, targetMenu, StringComparison.OrdinalIgnoreCase);

            if (targetType == typeof(Brush))
            {
                return isActive ? new SolidColorBrush(Color.FromRgb(30, 41, 59)) : Brushes.Transparent; // #1E293B
            }

            if (targetType == typeof(FontWeight))
            {
                return isActive ? FontWeights.Bold : FontWeights.Normal;
            }

            return isActive;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Chuyển đổi trạng thái loại thông báo sang màu viền / nền banner thông báo
    /// </summary>
    public class NotificationTypeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string type = value as string;
            switch (type)
            {
                case "Success":
                    return new SolidColorBrush(Color.FromRgb(16, 185, 129)); // #10B981 Emerald
                case "Error":
                    return new SolidColorBrush(Color.FromRgb(239, 68, 68)); // #EF4444 Red
                case "Warning":
                    return new SolidColorBrush(Color.FromRgb(245, 158, 11)); // #F59E0B Amber
                default:
                    return new SolidColorBrush(Color.FromRgb(2, 132, 199)); // #0284C7 Sky
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Chuyển đổi chuỗi URL ảnh (Cloudinary / HTTP / File cục bộ) sang ImageSource an toàn,
    /// tự động bắt lỗi nếu URL không hợp lệ hoặc không có mạng mà không làm crash giao diện.
    /// </summary>
    public class SafeImageUrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string url = value as string;
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            try
            {
                string cleanUrl = url.Trim();
                Uri uri = null;

                if (Uri.TryCreate(cleanUrl, UriKind.Absolute, out Uri parsedUri) &&
                    (parsedUri.Scheme == Uri.UriSchemeHttp || parsedUri.Scheme == Uri.UriSchemeHttps || parsedUri.IsFile))
                {
                    uri = parsedUri;
                }
                else if (File.Exists(cleanUrl))
                {
                    uri = new Uri(Path.GetFullPath(cleanUrl), UriKind.Absolute);
                }

                if (uri == null) return null;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.CacheOption = BitmapCacheOption.OnDemand;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bitmap.EndInit();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Đảo ngược giá trị bool sang Visibility (true -> Collapsed, false -> Visible)
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            return flag ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
            {
                return v != Visibility.Visible;
            }
            return false;
        }
    }
}

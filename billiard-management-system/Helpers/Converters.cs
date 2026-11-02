using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace billiard_management_system.Helpers
{
    /// <summary>
    /// Value converter chuyển đổi giữa bool và Visibility (Visible / Collapsed)
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;
            if (value is bool b)
            {
                flag = b;
            }

            if (Invert) flag = !flag;

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility vis)
            {
                bool result = vis == Visibility.Visible;
                return Invert ? !result : result;
            }
            return false;
        }
    }

    /// <summary>
    /// Value converter định dạng số tiền sang chuẩn VND (ví dụ: 50.000 đ)
    /// </summary>
    public class CurrencyFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "0 đ";

            if (decimal.TryParse(value.ToString(), out decimal amount))
            {
                return string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} đ", amount);
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

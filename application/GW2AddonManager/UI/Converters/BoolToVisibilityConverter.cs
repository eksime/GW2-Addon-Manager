namespace GW2AddonManager.UI.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                true => Visibility.Visible,
                false => Visibility.Collapsed,
                _ => throw new InvalidOperationException()
            };
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                Visibility.Visible => true,
                Visibility.Hidden => false,
                Visibility.Collapsed => false,
                _ => throw new InvalidOperationException()
            };
        }
    }
}

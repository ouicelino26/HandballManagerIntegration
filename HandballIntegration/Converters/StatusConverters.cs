using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HandballIntegration.Converters
{
    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            value is long l && l > 0 || value is int i && i > 0
                ? Visibility.Visible
                : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class AdminPageStateKindToBadgeBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is not string s) return Brushes.Transparent;
            return s switch
            {
                "Loaded" or "Available" or "AVAILABLE" => new SolidColorBrush(Color.FromRgb(0xE9, 0xEF, 0xEA)),
                "Partial" or "FoundationReady" or "PARTIAL" => new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xE0)),
                "Loading" or "Idle" => new SolidColorBrush(Color.FromRgb(0xE8, 0xEC, 0xF0)),
                _ => new SolidColorBrush(Color.FromRgb(0xFD, 0xEB, 0xEB))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class AdminPageStateKindToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is not string s) return Brushes.Gray;
            return s switch
            {
                "Loaded" or "Available" or "AVAILABLE" => new SolidColorBrush(Color.FromRgb(0x25, 0x73, 0x52)),
                "Partial" or "FoundationReady" or "PARTIAL" => new SolidColorBrush(Color.FromRgb(0xA8, 0x64, 0x16)),
                "Loading" or "Idle" => new SolidColorBrush(Color.FromRgb(0x52, 0x61, 0x5B)),
                _ => new SolidColorBrush(Color.FromRgb(0xB5, 0x41, 0x3D))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }


    public class StatusToSuccessVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is IntegrationStatus.Success
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StatusToErrorVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is IntegrationStatus.Error
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StatusToPendingVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is IntegrationStatus.Pending
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StatusToBusyVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is IntegrationStatus status &&
                   (status == IntegrationStatus.Converting || status == IntegrationStatus.Integrating)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }
}

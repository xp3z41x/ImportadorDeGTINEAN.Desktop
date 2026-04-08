using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ImportadorDeGTINEAN.Desktop.Models;

namespace ImportadorDeGTINEAN.Desktop.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ImportStatus status)
                return Brushes.Transparent;

            return status switch
            {
                ImportStatus.Matched => new SolidColorBrush(Color.FromRgb(0xE8, 0xF5, 0xE9)),    // Light green
                ImportStatus.Updated => new SolidColorBrush(Color.FromRgb(0xC8, 0xE6, 0xC9)),     // Green
                ImportStatus.NoMatch => new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xE0)),     // Light orange
                ImportStatus.InvalidBarcode => new SolidColorBrush(Color.FromRgb(0xFF, 0xEB, 0xEE)), // Light red
                ImportStatus.DuplicateBarcode => new SolidColorBrush(Color.FromRgb(0xFC, 0xE4, 0xEC)), // Light pink
                ImportStatus.AlreadySet => new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)),  // Light gray
                ImportStatus.Error => new SolidColorBrush(Color.FromRgb(0xFF, 0xCD, 0xD2)),       // Red
                _ => Brushes.Transparent
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class StatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ImportStatus status)
                return string.Empty;

            return status switch
            {
                ImportStatus.Pending => "Pendente",
                ImportStatus.Matched => "OK",
                ImportStatus.Updated => "Atualizado",
                ImportStatus.NoMatch => "Não Encontrado",
                ImportStatus.InvalidBarcode => "EAN Inválido",
                ImportStatus.DuplicateBarcode => "Duplicado",
                ImportStatus.AlreadySet => "Já Existe",
                ImportStatus.Error => "Erro",
                _ => string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class StatusToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ImportStatus status)
                return Brushes.Black;

            return status switch
            {
                ImportStatus.Matched or ImportStatus.Updated => new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)),
                ImportStatus.NoMatch => new SolidColorBrush(Color.FromRgb(0xE6, 0x51, 0x00)),
                ImportStatus.InvalidBarcode or ImportStatus.Error => new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)),
                ImportStatus.DuplicateBarcode => new SolidColorBrush(Color.FromRgb(0xAD, 0x14, 0x57)),
                ImportStatus.AlreadySet => new SolidColorBrush(Color.FromRgb(0x75, 0x75, 0x75)),
                _ => Brushes.Black
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            return System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

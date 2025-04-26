using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using RobustEstimation.Models;

namespace RobustEstimation.Properties
{
    public class MethodTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MethodType m)
            {
                // Assumes your .resx has keys: Method_Median, Method_Huber, etc.
                return Resources.ResourceManager
                                 .GetString($"Methods_{m}", culture)
                   ?? m.ToString();
            }
            return value ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class LanguageToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AppLanguage l)
            {
                // Assumes your .resx has keys: Lang_English, Lang_Czech, Lang_Russian
                return Resources.ResourceManager
                                 .GetString($"Languages_{l}", culture)
                   ?? l.ToString();
            }
            return value ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

using System;
using System.Globalization;
using System.Windows.Data;

namespace ElectroStore1.Converters
{
    public class NullToTextConverter : IValueConverter
    {
        public string DefaultText { get; set; } = "Не указано";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Прямое преобразование (для отображения)
            if (value == null || (value is string str && string.IsNullOrEmpty(str)))
            {
                return parameter as string ?? DefaultText;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Обратное преобразование (при редактировании)
            if (value is string stringValue)
            {
                // Если ввели текст "Не указано" или пустую строку — возвращаем null
                if (stringValue == DefaultText || string.IsNullOrEmpty(stringValue))
                {
                    return null;
                }
                return stringValue;
            }
            return value;
        }
    }
}
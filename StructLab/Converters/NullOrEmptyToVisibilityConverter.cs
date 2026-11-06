using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StructLab.Converters
{
	/// <summary>
	/// Возвращает Visible, если значение НЕ null и НЕ пустая строка.
	/// Используется для показа сообщения об ошибке.
	/// </summary>
	public class NullOrEmptyToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) return Visibility.Collapsed;
			if (value is string s && string.IsNullOrWhiteSpace(s)) return Visibility.Collapsed;
			return Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
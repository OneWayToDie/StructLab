using System;
using System.Globalization;
using System.Windows.Data;

namespace StructLab.Converters
{
	/// <summary>
	/// Возвращает true, если значение (int) больше нуля.
	/// Используется для DataTrigger: подсветка бейджа должников.
	/// </summary>
	public class GreaterThanZeroConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> value is int i && i > 0;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}
}

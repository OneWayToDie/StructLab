using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace StructLab.Reading.Helpers
{
	/// <summary>
	/// Конвертирует plain text в FlowDocument для FlowDocumentReader.
	/// Разбивает по пустым строкам на параграфы.
	/// </summary>
	public static class FlowDocumentConverter
	{
		public static FlowDocument FromPlainText(
			string title, string text, double fontSize, string fontFamily, string theme)
		{
			var (bg, fg) = ThemeColors(theme);

			var doc = new FlowDocument
			{
				FontFamily = new FontFamily(string.IsNullOrWhiteSpace(fontFamily) ? "Georgia" : fontFamily),
				FontSize = fontSize,
				PagePadding = new Thickness(48, 32, 48, 48),  
				Background = bg,
				Foreground = fg,
				ColumnWidth = 760,
				PageWidth = 800
			};

			if (!string.IsNullOrWhiteSpace(title))
			{
				doc.Blocks.Add(new Paragraph(new Run(title))
				{
					FontSize = fontSize * 1.7,
					FontWeight = FontWeights.Bold,
					Margin = new Thickness(0, 0, 0, 24),
					TextAlignment = TextAlignment.Center
				});
			}

			var paragraphs = (text ?? "")
				.Replace("\r\n", "\n")
				.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var raw in paragraphs)
			{
				var trimmed = raw.Trim();
				if (string.IsNullOrEmpty(trimmed)) continue;

				doc.Blocks.Add(new Paragraph(new Run(trimmed))
				{
					Margin = new Thickness(0, 0, 0, 14),
					TextIndent = 24,
					LineHeight = fontSize * 1.7
				});
			}

			return doc;
		}

		public static (Brush Background, Brush Foreground) ThemeColors(string theme)
		{
			switch (theme)
			{
				case "Dark":
					return (B("#1E1E1E"), B("#E0E0E0"));
				case "Sepia":
					return (B("#F4ECD8"), B("#3B2E1E"));
				default:
					return (B("#FFFFFF"), B("#1A1A1A"));
			}
		}

		private static SolidColorBrush B(string hex)
		{
			var c = (Color)ColorConverter.ConvertFromString(hex);
			var brush = new SolidColorBrush(c);
			brush.Freeze();
			return brush;
		}
	}
}

using System.Text.Json.Serialization;

namespace StructLab.Reading.Models
{
	/// <summary>
	/// Глава книги. Для Викитеки глава = подстраница произведения.
	/// </summary>
	public class Chapter
	{
		/// <summary>Порядковый номер с нуля.</summary>
		public int Index { get; set; }

		public string Title { get; set; }
		public string Text { get; set; }
		public int WordCount { get; set; }

		public Chapter()
		{
		}

		public Chapter(int index, string title, string text)
		{
			Index = index;
			Title = title;
			Text = text ?? "";
			WordCount = CountWords(Text);
		}

		/// <summary>Номер для показа пользователю (с единицы).</summary>
		[JsonIgnore]
		public int Number => Index + 1;

		public static int CountWords(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return 0;

			var count = 0;
			var inWord = false;

			foreach (var ch in text)
			{
				if (char.IsWhiteSpace(ch))
				{
					inWord = false;
				}
				else if (!inWord)
				{
					inWord = true;
					count++;
				}
			}

			return count;
		}
	}
}

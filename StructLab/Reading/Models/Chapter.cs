using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace StructLab.Reading.Models
{
	/// <summary>
	/// Глава книги. Для Викитеки глава = подстраница произведения.
	/// </summary>
	public class Chapter : INotifyPropertyChanged
	{
		private bool _isRead;
		private bool _isCurrent;

		/// <summary>Порядковый номер с нуля.</summary>
		public int Index { get; set; }

		public string Title { get; set; }
		public string Text { get; set; }
		public int WordCount { get; set; }

		/// <summary>Глава уже прочитана (пользователь ушёл дальше по книге).</summary>
		[JsonIgnore]
		public bool IsRead
		{
			get => _isRead;
			set
			{
				if (_isRead == value) return;
				_isRead = value;
				OnPropertyChanged();
			}
		}

		/// <summary>Глава открыта прямо сейчас.</summary>
		[JsonIgnore]
		public bool IsCurrent
		{
			get => _isCurrent;
			set
			{
				if (_isCurrent == value) return;
				_isCurrent = value;
				OnPropertyChanged();
			}
		}

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

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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

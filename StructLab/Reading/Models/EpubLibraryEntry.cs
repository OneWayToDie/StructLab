using System;
using System.Text.Json.Serialization;

namespace StructLab.Reading.Models
{
	/// <summary>
	/// Запись в истории загруженных EPUB-книг.
	/// </summary>
	public class EpubLibraryEntry
	{
		/// <summary>Хэш файла (совпадает с Book.Id и именем файла кэша).</summary>
		public string Id { get; set; }

		public string Title { get; set; }
		public string Author { get; set; }
		public string FilePath { get; set; }
		public int ChapterCount { get; set; }
		public int WordCount { get; set; }
		public DateTime LastOpenedAt { get; set; }

		[JsonIgnore]
		public string Subtitle
		{
			get
			{
				var parts = new System.Collections.Generic.List<string>();

				if (!string.IsNullOrWhiteSpace(Author))
					parts.Add(Author);

				parts.Add("Глав: " + ChapterCount);
				return string.Join(" · ", parts);
			}
		}
	}
}

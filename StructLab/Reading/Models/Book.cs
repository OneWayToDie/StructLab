using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StructLab.Reading.Models
{
	/// <summary>
	/// Книга: метаданные + упорядоченный список глав.
	/// Пришла на смену плоскому <see cref="BookContent"/>.
	/// </summary>
	public class Book
	{
		/// <summary>Идентификатор внутри провайдера (для Викитеки — pageId).</summary>
		public string Id { get; set; }

		/// <summary>Идентификатор провайдера-источника (wikisource, epub, ...).</summary>
		public string ProviderId { get; set; }

		public string Title { get; set; }
		public string Author { get; set; }
		public List<Chapter> Chapters { get; set; }
		public BookMetadata Metadata { get; set; }
		public DateTime LoadedAt { get; set; }

		/// <summary>
		/// true, если у произведения было больше глав, чем разрешено загружать.
		/// </summary>
		public bool ChaptersTruncated { get; set; }

		public Book()
		{
			Chapters = new List<Chapter>();
			Metadata = new BookMetadata();
		}

		[JsonIgnore]
		public int ChapterCount => Chapters == null ? 0 : Chapters.Count;

		[JsonIgnore]
		public int WordCount
		{
			get
			{
				if (Chapters == null) return 0;

				var total = 0;
				foreach (var chapter in Chapters)
					total += chapter.WordCount;

				return total;
			}
		}

		/// <summary>Полный идентификатор: providerId:id.</summary>
		[JsonIgnore]
		public string FullId => (ProviderId ?? "") + ":" + (Id ?? "");
	}
}

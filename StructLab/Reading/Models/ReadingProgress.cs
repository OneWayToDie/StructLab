using System;

namespace StructLab.Reading.Models
{
	/// <summary>
	/// Сохранённая позиция чтения книги.
	/// BookId — полный идентификатор (provider:id), чтобы Викитека и EPUB не пересекались.
	/// </summary>
	public class ReadingProgress
	{
		public string BookId { get; set; }
		public int ChapterIndex { get; set; }
		public double ScrollOffset { get; set; }

		/// <summary>Наибольшая достигнутая глава — для «прочитано N из M».</summary>
		public int MaxChapterIndex { get; set; }

		public DateTime UpdatedAt { get; set; }
	}
}

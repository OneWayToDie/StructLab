using System.Text.Json.Serialization;

namespace StructLab.Reading.Models
{
	/// <summary>Найденный фрагмент при поиске по всем главам книги.</summary>
	public class BookSearchHit
	{
		public int ChapterIndex { get; set; }
		public string ChapterTitle { get; set; }
		public string Snippet { get; set; }

		/// <summary>Позиция совпадения внутри главы, 0..1 — для прокрутки.</summary>
		public double PositionRatio { get; set; }

		[JsonIgnore]
		public string Title => (ChapterIndex + 1) + ". " + ChapterTitle;
	}
}

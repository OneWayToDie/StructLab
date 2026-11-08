using System;

namespace StructLab.Reading.Models
{
	public class BookContent
	{
		public string PageId { get; set; }
		public string Title { get; set; }
		public string PlainText { get; set; }
		public DateTime LoadedAt { get; set; }

		/// <summary>
		/// true, если у произведения было больше глав, чем разрешено загружать.
		/// </summary>
		public bool ChaptersTruncated { get; set; }
	}
}
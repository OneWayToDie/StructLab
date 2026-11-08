namespace StructLab.Reading.Models
{
	/// <summary>
	/// Дополнительные сведения об источнике книги.
	/// Заполняется по возможности: если источник их не отдал, поля остаются пустыми.
	/// </summary>
	public class BookMetadata
	{
		public string Author { get; set; }
		public string Year { get; set; }
		public string License { get; set; }
		public string SourceUrl { get; set; }
		public string Language { get; set; }
		public string Description { get; set; }
	}
}

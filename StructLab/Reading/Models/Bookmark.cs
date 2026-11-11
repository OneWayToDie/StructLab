using System;
using System.Text.Json.Serialization;

namespace StructLab.Reading.Models
{
	/// <summary>Закладка в книге: глава + позиция прокрутки.</summary>
	public class Bookmark
	{
		public string Id { get; set; }
		public string BookId { get; set; }
		public int ChapterIndex { get; set; }
		public string ChapterTitle { get; set; }
		public double ScrollOffset { get; set; }
		public DateTime CreatedAt { get; set; }

		[JsonIgnore]
		public string Title
		{
			get
			{
				var chapter = string.IsNullOrWhiteSpace(ChapterTitle)
					? "Глава " + (ChapterIndex + 1)
					: ChapterTitle;
				return "Гл. " + (ChapterIndex + 1) + ". " + chapter;
			}
		}

		[JsonIgnore]
		public string Subtitle => CreatedAt.ToString("dd.MM.yyyy HH:mm");
	}
}

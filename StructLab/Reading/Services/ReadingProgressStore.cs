using System;
using System.Collections.Generic;
using StructLab.Reading.Helpers;
using StructLab.Reading.Models;

namespace StructLab.Reading.Services
{
	/// <summary>
	/// Позиции чтения: %LocalAppData%\StructLab\reading-progress.json.
	/// Одна запись на книгу (по полному идентификатору provider:id).
	/// </summary>
	public class ReadingProgressStore
	{
		private readonly string _path;
		private readonly List<ReadingProgress> _items;

		public ReadingProgressStore()
		{
			_path = ReadingPaths.ReadingProgress;
			_items = JsonFileStore.Load<List<ReadingProgress>>(_path)
				?? new List<ReadingProgress>();
		}

		public ReadingProgress Find(string bookId)
		{
			if (string.IsNullOrEmpty(bookId)) return null;

			foreach (var item in _items)
			{
				if (string.Equals(item.BookId, bookId, StringComparison.OrdinalIgnoreCase))
					return item;
			}

			return null;
		}

		public void Save(ReadingProgress progress)
		{
			if (progress == null || string.IsNullOrWhiteSpace(progress.BookId)) return;

			var existing = Find(progress.BookId);
			if (existing == null)
			{
				_items.Add(progress);
			}
			else
			{
				existing.ChapterIndex = progress.ChapterIndex;
				existing.ScrollOffset = progress.ScrollOffset;
				existing.MaxChapterIndex = progress.MaxChapterIndex;
				existing.UpdatedAt = progress.UpdatedAt;
			}

			JsonFileStore.Save(_path, _items);
		}
	}
}

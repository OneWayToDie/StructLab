using System;
using System.Collections.Generic;
using StructLab.Reading.Helpers;
using StructLab.Reading.Models;

namespace StructLab.Reading.Services
{
	/// <summary>
	/// Закладки всех книг: %LocalAppData%\StructLab\bookmarks.json.
	/// </summary>
	public class BookmarkStore
	{
		private readonly string _path;
		private readonly List<Bookmark> _items;

		public BookmarkStore()
		{
			_path = ReadingPaths.Bookmarks;
			_items = JsonFileStore.Load<List<Bookmark>>(_path)
				?? new List<Bookmark>();
		}

		public IReadOnlyList<Bookmark> All => _items;

		public List<Bookmark> ForBook(string bookId)
		{
			var result = new List<Bookmark>();
			if (string.IsNullOrEmpty(bookId)) return result;

			foreach (var item in _items)
			{
				if (string.Equals(item.BookId, bookId, StringComparison.OrdinalIgnoreCase))
					result.Add(item);
			}

			result.Sort((a, b) =>
			{
				var byChapter = a.ChapterIndex.CompareTo(b.ChapterIndex);
				return byChapter != 0 ? byChapter : a.ScrollOffset.CompareTo(b.ScrollOffset);
			});

			return result;
		}

		public void Add(Bookmark bookmark)
		{
			if (bookmark == null || string.IsNullOrWhiteSpace(bookmark.BookId)) return;
			_items.Add(bookmark);
			JsonFileStore.Save(_path, _items);
		}

		public bool Remove(string id)
		{
			if (string.IsNullOrEmpty(id)) return false;

			var removed = _items.RemoveAll(b =>
				string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase)) > 0;
			if (removed) JsonFileStore.Save(_path, _items);

			return removed;
		}
	}
}

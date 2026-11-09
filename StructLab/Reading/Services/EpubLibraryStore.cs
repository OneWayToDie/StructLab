using System;
using System.Collections.Generic;
using StructLab.Reading.Helpers;
using StructLab.Reading.Models;

namespace StructLab.Reading.Services
{
	/// <summary>
	/// История загруженных EPUB-книг: %LocalAppData%\StructLab\epub-library.json.
	/// </summary>
	public class EpubLibraryStore
	{
		private readonly string _path;
		private readonly List<EpubLibraryEntry> _entries;

		public EpubLibraryStore()
		{
			_path = ReadingPaths.EpubLibrary;
			_entries = JsonFileStore.Load<List<EpubLibraryEntry>>(_path)
				?? new List<EpubLibraryEntry>();
		}

		public IReadOnlyList<EpubLibraryEntry> Entries => _entries;

		public EpubLibraryEntry Find(string id)
		{
			if (string.IsNullOrEmpty(id)) return null;

			foreach (var entry in _entries)
			{
				if (string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase))
					return entry;
			}

			return null;
		}

		public void AddOrUpdate(Book book, string filePath)
		{
			if (book == null) return;

			var entry = Find(book.Id);
			if (entry == null)
			{
				entry = new EpubLibraryEntry { Id = book.Id };
				_entries.Add(entry);
			}

			entry.Title = book.Title;
			entry.Author = !string.IsNullOrWhiteSpace(book.Author)
				? book.Author
				: book.Metadata?.Author;
			entry.FilePath = filePath;
			entry.ChapterCount = book.ChapterCount;
			entry.WordCount = book.WordCount;
			entry.LastOpenedAt = DateTime.Now;

			Save();
		}

		public void Touch(string id)
		{
			var entry = Find(id);
			if (entry == null) return;

			entry.LastOpenedAt = DateTime.Now;
			Save();
		}

		private void Save()
			=> JsonFileStore.Save(_path, _entries);
	}
}

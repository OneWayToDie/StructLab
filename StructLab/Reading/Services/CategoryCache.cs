using System;
using System.Collections.Generic;
using System.Linq;
using StructLab.Reading.Helpers;

namespace StructLab.Reading.Services
{
	/// <summary>
	/// Дисковый кэш категорий страниц Викитеки.
	/// Позволяет не запрашивать категории повторно при каждом поиске.
	/// </summary>
	public sealed class CategoryCache
	{
		private sealed class Entry
		{
			public List<string> Categories { get; set; }
			public DateTime FetchedAtUtc { get; set; }
		}

		private readonly object _sync = new object();
		private readonly string _path;
		private readonly TimeSpan _ttl;
		private Dictionary<string, Entry> _entries;
		private bool _dirty;

		public CategoryCache()
			: this(ReadingPaths.WikiCategoriesCache, TimeSpan.FromDays(7))
		{
		}

		public CategoryCache(string path, TimeSpan ttl)
		{
			_path = path;
			_ttl = ttl;
		}

		public bool TryGet(string pageId, out List<string> categories)
		{
			categories = null;
			if (string.IsNullOrEmpty(pageId)) return false;

			lock (_sync)
			{
				EnsureLoaded();

				Entry entry;
				if (!_entries.TryGetValue(pageId, out entry) || entry == null)
					return false;

				if (DateTime.UtcNow - entry.FetchedAtUtc > _ttl)
					return false;

				categories = entry.Categories ?? new List<string>();
				return true;
			}
		}

		public void Set(string pageId, List<string> categories)
		{
			if (string.IsNullOrEmpty(pageId)) return;

			lock (_sync)
			{
				EnsureLoaded();
				_entries[pageId] = new Entry
				{
					Categories = categories ?? new List<string>(),
					FetchedAtUtc = DateTime.UtcNow
				};
				_dirty = true;
			}
		}

		public void SaveIfDirty()
		{
			lock (_sync)
			{
				if (!_dirty) return;
				JsonFileStore.Save(_path, _entries);
				_dirty = false;
			}
		}

		private void EnsureLoaded()
		{
			if (_entries != null) return;

			_entries = JsonFileStore.Load<Dictionary<string, Entry>>(_path)
					   ?? new Dictionary<string, Entry>();

			// Чистим просроченные записи, чтобы файл не рос бесконечно.
			var now = DateTime.UtcNow;
			var expired = _entries
				.Where(p => p.Value == null || now - p.Value.FetchedAtUtc > _ttl)
				.Select(p => p.Key)
				.ToList();

			foreach (var key in expired)
				_entries.Remove(key);

			if (expired.Count > 0) _dirty = true;
		}
	}
}

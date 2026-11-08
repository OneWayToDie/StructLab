using System;
using System.Collections.Generic;
using StructLab.Reading.Helpers;

namespace StructLab.Reading.Services
{
	/// <summary>
	/// Дисковый кэш списка подстраниц (глав) произведения Викитеки.
	/// Хранит только заголовки и pageid, без текстов.
	/// </summary>
	public sealed class SubpageCache
	{
		public sealed class CachedSubpage
		{
			public string Title { get; set; }
			public string PageId { get; set; }
		}

		private sealed class Entry
		{
			public List<CachedSubpage> Items { get; set; }
			public bool Truncated { get; set; }
			public DateTime FetchedAtUtc { get; set; }
		}

		private readonly object _sync = new object();
		private readonly string _path;
		private readonly TimeSpan _ttl;
		private Dictionary<string, Entry> _entries;
		private bool _dirty;

		public SubpageCache()
			: this(ReadingPaths.WikiSubpagesCache, TimeSpan.FromDays(7))
		{
		}

		public SubpageCache(string path, TimeSpan ttl)
		{
			_path = path;
			_ttl = ttl;
		}

		public bool TryGet(string parentTitle, out List<CachedSubpage> items, out bool truncated)
		{
			items = null;
			truncated = false;
			if (string.IsNullOrEmpty(parentTitle)) return false;

			lock (_sync)
			{
				EnsureLoaded();

				Entry entry;
				if (!_entries.TryGetValue(parentTitle, out entry) || entry == null)
					return false;

				if (DateTime.UtcNow - entry.FetchedAtUtc > _ttl)
					return false;

				items = entry.Items ?? new List<CachedSubpage>();
				truncated = entry.Truncated;
				return true;
			}
		}

		public void Set(string parentTitle, List<CachedSubpage> items, bool truncated)
		{
			if (string.IsNullOrEmpty(parentTitle)) return;

			lock (_sync)
			{
				EnsureLoaded();
				_entries[parentTitle] = new Entry
				{
					Items = items ?? new List<CachedSubpage>(),
					Truncated = truncated,
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

			var now = DateTime.UtcNow;
			var expired = new List<string>();
			foreach (var pair in _entries)
			{
				if (pair.Value == null || now - pair.Value.FetchedAtUtc > _ttl)
					expired.Add(pair.Key);
			}

			foreach (var key in expired)
				_entries.Remove(key);

			if (expired.Count > 0) _dirty = true;
		}
	}
}

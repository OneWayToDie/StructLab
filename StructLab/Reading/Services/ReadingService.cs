using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StructLab.Reading.Models;

namespace StructLab.Reading.Services
{
	/// <summary>
	/// Высокоуровневый сервис: поиск + загрузка с кэшированием.
	/// </summary>
	public class ReadingService
	{
		private readonly IBookSourceProvider _provider;
		private readonly IBookCache _cache;

		public ReadingService(IBookSourceProvider provider, IBookCache cache)
		{
			_provider = provider;
			_cache = cache;
		}

		public string ProviderDisplayName => _provider.DisplayName;

		public Task<IReadOnlyList<BookSearchResult>> SearchAsync(
			string query, CancellationToken ct)
			=> _provider.SearchAsync(query, 25, ct);

		public async Task<Book> LoadBookAsync(
			string pageId, bool forceRefresh, CancellationToken ct)
		{
			if (!forceRefresh)
			{
				var cached = await _cache.TryGetAsync(_provider.ProviderId, pageId, ct).ConfigureAwait(false);
				if (cached != null) return cached;
			}

			var book = await _provider.GetBookContentAsync(pageId, ct).ConfigureAwait(false);
			await _cache.SaveAsync(_provider.ProviderId, book, ct).ConfigureAwait(false);
			return book;
		}
	}
}

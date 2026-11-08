using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StructLab.Reading.Models;

namespace StructLab.Reading.Services
{
	/// <summary>
	/// Стратегия источника книг. Реализации: WikisourceProvider,
	/// (позже) GutenbergProvider, EpubProvider, PdfProvider.
	/// </summary>
	public interface IBookSourceProvider
	{
		string ProviderId { get; }
		string DisplayName { get; }

		Task<IReadOnlyList<BookSearchResult>> SearchAsync(
			string query, int limit, CancellationToken cancellationToken);

		Task<Book> GetBookContentAsync(
			string pageId, CancellationToken cancellationToken);
	}
}
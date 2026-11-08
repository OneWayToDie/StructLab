using System.Threading;
using System.Threading.Tasks;
using StructLab.Reading.Models;

namespace StructLab.Reading.Services
{
	public interface IBookCache
	{
		Task<Book> TryGetAsync(string providerId, string pageId, CancellationToken ct);
		Task SaveAsync(string providerId, Book book, CancellationToken ct);
	}
}

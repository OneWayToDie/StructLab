using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using StructLab.Reading.Helpers;
using StructLab.Reading.Models;

namespace StructLab.Reading.Services
{
	/// <summary>
	/// Кэш скачанных книг на диске: %LocalAppData%\StructLab\cache\reading\.
	/// Новый формат — JSON с главами; старые .txt-файлы читаются и
	/// автоматически переводятся в новый формат при первом обращении.
	/// </summary>
	public class FileBookCache : IBookCache
	{
		private readonly string _cacheDir;

		public FileBookCache()
		{
			_cacheDir = ReadingPaths.ReadingCache;
			Directory.CreateDirectory(_cacheDir);
		}

		private string GetJsonPath(string providerId, string pageId)
			=> Path.Combine(_cacheDir, providerId + "_" + pageId + ".json");

		private string GetLegacyPath(string providerId, string pageId)
			=> Path.Combine(_cacheDir, providerId + "_" + pageId + ".txt");

		public async Task<Book> TryGetAsync(string providerId, string pageId, CancellationToken ct)
		{
			ct.ThrowIfCancellationRequested();

			var jsonPath = GetJsonPath(providerId, pageId);
			if (File.Exists(jsonPath))
			{
				var cached = await Task.Run(() => JsonFileStore.Load<Book>(jsonPath), ct)
					.ConfigureAwait(false);
				if (cached != null) return cached;
			}

			var legacy = await TryReadLegacyAsync(providerId, pageId, ct).ConfigureAwait(false);
			if (legacy == null) return null;

			// Мигрируем старый .txt в новый JSON, чтобы второй раз читать быстрее.
			try
			{
				await SaveAsync(providerId, legacy, ct).ConfigureAwait(false);
				File.Delete(GetLegacyPath(providerId, pageId));
				ReadingLog.Info("Кэш: старый .txt переведён в JSON — " + pageId);
			}
			catch (Exception ex)
			{
				ReadingLog.Warn("Кэш: не удалось мигрировать .txt — " + pageId, ex);
			}

			return legacy;
		}

		public Task SaveAsync(string providerId, Book book, CancellationToken ct)
		{
			ct.ThrowIfCancellationRequested();
			if (book == null) return Task.CompletedTask;

			return Task.Run(() =>
			{
				var path = GetJsonPath(providerId, book.Id);
				JsonFileStore.Save(path, book);
			}, ct);
		}

		/// <summary>
		/// Читает старый текстовый кэш (Title \n LoadedAt \n PlainText)
		/// и превращает его в книгу из одной главы.
		/// </summary>
		private async Task<Book> TryReadLegacyAsync(string providerId, string pageId, CancellationToken ct)
		{
			var path = GetLegacyPath(providerId, pageId);
			if (!File.Exists(path)) return null;

			ct.ThrowIfCancellationRequested();

			string text;

			// StreamReader.ReadToEndAsync() есть в net48.
			// File.ReadAllTextAsync() — НЕТ (это .NET Core 2.0+).
			using (var reader = new StreamReader(path))
			{
				text = await reader.ReadToEndAsync().ConfigureAwait(false);
			}

			var firstNewline = text.IndexOf('\n');
			if (firstNewline < 0) return null;
			var secondNewline = text.IndexOf('\n', firstNewline + 1);
			if (secondNewline < 0) return null;

			try
			{
				var title = text.Substring(0, firstNewline);
				var loadedAt = DateTime.Parse(text.Substring(firstNewline + 1, secondNewline - firstNewline - 1));
				var plainText = text.Substring(secondNewline + 1);

				var book = new Book
				{
					Id = pageId,
					ProviderId = providerId,
					Title = title,
					LoadedAt = loadedAt
				};
				book.Chapters.Add(new Chapter(0, title, plainText));

				return book;
			}
			catch
			{
				return null;
			}
		}
	}
}

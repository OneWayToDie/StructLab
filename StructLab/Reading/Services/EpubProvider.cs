using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using StructLab.Reading.Helpers;
using StructLab.Reading.Models;
using VersOne.Epub;

namespace StructLab.Reading.Services
{
	/// <summary>
	/// Источник книг из локальных файлов EPUB.
	/// Поиск через API не поддерживается — книга выбирается файлом.
	/// </summary>
	public class EpubProvider : IBookSourceProvider
	{
		public const string ProviderKey = "epub";

		private readonly string _cacheDir;

		public EpubProvider()
		{
			_cacheDir = ReadingPaths.EpubCache;
			Directory.CreateDirectory(_cacheDir);
		}

		public string ProviderId => ProviderKey;
		public string DisplayName => "EPUB";

		public Task<IReadOnlyList<BookSearchResult>> SearchAsync(
			string query, int limit, CancellationToken cancellationToken)
		{
			IReadOnlyList<BookSearchResult> empty = new List<BookSearchResult>();
			return Task.FromResult(empty);
		}

		/// <summary>
		/// Для EPUB pageId — это хэш файла; книга берётся из локального кэша.
		/// </summary>
		public async Task<Book> GetBookContentAsync(string pageId, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(pageId))
				throw new ArgumentException("Не указан идентификатор EPUB.", nameof(pageId));

			var book = await Task.Run(
				() => JsonFileStore.Load<Book>(GetCachePath(pageId)), cancellationToken)
				.ConfigureAwait(false);

			if (book == null)
				throw new FileNotFoundException("EPUB не найден в кэше. Загрузите файл заново.");

			return book;
		}

		public Task<Book> LoadFromFileAsync(string path)
			=> LoadFromFileAsync(path, false, CancellationToken.None);

		public Task<Book> LoadFromFileAsync(string path, CancellationToken cancellationToken)
			=> LoadFromFileAsync(path, false, cancellationToken);

		/// <summary>
		/// Читает EPUB и кэширует результат в %LocalAppData%\StructLab\cache\epub\{hash}.json.
		/// Повторное открытие того же файла идёт из кэша.
		/// </summary>
		public async Task<Book> LoadFromFileAsync(
			string path, bool forceRefresh, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(path))
				throw new ArgumentException("Не указан путь к EPUB.", nameof(path));
			if (!File.Exists(path))
				throw new FileNotFoundException("Файл EPUB не найден.", path);

			var hash = await Task.Run(() => ComputeHash(path), cancellationToken).ConfigureAwait(false);
			var cachePath = GetCachePath(hash);

			if (!forceRefresh)
			{
				var cached = await Task.Run(
					() => JsonFileStore.Load<Book>(cachePath), cancellationToken)
					.ConfigureAwait(false);
				if (cached != null) return cached;
			}

			var book = await Task.Run(
				() => ParseBook(path, hash), cancellationToken)
				.ConfigureAwait(false);

			await Task.Run(
				() => JsonFileStore.Save(cachePath, book), cancellationToken)
				.ConfigureAwait(false);

			ReadingLog.Info("EPUB: «" + book.Title + "» — глав " + book.ChapterCount
				+ ", слов " + book.WordCount);
			return book;
		}

		private string GetCachePath(string hash)
			=> Path.Combine(_cacheDir, hash + ".json");

		private static string ComputeHash(string path)
		{
			using (var sha = SHA1.Create())
			using (var stream = File.OpenRead(path))
			{
				var bytes = sha.ComputeHash(stream);
				var builder = new StringBuilder(bytes.Length * 2);
				foreach (var b in bytes) builder.Append(b.ToString("x2"));
				return builder.ToString();
			}
		}

		private static Book ParseBook(string path, string hash)
		{
			EpubBook epub;

			try
			{
				epub = EpubReader.ReadBook(path);
			}
			catch (Exception ex)
			{
				throw new InvalidDataException("Не удалось прочитать EPUB: " + ex.Message, ex);
			}

			if (epub == null)
				throw new InvalidDataException("Не удалось прочитать EPUB.");

			var metadata = ReadMetadata(epub);
			var book = new Book
			{
				Id = hash,
				ProviderId = ProviderKey,
				Title = FirstNonEmpty(epub.Title, Path.GetFileNameWithoutExtension(path)),
				Author = FirstNonEmpty(epub.Author, metadata.Author),
				LoadedAt = DateTime.UtcNow,
				Metadata = metadata
			};

			var navigationTitles = BuildNavigationTitles(epub.Navigation);
			var chapters = new List<Chapter>();
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			AddChapters(chapters, seen, epub.ReadingOrder, navigationTitles, book.Title);

			if (chapters.Count == 0 &&
				epub.Content != null &&
				epub.Content.Html != null &&
				epub.Content.Html.Local != null)
			{
				AddChapters(chapters, seen, epub.Content.Html.Local, navigationTitles, book.Title);
			}

			book.Chapters = chapters;

			if (string.IsNullOrEmpty(book.Title) && chapters.Count > 0)
				book.Title = chapters[0].Title;
			if (string.IsNullOrEmpty(book.Author) && !string.IsNullOrEmpty(metadata.Author))
				book.Author = metadata.Author;

			return book;
		}

		private static void AddChapters(
			List<Chapter> chapters,
			HashSet<string> seen,
			IEnumerable<EpubLocalTextContentFile> files,
			Dictionary<string, string> navigationTitles,
			string bookTitle)
		{
			if (files == null) return;

			foreach (var file in files)
			{
				if (file == null || string.IsNullOrEmpty(file.Content)) continue;

				var filePath = NormalizePath(file.FilePath);
				if (filePath.Length == 0 || !seen.Add(filePath)) continue;

				var text = HtmlTextConverter.ToPlainText(file.Content);
				if (string.IsNullOrWhiteSpace(text)) continue;

				string title;
				if (!navigationTitles.TryGetValue(filePath, out title) ||
					string.IsNullOrWhiteSpace(title))
				{
					title = chapters.Count == 0
						? bookTitle
						: Path.GetFileNameWithoutExtension(file.FilePath);
				}

				title = CleanText(title) ?? ("Глава " + (chapters.Count + 1));
				chapters.Add(new Chapter(chapters.Count, title, text));
			}
		}

		private static Dictionary<string, string> BuildNavigationTitles(
			List<EpubNavigationItem> items)
		{
			var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			AddNavigationTitles(items, map);
			return map;
		}

		private static void AddNavigationTitles(
			IEnumerable<EpubNavigationItem> items,
			Dictionary<string, string> map)
		{
			if (items == null) return;

			foreach (var item in items)
			{
				if (item == null) continue;

				var filePath = item.HtmlContentFile != null
					? item.HtmlContentFile.FilePath
					: (item.Link != null ? item.Link.ContentFilePath : null);

				var key = NormalizePath(filePath);
				if (key.Length > 0 &&
					!string.IsNullOrWhiteSpace(item.Title) &&
					!map.ContainsKey(key))
				{
					map[key] = CleanText(item.Title);
				}

				AddNavigationTitles(item.NestedItems, map);
			}
		}

		private static BookMetadata ReadMetadata(EpubBook epub)
		{
			var metadata = new BookMetadata();
			if (epub == null) return metadata;

			metadata.Author = CleanText(epub.Author);
			if (string.IsNullOrEmpty(metadata.Author) && epub.AuthorList != null)
			{
				metadata.Author = CleanText(string.Join(", ",
					epub.AuthorList.Where(a => !string.IsNullOrWhiteSpace(a))));
			}

			metadata.Description = CleanText(epub.Description);

			var schemaMetadata = epub.Schema != null && epub.Schema.Package != null
				? epub.Schema.Package.Metadata
				: null;

			if (schemaMetadata != null)
			{
				if (schemaMetadata.Dates != null && schemaMetadata.Dates.Count > 0)
					metadata.Year = ExtractYear(schemaMetadata.Dates[0].Date);

				if (schemaMetadata.Rights != null && schemaMetadata.Rights.Count > 0)
					metadata.License = CleanText(schemaMetadata.Rights[0].Rights);

				if (schemaMetadata.Languages != null && schemaMetadata.Languages.Count > 0)
					metadata.Language = CleanText(schemaMetadata.Languages[0].Language);

				if (string.IsNullOrEmpty(metadata.Description) &&
					schemaMetadata.Descriptions != null &&
					schemaMetadata.Descriptions.Count > 0)
				{
					metadata.Description = CleanText(schemaMetadata.Descriptions[0].Description);
				}

				if (string.IsNullOrEmpty(metadata.Author) &&
					schemaMetadata.Creators != null &&
					schemaMetadata.Creators.Count > 0)
				{
					metadata.Author = CleanText(string.Join(", ",
						schemaMetadata.Creators
							.Select(c => c.Creator)
							.Where(c => !string.IsNullOrWhiteSpace(c))));
				}
			}

			metadata.SourceUrl = epub.FilePath;
			return metadata;
		}

		private static string ExtractYear(string date)
		{
			if (string.IsNullOrWhiteSpace(date)) return null;

			var match = Regex.Match(date, @"\d{4}");
			return match.Success ? match.Value : CleanText(date);
		}

		private static string NormalizePath(string path)
		{
			if (string.IsNullOrWhiteSpace(path)) return "";

			var normalized = path.Replace('\\', '/').Trim();
			var anchorIndex = normalized.IndexOf('#');
			if (anchorIndex >= 0) normalized = normalized.Substring(0, anchorIndex);

			return normalized.TrimStart('/').ToLowerInvariant();
		}

		private static string FirstNonEmpty(params string[] values)
		{
			foreach (var value in values)
			{
				var cleaned = CleanText(value);
				if (cleaned != null) return cleaned;
			}

			return null;
		}

		private static string CleanText(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;

			var cleaned = Regex.Replace(value, @"\s+", " ").Trim();
			return cleaned.Length == 0 ? null : cleaned;
		}
	}
}

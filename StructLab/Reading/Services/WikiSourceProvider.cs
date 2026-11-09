using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using StructLab.Reading.Models;
using StructLab.Reading.Helpers;

namespace StructLab.Reading.Services
{
	public class WikisourceProvider : IBookSourceProvider
	{
		private const string ApiUrl = "https://ru.wikisource.org/w/api.php";
		private const int BatchSize = 15;
		private const int DelayBetweenBatchesMs = 600;
		private const int MaxSubpages = 30;

		private static readonly HttpClient Http = CreateHttpClient();
		private static readonly Random Rnd = new Random();
		private static readonly CategoryCache Categories = new CategoryCache();
		private static readonly SubpageCache Subpages = new SubpageCache();

		public string ProviderId => "wikisource";
		public string DisplayName => "Викитека";

		private static HttpClient CreateHttpClient()
		{
			var handler = new HttpClientHandler
			{
				AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
			};
			var client = new HttpClient(handler);
			client.DefaultRequestHeaders.UserAgent.ParseAdd(
				"StructLab/1.0 (https://github.com/your-username/structlab; contact@example.com)");
			client.Timeout = TimeSpan.FromSeconds(60);
			return client;
		}

		// ============================================================
		// ПОИСК
		// ============================================================
		public async Task<IReadOnlyList<BookSearchResult>> SearchAsync(
			string query, int limit, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(query))
				return Array.Empty<BookSearchResult>();

			// Разбиваем на слова, отбрасываем короткие («А», «П») и пунктуацию
			var words = query
				.Split(new[] { ' ', '.', ',', '(', ')', '«', '»', '"', '\'', '-', '—' },
					StringSplitOptions.RemoveEmptyEntries)
				.Where(w => w.Length > 1)
				.ToList();

			if (words.Count == 0)
				return Array.Empty<BookSearchResult>();

			ReadingLog.Info("Поиск: «" + query + "»");

			// intitle: перед КАЖДЫМ словом — ищем только по заголовку
			var searchText = string.Join(" ", words.Select(w => "intitle:" + w));

			var url = $"{ApiUrl}?action=query&list=search" +
					  $"&srsearch={Uri.EscapeDataString(searchText)}" +
					  $"&srlimit={Math.Max(1, Math.Min(50, limit))}" +
					  $"&srnamespace=0" +
					  $"&format=json&formatversion=2";

			var raw = new List<BookSearchResult>();

			using (var response = await SendWithRetryAsync(url, cancellationToken).ConfigureAwait(false))
			{
				var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

				using (var doc = JsonDocument.Parse(json))
				{
					if (doc.RootElement.TryGetProperty("query", out var queryEl) &&
						queryEl.TryGetProperty("search", out var searchEl))
					{
						foreach (var item in searchEl.EnumerateArray())
						{
							var pageId = item.GetProperty("pageid").GetInt64().ToString();
							var title = item.GetProperty("title").GetString() ?? "";
							var snippetRaw = item.TryGetProperty("snippet", out var sn) ? sn.GetString() : "";

							if (IsServicePage(title)) continue;

							var snippet = Regex.Replace(snippetRaw ?? "", "<[^>]+>", "").Trim();

							raw.Add(new BookSearchResult
							{
								ProviderId = ProviderId,
								PageId = pageId,
								Title = title,
								Snippet = snippet,
								Url = $"https://ru.wikisource.org/?curid={pageId}"
							});
						}
					}
				}
			}

			var filtered = await FilterByCategoriesAsync(raw, cancellationToken).ConfigureAwait(false);
			ReadingLog.Info($"Поиск: найдено {raw.Count}, после фильтра {filtered.Count}");
			return filtered;
		}

		// ============================================================
		// КАТЕГОРИЙНЫЙ ФИЛЬТР
		// ============================================================
		private async Task<List<BookSearchResult>> FilterByCategoriesAsync(
			List<BookSearchResult> results, CancellationToken ct)
		{
			if (results.Count == 0) return results;

			var positiveKeywords = new[]
			{
				"Романы", "Роман", "Пьесы", "Пьеса", "Поэмы", "Поэма",
				"Повести", "Повесть", "Рассказы", "Рассказ",
				"Стихотворения", "Стихотворение", "Басни", "Басня",
				"Сказки", "Сказка", "Литературные произведения"
			};

			var negativeKeywords = new[]
			{
				"Критика", "Литературоведение", "Воспоминания",
				"Биографии", "Биография", "Статьи", "Статья",
				"Рецензии", "Рецензия", "Мемуары", "Публицистика",
				"Переписка", "Дневники", "Письма"
			};

			var keep = new List<BookSearchResult>();

			// Небольшие батчи: cllimit=max + 10 страниц не даст обрезать категории
			const int batchSize = 10;

			try
			{
				for (int i = 0; i < results.Count; i += batchSize)
				{
					ct.ThrowIfCancellationRequested();

					var batch = results.GetRange(i, Math.Min(batchSize, results.Count - i));
					var categoriesByPageId = new Dictionary<string, List<string>>();
					var missingPageIds = new List<string>();

					foreach (var r in batch)
					{
						List<string> cached;
						if (Categories.TryGet(r.PageId, out cached))
							categoriesByPageId[r.PageId] = cached;
						else
							missingPageIds.Add(r.PageId);
					}

					if (missingPageIds.Count > 0)
					{
						var pageIds = string.Join("|", missingPageIds);

						var url = $"{ApiUrl}?action=query&prop=categories&cllimit=max" +
								  $"&pageids={Uri.EscapeDataString(pageIds)}" +
								  $"&format=json&formatversion=2";

						try
						{
							using (var response = await SendWithRetryAsync(url, ct).ConfigureAwait(false))
							{
								var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

								using (var doc = JsonDocument.Parse(json))
								{
									if (doc.RootElement.TryGetProperty("query", out var queryEl) &&
										queryEl.TryGetProperty("pages", out var pagesEl))
									{
										foreach (var page in pagesEl.EnumerateArray())
										{
											var pid = page.GetProperty("pageid").GetInt64().ToString();
											var cats = new List<string>();

											if (page.TryGetProperty("categories", out var catsEl))
											{
												foreach (var c in catsEl.EnumerateArray())
												{
													var catTitle = c.GetProperty("title").GetString() ?? "";
													if (catTitle.StartsWith("Категория:"))
														catTitle = catTitle.Substring("Категория:".Length);
													cats.Add(catTitle);
												}
											}

											categoriesByPageId[pid] = cats;
											Categories.Set(pid, cats);
										}
									}
								}
							}

							ReadingLog.Info($"Категории: запрошено {missingPageIds.Count}, из кэша {batch.Count - missingPageIds.Count}");
						}
						catch (OperationCanceledException)
						{
							throw;
						}
						catch (Exception ex)
						{
							ReadingLog.Warn("Не удалось получить категории, страницы оставлены без фильтра", ex);
							keep.AddRange(batch);
							continue;
						}
					}
					else
					{
						ReadingLog.Info($"Категории: все {batch.Count} страниц из кэша");
					}

					foreach (var r in batch)
					{
						List<string> cats;
						if (!categoriesByPageId.TryGetValue(r.PageId, out cats) || cats.Count == 0)
						{
							// Нет категорий → оставляем (лучше показать, чем спрятать)
							keep.Add(r);
							continue;
						}

						bool hasPositive = false;
						bool hasNegative = false;

						foreach (var cat in cats)
						{
							foreach (var pk in positiveKeywords)
								if (cat.IndexOf(pk, StringComparison.OrdinalIgnoreCase) >= 0)
								{ hasPositive = true; break; }

							foreach (var nk in negativeKeywords)
								if (cat.IndexOf(nk, StringComparison.OrdinalIgnoreCase) >= 0)
								{ hasNegative = true; break; }
						}

						// Оставляем: если есть литературная категория (даже при негативной),
						// либо если нет ни положительной, ни отрицательной (нейтральная страница).
						// Отсеиваем: только если есть ТОЛЬКО отрицательные категории.
						if (hasPositive || !hasNegative)
							keep.Add(r);
					}

					// Пауза нужна только между сетевыми запросами, а не между кэш-хитами.
					if (missingPageIds.Count > 0 && i + batchSize < results.Count)
						await Task.Delay(400, ct).ConfigureAwait(false);
				}
			}
			finally
			{
				Categories.SaveIfDirty();
			}

			return keep;
		}

		private static bool IsServicePage(string title)
		{
			if (string.IsNullOrEmpty(title)) return true;

			var servicePrefixes = new[]
			{
				"Страница:", "Index:", "Author:", "Portal:", "Wikisource:",
				"Категория:", "Шаблон:", "Файл:", "Обсуждение:", "Участник:"
			};
			foreach (var prefix in servicePrefixes)
				if (title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
					return true;

			if (title.IndexOf(".pdf", StringComparison.OrdinalIgnoreCase) >= 0) return true;
			if (title.IndexOf(".djvu", StringComparison.OrdinalIgnoreCase) >= 0) return true;

			if (Regex.IsMatch(title, @"/\d+$")) return true;

			return false;
		}

		// ============================================================
		// ЗАГРУЗКА КНИГИ
		// ============================================================
		public async Task<Book> GetBookContentAsync(
			string pageId, CancellationToken cancellationToken)
		{
			var (title, content) = await FetchPageContentAsync(pageId, cancellationToken).ConfigureAwait(false);

			if (string.IsNullOrEmpty(content))
			{
				try
				{
					content = await GetContentViaParseAsync(pageId, cancellationToken).ConfigureAwait(false);
				}
				catch
				{
					content = null;
				}
			}

			if (!string.IsNullOrEmpty(content) && content.Length < 3000)
			{
				var subpageResult = await FetchSubpagesAsync(title, cancellationToken).ConfigureAwait(false);
				var subpages = subpageResult.Items;

				if (subpages.Count > 0)
				{
					ReadingLog.Info($"Загрузка «{title}»: глав {subpages.Count}" +
									(subpageResult.Truncated ? " (список обрезан)" : ""));

					var book = new Book
					{
						Id = pageId,
						ProviderId = ProviderId,
						Title = title,
						LoadedAt = DateTime.UtcNow,
						ChaptersTruncated = subpageResult.Truncated
					};

					for (int i = 0; i < subpages.Count; i += BatchSize)
					{
						cancellationToken.ThrowIfCancellationRequested();

						var batch = subpages.GetRange(i, Math.Min(BatchSize, subpages.Count - i));
						var batchTexts = await FetchPlainTextBatchAsync(batch, cancellationToken).ConfigureAwait(false);

						for (int j = 0; j < batch.Count; j++)
						{
							var sp = batch[j];
							var text = j < batchTexts.Count ? batchTexts[j] : "";

							if (string.IsNullOrWhiteSpace(text))
							{
								try
								{
									text = await GetContentViaParseAsync(sp.PageId, cancellationToken).ConfigureAwait(false);
								}
								catch { text = null; }
							}

							if (string.IsNullOrWhiteSpace(text)) continue;

							book.Chapters.Add(new Chapter(
								book.Chapters.Count,
								ShortTitle(sp.Title, title),
								text));
						}

						if (i + BatchSize < subpages.Count)
							await Task.Delay(DelayBetweenBatchesMs, cancellationToken).ConfigureAwait(false);
					}

					// Если главы не отдали текст — оставляем хотя бы страницу-оглавление.
					if (book.Chapters.Count == 0)
						book.Chapters.Add(new Chapter(0, title, content));

					book.Metadata = await TryFetchMetadataAsync(pageId, title, cancellationToken).ConfigureAwait(false);
					book.Author = book.Metadata.Author;

					return book;
				}
			}

			ReadingLog.Info($"Загрузка «{title}»: одна страница, {content?.Length ?? 0} символов");

			var single = new Book
			{
				Id = pageId,
				ProviderId = ProviderId,
				Title = title,
				LoadedAt = DateTime.UtcNow
			};
			single.Chapters.Add(new Chapter(
				0,
				title,
				string.IsNullOrEmpty(content) ? "(нет текста)" : content));

			single.Metadata = await TryFetchMetadataAsync(pageId, title, cancellationToken).ConfigureAwait(false);
			single.Author = single.Metadata.Author;

			return single;
		}

		/// <summary>
		/// «Народные русские сказки (Афанасьев)/Том 1» → «Том 1».
		/// </summary>
		private static string ShortTitle(string subpageTitle, string parentTitle)
		{
			if (string.IsNullOrEmpty(subpageTitle)) return "";

			if (!string.IsNullOrEmpty(parentTitle) &&
				subpageTitle.StartsWith(parentTitle + "/", StringComparison.Ordinal))
				return subpageTitle.Substring(parentTitle.Length + 1);

			return subpageTitle;
		}

		/// <summary>
		/// Пытается достать автора/год/лицензию из шапки страницы Викитеки.
		/// Любая ошибка не должна ломать загрузку книги — метаданные необязательны.
		/// </summary>
		private async Task<BookMetadata> TryFetchMetadataAsync(
			string pageId, string title, CancellationToken ct)
		{
			var metadata = new BookMetadata
			{
				SourceUrl = $"https://ru.wikisource.org/?curid={pageId}",
				Language = "ru"
			};

			try
			{
				// section=0 — только шапка/преамбула, без всего текста.
				var url = $"{ApiUrl}?action=parse&pageid={Uri.EscapeDataString(pageId)}" +
						  $"&prop=text&section=0&format=json&formatversion=2";

				using (var response = await SendWithRetryAsync(url, ct).ConfigureAwait(false))
				{
					var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

					using (var doc = JsonDocument.Parse(json))
					{
						if (!doc.RootElement.TryGetProperty("parse", out var parseEl) ||
							!parseEl.TryGetProperty("text", out var textEl))
							return metadata;

						var html = textEl.GetString() ?? "";
						if (html.Length > 20000) html = html.Substring(0, 20000);

						// Шапка Викитеки обычно в блоке ws-header; если её нет — берём начало.
						var headerMatch = Regex.Match(
							html,
							@"<div[^>]*class=""[^""]*ws-header[^""]*""[^>]*>(.*?)</div>\s*(?:<div|<p|<table|$)",
							RegexOptions.Singleline | RegexOptions.IgnoreCase);
						var headerHtml = headerMatch.Success ? headerMatch.Groups[1].Value : html;

						var plain = StripTags(headerHtml);
						if (plain.Length > 4000) plain = plain.Substring(0, 4000);

						var authorMatch = Regex.Match(
							plain,
							@"(?:Автор|Author)\s*[:\-—]?\s*([^\n]{2,100})",
							RegexOptions.IgnoreCase);
						if (authorMatch.Success)
							metadata.Author = CleanMetadataValue(authorMatch.Groups[1].Value);

						var yearMatch = Regex.Match(plain, @"\b(1[5-9]\d{2}|20\d{2})\b");
						if (yearMatch.Success)
							metadata.Year = yearMatch.Groups[1].Value;

						var licenses = new[]
						{
							"Общественное достояние", "Public domain",
							"CC BY-SA", "CC BY 4.0", "CC0"
						};
						foreach (var license in licenses)
						{
							if (plain.IndexOf(license, StringComparison.OrdinalIgnoreCase) >= 0)
							{
								metadata.License = license;
								break;
							}
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				ReadingLog.Warn($"Не удалось получить метаданные «{title}»", ex);
			}

			return metadata;
		}

		private static string CleanMetadataValue(string value)
		{
			if (string.IsNullOrEmpty(value)) return null;

			var line = value.Split('\n')[0].Trim();
			line = line.TrimEnd('.', ',', ';', ':', '—', '-').Trim();

			if (line.Length == 0) return null;
			if (line.Length > 100) line = line.Substring(0, 100).Trim();

			return line;
		}

		/// <summary>Грубая чистка HTML до текста (для шапки, без удаления ws-header).</summary>
		private static string StripTags(string html)
		{
			if (string.IsNullOrEmpty(html)) return "";

			var text = Regex.Replace(html, "<(script|style)[^>]*>.*?</\\1>",
				"", RegexOptions.Singleline | RegexOptions.IgnoreCase);
			text = Regex.Replace(text, "<!--.*?-->", "", RegexOptions.Singleline);
			text = Regex.Replace(text, "<[^>]+>", " ");
			text = System.Net.WebUtility.HtmlDecode(text);
			text = Regex.Replace(text, @"[ \t]+", " ");
			text = Regex.Replace(text, @" *\n *", "\n");
			text = Regex.Replace(text, @"\n{3,}", "\n\n");

			return text.Trim();
		}

		private async Task<(string Title, string Content)> FetchPageContentAsync(
			string pageId, CancellationToken ct)
		{
			var url = $"{ApiUrl}?action=query&prop=extracts&explaintext=1" +
					  $"&pageids={Uri.EscapeDataString(pageId)}" +
					  $"&format=json&formatversion=2";

			using (var response = await SendWithRetryAsync(url, ct).ConfigureAwait(false))
			{
				var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

				using (var doc = JsonDocument.Parse(json))
				{
					if (doc.RootElement.TryGetProperty("query", out var queryEl) &&
						queryEl.TryGetProperty("pages", out var pagesEl) &&
						pagesEl.GetArrayLength() > 0)
					{
						var page = pagesEl[0];
						var title = page.GetProperty("title").GetString() ?? "";
						var extract = page.TryGetProperty("extract", out var ex) ? ex.GetString() : null;
						return (title, extract);
					}
				}
			}
			return ("", null);
		}

		private async Task<List<string>> FetchPlainTextBatchAsync(
			List<SubpageInfo> batch, CancellationToken ct)
		{
			var pageIds = string.Join("|", batch.ConvertAll(s => s.PageId));

			var url = $"{ApiUrl}?action=query&prop=extracts&explaintext=1&exlimit={batch.Count}" +
					  $"&pageids={Uri.EscapeDataString(pageIds)}" +
					  $"&format=json&formatversion=2";

			var result = new List<string>();
			var byId = new Dictionary<string, string>();

			using (var response = await SendWithRetryAsync(url, ct).ConfigureAwait(false))
			{
				var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

				using (var doc = JsonDocument.Parse(json))
				{
					if (doc.RootElement.TryGetProperty("query", out var queryEl) &&
						queryEl.TryGetProperty("pages", out var pagesEl))
					{
						foreach (var page in pagesEl.EnumerateArray())
						{
							var pid = page.GetProperty("pageid").GetInt64().ToString();
							var extract = page.TryGetProperty("extract", out var ex) ? ex.GetString() : null;
							byId[pid] = extract ?? "";
						}
					}
				}
			}

			foreach (var sp in batch)
				result.Add(byId.TryGetValue(sp.PageId, out var t) ? t : "");

			return result;
		}

		private async Task<SubpageFetchResult> FetchSubpagesAsync(string parentTitle, CancellationToken ct)
		{
			List<SubpageCache.CachedSubpage> cachedItems;
			bool cachedTruncated;
			if (Subpages.TryGet(parentTitle, out cachedItems, out cachedTruncated))
			{
				ReadingLog.Info($"Подстраницы из кэша: «{parentTitle}» ({cachedItems.Count})");

				var cachedList = new List<SubpageInfo>(cachedItems.Count);
				foreach (var item in cachedItems)
					cachedList.Add(new SubpageInfo { Title = item.Title, PageId = item.PageId });

				return new SubpageFetchResult { Items = cachedList, Truncated = cachedTruncated };
			}

			var url = $"{ApiUrl}?action=query&list=allpages" +
					  $"&apprefix={Uri.EscapeDataString(parentTitle + "/")}" +
					  $"&apnamespace=0&aplimit={MaxSubpages + 1}" +
					  $"&format=json&formatversion=2";

			var list = new List<SubpageInfo>();

			using (var response = await SendWithRetryAsync(url, ct).ConfigureAwait(false))
			{
				var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

				using (var doc = JsonDocument.Parse(json))
				{
					if (doc.RootElement.TryGetProperty("query", out var queryEl) &&
						queryEl.TryGetProperty("allpages", out var pagesEl))
					{
						foreach (var p in pagesEl.EnumerateArray())
						{
							var t = p.GetProperty("title").GetString() ?? "";
							var id = p.GetProperty("pageid").GetInt64().ToString();

							var afterPrefix = t.Substring(parentTitle.Length + 1);
							if (afterPrefix.IndexOf('/') >= 0) continue;
							if (IsServicePage(t)) continue;

							list.Add(new SubpageInfo { Title = t, PageId = id });
						}
					}
				}
			}

			bool truncated = list.Count > MaxSubpages;
			if (truncated)
				list = list.GetRange(0, MaxSubpages);

			var toCache = new List<SubpageCache.CachedSubpage>(list.Count);
			foreach (var item in list)
				toCache.Add(new SubpageCache.CachedSubpage { Title = item.Title, PageId = item.PageId });

			Subpages.Set(parentTitle, toCache, truncated);
			Subpages.SaveIfDirty();

			ReadingLog.Info($"Подстраницы: «{parentTitle}» — {list.Count}" + (truncated ? " (есть ещё)" : ""));

			return new SubpageFetchResult { Items = list, Truncated = truncated };
		}

		// ============================================================
		// FALLBACK: action=parse → HTML → plain text
		// ============================================================
		private async Task<string> GetContentViaParseAsync(string pageId, CancellationToken ct)
		{
			var url = $"{ApiUrl}?action=parse&pageid={Uri.EscapeDataString(pageId)}" +
					  $"&prop=text&format=json&formatversion=2";

			using (var response = await SendWithRetryAsync(url, ct).ConfigureAwait(false))
			{
				var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

				using (var doc = JsonDocument.Parse(json))
				{
					if (!doc.RootElement.TryGetProperty("parse", out var parseEl) ||
						!parseEl.TryGetProperty("text", out var textEl))
						return "";

					var html = textEl.GetString() ?? "";
					return HtmlToPlainText(html);
				}
			}
		}

		// ============================================================
		// ЧИСТКА HTML → plain text
		// ============================================================
		private static string HtmlToPlainText(string html)
			=> HtmlTextConverter.ToPlainText(html, true);

		// ============================================================
		// RETRY при 429
		// ============================================================
		private static async Task<HttpResponseMessage> SendWithRetryAsync(
			string url, CancellationToken ct)
		{
			const int maxAttempts = 4;
			for (int attempt = 1; attempt <= maxAttempts; attempt++)
			{
				ct.ThrowIfCancellationRequested();

				// Все запросы к API проходят через общий ограничитель (2 запроса/сек).
				await RateLimiter.Instance.WaitAsync(ct).ConfigureAwait(false);

				var startedAt = DateTime.UtcNow;
				var response = await Http.GetAsync(url, ct).ConfigureAwait(false);

				if (response.StatusCode != (System.Net.HttpStatusCode)429)
				{
					ReadingLog.Info($"HTTP {(int)response.StatusCode} за {(int)(DateTime.UtcNow - startedAt).TotalMilliseconds} мс");
					response.EnsureSuccessStatusCode();
					return response;
				}

				var retryAfter = GetRetryAfter(response);
				response.Dispose();

				if (attempt == maxAttempts)
				{
					ReadingLog.Warn("429: попытки исчерпаны, запрос прерван");
					throw new HttpRequestException("Слишком много запросов к Викитеке (429). Подождите минуту.");
				}

				int backoffMs = (int)(Math.Pow(2, attempt) * 1000) + Rnd.Next(0, 500);
				int retryAfterMs = retryAfter.HasValue
					? (int)Math.Min(retryAfter.Value.TotalMilliseconds, 60000)
					: 0;
				int delayMs = Math.Max(backoffMs, retryAfterMs);

				ReadingLog.Warn($"429: попытка {attempt}/{maxAttempts}, пауза {delayMs} мс" +
								(retryAfter.HasValue ? $" (Retry-After {retryAfter.Value.TotalSeconds:0} с)" : ""));

				await Task.Delay(delayMs, ct).ConfigureAwait(false);
			}

			throw new HttpRequestException("Не удалось выполнить запрос.");
		}

		/// <summary>
		/// Разбирает заголовок Retry-After: число секунд или HTTP-дата.
		/// </summary>
		private static TimeSpan? GetRetryAfter(HttpResponseMessage response)
		{
			var header = response.Headers.RetryAfter;
			if (header != null)
			{
				if (header.Delta.HasValue)
					return header.Delta.Value;

				if (header.Date.HasValue)
				{
					var delay = header.Date.Value - DateTimeOffset.UtcNow;
					if (delay > TimeSpan.Zero)
						return delay;
				}
			}

			return null;
		}

		private class SubpageInfo
		{
			public string Title { get; set; }
			public string PageId { get; set; }
		}

		private class SubpageFetchResult
		{
			public List<SubpageInfo> Items { get; set; }
			public bool Truncated { get; set; }
		}
	}
}
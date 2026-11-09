using System.Text.RegularExpressions;

namespace StructLab.Reading.Helpers
{
	/// <summary>
	/// Превращает HTML/XHTML в простой текст для читалки.
	/// wikiMode = true включает дополнительную чистку служебных блоков Викитеки
	/// (сноски, кнопки «править», таблицы, навигационные вставки).
	/// </summary>
	public static class HtmlTextConverter
	{
		public static string ToPlainText(string html)
			=> ToPlainText(html, false);

		public static string ToPlainText(string html, bool wikiMode)
		{
			if (string.IsNullOrEmpty(html)) return "";

			if (wikiMode)
			{
				// Отсечь всё до mw-parser-output
				var parserMatch = Regex.Match(html,
					@"<div class=""mw-parser-output"">(.*?)(?:<div class=""printfooter""|<div id=""catlinks""|<div class=""catlinks""|$)",
					RegexOptions.Singleline | RegexOptions.IgnoreCase);
				if (parserMatch.Success && parserMatch.Groups[1].Value.Length > 100)
					html = parserMatch.Groups[1].Value;
			}

			// Скрипты, стили, комментарии, head
			html = Regex.Replace(html, "<(script|style)[^>]*>.*?</\\1>",
				"", RegexOptions.Singleline | RegexOptions.IgnoreCase);
			html = Regex.Replace(html, "<head[^>]*>.*?</head>",
				"", RegexOptions.Singleline | RegexOptions.IgnoreCase);
			html = Regex.Replace(html, "<title[^>]*>.*?</title>",
				"", RegexOptions.Singleline | RegexOptions.IgnoreCase);
			html = Regex.Replace(html, "<\\?xml[^>]*\\?>", "",
				RegexOptions.IgnoreCase);
			html = Regex.Replace(html, "<!DOCTYPE[^>]*>", "",
				RegexOptions.IgnoreCase);
			html = Regex.Replace(html, "<!--.*?-->", "", RegexOptions.Singleline);

			if (wikiMode)
			{
				// Сноски [1]
				html = Regex.Replace(html,
					"<sup[^>]*class=\"[^\"]*reference[^\"]*\"[^>]*>.*?</sup>",
					"", RegexOptions.Singleline | RegexOptions.IgnoreCase);

				// Кнопки [править]
				html = Regex.Replace(html,
					"<span[^>]*class=\"[^\"]*mw-editsection[^\"]*\"[^>]*>.*?</span>",
					"", RegexOptions.Singleline | RegexOptions.IgnoreCase);

				// Служебные блоки
				var serviceClasses = new[]
				{
					"noprint", "ws-noexport", "sistersitebox", "catlinks",
					"mw-references-wrap", "reflist", "printfooter", "mw-jump-link",
					"mw-indicators", "toc", "ws-header", "navbox", "vertical-navbox",
					"metadata", "mw-empty-elt", "hatnote", "ambox", "mbox",
					"redirectMsg", "mw-hidden-catlinks", "mw-normal-catlinks"
				};
				foreach (var cls in serviceClasses)
				{
					var escaped = Regex.Escape(cls);
					html = Regex.Replace(html,
						$@"<div[^>]*class=""[^""]*\b{escaped}\b[^""]*""[^>]*>.*?</div>",
						"", RegexOptions.Singleline | RegexOptions.IgnoreCase);
					html = Regex.Replace(html,
						$@"<div[^>]*id=""{escaped}""[^>]*>.*?</div>",
						"", RegexOptions.Singleline | RegexOptions.IgnoreCase);
				}

				// Таблицы
				html = Regex.Replace(html, "<table[^>]*>.*?</table>",
					"", RegexOptions.Singleline | RegexOptions.IgnoreCase);

				// Списки сносок
				html = Regex.Replace(html,
					"<ol[^>]*class=\"[^\"]*references[^\"]*\"[^>]*>.*?</ol>",
					"", RegexOptions.Singleline | RegexOptions.IgnoreCase);
			}

			// Блочные теги → перенос
			html = Regex.Replace(html, "</p>|</div>|</h[1-6]>|</li>|</tr>|<br\\s*/?>",
				"\n\n", RegexOptions.IgnoreCase);

			// Ячейки таблиц → пробел
			html = Regex.Replace(html, "</td>|</th>", " ", RegexOptions.IgnoreCase);

			// Оставшиеся теги
			html = Regex.Replace(html, "<[^>]+>", "");

			// Entities
			html = System.Net.WebUtility.HtmlDecode(html);

			if (wikiMode)
			{
				// Текстовые хвосты
				html = Regex.Replace(html, @"\[\s*(?:править|ред\.|edit|править\s+код)\s*\]", "", RegexOptions.IgnoreCase);
				html = Regex.Replace(html, @"\[\d+\]", "");
				html = Regex.Replace(html, @"[↗↑↓←→]", "");
				html = Regex.Replace(html, @"Страница:\s*[^\n]*?\.(?:pdf|djvu)/\d+", "", RegexOptions.IgnoreCase);
				html = Regex.Replace(html, @"\[источник не указан[^\]]*\]", "", RegexOptions.IgnoreCase);
			}

			// Пробелы
			html = Regex.Replace(html, @"[ \t]+", " ");
			html = Regex.Replace(html, @" *\n *", "\n");
			html = Regex.Replace(html, @"\n{3,}", "\n\n");
			html = html.Trim();

			return html;
		}
	}
}

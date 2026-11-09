using System;
using System.IO;

namespace StructLab.Reading.Helpers
{
	/// <summary>
	/// Единые пути для данных читалки: кэш, логи, пользовательские настройки.
	/// </summary>
	internal static class ReadingPaths
	{
		public static string Root => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"StructLab");

		public static string Cache => Path.Combine(Root, "cache");

		public static string ReadingCache => Path.Combine(Cache, "reading");

		public static string EpubCache => Path.Combine(Cache, "epub");

		public static string EpubLibrary => Path.Combine(Root, "epub-library.json");

		public static string Logs => Path.Combine(Root, "logs");

		public static string WikiCategoriesCache => Path.Combine(Cache, "wiki-categories.json");

		public static string WikiSubpagesCache => Path.Combine(Cache, "wiki-subpages.json");
	}
}

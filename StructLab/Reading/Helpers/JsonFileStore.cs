using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using StructLab.Reading.Services;

namespace StructLab.Reading.Helpers
{
	/// <summary>
	/// Небольшой помощник для чтения/записи JSON-файлов без риска
	/// оставить полузаписанный файл при падении приложения.
	/// </summary>
	internal static class JsonFileStore
	{
		private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
		{
			WriteIndented = false
		};

		private static readonly object Sync = new object();

		public static T Load<T>(string path) where T : class
		{
			try
			{
				if (!File.Exists(path)) return null;

				string json;
				using (var reader = new StreamReader(path, Encoding.UTF8))
				{
					json = reader.ReadToEnd();
				}

				return JsonSerializer.Deserialize<T>(json, Options);
			}
			catch (Exception ex)
			{
				ReadingLog.Warn("Не удалось прочитать JSON: " + path, ex);
				return null;
			}
		}

		public static void Save<T>(string path, T value)
		{
			if (value == null) return;

			lock (Sync)
			{
				try
				{
					var dir = Path.GetDirectoryName(path);
					if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

					var tmp = path + ".tmp";
					using (var writer = new StreamWriter(tmp, false, new UTF8Encoding(false)))
					{
						writer.Write(JsonSerializer.Serialize(value, Options));
					}

					if (File.Exists(path))
						File.Copy(tmp, path, true);
					else
						File.Move(tmp, path);

					try { File.Delete(tmp); } catch { }
				}
				catch (Exception ex)
				{
					ReadingLog.Warn("Не удалось сохранить JSON: " + path, ex);
				}
			}
		}
	}
}

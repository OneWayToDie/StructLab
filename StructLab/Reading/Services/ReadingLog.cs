using System;
using System.IO;
using System.Text;
using System.Threading;

namespace StructLab.Reading.Services
{
	/// <summary>
	/// Простой файловый лог читалки: %LocalAppData%\StructLab\logs\reading.log.
	/// Логирование не должно ронять приложение, поэтому все ошибки глотаются.
	/// </summary>
	public static class ReadingLog
	{
		private const long MaxBytes = 2 * 1024 * 1024;

		private static readonly object Sync = new object();
		private static readonly string LogDir = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"StructLab", "logs");
		private static readonly string LogPath = Path.Combine(LogDir, "reading.log");
		private static readonly Encoding Utf8 = new UTF8Encoding(false);

		public static void Info(string message)
		{
			Write("INFO", message, null);
		}

		public static void Warn(string message, Exception exception = null)
		{
			Write("WARN", message, exception);
		}

		public static void Error(string message, Exception exception = null)
		{
			Write("ERROR", message, exception);
		}

		private static void Write(string level, string message, Exception exception)
		{
			try
			{
				lock (Sync)
				{
					Directory.CreateDirectory(LogDir);
					RotateIfNeeded();

					var sb = new StringBuilder();
					sb.Append(DateTime.UtcNow.ToString("O"))
					  .Append(' ')
					  .Append(level)
					  .Append(' ')
					  .Append(message ?? "");

					if (exception != null)
					{
						sb.Append(" | ")
						  .Append(exception.GetType().Name)
						  .Append(": ")
						  .Append(exception.Message);
					}

					sb.AppendLine();
					File.AppendAllText(LogPath, sb.ToString(), Utf8);
				}
			}
			catch
			{
				// Лог — вспомогательный механизм, не должен влиять на работу читалки.
			}
		}

		private static void RotateIfNeeded()
		{
			try
			{
				var info = new FileInfo(LogPath);
				if (!info.Exists || info.Length < MaxBytes) return;

				var backup = LogPath + ".1";
				if (File.Exists(backup)) File.Delete(backup);
				File.Move(LogPath, backup);
			}
			catch
			{
				// Если ротация не удалась — просто продолжим писать в текущий файл.
			}
		}
	}
}

using System;
using System.IO;
using System.Text.Json;

namespace StructLab.Reading.Models
{
	public class ReadingSettings
	{
		public double FontSize { get; set; } = 17;
		public string FontFamily { get; set; } = "Georgia";
		public string Theme { get; set; } = "Light"; // Light | Sepia | Dark

		private static readonly string _path = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"StructLab", "reading-settings.json");

		public static ReadingSettings Load()
		{
			try
			{
				if (!File.Exists(_path)) return new ReadingSettings();
				var json = File.ReadAllText(_path);
				return JsonSerializer.Deserialize<ReadingSettings>(json) ?? new ReadingSettings();
			}
			catch { return new ReadingSettings(); }
		}

		public void Save()
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(_path));
				File.WriteAllText(_path, JsonSerializer.Serialize(this));
			}
			catch { /* настройки — не критично, если не сохранились */ }
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using StructLab.Models.Lab02;

namespace StructLab.Services
{
	public class NotificationService
	{
		private static readonly HashSet<string> _alreadyNotified = new HashSet<string>();

		/// <summary>
		/// Проверяет задачи. Для каждой сработавшей — показывает ToastWindow.
		/// Логика:
		/// 1) за N минут до события (если включено);
		/// 2) в момент события (если включено);
		/// 3) просрочка (один раз).
		/// </summary>
		public void CheckAndNotify(IEnumerable<TaskItem> tasks, bool playSound,
									bool enableBefore, bool enableAtTime, int beforeMinutes)
		{
			var list = tasks?.ToList();
			if (list == null || list.Count == 0) return;

			var now = DateTime.Now;
			var messages = new List<string>();

			foreach (var t in list)
			{
				if (t.Status == TaskStatus.Done) continue;

				DateTime due;
				if (!DateTime.TryParse(t.DueDate, out due)) continue;

				DateTime dueDateTime = due.Date;
				if (!string.IsNullOrWhiteSpace(t.DueTime))
				{
					TimeSpan ts;
					if (TimeSpan.TryParse(t.DueTime, out ts))
						dueDateTime = due.Date + ts;
				}
				else
				{
					dueDateTime = due.Date.AddDays(1).AddSeconds(-1);
				}

				// 1) За N минут до
				if (enableBefore && beforeMinutes > 0)
				{
					var windowStart = dueDateTime.AddMinutes(-beforeMinutes);
					if (now >= windowStart && now < dueDateTime)
					{
						string key = t.Id + ":before";
						if (!_alreadyNotified.Contains(key))
						{
							messages.Add($"Скоро: «{t.Title}» в {dueDateTime:HH:mm}");
							_alreadyNotified.Add(key);
						}
					}
				}

				// 2) В момент
				if (enableAtTime)
				{
					if (now >= dueDateTime && now < dueDateTime.AddMinutes(1))
					{
						string key = t.Id + ":at";
						if (!_alreadyNotified.Contains(key))
						{
							messages.Add($"Сейчас: «{t.Title}»");
							_alreadyNotified.Add(key);
						}
					}
				}

				// 3) Просрочено
				if (now >= dueDateTime.AddMinutes(1))
				{
					string key = t.Id + ":overdue";
					if (!_alreadyNotified.Contains(key))
					{
						messages.Add($"Просрочено: «{t.Title}» (было {dueDateTime:dd.MM HH:mm})");
						_alreadyNotified.Add(key);
					}
				}
			}

			if (messages.Count == 0) return;

			// Показываем один Toast со списком, если уведомлений несколько
			var text = messages.Count == 1
				? messages[0]
				: $"{messages[0]}\n(+{messages.Count - 1} ещё)";

			ToastService.ShowToast(text);

			if (playSound)
			{
				try { SystemSounds.Exclamation.Play(); } catch { }
			}
		}
	}
}
using System;
using System.Collections.Generic;
using StructLab.Views.Notifications;

namespace StructLab.Services
{
	/// <summary>
	/// Показ кастомных всплывающих окон (ToastWindow) справа снизу.
	/// Не более 3 одновременно — старые закрываются.
	/// </summary>
	public static class ToastService
	{
		private static readonly List<ToastWindow> _open = new List<ToastWindow>();
		private const int MaxToasts = 3;

		public static void ShowToast(string message)
		{
			try
			{
				// Закрываем старые, если больше лимита
				while (_open.Count >= MaxToasts)
				{
					try { _open[0].Close(); } catch { }
					_open.RemoveAt(0);
				}

				var toast = new ToastWindow(message);
				toast.Closed += (s, e) => _open.Remove(toast);
				_open.Add(toast);
				toast.Show();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("ToastService: " + ex.Message);
			}
		}
	}
}
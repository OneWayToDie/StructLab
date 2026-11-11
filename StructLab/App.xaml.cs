using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using StructLab.Reading.Models;
using StructLab.Reading.Services;
using StructLab.Reading.ViewModels;

namespace StructLab
{
	public partial class App : Application
	{
		/// <summary>Контейнер зависимостей приложения.</summary>
		public static IServiceProvider Services { get; private set; }

		/// <summary>Достаёт зарегистрированный сервис (или бросает, если его нет).</summary>
		public static T GetService<T>() where T : class
			=> Services != null
				? Services.GetRequiredService<T>()
				: throw new InvalidOperationException("Service provider is not initialized yet.");

		protected override void OnStartup(StartupEventArgs e)
		{
			var services = new ServiceCollection();
			ConfigureServices(services);
			Services = services.BuildServiceProvider();

			base.OnStartup(e);
		}

		private static void ConfigureServices(IServiceCollection services)
		{
			// Настройки читалки — одни на приложение.
			services.AddSingleton(_ => ReadingSettings.Load());

			// Викитека: провайдер + файловый кэш + высокоуровневый сервис.
			services.AddSingleton<IBookSourceProvider, WikisourceProvider>();
			services.AddSingleton<IBookCache, FileBookCache>();
			services.AddSingleton<ReadingService>();

			// EPUB и локальное хранилище.
			services.AddSingleton<EpubProvider>();
			services.AddSingleton<EpubLibraryStore>();

			// Прогресс чтения и закладки.
			services.AddSingleton<ReadingProgressStore>();
			services.AddSingleton<BookmarkStore>();

			// Окно читалки получает новую модель на каждое открытие.
			services.AddTransient<ReaderViewModel>();
		}
	}
}

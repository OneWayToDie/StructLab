using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using StructLab.Models;
using StructLab.Views;
using StructLab.Views.Lab01;
using StructLab.Views.Lab02;

namespace StructLab.ViewModels
{
	public class MainViewModel : ViewModelBase
	{
		private object _currentView;
		private LabInfo _selectedLab;
		private bool _isSettingsOpen;
		private bool _isDarkTheme;
		private bool _animationsEnabled;

		public ObservableCollection<LabInfo> Labs { get; }

		public object CurrentView
		{
			get => _currentView;
			set => SetProperty(ref _currentView, value);
		}

		public LabInfo SelectedLab
		{
			get => _selectedLab;
			set
			{
				if (!SetProperty(ref _selectedLab, value) || value == null)
				{
					return;
				}

				try
				{
					NavigateToLab(value);
				}
				catch (Exception ex)
				{
					MessageBox.Show(
						"Не удалось открыть раздел «" + value.Title + "».\n\n" + ex.Message,
						"StructLab", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		public bool IsSettingsOpen
		{
			get => _isSettingsOpen;
			set => SetProperty(ref _isSettingsOpen, value);
		}

		public bool IsDarkTheme
		{
			get => _isDarkTheme;
			set
			{
				if (!SetProperty(ref _isDarkTheme, value)) return;

				ApplyTheme(value);

				Properties.Settings.Default.IsDarkTheme = value;
				Properties.Settings.Default.Save();
			}
		}

		public bool AnimationsEnabled
		{
			get => _animationsEnabled;
			set
			{
				if (!SetProperty(ref _animationsEnabled, value)) return;

				Properties.Settings.Default.EnableFunAnimations = value;
				Properties.Settings.Default.Save();
			}
		}

		public RelayCommand OpenSettingsCommand { get; }

		public MainViewModel()
		{
			Labs = new ObservableCollection<LabInfo>
			{
				new LabInfo
				{
					Number = "1",
					Title = "Динамические структуры",
					Description = "Списки, стеки, очереди",
					ViewType = typeof(Lab01View)
				},
				new LabInfo
				{
					Number = "2",
					Title = "Деревья",
					Description = "BST и AVL · Менеджер задач",
					ViewType = typeof(Lab02View)
				},
				new LabInfo { Number = "3", Title = "Классы и объекты", Description = "Простейшие классы", ViewType = null },
				new LabInfo { Number = "4", Title = "Разработка классов", Description = "Перегрузка операторов", ViewType = null },
				new LabInfo { Number = "5", Title = "Классы для структур", Description = "ООП поверх списков", ViewType = null },
				new LabInfo { Number = "6", Title = "Шаблоны классов", Description = "Обобщённое программирование", ViewType = null },
				new LabInfo { Number = "7", Title = "Наследование", Description = "Иерархии классов", ViewType = null },
				new LabInfo { Number = "8", Title = "Графический интерфейс", Description = "WinForms (C++/CLI)", ViewType = null }
			};

			OpenSettingsCommand = new RelayCommand(_ => IsSettingsOpen = true);

			CurrentView = new HomeView();

			// Восстанавливаем сохранённую тему при запуске
			IsDarkTheme = Properties.Settings.Default.IsDarkTheme;
			_animationsEnabled = Properties.Settings.Default.EnableFunAnimations;
		}

		private void NavigateToLab(LabInfo lab)
		{
			if (lab.ViewType == null)
			{
				CurrentView = new HomeView();
				return;
			}

			CurrentView = Activator.CreateInstance(lab.ViewType);
		}

		private void ApplyTheme(bool dark)
		{
			// 1. Обновляем Material Design контролы
			var paletteHelper = new PaletteHelper();
			ITheme theme = paletteHelper.GetTheme();

			// v4.9+ : BaseTheme — это enum, но SetBaseTheme принимает IBaseTheme,
			// поэтому конвертируем через явную реализацию IBaseTheme
			theme.SetBaseTheme(dark
				? (IBaseTheme)new MaterialDesignDarkTheme()
				: (IBaseTheme)new MaterialDesignLightTheme());

			paletteHelper.SetTheme(theme);

			// 2. Обновляем наши кастомные кисти
			var r = Application.Current.Resources;

			if (dark)
			{
				r["AppBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A));
				r["SurfaceBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
				r["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
				r["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
			}
			else
			{
				r["AppBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
				r["SurfaceBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
				r["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A));
				r["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
			}
		}
	}
}
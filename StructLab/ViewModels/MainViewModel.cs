using System;
using System.Collections.ObjectModel;
using StructLab.Models;
using StructLab.Views;
using StructLab.Views.Lab01;

namespace StructLab.ViewModels
{
	public class MainViewModel : ViewModelBase
	{
		private object _currentView;
		private LabInfo _selectedLab;

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
				if (SetProperty(ref _selectedLab, value) && value != null)
				{
					NavigateToLab(value);
				}
			}
		}
		private bool _isSettingsOpen;

		public bool IsSettingsOpen
		{
			get => _isSettingsOpen;
			set => SetProperty(ref _isSettingsOpen, value);
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
					Description = "Бинарные деревья поиска",
					ViewType = null  // добавим позже
                },
				new LabInfo
				{
					Number = "3",
					Title = "Классы и объекты",
					Description = "Простейшие классы",
					ViewType = null
				},
				new LabInfo
				{
					Number = "4",
					Title = "Разработка классов",
					Description = "Перегрузка операторов",
					ViewType = null
				},
				new LabInfo
				{
					Number = "5",
					Title = "Классы для структур",
					Description = "ООП поверх списков",
					ViewType = null
				},
				new LabInfo
				{
					Number = "6",
					Title = "Шаблоны классов",
					Description = "Обобщённое программирование",
					ViewType = null
				},
				new LabInfo
				{
					Number = "7",
					Title = "Наследование",
					Description = "Иерархии классов",
					ViewType = null
				},
				new LabInfo
				{
					Number = "8",
					Title = "Графический интерфейс",
					Description = "WinForms (C++/CLI)",
					ViewType = null
				}
			};

			OpenSettingsCommand = new RelayCommand(_ => IsSettingsOpen = true);

			// По умолчанию показываем домашнюю страницу
			CurrentView = new HomeView();
		}

		private void NavigateToLab(LabInfo lab)
		{
			if (lab.ViewType == null)
			{
				CurrentView = new HomeView();  // заглушка для ещё не готовых лаб
				return;
			}
			CurrentView = Activator.CreateInstance(lab.ViewType);
		}
	}
}

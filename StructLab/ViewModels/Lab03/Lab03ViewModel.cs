using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using StructLab.Models.Lab03;
using StructLab.Services;

namespace StructLab.ViewModels.Lab03
{
	// Вспомогательный класс для красивого отображения Enum в ComboBox
	public class EnumItem<T>
	{
		public T Value { get; set; }
		public string Name { get; set; }
	}

	public class Lab03ViewModel : ViewModelBase
	{
		private IntPtr _clinicHandle;

		public ObservableCollection<Patient> FilteredPatients { get; } = new ObservableCollection<Patient>();

		// Списки для ComboBox
		public List<EnumItem<PatientType>> PatientTypes { get; }
		public List<EnumItem<Species>> SpeciesTypes { get; }
		public List<EnumItem<Severity>> Severities { get; }
		public ObservableCollection<string> AvailableAvatars { get; } = new ObservableCollection<string>();

		public int TotalCount => FilteredPatients.Count;
		public int CriticalCount => FilteredPatients.Count(p => p.Severity == Severity.Critical);

		// Кэши
		private readonly Dictionary<int, string> _avatarCache = new Dictionary<int, string>();
		private readonly Dictionary<int, Species> _speciesCache = new Dictionary<int, Species>(); // Запоминаем вид животного
		private readonly Random _rnd = new Random();
		private readonly Dictionary<string, List<string>> _pools = new Dictionary<string, List<string>>();

		// Фильтры
		private string _searchDiagnosis;
		public string SearchDiagnosis { get => _searchDiagnosis; set { if (SetProperty(ref _searchDiagnosis, value)) RefreshPatients(); } }

		private int _minCard = 1000;
		public int MinCard { get => _minCard; set { if (SetProperty(ref _minCard, value)) RefreshPatients(); } }

		private int _maxCard = 9999;
		public int MaxCard { get => _maxCard; set { if (SetProperty(ref _maxCard, value)) RefreshPatients(); } }

		private Patient _selectedPatient;
		public Patient SelectedPatient { get => _selectedPatient; set => SetProperty(ref _selectedPatient, value); }

		private bool _isAddDialogOpen;
		public bool IsAddDialogOpen { get => _isAddDialogOpen; set => SetProperty(ref _isAddDialogOpen, value); }

		private Patient _newPatient = new Patient();
		public Patient NewPatient
		{
			get => _newPatient;
			set
			{
				if (SetProperty(ref _newPatient, value) && _newPatient != null)
				{
					_newPatient.PropertyChanged += (s, e) =>
					{
						if (e.PropertyName == nameof(Patient.Type) || e.PropertyName == nameof(Patient.Species))
							UpdateAvailableAvatars();
					};
				}
			}
		}

		private bool _isDetailsOpen;
		public bool IsDetailsOpen { get => _isDetailsOpen; set => SetProperty(ref _isDetailsOpen, value); }

		private Patient _detailsPatient;
		public Patient DetailsPatient { get => _detailsPatient; set => SetProperty(ref _detailsPatient, value); }

		public RelayCommand OpenAddDialogCommand { get; }
		public RelayCommand ConfirmAddCommand { get; }
		public RelayCommand CancelAddCommand { get; }
		public RelayCommand AddRandomCommand { get; }
		public RelayCommand DeleteCommand { get; }
		public RelayCommand ClearAllCommand { get; }
		public RelayCommand OpenDetailsCommand { get; }

		public Lab03ViewModel()
		{
			// Заполняем списки русскими названиями
			PatientTypes = new List<EnumItem<PatientType>>
			{
				new EnumItem<PatientType> { Value = PatientType.Human, Name = "Человек" },
				new EnumItem<PatientType> { Value = PatientType.Animal, Name = "Животное" }
			};

			SpeciesTypes = new List<EnumItem<Species>>
			{
				new EnumItem<Species> { Value = Species.Cat, Name = "Кот" },
				new EnumItem<Species> { Value = Species.Dog, Name = "Собака" },
				new EnumItem<Species> { Value = Species.Hamster, Name = "Хомяк" },
				new EnumItem<Species> { Value = Species.Parrot, Name = "Попугай" }
			};

			Severities = new List<EnumItem<Severity>>
			{
				new EnumItem<Severity> { Value = Severity.Light, Name = "Легкое" },
				new EnumItem<Severity> { Value = Severity.Medium, Name = "Среднее" },
				new EnumItem<Severity> { Value = Severity.Critical, Name = "Критическое" }
			};

			_clinicHandle = NativeMethods.Lab03_Create();

			if (NativeMethods.Lab03_GetPatients(_clinicHandle, null, 0, 99999, new CPatient[100], 100) == 0)
			{
				NativeMethods.Lab03_AddPatient(_clinicHandle, 1, "Барсик", "Иванов И.И.", 5001, "Отравление", 2);
				NativeMethods.Lab03_AddPatient(_clinicHandle, 0, "Петров П.П.", "ул. Ленина 5", 1005, "Простуда", 0);
			}

			RefreshPatients();

			OpenAddDialogCommand = new RelayCommand(_ =>
			{
				NewPatient = new Patient { Type = PatientType.Human, Species = Species.Cat };
				UpdateAvailableAvatars();
				IsAddDialogOpen = true;
			});

			CancelAddCommand = new RelayCommand(_ => IsAddDialogOpen = false);

			ConfirmAddCommand = new RelayCommand(_ =>
			{
				int newId = NativeMethods.Lab03_AddPatient(_clinicHandle,
					(int)NewPatient.Type,
					NewPatient.FullName ?? "",
					NewPatient.Address ?? "",
					NewPatient.CardNumber,
					NewPatient.Diagnosis ?? "",
					(int)NewPatient.Severity);

				// Сохраняем вид и выбранную аватарку для нового пациента в кэш
				if (NewPatient.Type == PatientType.Animal) _speciesCache[newId] = NewPatient.Species;
				if (!string.IsNullOrEmpty(NewPatient.AvatarPath)) _avatarCache[newId] = NewPatient.AvatarPath;

				IsAddDialogOpen = false;
				RefreshPatients();
			});

			DeleteCommand = new RelayCommand(_ =>
			{
				if (SelectedPatient != null)
				{
					NativeMethods.Lab03_RemovePatient(_clinicHandle, SelectedPatient.Id);
					_avatarCache.Remove(SelectedPatient.Id);
					_speciesCache.Remove(SelectedPatient.Id);
					RefreshPatients();
				}
			}, _ => SelectedPatient != null);

			AddRandomCommand = new RelayCommand(_ =>
			{
				var rnd = new Random();
				bool isHuman = rnd.Next(2) == 0;
				Species species = isHuman ? Species.None : (Species)rnd.Next(1, 5); // 1-4 (Кот, Собака, Хомяк, Попугай)

				int newId = NativeMethods.Lab03_AddPatient(_clinicHandle,
					isHuman ? 0 : 1,
					isHuman ? $"Пациент_{rnd.Next(100, 999)}" : $"Питомец_{rnd.Next(100, 999)}",
					"Адрес/Владелец",
					isHuman ? rnd.Next(1000, 4999) : rnd.Next(5000, 9999),
					isHuman ? "Грипп" : "Блохи",
					rnd.Next(0, 3));

				if (!isHuman) _speciesCache[newId] = species;
				RefreshPatients();
			});

			ClearAllCommand = new RelayCommand(_ =>
			{
				NativeMethods.Lab03_Clear(_clinicHandle);
				_avatarCache.Clear();
				_speciesCache.Clear();
				RefreshPatients();
			});

			OpenDetailsCommand = new RelayCommand(_ =>
			{
				if (SelectedPatient != null)
				{
					DetailsPatient = SelectedPatient;
					IsDetailsOpen = true;
				}
			});
		}

		private void UpdateAvailableAvatars()
		{
			AvailableAvatars.Clear();
			if (NewPatient == null) return;

			string folder = "Humans";
			string prefix = "Human"; // С большой буквы, как на скриншоте
			int count = 13;

			if (NewPatient.Type == PatientType.Human)
			{
				folder = "Humans"; prefix = "Human"; count = 13;
			}
			else
			{
				switch (NewPatient.Species)
				{
					case Species.Cat: folder = "Cats"; prefix = "cat"; count = 10; break;
					case Species.Dog: folder = "Dogs"; prefix = "dog"; count = 10; break;
					case Species.Hamster: folder = "Hamsters"; prefix = "hamster"; count = 10; break;
					case Species.Parrot: folder = "Parrots"; prefix = "parrot"; count = 10; break; // Предполагаю, что тут тоже 10
				}
			}

			for (int i = 1; i <= count; i++)
			{
				// Теперь мы точно знаем, что это .jpg для людей и .png для животных
				string ext = NewPatient.Type == PatientType.Human ? ".jpg" : ".png";
				AvailableAvatars.Add($"pack://application:,,,/Assets/Avatars/{folder}/{prefix}{i}{ext}");
			}
		}

		private string GetRandomAvatarPath(PatientType type, Species species)
		{
			string folder = "Humans";
			string prefix = "Human";
			string ext = ".jpg";
			int maxCount = 13;

			if (type == PatientType.Animal)
			{
				ext = ".png";
				maxCount = 10;
				switch (species)
				{
					case Species.Cat: folder = "Cats"; prefix = "cat"; break;
					case Species.Dog: folder = "Dogs"; prefix = "dog"; break;
					case Species.Hamster: folder = "Hamsters"; prefix = "hamster"; break;
					case Species.Parrot: folder = "Parrots"; prefix = "parrot"; break;
				}
			}

			string poolKey = folder;
			// Если пул пуст или его нет - заполняем ТОЛЬКО существующими файлами
			if (!_pools.ContainsKey(poolKey) || _pools[poolKey].Count == 0)
			{
				var newPool = new List<string>();
				for (int i = 1; i <= maxCount; i++)
				{
					newPool.Add($"pack://application:,,,/Assets/Avatars/{folder}/{prefix}{i}{ext}");
				}
				_pools[poolKey] = newPool;
			}

			int idx = _rnd.Next(_pools[poolKey].Count);
			string path = _pools[poolKey][idx];
			_pools[poolKey].RemoveAt(idx);
			return path;
		}

		private void RefreshPatients()
		{
			const int max = 1000;
			var buffer = new CPatient[max];
			int count = NativeMethods.Lab03_GetPatients(_clinicHandle, SearchDiagnosis, MinCard, MaxCard, buffer, max);

			FilteredPatients.Clear();
			for (int i = 0; i < count; i++)
			{
				var cp = buffer[i];

				// Восстанавливаем вид животного (если его нет в кэше, ставим Кота по умолчанию)
				Species species = Species.None;
				if ((PatientType)cp.Type == PatientType.Animal)
				{
					if (!_speciesCache.TryGetValue(cp.Id, out species)) { species = Species.Cat; _speciesCache[cp.Id] = species; }
				}

				var p = new Patient
				{
					Id = cp.Id,
					Type = (PatientType)cp.Type,
					Species = species,
					FullName = cp.FullName,
					Address = cp.Address,
					CardNumber = cp.CardNumber,
					Diagnosis = cp.Diagnosis,
					Severity = (Severity)cp.Severity
				};

				if (!_avatarCache.ContainsKey(p.Id))
				{
					_avatarCache[p.Id] = GetRandomAvatarPath(p.Type, p.Species);
				}
				p.AvatarPath = _avatarCache[p.Id];

				FilteredPatients.Add(p);
			}
			OnPropertyChanged(nameof(TotalCount));
			OnPropertyChanged(nameof(CriticalCount));
		}
	}
}
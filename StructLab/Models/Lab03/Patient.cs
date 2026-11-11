using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace StructLab.Models.Lab03
{
	public enum PatientType { Human = 0, Animal = 1 }
	public enum Severity { Light = 0, Medium = 1, Critical = 2 }
	public enum Species { None = 0, Cat = 1, Dog = 2, Hamster = 3, Parrot = 4 }

	public class Patient : ObservableModel
	{
		private int _id;
		private PatientType _type;
		private Species _species;
		private string _fullName;
		private string _address; // Для людей - адрес, для животных - владелец
		private int _cardNumber;
		private string _diagnosis;
		private Severity _severity;
		private string _avatarPath;

		public int Id { get => _id; set => SetProperty(ref _id, value); }

		public PatientType Type
		{
			get => _type;
			set
			{
				if (SetProperty(ref _type, value))
				{
					OnPropertyChanged(nameof(IsHuman));
					OnPropertyChanged(nameof(IsAnimal));
					OnPropertyChanged(nameof(TypeIcon));
					OnPropertyChanged(nameof(TypeLabel));
				}
			}
		}

		public Species Species
		{
			get => _species;
			set => SetProperty(ref _species, value);
		}

		public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }

		// Если животное, это свойство хранит ФИО владельца
		public string Address { get => _address; set => SetProperty(ref _address, value); }

		public int CardNumber { get => _cardNumber; set => SetProperty(ref _cardNumber, value); }
		public string Diagnosis { get => _diagnosis; set => SetProperty(ref _diagnosis, value); }

		public Severity Severity
		{
			get => _severity;
			set
			{
				if (SetProperty(ref _severity, value))
				{
					OnPropertyChanged(nameof(SeverityLabel));
					OnPropertyChanged(nameof(SeverityColor));
				}
			}
		}

		public string AvatarPath
		{
			get => _avatarPath;
			set => SetProperty(ref _avatarPath, value);
		}

		// Свойства для удобной привязки в XAML
		public bool IsHuman => Type == PatientType.Human;
		public bool IsAnimal => Type == PatientType.Animal;

		public PackIconKind TypeIcon => IsHuman ? PackIconKind.Account : PackIconKind.Paw;
		public string TypeLabel => IsHuman ? "Человек" : "Животное";

		public string SeverityLabel
		{
			get
			{
				switch (Severity)
				{
					case Severity.Critical: return "Критическое";
					case Severity.Medium: return "Среднее";
					default: return "Легкое";
				}
			}
		}

		public Brush SeverityColor
		{
			get
			{
				string hex;
				switch (Severity)
				{
					case Severity.Critical: hex = "#EF4444"; break;
					case Severity.Medium: hex = "#F59E0B"; break;
					default: hex = "#10B981"; break;
				}

				var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
				brush.Freeze();
				return brush;
			}
		}

		public void CopyFrom(Patient other)
		{
			Id = other.Id;
			Type = other.Type;
			Species = other.Species;
			FullName = other.FullName;
			Address = other.Address;
			CardNumber = other.CardNumber;
			Diagnosis = other.Diagnosis;
			Severity = other.Severity;
			AvatarPath = other.AvatarPath;
		}
	}
}
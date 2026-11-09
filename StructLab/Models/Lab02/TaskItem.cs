using StructLab.Models;

namespace StructLab.Models.Lab02
{
	public enum TaskStatus
	{
		Pending = 0,
		InProgress = 1,
		Done = 2
	}

	public class TaskItem : ObservableModel
	{
		private int _id;
		private string _title;
		private string _description;
		private int _priority;
		private string _dueDate;
		private TaskStatus _status;
		private bool _isManual;
		

		public int Id { get => _id; set => SetProperty(ref _id, value); }
		public string Title { get => _title; set => SetProperty(ref _title, value); }
		public string Description { get => _description; set => SetProperty(ref _description, value); }
		public bool IsManual { get => _isManual; set => SetProperty(ref _isManual, value); }
		public string DueDate
		{
			get => _dueDate;
			set
			{
				if (SetProperty(ref _dueDate, value))
					OnPropertyChanged(nameof(DueDisplay));
			}
		}

		private string _dueTime;
		public string DueTime
		{
			get => _dueTime;
			set
			{
				if (SetProperty(ref _dueTime, value))
					OnPropertyChanged(nameof(DueDisplay));
			}
		}
		// Отображение срока: "2026-11-15 14:30" или "2026-11-15".
		public string DueDisplay
		{
			get
			{
				if (string.IsNullOrWhiteSpace(_dueDate)) return "";
				if (string.IsNullOrWhiteSpace(_dueTime)) return _dueDate;
				return _dueDate + " " + _dueTime;
			}
		}
		public int Priority
		{
			get => _priority;
			set
			{
				if (SetProperty(ref _priority, value))
				{
					OnPropertyChanged(nameof(PriorityLabel));
					OnPropertyChanged(nameof(NodeColor));   
				}
			}
		}
		public TaskStatus Status
		{
			get => _status;
			set
			{
				if (SetProperty(ref _status, value))
				{
					OnPropertyChanged(nameof(StatusLabel));
					OnPropertyChanged(nameof(IsDone));
					OnPropertyChanged(nameof(IsPending));
					OnPropertyChanged(nameof(IsInProgress));
				}
			}
		}

		public string PriorityLabel
		{
			get
			{
				if (_priority >= 9) return "Критично";
				if (_priority >= 7) return "Высокий";
				if (_priority >= 4) return "Средний";
				return "Низкий";
			}
		}

		public string StatusLabel
		{
			get
			{
				switch (_status)
				{
					case TaskStatus.Done: return "Выполнена";
					case TaskStatus.InProgress: return "В работе";
					default: return "Ожидает";
				}
			}
		}

		public bool IsDone => _status == TaskStatus.Done;
		public bool IsPending => _status == TaskStatus.Pending;
		public bool IsInProgress => _status == TaskStatus.InProgress;

		public System.Windows.Media.Brush NodeColor
		{
			get
			{
				if (_priority >= 9) return MakeBrush("#EF4444");
				if (_priority >= 7) return MakeBrush("#F59E0B");
				if (_priority >= 4) return MakeBrush("#4F46E5");
				return MakeBrush("#94A3B8");
			}
		}

		private static System.Windows.Media.Brush MakeBrush(string hex)
		{
			var brush = new System.Windows.Media.SolidColorBrush(
				(System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
			brush.Freeze();
			return brush;
		}

		public void CopyFrom(TaskItem other)
		{
			Id = other.Id;
			Title = other.Title;
			Description = other.Description;
			Priority = other.Priority;
			DueDate = other.DueDate;
			DueTime = other.DueTime;
			Status = other.Status;
			IsManual = other.IsManual;
		}
	}
}

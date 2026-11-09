using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using StructLab.Models.Lab02;
using StructLab.Services;
using StructLab.Models.Dialogs;

using TaskStatus = StructLab.Models.Lab02.TaskStatus;

namespace StructLab.ViewModels.Lab02
{
	public enum TaskFilter
	{
		All,
		Pending,
		InProgress,
		Done
	}

	public class Lab02ViewModel : ViewModelBase
	{
		private IntPtr _bstHandle;
		private IntPtr _avlHandle;

		// ============================================================
		// КОЛЛЕКЦИИ
		// ============================================================
		public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
		public ObservableCollection<TaskItem> FilteredTasks { get; } = new ObservableCollection<TaskItem>();

		public ObservableCollection<TreeNodeView> BstNodes { get; } = new ObservableCollection<TreeNodeView>();
		public ObservableCollection<TreeEdgeView> BstEdges { get; } = new ObservableCollection<TreeEdgeView>();
		public ObservableCollection<TreeNodeView> AvlNodes { get; } = new ObservableCollection<TreeNodeView>();
		public ObservableCollection<TreeEdgeView> AvlEdges { get; } = new ObservableCollection<TreeEdgeView>();
		


		// ============================================================
		// РАЗМЕРЫ CANVAS
		// ============================================================
		private double _bstCanvasWidth = 400;
		private double _bstCanvasHeight = 300;
		private double _avlCanvasWidth = 400;
		private double _avlCanvasHeight = 300;

		public double BstCanvasWidth { get => _bstCanvasWidth; private set => SetProperty(ref _bstCanvasWidth, value); }
		public double BstCanvasHeight { get => _bstCanvasHeight; private set => SetProperty(ref _bstCanvasHeight, value); }
		public double AvlCanvasWidth { get => _avlCanvasWidth; private set => SetProperty(ref _avlCanvasWidth, value); }
		public double AvlCanvasHeight { get => _avlCanvasHeight; private set => SetProperty(ref _avlCanvasHeight, value); }

		// ============================================================
		// СТАТИСТИКА
		// ============================================================
		public string BstInfo => $"BST: {NativeMethods.Lab02_GetIndexNodeCount(_bstHandle)} узлов, высота {NativeMethods.Lab02_GetTreeHeight(_bstHandle)}";
		public string AvlInfo => $"AVL: {NativeMethods.Lab02_GetIndexNodeCount(_avlHandle)} узлов, высота {NativeMethods.Lab02_GetTreeHeight(_avlHandle)}";

		// ============================================================
		// ФИЛЬТР
		// ============================================================
		private TaskFilter _selectedFilter = TaskFilter.All;
		public TaskFilter SelectedFilter
		{
			get => _selectedFilter;
			set
			{
				if (SetProperty(ref _selectedFilter, value))
				{
					OnPropertyChanged(nameof(IsFilterAll));
					OnPropertyChanged(nameof(IsFilterPending));
					OnPropertyChanged(nameof(IsFilterInProgress));
					OnPropertyChanged(nameof(IsFilterDone));
					ApplyFilter();
				}
			}
		}

		public bool IsFilterAll { get => _selectedFilter == TaskFilter.All; set { if (value) SelectedFilter = TaskFilter.All; } }
		public bool IsFilterPending { get => _selectedFilter == TaskFilter.Pending; set { if (value) SelectedFilter = TaskFilter.Pending; } }
		public bool IsFilterInProgress { get => _selectedFilter == TaskFilter.InProgress; set { if (value) SelectedFilter = TaskFilter.InProgress; } }
		public bool IsFilterDone { get => _selectedFilter == TaskFilter.Done; set { if (value) SelectedFilter = TaskFilter.Done; } }

		// ============================================================
		// ВЫДЕЛЕНИЕ
		// ============================================================
		private TaskItem _selectedTask;
		public TaskItem SelectedTask { get => _selectedTask; set => SetProperty(ref _selectedTask, value); }

		// ============================================================
		// ДИАЛОГ ДОБАВЛЕНИЯ
		// ============================================================
		private bool _isAddDialogOpen;
		private TaskItem _newTask = new TaskItem { Priority = 5 };
		private string _newTaskError;

		public bool IsAddDialogOpen
		{
			get => _isAddDialogOpen;
			set { if (SetProperty(ref _isAddDialogOpen, value)) OnPropertyChanged(nameof(IsAnyDialogOpen)); }
		}
		public TaskItem NewTask { get => _newTask; set => SetProperty(ref _newTask, value); }
		public string NewTaskError { get => _newTaskError; set => SetProperty(ref _newTaskError, value); }

		// ============================================================
		// ДИАЛОГ «СРОЧНЫЕ»
		// ============================================================
		private bool _isUrgentDialogOpen;
		private string _urgentMessage;
		public bool IsUrgentDialogOpen
		{
			get => _isUrgentDialogOpen;
			set { if (SetProperty(ref _isUrgentDialogOpen, value)) OnPropertyChanged(nameof(IsAnyDialogOpen)); }
		}
		public ObservableCollection<TaskItem> UrgentTasks { get; } = new ObservableCollection<TaskItem>();
		public string UrgentMessage { get => _urgentMessage; set => SetProperty(ref _urgentMessage, value); }

		public bool IsAnyDialogOpen
		{
			get => _isAddDialogOpen || _isUrgentDialogOpen;
			set { if (!value) { IsAddDialogOpen = false; IsUrgentDialogOpen = false; } }
		}
		// ============================================================
		// ДЕГРАДАЦИЯ
		// ============================================================

		// В Lab02ViewModel.cs найдите и замените свойство IsDegraded:
		private bool _isDegraded;
		public string DegradeLabel => _isDegraded ? "Нормализовать" : "Деградация";
		public string DegradeIcon => _isDegraded ? "TrendingUp" : "TrendingDown";

		public bool IsDegraded
		{
			get => _isDegraded;
			set
			{
				if (SetProperty(ref _isDegraded, value))
				{
					OnPropertyChanged(nameof(DegradeLabel));
					OnPropertyChanged(nameof(DegradeIcon));
					CommandManager.InvalidateRequerySuggested();

					// Вызываем логику при переключении
					DegradeBst();
				}
			}
		}

		// И замените метод DegradeBst:
		private void DegradeBst()
		{
			if (!CanRandomize) return;

			NativeMethods.Lab02_Clear(_bstHandle);
			NativeMethods.Lab02_Clear(_avlHandle);
			Tasks.Clear();
			FilteredTasks.Clear();
			SelectedTask = null;

			if (_taskPool.Count < 10) return;
			var rand = new Random();

			for (int i = 0; i < 10 && _taskPool.Count > 0; i++)
			{
				string title = _taskPool[0];
				_taskPool.RemoveAt(0);
				string due = DateTime.Today.AddDays(rand.Next(1, 30)).ToString("yyyy-MM-dd");
				int priority = rand.Next(1, 11);

				int idToUse;
				if (IsDegraded)
				{
					// Деградация: строго последовательные ID (1, 2, 3...) -> BST вырождается
					idToUse = i + 1;
				}
				else
				{
					// Нормализация: случайные ID -> BST выглядит как обычное дерево
					idToUse = rand.Next(1, 9999);
					while (Tasks.Any(t => t.Id == idToUse)) idToUse = rand.Next(1, 9999);
				}

				NativeMethods.Lab02_AddTaskWithId(_bstHandle, idToUse, title, "", priority, due, "", 0, 0);
				NativeMethods.Lab02_AddTaskWithId(_avlHandle, idToUse, title, "", priority, due, "", 0, 0);
			}

			RefreshTasks();
		}


		// ============================================================
		// ПОЛНОЭКРАННЫЙ РЕЖИМ (BST / AVL / ТАБЛИЦА)
		// ============================================================


		private int _fullscreenMode;   // 0 = None, 1 = BST, 2 = AVL, 3 = Table
		public int FullscreenMode
		{
			get => _fullscreenMode;
			set
			{
				if (SetProperty(ref _fullscreenMode, value))
				{
					OnPropertyChanged(nameof(IsFullscreenOpen));
					OnPropertyChanged(nameof(IsBstFullscreen));
					OnPropertyChanged(nameof(IsAvlFullscreen));
					OnPropertyChanged(nameof(IsTableFullscreen));
				}
			}
		}

		public bool IsFullscreenOpen => _fullscreenMode > 0;
		public bool IsBstFullscreen => _fullscreenMode == 1;
		public bool IsAvlFullscreen => _fullscreenMode == 2;
		public bool IsTableFullscreen => _fullscreenMode == 3;

		// ============================================================
		// ПУЛ ЗАДАЧ
		// ============================================================
		private readonly List<string> _taskPool = new List<string>();

		public int RemainingPoolCount => _taskPool.Count;
		public string PoolStatusText => $"Осталось в пуле: {_taskPool.Count} из {TaskTemplatePool.All.Length}";

		// ============================================================
		// РЕАЛЬНЫЙ РЕЖИМ
		// ============================================================
		private bool _hasManualTasks;
		public bool HasManualTasks
		{
			get => _hasManualTasks;
			private set
			{
				if (SetProperty(ref _hasManualTasks, value))
				{
					OnPropertyChanged(nameof(CanRandomize));
					OnPropertyChanged(nameof(ManualModeHint));
					CommandManager.InvalidateRequerySuggested();
				}
			}
		}

		public bool CanRandomize => !_hasManualTasks;

		public string ManualModeHint =>
			_hasManualTasks
				? "Включён режим реальных задач: добавлены вручную. Случайные задачи отключены."
				: null;

		// ============================================================
		// УВЕДОМЛЕНИЯ
		// ============================================================
		private readonly NotificationService _notifications = new NotificationService();
		private DispatcherTimer _notifyTimer;

		public bool NotificationsEnabled
		{
			get => Properties.Settings.Default.EnableNotifications;
			set
			{
				if (value == Properties.Settings.Default.EnableNotifications) return;
				Properties.Settings.Default.EnableNotifications = value;
				Properties.Settings.Default.Save();
				OnPropertyChanged();
				UpdateNotificationsTimer();
			}
		}

		public bool NotificationsSound
		{
			get => Properties.Settings.Default.EnableNotificationSound;
			set
			{
				if (value == Properties.Settings.Default.EnableNotificationSound) return;
				Properties.Settings.Default.EnableNotificationSound = value;
				Properties.Settings.Default.Save();
				OnPropertyChanged();
			}
		}

		private void UpdateNotificationsTimer()
		{
			if (_notifyTimer == null) return;

			_notifyTimer.Stop();

			// Отписываемся от старого обработчика
			_notifyTimer.Tick -= OnNotifyTick;

			if (Properties.Settings.Default.EnableNotifications)
			{
				// Проверяем раз в 30 секунд (это техническая частота)
				_notifyTimer.Interval = TimeSpan.FromSeconds(30);
				_notifyTimer.Tick += OnNotifyTick;
				_notifyTimer.Start();
			}
		}

		private void OnNotifyTick(object sender, EventArgs e)
		{
			bool sound = Properties.Settings.Default.EnableNotificationSound;
			bool before = Properties.Settings.Default.EnableNotifyBefore;
			bool atTime = Properties.Settings.Default.EnableNotifyAtTime;
			int minutes = Properties.Settings.Default.NotifyBeforeMinutes;

			_notifications.CheckAndNotify(Tasks, sound, before, atTime, minutes);
		}

		// ============================================================
		// КОМАНДЫ
		// ============================================================
		public RelayCommand OpenAddDialogCommand { get; }
		public RelayCommand ConfirmAddCommand { get; }
		public RelayCommand CancelAddCommand { get; }
		public RelayCommand DeleteSelectedCommand { get; }
		public RelayCommand MarkPendingCommand { get; }
		public RelayCommand MarkInProgressCommand { get; }
		public RelayCommand MarkDoneCommand { get; }
		public RelayCommand AddRandom10Command { get; }
		public RelayCommand AddRandom50Command { get; }
		public RelayCommand AddRandom100Command { get; }
		public RelayCommand ClearAllCommand { get; }
		public RelayCommand ShowUrgentCommand { get; }
		public RelayCommand CloseUrgentCommand { get; }
		public RelayCommand DegradeBstCommand { get; }

		public RelayCommand ExportTasksCommand { get; }
		public RelayCommand FullscreenBstCommand { get; }
		public RelayCommand FullscreenAvlCommand { get; }
		public RelayCommand FullscreenTableCommand { get; }
		public RelayCommand CloseFullscreenCommand { get; }

		// ============================================================
		// ПЕРСИСТЕНТНОСТЬ
		// ============================================================
		private readonly string _tasksFilePath =
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lab02_tasks.json");

		// ============================================================
		// КОНСТРУКТОР
		// ============================================================
		public Lab02ViewModel()
		{
			_bstHandle = NativeMethods.Lab02_Create();
			NativeMethods.Lab02_SetUseAvl(_bstHandle, false);
			_avlHandle = NativeMethods.Lab02_Create();
			NativeMethods.Lab02_SetUseAvl(_avlHandle, true);

			// --- Загрузка / Инициализация ---
			_taskPool.Clear();                       // ← здесь и ТОЛЬКО здесь
			bool loaded = LoadTasks();

			if (!loaded)
			{
				// Первый запуск (файла нет)
				_taskPool.Clear();
				_taskPool.AddRange(TaskTemplatePool.All);  // ← ровно 170
				AddRandom(6);                               // → 164
				SaveTasks();
			}

			// --- Команды ---
			OpenAddDialogCommand = new RelayCommand(_ =>
			{
				IsUrgentDialogOpen = false;
				NewTask = new TaskItem
				{
					Priority = 5,
					DueDate = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd"),
					DueTime = ""   // ← НОВОЕ
				};
				NewTaskError = null;
				IsAddDialogOpen = true;
			});

			ConfirmAddCommand = new RelayCommand(_ => ConfirmAdd());
			CancelAddCommand = new RelayCommand(_ => IsAddDialogOpen = false);

			DeleteSelectedCommand = new RelayCommand(_ => DeleteSelected(), _ => SelectedTask != null);
			MarkPendingCommand = new RelayCommand(_ => ChangeStatus(TaskStatus.Pending), _ => SelectedTask != null && !SelectedTask.IsPending);
			MarkInProgressCommand = new RelayCommand(_ => ChangeStatus(TaskStatus.InProgress), _ => SelectedTask != null && !SelectedTask.IsInProgress);
			MarkDoneCommand = new RelayCommand(_ => ChangeStatus(TaskStatus.Done), _ => SelectedTask != null && !SelectedTask.IsDone);

			AddRandom10Command = new RelayCommand(_ => AddRandom(10), _ => CanRandomize && _taskPool.Count >= 10);
			AddRandom50Command = new RelayCommand(_ => AddRandom(50), _ => CanRandomize && _taskPool.Count >= 50);
			AddRandom100Command = new RelayCommand(_ => AddRandom(100), _ => CanRandomize && _taskPool.Count >= 100);
			DegradeBstCommand = new RelayCommand(_ => DegradeBst(), _ => CanRandomize && _taskPool.Count >= 10);

			ClearAllCommand = new RelayCommand(_ => ClearAll(), _ => Tasks.Count > 0);
			ShowUrgentCommand = new RelayCommand(_ => ShowUrgent());
			CloseUrgentCommand = new RelayCommand(_ => IsUrgentDialogOpen = false);


			ExportTasksCommand = new RelayCommand(_ => _ = ExportTasksAsync());

			FullscreenBstCommand = new RelayCommand(_ => FullscreenMode = 1, _ => BstNodes.Count > 0);
			FullscreenAvlCommand = new RelayCommand(_ => FullscreenMode = 2, _ => AvlNodes.Count > 0);
			FullscreenTableCommand = new RelayCommand(_ => FullscreenMode = 3, _ => Tasks.Count > 0);
			CloseFullscreenCommand = new RelayCommand(_ => FullscreenMode = 0);

			// --- Уведомления ---
			_notifyTimer = new DispatcherTimer();
			UpdateNotificationsTimer();
		}

		// ============================================================
		// CRUD
		// ============================================================
		private void ConfirmAdd()
		{
			if (string.IsNullOrWhiteSpace(NewTask.Title))
			{ NewTaskError = "Укажите название."; return; }
			if (NewTask.Priority < 1 || NewTask.Priority > 10)
			{ NewTaskError = "Приоритет от 1 до 10."; return; }

			// Валидация времени
			string time = (NewTask.DueTime ?? "").Trim();
			if (!string.IsNullOrEmpty(time))
			{
				if (string.IsNullOrWhiteSpace(NewTask.DueDate))
				{ NewTaskError = "Время указано без даты."; return; }
				TimeSpan ts;
				if (!TimeSpan.TryParse(time, out ts) || ts.TotalHours >= 24)
				{ NewTaskError = "Время в формате HH:mm (например, 14:30)."; return; }
				time = ts.ToString(@"hh\:mm");   // нормализация "9:5" → "09:05"
			}

			int newId = new Random().Next(10000, 99999);
			while (Tasks.Any(t => t.Id == newId))
				newId = new Random().Next(10000, 99999);

			NativeMethods.Lab02_AddTaskWithId(_bstHandle,
				newId, NewTask.Title, NewTask.Description ?? "",
				NewTask.Priority, NewTask.DueDate ?? "", time, 0, 1);
			NativeMethods.Lab02_AddTaskWithId(_avlHandle,
				newId, NewTask.Title, NewTask.Description ?? "",
				NewTask.Priority, NewTask.DueDate ?? "", time, 0, 1);

			HasManualTasks = true;
			IsAddDialogOpen = false;
			RefreshTasks();
		}

		private void DeleteSelected()
		{
			if (SelectedTask == null) return;
			int id = SelectedTask.Id;
			NativeMethods.Lab02_DeleteTask(_bstHandle, id);
			NativeMethods.Lab02_DeleteTask(_avlHandle, id);
			SelectedTask = null;
			RefreshTasks();
		}

		private void ChangeStatus(TaskStatus status)
		{
			if (SelectedTask == null) return;
			int id = SelectedTask.Id;
			NativeMethods.Lab02_SetStatus(_bstHandle, id, (int)status);
			NativeMethods.Lab02_SetStatus(_avlHandle, id, (int)status);
			SelectedTask.Status = status;
			ApplyFilter();
			SaveTasks();
		}

		private void AddRandom(int count)
		{
			if (!CanRandomize) return;
			if (_taskPool.Count < count) count = _taskPool.Count;
			if (count <= 0) return;

			var rand = new Random();
			for (int i = 0; i < count; i++)
			{
				int idx = rand.Next(_taskPool.Count);
				string title = _taskPool[idx];
				_taskPool.RemoveAt(idx);

				int newId = rand.Next(1, 9999);
				while (Tasks.Any(t => t.Id == newId)) newId = rand.Next(1, 9999);

				int priority = rand.Next(1, 11);
				string due = rand.Next(2) == 0
					? DateTime.Today.AddDays(rand.Next(-5, 30)).ToString("yyyy-MM-dd")
					: "";

				NativeMethods.Lab02_AddTaskWithId(_bstHandle, newId, title, "", priority, due, "", 0, 0);
				NativeMethods.Lab02_AddTaskWithId(_avlHandle, newId, title, "", priority, due, "", 0, 0);
			}

			RefreshTasks();
		}


		private void ClearAll()
		{
			NativeMethods.Lab02_Clear(_bstHandle);
			NativeMethods.Lab02_Clear(_avlHandle);
			Tasks.Clear();
			FilteredTasks.Clear();
			SelectedTask = null;

			HasManualTasks = false;
			IsDegraded = false;
			_taskPool.Clear();
			_taskPool.AddRange(TaskTemplatePool.All);

			try { if (File.Exists(_tasksFilePath)) File.Delete(_tasksFilePath); } catch { }

			RefreshTrees();
			OnPropertyChanged(nameof(BstInfo));
			OnPropertyChanged(nameof(AvlInfo));
			OnPropertyChanged(nameof(RemainingPoolCount));
			OnPropertyChanged(nameof(PoolStatusText));
			CommandManager.InvalidateRequerySuggested();
		}

		private void ShowUrgent()
		{
			const int max = 1000;
			var buffer = new int[max];
			int n = NativeMethods.Lab02_GetTopUrgent(_bstHandle, 10, buffer, max);

			UrgentTasks.Clear();
			UrgentMessage = null;

			if (n == 0)
			{
				UrgentMessage = "Нет невыполненных задач. Всё сделано!";
				IsUrgentDialogOpen = true;
				return;
			}

			var urgentIds = new HashSet<int>();
			for (int i = 0; i < n; i++) urgentIds.Add(buffer[i]);

			var urgent = Tasks.Where(t => urgentIds.Contains(t.Id))
							  .OrderByDescending(t => t.Priority)
							  .ToList();

			foreach (var t in urgent) UrgentTasks.Add(t);
			IsUrgentDialogOpen = true;
		}

		// ============================================================
		// ОБНОВЛЕНИЕ
		// ============================================================
		private void RefreshTasks()
		{
			const int max = 10000;
			var buffer = new CTask[max];
			int n = NativeMethods.Lab02_GetAllTasks(_bstHandle, buffer, max);

			var newIds = new HashSet<int>();
			for (int i = 0; i < n; i++) newIds.Add(buffer[i].Id);

			for (int i = Tasks.Count - 1; i >= 0; i--)
				if (!newIds.Contains(Tasks[i].Id))
					Tasks.RemoveAt(i);

			var existing = Tasks.ToDictionary(t => t.Id);

			for (int i = 0; i < n; i++)
			{
				var ct = buffer[i];
				if (existing.TryGetValue(ct.Id, out var current))
				{
					current.Title = ct.Title;
					current.Description = ct.Description;
					current.Priority = ct.Priority;
					current.DueDate = ct.DueDate;
					current.DueTime = ct.DueTime;   // ← НОВОЕ
					current.Status = (TaskStatus)ct.Status;
					current.IsManual = ct.IsManual != 0;
				}
				else
				{
					Tasks.Add(new TaskItem
					{
						Id = ct.Id,
						Title = ct.Title,
						Description = ct.Description,
						Priority = ct.Priority,
						DueDate = ct.DueDate,
						DueTime = ct.DueTime,       // ← НОВОЕ
						Status = (TaskStatus)ct.Status,
						IsManual = ct.IsManual != 0
					});
				}
			}

			//SyncTrees();
			ApplyFilter();
			RefreshTrees();

			OnPropertyChanged(nameof(BstInfo));
			OnPropertyChanged(nameof(AvlInfo));
			OnPropertyChanged(nameof(RemainingPoolCount));
			OnPropertyChanged(nameof(PoolStatusText));

			SaveTasks();
		}

		private void SyncTrees()
		{
			NativeMethods.Lab02_Clear(_bstHandle);
			NativeMethods.Lab02_Clear(_avlHandle);

			foreach (var t in Tasks)
			{
				int isManual = t.IsManual ? 1 : 0;
				int status = (int)t.Status;
				NativeMethods.Lab02_AddTaskWithId(_bstHandle, t.Id, t.Title ?? "", t.Description ?? "", t.Priority, t.DueDate ?? "", t.DueTime ?? "", status, isManual);
				NativeMethods.Lab02_AddTaskWithId(_avlHandle, t.Id, t.Title ?? "", t.Description ?? "", t.Priority, t.DueDate ?? "", t.DueTime ?? "", status, isManual);
			}
		}

		private void ApplyFilter()
		{
			FilteredTasks.Clear();
			IEnumerable<TaskItem> source = Tasks;

			switch (_selectedFilter)
			{
				case TaskFilter.Pending: source = source.Where(t => t.Status == TaskStatus.Pending); break;
				case TaskFilter.InProgress: source = source.Where(t => t.Status == TaskStatus.InProgress); break;
				case TaskFilter.Done: source = source.Where(t => t.Status == TaskStatus.Done); break;
			}

			source = source.OrderByDescending(t => t.Priority).ThenBy(t => t.Id);
			foreach (var t in source) FilteredTasks.Add(t);
		}

		// ============================================================
		// ВИЗУАЛИЗАЦИЯ
		// ============================================================
		private void RefreshTrees()
		{
			var bstResult = ComputeTreeLayout(_bstHandle);
			BstNodes.Clear(); BstEdges.Clear();
			foreach (var n in bstResult.Nodes) BstNodes.Add(n);
			foreach (var e in bstResult.Edges) BstEdges.Add(e);
			BstCanvasWidth = bstResult.Width;
			BstCanvasHeight = bstResult.Height;

			var avlResult = ComputeTreeLayout(_avlHandle);
			AvlNodes.Clear(); AvlEdges.Clear();
			foreach (var n in avlResult.Nodes) AvlNodes.Add(n);
			foreach (var e in avlResult.Edges) AvlEdges.Add(e);
			AvlCanvasWidth = avlResult.Width;
			AvlCanvasHeight = avlResult.Height;
		}

		private class LayoutResult
		{
			public List<TreeNodeView> Nodes = new List<TreeNodeView>();
			public List<TreeEdgeView> Edges = new List<TreeEdgeView>();
			public double Width;
			public double Height;
		}

		private LayoutResult ComputeTreeLayout(IntPtr handle)
		{
			var result = new LayoutResult();
			const int max = 2000;
			var buffer = new CSnapshotNode[max];
			int n = NativeMethods.Lab02_GetSnapshot(handle, buffer, max);

			if (n == 0) { result.Width = 400; result.Height = 200; return result; }

			var nodes = new List<TreeNodeView>(n);
			var children = new List<int>[n];
			int rootIdx = -1;

			for (int i = 0; i < n; i++)
			{
				children[i] = new List<int>();
				nodes.Add(new TreeNodeView
				{
					Index = i,
					Key = buffer[i].Key,
					Priority = buffer[i].Priority,
					ParentIndex = buffer[i].ParentIndex,
					IsLeft = buffer[i].IsLeft
				});
			}

			for (int i = 0; i < n; i++)
			{
				if (buffer[i].ParentIndex >= 0) children[buffer[i].ParentIndex].Add(i);
				else rootIdx = i;
			}

			if (rootIdx < 0) return result;

			int xCounter = 0;
			var positions = new Point[n];

			void InOrder(int idx, int depth)
			{
				if (idx < 0) return;
				int leftChild = -1, rightChild = -1;
				foreach (var c in children[idx])
				{
					if (nodes[c].IsLeft == 1) leftChild = c;
					else rightChild = c;
				}
				InOrder(leftChild, depth + 1);
				positions[idx] = new Point(xCounter++, depth);
				InOrder(rightChild, depth + 1);
			}

			InOrder(rootIdx, 0);

			const double spacingX = 62;
			const double spacingY = 92;
			const double pad = 40;

			for (int i = 0; i < n; i++)
			{
				nodes[i].X = positions[i].X * spacingX + pad;
				nodes[i].Y = positions[i].Y * spacingY + pad;
			}

			foreach (var node in nodes)
			{
				if (node.ParentIndex < 0) continue;
				var parent = nodes[node.ParentIndex];
				result.Edges.Add(new TreeEdgeView
				{
					X1 = parent.X + 20,
					Y1 = parent.Y + 20,
					X2 = node.X + 20,
					Y2 = node.Y + 20
				});
			}

			result.Nodes = nodes;
			result.Width = xCounter * spacingX + 2 * pad;
			result.Height = (nodes.Max(nd => nd.Y) + 40 + pad);
			return result;
		}

		// ============================================================
		// ПЕРСИСТЕНТНОСТЬ
		// ============================================================
		private void SaveTasks()
		{
			try
			{
				var list = Tasks.Select(t => new PersistedTask
				{
					Id = t.Id,
					Title = t.Title,
					Description = t.Description,
					Priority = t.Priority,
					DueDate = t.DueDate,
					DueTime = t.DueTime,   // ← НОВОЕ
					Status = (int)t.Status,
					IsManual = t.IsManual
				}).ToList();

				var json = JsonSerializer.Serialize(list,
					new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(_tasksFilePath, json);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("SaveTasks: " + ex.Message);
			}
		}

		private async Task ExportTasksAsync()
		{
			var dialog = new Microsoft.Win32.SaveFileDialog
			{
				Title = "Экспорт списка задач",
				FileName = $"tasks_{DateTime.Now:yyyy-MM-dd_HH-mm}.json",
				Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*",
				DefaultExt = ".json",
				AddExtension = true
			};

			bool? result = dialog.ShowDialog();
			if (result != true) return;

			try
			{
				var list = Tasks.Select(t => new PersistedTask
				{
					Id = t.Id,
					Title = t.Title,
					Description = t.Description,
					Priority = t.Priority,
					DueDate = t.DueDate,
					DueTime = t.DueTime,
					Status = (int)t.Status,
					IsManual = t.IsManual
				}).ToList();

				var json = JsonSerializer.Serialize(list,
					new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(dialog.FileName, json);

				await DialogService.ShowMessageAsync(
					"Экспорт выполнен",
					$"Сохранено {list.Count} задач в:\n{dialog.FileName}",
					MessageKind.Info);
			}
			catch (Exception ex)
			{
				await DialogService.ShowMessageAsync(
					"Ошибка экспорта",
					ex.Message,
					MessageKind.Error);
			}
		}
		private bool LoadTasks()
		{
			if (!File.Exists(_tasksFilePath))
				return false;

			try
			{
				var json = File.ReadAllText(_tasksFilePath);
				var list = JsonSerializer.Deserialize<List<PersistedTask>>(json)
						   ?? new List<PersistedTask>();

				var rand = new Random();
				for (int i = list.Count - 1; i > 0; i--)
				{
					int j = rand.Next(i + 1);
					var temp = list[i];
					list[i] = list[j];
					list[j] = temp;
				}

				bool anyManual = false;

				foreach (var p in list)
				{
					NativeMethods.Lab02_AddTaskWithId(_bstHandle, p.Id, p.Title ?? "", p.Description ?? "",
						p.Priority, p.DueDate ?? "", p.DueTime ?? "", p.Status, p.IsManual ? 1 : 0);
					NativeMethods.Lab02_AddTaskWithId(_avlHandle, p.Id, p.Title ?? "", p.Description ?? "",
						p.Priority, p.DueDate ?? "", p.DueTime ?? "", p.Status, p.IsManual ? 1 : 0);
					if (p.IsManual) anyManual = true;
				}

				HasManualTasks = anyManual;

				RefreshTasks();

				// Восстанавливаем пул: все названия, которых нет в задачах
				var existingTitles = new HashSet<string>(
					Tasks.Select(t => t.Title), StringComparer.OrdinalIgnoreCase);
				_taskPool.Clear();
				_taskPool.AddRange(TaskTemplatePool.All.Where(t => !existingTitles.Contains(t)));

				OnPropertyChanged(nameof(RemainingPoolCount));
				OnPropertyChanged(nameof(PoolStatusText));

				return true;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("LoadTasks: " + ex.Message);
				return false;
			}
		}
	}
}
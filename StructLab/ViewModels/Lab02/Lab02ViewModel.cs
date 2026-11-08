using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using StructLab.Models.Lab02;
using StructLab.Services;

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
		// Два независимых дерева: BST и AVL
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
			set
			{
				if (SetProperty(ref _isAddDialogOpen, value))
					OnPropertyChanged(nameof(IsAnyDialogOpen));
			}
		}
		public TaskItem NewTask { get => _newTask; set => SetProperty(ref _newTask, value); }
		public string NewTaskError { get => _newTaskError; set => SetProperty(ref _newTaskError, value); }

		// ============ ДИАЛОГ «СРОЧНЫЕ ЗАДАЧИ» ============
		private bool _isUrgentDialogOpen;
		private string _urgentMessage;

		public bool IsUrgentDialogOpen
		{
			get => _isUrgentDialogOpen;
			set
			{
				if (SetProperty(ref _isUrgentDialogOpen, value))
					OnPropertyChanged(nameof(IsAnyDialogOpen));
			}
		}
		public ObservableCollection<TaskItem> UrgentTasks { get; } = new ObservableCollection<TaskItem>();
		public string UrgentMessage { get => _urgentMessage; set => SetProperty(ref _urgentMessage, value); }

		public bool IsAnyDialogOpen
		{
			get => _isAddDialogOpen || _isUrgentDialogOpen;
			set
			{
				if (!value)
				{
					IsAddDialogOpen = false;
					IsUrgentDialogOpen = false;
				}
			}
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

		// ============================================================
		// КОНСТРУКТОР
		// ============================================================
		public Lab02ViewModel()
		{
			// Создаём два параллельных дерева
			_bstHandle = NativeMethods.Lab02_Create();
			NativeMethods.Lab02_SetUseAvl(_bstHandle, false);

			_avlHandle = NativeMethods.Lab02_Create();
			NativeMethods.Lab02_SetUseAvl(_avlHandle, true);

			// Команды
			OpenAddDialogCommand = new RelayCommand(_ =>
			{
				IsUrgentDialogOpen = false;
				NewTask = new TaskItem { Priority = 5, DueDate = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd") };
				NewTaskError = null;
				IsAddDialogOpen = true;
			});

			ConfirmAddCommand = new RelayCommand(_ => ConfirmAdd());
			CancelAddCommand = new RelayCommand(_ => IsAddDialogOpen = false);

			DeleteSelectedCommand = new RelayCommand(_ => DeleteSelected(), _ => SelectedTask != null);
			MarkPendingCommand = new RelayCommand(_ => ChangeStatus(TaskStatus.Pending), _ => SelectedTask != null && !SelectedTask.IsPending);
			MarkInProgressCommand = new RelayCommand(_ => ChangeStatus(TaskStatus.InProgress), _ => SelectedTask != null && !SelectedTask.IsInProgress);
			MarkDoneCommand = new RelayCommand(_ => ChangeStatus(TaskStatus.Done), _ => SelectedTask != null && !SelectedTask.IsDone);

			AddRandom10Command = new RelayCommand(_ => AddRandom(10));
			AddRandom50Command = new RelayCommand(_ => AddRandom(50));
			AddRandom100Command = new RelayCommand(_ => AddRandom(100));
			ClearAllCommand = new RelayCommand(_ => ClearAll(), _ => Tasks.Count > 0);
			ShowUrgentCommand = new RelayCommand(_ => ShowUrgent());
			CloseUrgentCommand = new RelayCommand(_ => IsUrgentDialogOpen = false);

			// Демо-данные
			SeedDemo();
		}

		// ============================================================
		// ДОБАВЛЕНИЕ / УДАЛЕНИЕ
		// ============================================================
		private void SeedDemo()
		{
			AddTaskInternal("Сдать ЛР1", "Отчёт + защита", 8, "2026-11-15");
			AddTaskInternal("Прочитать методичку ЛР2", "Деревья", 5, "2026-11-12");
			AddTaskInternal("Разобраться с AVL", "Визуализация вращений", 7, "");
			AddTaskInternal("Купить продукты", "Молоко, хлеб, яйца", 2, "2026-11-08");
			AddTaskInternal("Собеседование", "Яндекс, middle C#", 10, "2026-11-20");
			AddTaskInternal("Позвонить маме", "", 6, "2026-11-07");

			RefreshTasks();
		}

		private void AddTaskInternal(string title, string desc, int priority, string dueDate)
		{
			NativeMethods.Lab02_AddTask(_bstHandle, title, desc, priority, dueDate);
			NativeMethods.Lab02_AddTask(_avlHandle, title, desc, priority, dueDate);
		}

		private void ConfirmAdd()
		{
			if (string.IsNullOrWhiteSpace(NewTask.Title)) { NewTaskError = "Укажите название."; return; }
			if (NewTask.Priority < 1 || NewTask.Priority > 10) { NewTaskError = "Приоритет от 1 до 10."; return; }

			AddTaskInternal(NewTask.Title, NewTask.Description ?? "", NewTask.Priority, NewTask.DueDate ?? "");
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
		}

		private void AddRandom(int count)
		{
			var rand = new Random();
			var titles = new[]
			{
				"Проверить почту", "Совещание", "Сделать отчёт", "Позвонить клиенту",
				"Купить подарок", "Записаться к врачу", "Починить баг", "Написать тесты",
				"Обновить документацию", "Проверить код-ревью", "Встретить коллегу",
				"Подумать над архитектурой", "Съездить в офис", "Разобрать завалы",
				"Изучить новую библиотеку"
			};

			for (int i = 0; i < count; i++)
			{
				int priority = rand.Next(1, 11);
				string title = titles[rand.Next(titles.Length)];
				string due = rand.Next(2) == 0
					? DateTime.Today.AddDays(rand.Next(-5, 30)).ToString("yyyy-MM-dd")
					: "";
				AddTaskInternal(title, "", priority, due);
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
			RefreshTrees();
			OnPropertyChanged(nameof(BstInfo));
			OnPropertyChanged(nameof(AvlInfo));
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
			// Читаем из BST (master); id одинаковы в обоих деревьях
			const int max = 10000;
			var buffer = new CTask[max];
			int n = NativeMethods.Lab02_GetAllTasks(_bstHandle, buffer, max);

			// Синхронизируем коллекцию
			var newIds = new HashSet<int>();
			for (int i = 0; i < n; i++) newIds.Add(buffer[i].Id);

			// Удаляем отсутствующие
			for (int i = Tasks.Count - 1; i >= 0; i--)
			{
				if (!newIds.Contains(Tasks[i].Id))
					Tasks.RemoveAt(i);
			}

			var existing = Tasks.ToDictionary(t => t.Id);

			// Добавляем/обновляем
			for (int i = 0; i < n; i++)
			{
				var ct = buffer[i];
				if (existing.TryGetValue(ct.Id, out var current))
				{
					current.Title = ct.Title;
					current.Description = ct.Description;
					current.Priority = ct.Priority;
					current.DueDate = ct.DueDate;
					current.Status = (TaskStatus)ct.Status;
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
						Status = (TaskStatus)ct.Status
					});
				}
			}

			ApplyFilter();
			RefreshTrees();
			OnPropertyChanged(nameof(BstInfo));
			OnPropertyChanged(nameof(AvlInfo));
		}

		private void ApplyFilter()
		{
			FilteredTasks.Clear();
			IEnumerable<TaskItem> source = Tasks;

			switch (_selectedFilter)
			{
				case TaskFilter.Pending:
					source = source.Where(t => t.Status == TaskStatus.Pending); break;
				case TaskFilter.InProgress:
					source = source.Where(t => t.Status == TaskStatus.InProgress); break;
				case TaskFilter.Done:
					source = source.Where(t => t.Status == TaskStatus.Done); break;
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

			if (n == 0)
			{
				result.Width = 400;
				result.Height = 200;
				return result;
			}

			// Собираем узлы и дочерние связи
			var nodes = new List<TreeNodeView>(n);
			var children = new List<int>[n];
			int rootIdx = -1;

			for (int i = 0; i < n; i++)
			{
				children[i] = new List<int>();
				nodes.Add(new TreeNodeView
				{
					Index = i,
					Priority = buffer[i].Priority,
					ParentIndex = buffer[i].ParentIndex,
					IsLeft = buffer[i].IsLeft
				});
			}

			for (int i = 0; i < n; i++)
			{
				if (buffer[i].ParentIndex >= 0)
					children[buffer[i].ParentIndex].Add(i);
				else
					rootIdx = i;
			}

			if (rootIdx < 0) return result;

			// InOrder обход → X-координаты, depth → Y-координаты
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

			const double spacingX = 56;
			const double spacingY = 72;
			const double pad = 30;

			for (int i = 0; i < n; i++)
			{
				nodes[i].X = positions[i].X * spacingX + pad;
				nodes[i].Y = positions[i].Y * spacingY + pad;
			}

			// Рёбра
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
			result.Height = (nodes.Max(nd => nd.Y) + 20 + pad);
			return result;
		}
	}
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;
using MaterialDesignThemes.Wpf;
using StructLab.Models.Lab01;
using StructLab.Reading.ViewModels;
using StructLab.Reading.Views;
using StructLab.Services;

namespace StructLab.ViewModels.Lab01
{
	public class Lab01ViewModel : ViewModelBase
	{
		// ============================================================
		// ПАГИНАЦИЯ
		// ============================================================
		private const int PageSize = 10;
		private int _currentPage = 1;

		public event EventHandler<PageChangedEventArgs> PageChanged;
		public event EventHandler<BookAddedEventArgs> BookAdded;
		public event EventHandler<BookDeletingEventArgs> BookDeleting;
		public event EventHandler<MessageShownEventArgs> MessageShown;
		public event EventHandler<LoanReturnedEventArgs> LoanReturned;

		public int CurrentPage
		{
			get => _currentPage;
			set
			{
				int oldPage = _currentPage;
				if (SetProperty(ref _currentPage, value))
				{
					UpdatePagedBooks();
					CommandManager.InvalidateRequerySuggested();
					PageChanged?.Invoke(this, new PageChangedEventArgs(oldPage, _currentPage));
				}
			}
		}

		public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)Books.Count / PageSize));
		public string PageInfo => $"Страница {CurrentPage} из {TotalPages}";
		public string TotalBooksInfo => $"Всего книг: {Books.Count}";

		private bool _isBusy;
		public bool IsBusy
		{
			get => _isBusy;
			set => SetProperty(ref _isBusy, value);
		}

		public double ShelfFillPercentage
		{
			get
			{
				int onShelf = Books.Sum(b => Math.Max(0, b.Copies));
				int activeLoans = Loans.Count(l => !l.IsReturned);
				int total = onShelf + activeLoans;
				if (total <= 0) return 0;
				return 100.0 * onShelf / total;
			}
		}

		// Прогресс-бар показывается только для больших баз и когда есть выдачи
		public bool IsShelfProgressVisible => Books.Count >= 25 && ShelfFillPercentage < 100.0;

		public ObservableCollection<Book> PagedBooks { get; } = new ObservableCollection<Book>();
		public ObservableCollection<Book> Books { get; } = new ObservableCollection<Book>();

		public RelayCommand NextPageCommand { get; }
		public RelayCommand PrevPageCommand { get; }

		// ============================================================
		// КНИГИ
		// ============================================================
		private IntPtr _listHandle;
		private readonly string _dataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lab01_books.txt");
		private int _nextBookId = 1;

		private bool _isDoubly;
		public bool IsDoubly
		{
			get => _isDoubly;
			set
			{
				if (SetProperty(ref _isDoubly, value))
				{
					OnPropertyChanged(nameof(ListTypeLabel));
					SwitchListType();
				}
			}
		}
		public string ListTypeLabel => _isDoubly ? "Двусвязный" : "Односвязный";

		private Book _selectedBook;
		public Book SelectedBook { get => _selectedBook; set => SetProperty(ref _selectedBook, value); }

		// ============================================================
		// ЧИТАТЕЛИ
		// ============================================================
		private IntPtr _readerListHandle;
		private readonly string _readerFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lab01_readers.txt");
		private int _nextReaderId = 1;

		private Reader _selectedReader;
		public Reader SelectedReader { get => _selectedReader; set => SetProperty(ref _selectedReader, value); }

		public ObservableCollection<Reader> Readers { get; } = new ObservableCollection<Reader>();

		// ============================================================
		// ВЫДАЧИ
		// ============================================================
		private IntPtr _loanListHandle;
		private readonly string _loanFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lab01_loans.txt");
		private int _nextLoanId = 1;
		private bool _showOnlyOverdue;

		public ObservableCollection<Loan> Loans { get; } = new ObservableCollection<Loan>();
		public ObservableCollection<Loan> FilteredLoans { get; } = new ObservableCollection<Loan>();

		public bool ShowOnlyOverdue
		{
			get => _showOnlyOverdue;
			set
			{
				if (SetProperty(ref _showOnlyOverdue, value))
				{
					UpdateFilteredLoans();
				}
			}
		}

		public int OverdueCount => Loans.Count(l => l.IsOverdue);
		public string OverdueCountInfo => $"Должников: {OverdueCount}";
		public string TotalLoansInfo => $"Всего выдач: {Loans.Count}";

		private Loan _selectedLoan;
		public Loan SelectedLoan { get => _selectedLoan; set => SetProperty(ref _selectedLoan, value); }

		// ============================================================
		// ДИАЛОГИ
		// ============================================================
		private bool _isAddDialogOpen;
		private bool _isRequestDialogOpen;
		private bool _isMessageDialogOpen;
		private bool _isAddReaderDialogOpen;
		private bool _isIssueDialogOpen;
		private bool _isBookDetailsDialogOpen;
		private Book _detailsBook;

		public bool IsAddDialogOpen
		{
			get => _isAddDialogOpen;
			set { if (SetProperty(ref _isAddDialogOpen, value)) OnPropertyChanged(nameof(IsAnyDialogOpen)); }
		}
		public bool IsRequestDialogOpen
		{
			get => _isRequestDialogOpen;
			set { if (SetProperty(ref _isRequestDialogOpen, value)) OnPropertyChanged(nameof(IsAnyDialogOpen)); }
		}
		public bool IsMessageDialogOpen
		{
			get => _isMessageDialogOpen;
			set { if (SetProperty(ref _isMessageDialogOpen, value)) OnPropertyChanged(nameof(IsAnyDialogOpen)); }
		}
		public bool IsAddReaderDialogOpen
		{
			get => _isAddReaderDialogOpen;
			set { if (SetProperty(ref _isAddReaderDialogOpen, value)) OnPropertyChanged(nameof(IsAnyDialogOpen)); }
		}
		public bool IsIssueDialogOpen
		{
			get => _isIssueDialogOpen;
			set { if (SetProperty(ref _isIssueDialogOpen, value)) OnPropertyChanged(nameof(IsAnyDialogOpen)); }
		}
		public bool IsBookDetailsDialogOpen
		{
			get => _isBookDetailsDialogOpen;
			set { if (SetProperty(ref _isBookDetailsDialogOpen, value)) OnPropertyChanged(nameof(IsAnyDialogOpen)); }
		}
		public Book DetailsBook
		{
			get => _detailsBook;
			private set => SetProperty(ref _detailsBook, value);
		}

		public void ShowBookDetails(Book book)
		{
			if (book == null) return;
			DetailsBook = book;
			IsBookDetailsDialogOpen = true;
		}

		public bool IsAnyDialogOpen
		{
			get => _isAddDialogOpen || _isRequestDialogOpen || _isMessageDialogOpen ||
				   _isAddReaderDialogOpen || _isIssueDialogOpen || _isBookDetailsDialogOpen;
			set
			{
				if (!value)
				{
					IsAddDialogOpen = false;
					IsRequestDialogOpen = false;
					IsMessageDialogOpen = false;
					IsAddReaderDialogOpen = false;
					IsIssueDialogOpen = false;
					IsBookDetailsDialogOpen = false;
				}
			}
		}

		// ============================================================
		// СООБЩЕНИЕ
		// ============================================================
		private string _messageTitle;
		private string _messageText;
		private PackIconKind _messageIcon;

		public string MessageTitle { get => _messageTitle; set => SetProperty(ref _messageTitle, value); }
		public string MessageText { get => _messageText; set => SetProperty(ref _messageText, value); }
		public PackIconKind MessageIcon { get => _messageIcon; set => SetProperty(ref _messageIcon, value); }

		// ============================================================
		// ДОБАВЛЕНИЕ КНИГИ
		// ============================================================
		private Book _newBook = new Book();
		private string _newBookError;
		private string _targetTitleForInsert;
		private InsertMode _currentInsertMode;

		public Book NewBook { get => _newBook; set => SetProperty(ref _newBook, value); }
		public string NewBookError { get => _newBookError; set => SetProperty(ref _newBookError, value); }
		public string TargetTitleForInsert { get => _targetTitleForInsert; set => SetProperty(ref _targetTitleForInsert, value); }

		// ============================================================
		// ЗАЯВКА
		// ============================================================
		private string _requestGenre;
		private int _requestPages;
		public string RequestGenre { get => _requestGenre; set => SetProperty(ref _requestGenre, value); }
		public int RequestPages { get => _requestPages; set => SetProperty(ref _requestPages, value); }

		// ============================================================
		// ЧИТАТЕЛЬ
		// ============================================================
		private Reader _newReader = new Reader();
		private string _newReaderError;
		public Reader NewReader { get => _newReader; set => SetProperty(ref _newReader, value); }
		public string NewReaderError { get => _newReaderError; set => SetProperty(ref _newReaderError, value); }

		// ============================================================
		// ВЫДАЧА
		// ============================================================
		private Book _issueSelectedBook;
		private Reader _issueSelectedReader;
		private string _issueError;
		public Book IssueSelectedBook { get => _issueSelectedBook; set => SetProperty(ref _issueSelectedBook, value); }
		public Reader IssueSelectedReader { get => _issueSelectedReader; set => SetProperty(ref _issueSelectedReader, value); }
		public string IssueError { get => _issueError; set => SetProperty(ref _issueError, value); }

		public enum InsertMode { Before, After, Sorted, End }

		// ============================================================
		// КОМАНДЫ
		// ============================================================
		public RelayCommand OpenAddDialogCommand { get; }
		public RelayCommand ConfirmAddCommand { get; }
		public RelayCommand CancelAddCommand { get; }
		public RelayCommand DeleteSelectedCommand { get; }
		public RelayCommand AddSortedCommand { get; }
		public RelayCommand ClearAllCommand { get; }
		public RelayCommand OpenRequestDialogCommand { get; }
		public RelayCommand ConfirmRequestCommand { get; }
		public RelayCommand CancelRequestCommand { get; }

		public RelayCommand OpenAddReaderDialogCommand { get; }
		public RelayCommand ConfirmAddReaderCommand { get; }
		public RelayCommand CancelAddReaderCommand { get; }
		public RelayCommand DeleteSelectedReaderCommand { get; }

		public RelayCommand OpenIssueDialogCommand { get; }
		public RelayCommand ConfirmIssueCommand { get; }
		public RelayCommand CancelIssueCommand { get; }
		public RelayCommand ReturnLoanCommand { get; }
		public RelayCommand DeleteLoanCommand { get; }
		public RelayCommand OpenLibraryCommand { get; }

		// ============================================================
		// КОНСТРУКТОР
		// ============================================================
		public Lab01ViewModel()
		{
			// 1. Книги
			_listHandle = NativeMethods.Lab01_CreateList(0);
			if (!NativeMethods.Lab01_LoadFromFile(_listHandle, _dataFilePath))
			{
				SeedDemoData();
				NativeMethods.Lab01_SaveToFile(_listHandle, _dataFilePath);
			}
			RefreshBooks();

			// 2. Читатели
			_readerListHandle = NativeMethods.Lab01_Reader_CreateList();
			if (!NativeMethods.Lab01_Reader_LoadFromFile(_readerListHandle, _readerFilePath))
			{
				AddReaderInternal(new Reader { FullName = "Иванов Иван Иванович", Phone = "+7 999 111-22-33", TicketNumber = "ЧБ-001", Address = "ул. Ленина, д. 1" }, false);
				AddReaderInternal(new Reader { FullName = "Петрова Анна Сергеевна", Phone = "+7 999 444-55-66", TicketNumber = "ЧБ-002", Address = "ул. Мира, д. 10" }, false);
				NativeMethods.Lab01_Reader_SaveToFile(_readerListHandle, _readerFilePath);
			}
			RefreshReaders();

			// 3. Выдачи
			_loanListHandle = NativeMethods.Lab01_Loan_CreateList();
			NativeMethods.Lab01_Loan_LoadFromFile(_loanListHandle, _loanFilePath);
			RefreshLoans();

			// 4. Команды книг
			OpenAddDialogCommand = new RelayCommand(_ => OpenAddDialog(InsertMode.End));
			ConfirmAddCommand = new RelayCommand(_ => ConfirmAdd(), _ => NewBook != null);
			CancelAddCommand = new RelayCommand(_ => IsAddDialogOpen = false);
			DeleteSelectedCommand = new RelayCommand(_ => DeleteSelected(), _ => SelectedBook != null);
			AddSortedCommand = new RelayCommand(_ => OpenAddDialog(InsertMode.Sorted));
			ClearAllCommand = new RelayCommand(_ => ClearAll(), _ => Books.Count > 0);
			OpenRequestDialogCommand = new RelayCommand(_ => OpenRequestDialog());
			ConfirmRequestCommand = new RelayCommand(_ => ConfirmRequest());
			CancelRequestCommand = new RelayCommand(_ => IsRequestDialogOpen = false);

			// 5. Команды читателей
			OpenAddReaderDialogCommand = new RelayCommand(_ =>
			{
				NewReader = new Reader();
				NewReaderError = null;
				IsAddReaderDialogOpen = true;
			});
			ConfirmAddReaderCommand = new RelayCommand(_ => ConfirmAddReader());
			CancelAddReaderCommand = new RelayCommand(_ => IsAddReaderDialogOpen = false);
			DeleteSelectedReaderCommand = new RelayCommand(_ => DeleteSelectedReader(), _ => SelectedReader != null);

			// 6. Команды выдач
			OpenIssueDialogCommand = new RelayCommand(_ =>
			{
				var availableBooks = Books.Where(b => b.Copies > 0).ToList();
				if (availableBooks.Count == 0)
				{
					ShowMessage("Нет доступных книг",
						"Все экземпляры на руках. Добавьте книги или дождитесь возврата.",
						PackIconKind.AlertCircleOutline);
					return;
				}
				if (Readers.Count == 0)
				{
					ShowMessage("Нет читателей",
						"Сначала добавьте хотя бы одного читателя.",
						PackIconKind.AlertCircleOutline);
					return;
				}
				IssueSelectedBook = availableBooks.First();
				IssueSelectedReader = Readers.First();
				IssueError = null;
				IsIssueDialogOpen = true;
			});
			ConfirmIssueCommand = new RelayCommand(_ => ConfirmIssue());
			CancelIssueCommand = new RelayCommand(_ => IsIssueDialogOpen = false);
			ReturnLoanCommand = new RelayCommand(_ => ReturnLoan(), _ => SelectedLoan != null && !SelectedLoan.IsReturned);
			DeleteLoanCommand = new RelayCommand(_ => DeleteLoan(), _ => SelectedLoan != null);

			// 7. Библиотека (читалка)
			OpenLibraryCommand = new RelayCommand(_ =>
			{
				var window = new ReaderWindow
				{
					DataContext = StructLab.App.GetService<ReaderViewModel>(),
					Owner = Application.Current != null ? Application.Current.MainWindow : null
				};
				window.ShowDialog();
			});

			// 8. Пагинация
			NextPageCommand = new RelayCommand(_ => CurrentPage++, _ => CurrentPage < TotalPages);
			PrevPageCommand = new RelayCommand(_ => CurrentPage--, _ => CurrentPage > 1);
		}

		// ============================================================
		// ПАГИНАЦИЯ
		// ============================================================
		private void UpdatePagedBooks()
		{
			var page = new List<Book>();
			int start = (CurrentPage - 1) * PageSize;
			int end = Math.Min(start + PageSize, Books.Count);
			for (int i = start; i < end; i++)
				page.Add(Books[i]);

			Reconcile(PagedBooks, page, b => b.Id);
		}

		/// <summary>
		/// Синхронизирует ObservableCollection со снапшотом по ключу:
		/// сохраняет существующие объекты (и строки UI), добавляет/удаляет/переставляет.
		/// </summary>
		private static void Reconcile<T>(ObservableCollection<T> target, IList<T> source,
			Func<T, int> key, Action<T, T> update = null)
		{
			var sourceKeys = new HashSet<int>(source.Select(key));

			for (int i = target.Count - 1; i >= 0; i--)
			{
				if (!sourceKeys.Contains(key(target[i])))
					target.RemoveAt(i);
			}

			var byKey = target.ToDictionary(key);
			for (int i = 0; i < source.Count; i++)
			{
				T desired = source[i];
				T current;
				if (byKey.TryGetValue(key(desired), out current))
				{
					if (update != null) update(current, desired);
					int currentIndex = target.IndexOf(current);
					if (currentIndex != i) target.Move(currentIndex, i);
				}
				else
				{
					target.Insert(i, desired);
					byKey[key(desired)] = desired;
				}
			}
		}

		// ============================================================
		// КНИГИ: СИНХРОНИЗАЦИЯ
		// ============================================================
		private CBook ToCBook(Book b) => new CBook
		{
			Id = b.Id,
			Author = b.Author ?? "",
			Title = b.Title ?? "",
			Genre = b.Genre ?? "",
			Year = b.Year,
			Pages = b.Pages,
			Copies = b.Copies
		};

		private void RefreshBooks()
		{
			var snapshot = new List<Book>();
			int count = NativeMethods.Lab01_GetCount(_listHandle);
			for (int i = 0; i < count; i++)
			{
				CBook cb;
				if (NativeMethods.Lab01_GetAt(_listHandle, i, out cb))
				{
					snapshot.Add(new Book
					{
						Id = cb.Id,
						Author = cb.Author,
						Title = cb.Title,
						Genre = cb.Genre,
						Year = cb.Year,
						Pages = cb.Pages,
						Copies = cb.Copies
					});
				}
			}

			Reconcile(Books, snapshot, b => b.Id, (target, src) => target.CopyFrom(src));

			_nextBookId = Books.Count > 0 ? Books.Max(b => b.Id) + 1 : 1;

			if (_currentPage > TotalPages) _currentPage = TotalPages;
			if (_currentPage < 1) _currentPage = 1;

			UpdatePagedBooks();
			OnPropertyChanged(nameof(CurrentPage));
			OnPropertyChanged(nameof(TotalPages));
			OnPropertyChanged(nameof(PageInfo));
			OnPropertyChanged(nameof(TotalBooksInfo));
			OnPropertyChanged(nameof(ShelfFillPercentage));
			OnPropertyChanged(nameof(IsShelfProgressVisible));
			CommandManager.InvalidateRequerySuggested();

			if (_loanListHandle != IntPtr.Zero) RefreshLoans();
		}

		private void SaveData() => NativeMethods.Lab01_SaveToFile(_listHandle, _dataFilePath);

		private bool _isSwitchingList;

		private async void SwitchListType()
		{
			if (_isSwitchingList) return;
			_isSwitchingList = true;

			Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

			try
			{
				IsBusy = true;
				await Task.Delay(140).ConfigureAwait(false);

				await RunOnDispatcherAsync(dispatcher, () =>
				{
					if (_listHandle != IntPtr.Zero)
					{
						NativeMethods.Lab01_SaveToFile(_listHandle, _dataFilePath);
						NativeMethods.Lab01_DestroyList(_listHandle);
					}
					_listHandle = NativeMethods.Lab01_CreateList(_isDoubly ? 1 : 0);
					NativeMethods.Lab01_LoadFromFile(_listHandle, _dataFilePath);
					_currentPage = 1;
					RefreshBooks();
				});
			}
			finally
			{
				if (dispatcher.CheckAccess())
				{
					IsBusy = false;
					_isSwitchingList = false;
				}
				else
				{
					try
					{
						await RunOnDispatcherAsync(dispatcher, () =>
						{
							IsBusy = false;
							_isSwitchingList = false;
						});
					}
					catch (TaskCanceledException)
					{
						_isSwitchingList = false;
					}
				}
			}
		}

		private static Task RunOnDispatcherAsync(Dispatcher dispatcher, Action action)
		{
			if (dispatcher.CheckAccess())
			{
				action();
				return Task.FromResult(true);
			}
			return dispatcher.InvokeAsync(action).Task;
		}

		private void SeedDemoData()
		{
			AddBookInternal(new Book { Author = "Л. Н. Толстой", Title = "Война и мир", Genre = "Роман", Year = 1869, Pages = 1225, Copies = 3 }, false);
			AddBookInternal(new Book { Author = "Ф. М. Достоевский", Title = "Преступление и наказание", Genre = "Роман", Year = 1866, Pages = 671, Copies = 2 }, false);
			AddBookInternal(new Book { Author = "А. С. Пушкин", Title = "Евгений Онегин", Genre = "Поэма", Year = 1833, Pages = 224, Copies = 5 }, false);
			AddBookInternal(new Book { Author = "М. А. Булгаков", Title = "Мастер и Маргарита", Genre = "Роман", Year = 1967, Pages = 480, Copies = 1 }, false);
			AddBookInternal(new Book { Author = "А. П. Чехов", Title = "Вишнёвый сад", Genre = "Пьеса", Year = 1904, Pages = 96, Copies = 4 }, false);
			RefreshBooks();
		}

		private void OpenAddDialog(InsertMode mode)
		{
			IsRequestDialogOpen = false;
			IsAddReaderDialogOpen = false;
			IsIssueDialogOpen = false;
			_currentInsertMode = mode;
			NewBook = new Book { Year = 2024, Pages = 300, Copies = 1 };
			NewBookError = null;
			TargetTitleForInsert = SelectedBook != null ? SelectedBook.Title : "";
			IsAddDialogOpen = true;
		}

		private void ConfirmAdd()
		{
			if (string.IsNullOrWhiteSpace(NewBook.Author)) { NewBookError = "Укажите автора."; return; }
			if (string.IsNullOrWhiteSpace(NewBook.Title)) { NewBookError = "Укажите название."; return; }
			if (string.IsNullOrWhiteSpace(NewBook.Genre)) { NewBookError = "Укажите жанр."; return; }
			if (NewBook.Year < 1000 || NewBook.Year > 2100) { NewBookError = "Год от 1000 до 2100."; return; }

			NewBook.Id = _nextBookId;
			CBook cb = ToCBook(NewBook);

			if (_currentInsertMode == InsertMode.Sorted)
				NativeMethods.Lab01_AddSorted(_listHandle, cb);
			else if (_currentInsertMode == InsertMode.Before)
			{
				if (!NativeMethods.Lab01_AddBefore(_listHandle, TargetTitleForInsert, cb))
				{ NewBookError = "Книга не найдена."; return; }
			}
			else if (_currentInsertMode == InsertMode.After && !string.IsNullOrEmpty(TargetTitleForInsert))
			{
				if (!NativeMethods.Lab01_AddAfter(_listHandle, TargetTitleForInsert, cb))
				{ NewBookError = "Книга не найдена."; return; }
			}
			else
				NativeMethods.Lab01_AddLast(_listHandle, cb);

			IsAddDialogOpen = false;
			RefreshBooks();
			SaveData();

			var added = Books.FirstOrDefault(b => b.Id == NewBook.Id);
			if (added != null)
			{
				CurrentPage = Books.IndexOf(added) / PageSize + 1;
				added.IsNew = true;
				BookAdded?.Invoke(this, new BookAddedEventArgs(added));
				_ = ClearNewFlagAsync(added);
			}
			else
			{
				CurrentPage = TotalPages;
			}
		}

		private async Task ClearNewFlagAsync(Book book)
		{
			Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
			await Task.Delay(1600).ConfigureAwait(false);
			await RunOnDispatcherAsync(dispatcher, () => book.IsNew = false);
		}

		private void AddBookInternal(Book book, bool refresh)
		{
			book.Id = _nextBookId++;
			NativeMethods.Lab01_AddLast(_listHandle, ToCBook(book));
			if (refresh) { RefreshBooks(); SaveData(); }
		}

		private bool _isDeleting;

		private async void DeleteSelected()
		{
			if (_isDeleting || SelectedBook == null) return;
			_isDeleting = true;

			Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

			try
			{
				Book book = SelectedBook;

				var handler = BookDeleting;
				if (handler != null)
				{
					var args = new BookDeletingEventArgs(book);
					handler(this, args);
					await Task.WhenAny(args.Completion.Task, Task.Delay(900)).ConfigureAwait(false);
				}

				await RunOnDispatcherAsync(dispatcher, () =>
				{
					NativeMethods.Lab01_DeleteByTitle(_listHandle, book.Title);
					SelectedBook = null;
					RefreshBooks();
					SaveData();
				});
			}
			finally
			{
				if (dispatcher.CheckAccess())
				{
					_isDeleting = false;
				}
				else
				{
					try
					{
						await RunOnDispatcherAsync(dispatcher, () => _isDeleting = false);
					}
					catch (TaskCanceledException)
					{
						_isDeleting = false;
					}
				}
			}
		}

		private void ClearAll()
		{
			NativeMethods.Lab01_Clear(_listHandle);
			RefreshBooks();
			SaveData();
		}

		private void OpenRequestDialog()
		{
			IsAddDialogOpen = false;
			IsAddReaderDialogOpen = false;
			IsIssueDialogOpen = false;
			RequestGenre = "Роман";
			RequestPages = 500;
			IsRequestDialogOpen = true;
		}

		private void ConfirmRequest()
		{
			if (string.IsNullOrWhiteSpace(RequestGenre)) return;
			int index = NativeMethods.Lab01_FindForRequest(_listHandle, RequestGenre, RequestPages);

			if (index >= 0)
			{
				CBook foundBook;
				NativeMethods.Lab01_GetAt(_listHandle, index, out foundBook);
				NativeMethods.Lab01_DeleteByTitle(_listHandle, foundBook.Title);
				RefreshBooks();
				SaveData();
				ShowMessage("Заявка удовлетворена",
					$"Читателю выдана книга:\n\n«{foundBook.Title}» — {foundBook.Author}\n" +
					$"Жанр: {foundBook.Genre}, Страниц: {foundBook.Pages}\n\n" +
					"Книга списана из картотеки.",
					PackIconKind.CheckCircleOutline);
			}
			else
			{
				var requestBook = new Book
				{
					Author = "Заявка читателя",
					Title = $"Ожидание: {RequestGenre}, ~{RequestPages} стр.",
					Genre = RequestGenre,
					Year = DateTime.Now.Year,
					Pages = RequestPages,
					Copies = 0
				};
				AddBookInternal(requestBook, false);
				RefreshBooks();
				SaveData();
				ShowMessage("Заявка оформлена",
					"Подходящей книги не найдено.\n\nЗаявка добавлена в картотеку ожидания.",
					PackIconKind.AlertCircleOutline);
			}
		}

		private void ShowMessage(string title, string text, PackIconKind icon)
		{
			MessageTitle = title;
			MessageText = text;
			MessageIcon = icon;
			IsMessageDialogOpen = true;
			MessageShown?.Invoke(this, new MessageShownEventArgs(icon, title));
		}

		// ============================================================
		// ЧИТАТЕЛИ
		// ============================================================
		private CReader ToCReader(Reader r) => new CReader
		{
			Id = r.Id,
			FullName = r.FullName ?? "",
			Phone = r.Phone ?? "",
			TicketNumber = r.TicketNumber ?? "",
			Address = r.Address ?? ""
		};

		private void RefreshReaders()
		{
			var snapshot = new List<Reader>();
			int count = NativeMethods.Lab01_Reader_GetCount(_readerListHandle);
			for (int i = 0; i < count; i++)
			{
				CReader cr;
				if (NativeMethods.Lab01_Reader_GetAt(_readerListHandle, i, out cr))
				{
					snapshot.Add(new Reader
					{
						Id = cr.Id,
						FullName = cr.FullName,
						Phone = cr.Phone,
						TicketNumber = cr.TicketNumber,
						Address = cr.Address
					});
				}
			}

			Reconcile(Readers, snapshot, r => r.Id, (target, src) => target.CopyFrom(src));
			_nextReaderId = Readers.Count > 0 ? Readers.Max(r => r.Id) + 1 : 1;

			if (_loanListHandle != IntPtr.Zero) RefreshLoans();
		}

		private void AddReaderInternal(Reader r, bool refresh)
		{
			r.Id = _nextReaderId++;
			NativeMethods.Lab01_Reader_Add(_readerListHandle, ToCReader(r));
			if (refresh)
			{
				RefreshReaders();
				NativeMethods.Lab01_Reader_SaveToFile(_readerListHandle, _readerFilePath);
			}
		}

		private void ConfirmAddReader()
		{
			if (string.IsNullOrWhiteSpace(NewReader.FullName)) { NewReaderError = "Укажите ФИО."; return; }
			if (string.IsNullOrWhiteSpace(NewReader.TicketNumber)) { NewReaderError = "Укажите номер читательского билета."; return; }

			AddReaderInternal(NewReader, true);
			IsAddReaderDialogOpen = false;
		}

		private void DeleteSelectedReader()
		{
			if (SelectedReader == null) return;
			NativeMethods.Lab01_Reader_DeleteById(_readerListHandle, SelectedReader.Id);
			SelectedReader = null;
			RefreshReaders();
			NativeMethods.Lab01_Reader_SaveToFile(_readerListHandle, _readerFilePath);
		}

		// ============================================================
		// ВЫДАЧИ
		// ============================================================
		private CLoan ToCLoan(Loan l) => new CLoan
		{
			Id = l.Id,
			BookId = l.BookId,
			ReaderId = l.ReaderId,
			IssueDate = l.IssueDate ?? "",
			DueDate = l.DueDate ?? "",
			ReturnDate = l.ReturnDate ?? ""
		};

		private void RefreshLoans()
		{
			var snapshot = new List<Loan>();
			int count = NativeMethods.Lab01_Loan_GetCount(_loanListHandle);
			for (int i = 0; i < count; i++)
			{
				CLoan cl;
				if (NativeMethods.Lab01_Loan_GetAt(_loanListHandle, i, out cl))
				{
					var book = Books.FirstOrDefault(b => b.Id == cl.BookId);
					var reader = Readers.FirstOrDefault(r => r.Id == cl.ReaderId);

					snapshot.Add(new Loan
					{
						Id = cl.Id,
						BookId = cl.BookId,
						ReaderId = cl.ReaderId,
						IssueDate = cl.IssueDate,
						DueDate = cl.DueDate,
						ReturnDate = cl.ReturnDate,
						BookTitle = book != null ? book.Title : $"<книга #{cl.BookId} удалена>",
						ReaderName = reader != null ? reader.FullName : $"<читатель #{cl.ReaderId} удалён>"
					});
				}
			}

			Reconcile(Loans, snapshot, l => l.Id, (target, src) => target.CopyFrom(src));
			_nextLoanId = Loans.Count > 0 ? Loans.Max(l => l.Id) + 1 : 1;
			UpdateFilteredLoans();
			OnPropertyChanged(nameof(OverdueCount));
			OnPropertyChanged(nameof(OverdueCountInfo));
			OnPropertyChanged(nameof(TotalLoansInfo));
			OnPropertyChanged(nameof(ShelfFillPercentage));
			OnPropertyChanged(nameof(IsShelfProgressVisible));
		}

		private void UpdateFilteredLoans()
		{
			IEnumerable<Loan> query = _showOnlyOverdue
				? (IEnumerable<Loan>)Loans.Where(l => l.IsOverdue).OrderBy(l => l.DueDate, StringComparer.Ordinal)
				: Loans;

			Reconcile(FilteredLoans, query.ToList(), l => l.Id);
			CommandManager.InvalidateRequerySuggested();
		}

		private void SaveLoans() => NativeMethods.Lab01_Loan_SaveToFile(_loanListHandle, _loanFilePath);

		private void ConfirmIssue()
		{
			if (IssueSelectedBook == null) { IssueError = "Выберите книгу."; return; }
			if (IssueSelectedReader == null) { IssueError = "Выберите читателя."; return; }
			if (IssueSelectedBook.Copies <= 0) { IssueError = "Экземпляры закончились."; return; }

			var today = DateTime.Today;
			var due = today.AddDays(14);

			var loan = new Loan
			{
				Id = _nextLoanId++,
				BookId = IssueSelectedBook.Id,
				ReaderId = IssueSelectedReader.Id,
				IssueDate = today.ToString("yyyy-MM-dd"),
				DueDate = due.ToString("yyyy-MM-dd"),
				ReturnDate = ""
			};

			NativeMethods.Lab01_Loan_Add(_loanListHandle, ToCLoan(loan));
			NativeMethods.Lab01_AdjustCopies(_listHandle, IssueSelectedBook.Id, -1);

			string bookTitle = IssueSelectedBook.Title;
			string readerName = IssueSelectedReader.FullName;

			IsIssueDialogOpen = false;
			RefreshBooks();
			SaveData();
			SaveLoans();
			UpdatePagedBooks();

			ShowMessage("Книга выдана",
				$"«{bookTitle}»\nвыдана читателю:\n{readerName}\n\nСрок возврата: {due:dd.MM.yyyy}",
				PackIconKind.BookCheck);
		}

		private void ReturnLoan()
		{
			if (SelectedLoan == null || SelectedLoan.IsReturned) return;

			Loan loan = SelectedLoan;
			bool wasOverdue = loan.IsOverdue;
			int penalty = wasOverdue ? loan.Penalty : 0;

			var today = DateTime.Today.ToString("yyyy-MM-dd");
			NativeMethods.Lab01_Loan_MarkReturned(_loanListHandle, loan.Id, today);
			NativeMethods.Lab01_AdjustCopies(_listHandle, loan.BookId, +1);

			string title = loan.BookTitle;
			string reader = loan.ReaderName;

			RefreshBooks();
			SaveData();
			SaveLoans();
			UpdatePagedBooks();

			LoanReturned?.Invoke(this, new LoanReturnedEventArgs(loan, wasOverdue, penalty));

			ShowMessage("Книга возвращена",
				$"«{title}»\nвозвращена от:\n{reader}" +
				(penalty > 0 ? $"\n\nНачислен штраф: {penalty} ₽" : ""),
				PackIconKind.CheckCircleOutline);
		}

		private void DeleteLoan()
		{
			if (SelectedLoan == null) return;

			if (!SelectedLoan.IsReturned)
			{
				NativeMethods.Lab01_AdjustCopies(_listHandle, SelectedLoan.BookId, +1);
			}
			NativeMethods.Lab01_Loan_DeleteById(_loanListHandle, SelectedLoan.Id);
			SelectedLoan = null;

			RefreshBooks();
			SaveData();
			SaveLoans();
			UpdatePagedBooks();
		}
	}
}
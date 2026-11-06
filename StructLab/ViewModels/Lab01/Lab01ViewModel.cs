using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using StructLab.Models.Lab01;
using StructLab.Services;

namespace StructLab.ViewModels.Lab01
{
	public class Lab01ViewModel : ViewModelBase
	{
		// ---- Пагинация ----
		private const int PageSize = 10;
		private int _currentPage = 1;

		public int CurrentPage
		{
			get => _currentPage;
			set
			{
				if (SetProperty(ref _currentPage, value))
				{
					UpdatePagedBooks();
					CommandManager.InvalidateRequerySuggested();
				}
			}
		}

		public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)Books.Count / PageSize));
		public string PageInfo => $"Страница {CurrentPage} из {TotalPages}";
		public string TotalBooksInfo => $"Всего книг: {Books.Count}";

		// Коллекция, привязанная к DataGrid (только текущая страница)
		public ObservableCollection<Book> PagedBooks { get; } = new ObservableCollection<Book>();

		public RelayCommand NextPageCommand { get; }
		public RelayCommand PrevPageCommand { get; }

		// ---- Остальное ----
		private IntPtr _listHandle;
		private readonly string _dataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lab01_books.txt");

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
		private bool _isAddDialogOpen;
		private bool _isRequestDialogOpen;
		private bool _isMessageDialogOpen;

		private string _messageTitle;
		private string _messageText;
		private PackIconKind _messageIcon;

		private Book _newBook = new Book();
		private string _newBookError;
		private string _targetTitleForInsert;
		private InsertMode _currentInsertMode;

		private string _requestGenre;
		private int _requestPages;

		public enum InsertMode { Before, After, Sorted, End }

		public ObservableCollection<Book> Books { get; } = new ObservableCollection<Book>();

		public Book SelectedBook { get => _selectedBook; set => SetProperty(ref _selectedBook, value); }

		public bool IsAddDialogOpen { get => _isAddDialogOpen; set { if (SetProperty(ref _isAddDialogOpen, value)) OnPropertyChanged(nameof(IsAnyDialogOpen)); } }
		public bool IsRequestDialogOpen { get => _isRequestDialogOpen; set { if (SetProperty(ref _isRequestDialogOpen, value)) OnPropertyChanged(nameof(IsAnyDialogOpen)); } }
		public bool IsMessageDialogOpen { get => _isMessageDialogOpen; set { if (SetProperty(ref _isMessageDialogOpen, value)) OnPropertyChanged(nameof(IsAnyDialogOpen)); } }

		public bool IsAnyDialogOpen
		{
			get => _isAddDialogOpen || _isRequestDialogOpen || _isMessageDialogOpen;
			set { if (!value) { IsAddDialogOpen = false; IsRequestDialogOpen = false; IsMessageDialogOpen = false; } }
		}

		public string MessageTitle { get => _messageTitle; set => SetProperty(ref _messageTitle, value); }
		public string MessageText { get => _messageText; set => SetProperty(ref _messageText, value); }
		public PackIconKind MessageIcon { get => _messageIcon; set => SetProperty(ref _messageIcon, value); }

		public Book NewBook { get => _newBook; set => SetProperty(ref _newBook, value); }
		public string NewBookError { get => _newBookError; set => SetProperty(ref _newBookError, value); }
		public string TargetTitleForInsert { get => _targetTitleForInsert; set => SetProperty(ref _targetTitleForInsert, value); }

		public string RequestGenre { get => _requestGenre; set => SetProperty(ref _requestGenre, value); }
		public int RequestPages { get => _requestPages; set => SetProperty(ref _requestPages, value); }

		public RelayCommand OpenAddDialogCommand { get; }
		public RelayCommand ConfirmAddCommand { get; }
		public RelayCommand CancelAddCommand { get; }
		public RelayCommand DeleteSelectedCommand { get; }
		public RelayCommand AddSortedCommand { get; }
		public RelayCommand ClearAllCommand { get; }
		public RelayCommand OpenRequestDialogCommand { get; }
		public RelayCommand ConfirmRequestCommand { get; }
		public RelayCommand CancelRequestCommand { get; }

		public Lab01ViewModel()
		{
			_listHandle = NativeMethods.Lab01_CreateList(0);

			if (!NativeMethods.Lab01_LoadFromFile(_listHandle, _dataFilePath))
			{
				SeedDemoData();
				NativeMethods.Lab01_SaveToFile(_listHandle, _dataFilePath);
			}
			RefreshBooks();

			OpenAddDialogCommand = new RelayCommand(_ => OpenAddDialog(InsertMode.End));
			ConfirmAddCommand = new RelayCommand(_ => ConfirmAdd(), _ => NewBook != null);
			CancelAddCommand = new RelayCommand(_ => IsAddDialogOpen = false);
			DeleteSelectedCommand = new RelayCommand(_ => DeleteSelected(), _ => SelectedBook != null);
			AddSortedCommand = new RelayCommand(_ => OpenAddDialog(InsertMode.Sorted));
			ClearAllCommand = new RelayCommand(_ => ClearAll(), _ => Books.Count > 0);
			OpenRequestDialogCommand = new RelayCommand(_ => OpenRequestDialog());
			ConfirmRequestCommand = new RelayCommand(_ => ConfirmRequest());
			CancelRequestCommand = new RelayCommand(_ => IsRequestDialogOpen = false);

			// Пагинация
			NextPageCommand = new RelayCommand(_ => CurrentPage++, _ => CurrentPage < TotalPages);
			PrevPageCommand = new RelayCommand(_ => CurrentPage--, _ => CurrentPage > 1);
		}

		// ---- Пагинация ----
		private void UpdatePagedBooks()
		{
			PagedBooks.Clear();
			int start = (CurrentPage - 1) * PageSize;
			int end = Math.Min(start + PageSize, Books.Count);
			for (int i = start; i < end; i++)
				PagedBooks.Add(Books[i]);
		}

		// ---- Синхронизация с C++ ----
		private void RefreshBooks()
		{
			Books.Clear();
			int count = NativeMethods.Lab01_GetCount(_listHandle);
			for (int i = 0; i < count; i++)
			{
				if (NativeMethods.Lab01_GetAt(_listHandle, i, out CBook cb))
				{
					Books.Add(new Book { Id = cb.Id, Author = cb.Author, Title = cb.Title, Genre = cb.Genre, Year = cb.Year, Pages = cb.Pages, Copies = cb.Copies });
				}
			}

			// Пересчёт страниц + clamp
			if (_currentPage > TotalPages) _currentPage = TotalPages;
			if (_currentPage < 1) _currentPage = 1;

			UpdatePagedBooks();
			OnPropertyChanged(nameof(CurrentPage));
			OnPropertyChanged(nameof(TotalPages));
			OnPropertyChanged(nameof(PageInfo));
			OnPropertyChanged(nameof(TotalBooksInfo));
			CommandManager.InvalidateRequerySuggested();
		}

		private void SwitchListType()
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
		}

		private void ShowMessage(string title, string text, PackIconKind icon)
		{
			MessageTitle = title; MessageText = text; MessageIcon = icon; IsMessageDialogOpen = true;
		}

		private CBook ToCBook(Book b) => new CBook { Id = b.Id, Author = b.Author ?? "", Title = b.Title ?? "", Genre = b.Genre ?? "", Year = b.Year, Pages = b.Pages, Copies = b.Copies };

		private void SaveData() => NativeMethods.Lab01_SaveToFile(_listHandle, _dataFilePath);

		private void SeedDemoData()
		{
			AddBookInternal(new Book { Author = "Л. Н. Толстой", Title = "Война и мир", Genre = "Роман", Year = 1869, Pages = 1225, Copies = 3 }, false);
			AddBookInternal(new Book { Author = "Ф. М. Достоевский", Title = "Преступление и наказание", Genre = "Роман", Year = 1866, Pages = 671, Copies = 2 }, false);
			AddBookInternal(new Book { Author = "А. С. Пушкин", Title = "Евгений Онегин", Genre = "Поэма", Year = 1833, Pages = 224, Copies = 5 }, false);
			RefreshBooks();
		}

		private void OpenAddDialog(InsertMode mode)
		{
			IsRequestDialogOpen = false;
			_currentInsertMode = mode;
			NewBook = new Book { Year = 2024, Pages = 300, Copies = 1 };
			NewBookError = null;
			TargetTitleForInsert = SelectedBook?.Title ?? "";
			IsAddDialogOpen = true;
		}

		private void ConfirmAdd()
		{
			if (string.IsNullOrWhiteSpace(NewBook.Author)) { NewBookError = "Укажите автора."; return; }
			if (string.IsNullOrWhiteSpace(NewBook.Title)) { NewBookError = "Укажите название."; return; }
			if (string.IsNullOrWhiteSpace(NewBook.Genre)) { NewBookError = "Укажите жанр."; return; }
			if (NewBook.Year < 1000 || NewBook.Year > 2100) { NewBookError = "Год от 1000 до 2100."; return; }

			NewBook.Id = NativeMethods.Lab01_GetCount(_listHandle) + 1;
			CBook cb = ToCBook(NewBook);

			if (_currentInsertMode == InsertMode.Sorted)
				NativeMethods.Lab01_AddSorted(_listHandle, cb);
			else if (_currentInsertMode == InsertMode.Before)
			{
				if (!NativeMethods.Lab01_AddBefore(_listHandle, TargetTitleForInsert, cb)) { NewBookError = "Книга не найдена."; return; }
			}
			else if (_currentInsertMode == InsertMode.After && !string.IsNullOrEmpty(TargetTitleForInsert))
			{
				if (!NativeMethods.Lab01_AddAfter(_listHandle, TargetTitleForInsert, cb)) { NewBookError = "Книга не найдена."; return; }
			}
			else
				NativeMethods.Lab01_AddLast(_listHandle, cb);

			IsAddDialogOpen = false;
			RefreshBooks(); SaveData();
		}

		private void AddBookInternal(Book book, bool refresh)
		{
			book.Id = NativeMethods.Lab01_GetCount(_listHandle) + 1;
			NativeMethods.Lab01_AddLast(_listHandle, ToCBook(book));
			if (refresh) { RefreshBooks(); SaveData(); }
		}

		private void DeleteSelected()
		{
			if (SelectedBook == null) return;
			NativeMethods.Lab01_DeleteByTitle(_listHandle, SelectedBook.Title);
			RefreshBooks(); SaveData();
		}

		private void ClearAll()
		{
			NativeMethods.Lab01_Clear(_listHandle);
			RefreshBooks(); SaveData();
		}

		private void OpenRequestDialog()
		{
			IsAddDialogOpen = false;
			RequestGenre = "Роман"; RequestPages = 500;
			IsRequestDialogOpen = true;
		}

		private void ConfirmRequest()
		{
			if (string.IsNullOrWhiteSpace(RequestGenre)) return;
			int index = NativeMethods.Lab01_FindForRequest(_listHandle, RequestGenre, RequestPages);

			if (index >= 0)
			{
				NativeMethods.Lab01_GetAt(_listHandle, index, out CBook foundBook);
				NativeMethods.Lab01_DeleteByTitle(_listHandle, foundBook.Title);
				IsRequestDialogOpen = false; RefreshBooks(); SaveData();
				ShowMessage("Заявка удовлетворена", $"Читателю выдана книга:\n\n«{foundBook.Title}» — {foundBook.Author}\nЖанр: {foundBook.Genre}, Страниц: {foundBook.Pages}\n\nКнига списана из картотеки.", PackIconKind.CheckCircleOutline);
			}
			else
			{
				var requestBook = new Book { Author = "Заявка читателя", Title = $"Ожидание: {RequestGenre}, ~{RequestPages} стр.", Genre = RequestGenre, Year = DateTime.Now.Year, Pages = RequestPages, Copies = 0 };
				AddBookInternal(requestBook, false);
				IsRequestDialogOpen = false; RefreshBooks(); SaveData();
				ShowMessage("Заявка оформлена", "Подходящей книги не найдено.\n\nЗаявка добавлена в картотеку ожидания.", PackIconKind.AlertCircleOutline);
			}
		}
	}
}
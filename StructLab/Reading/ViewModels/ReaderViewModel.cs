using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using StructLab.Reading.Models;
using StructLab.Reading.Services;
using StructLab.ViewModels;

namespace StructLab.Reading.ViewModels
{
	public class ReaderViewModel : ViewModelBase
	{
		private readonly ReadingService _service;
		private readonly ReadingSettings _settings;
		private readonly EpubProvider _epubProvider;
		private readonly EpubLibraryStore _epubLibrary;
		private CancellationTokenSource _currentOp;

		private string _searchQuery = "";
		private bool _isSearching;
		private bool _isLoadingBook;
		private bool _isSearchMode = true;
		private string _statusMessage;
		private string _bookWarning;
		private BookSearchResult _selectedResult;
		private Book _currentBook;
		private Chapter _currentChapter;
		private int _chapterIndex = -1;

		public ObservableCollection<BookSearchResult> SearchResults { get; } = new ObservableCollection<BookSearchResult>();

		/// <summary>История загруженных EPUB-книг (свежие сверху).</summary>
		public ObservableCollection<EpubLibraryEntry> EpubBooks { get; } = new ObservableCollection<EpubLibraryEntry>();

		public bool HasEpubBooks => EpubBooks.Count > 0;
		public bool HasNoEpubBooks => EpubBooks.Count == 0;

		public string SearchQuery
		{
			get => _searchQuery;
			set => SetProperty(ref _searchQuery, value);
		}

		public bool IsSearching
		{
			get => _isSearching;
			private set => SetProperty(ref _isSearching, value);
		}

		public bool IsLoadingBook
		{
			get => _isLoadingBook;
			private set => SetProperty(ref _isLoadingBook, value);
		}

		/// <summary>
		/// true — левая панель показывает поиск, false — оглавление открытой книги.
		/// </summary>
		public bool IsSearchMode
		{
			get => _isSearchMode;
			private set
			{
				if (SetProperty(ref _isSearchMode, value))
					OnPropertyChanged(nameof(IsTocMode));
			}
		}

		public bool IsTocMode => !_isSearchMode;

		public string StatusMessage
		{
			get => _statusMessage;
			private set => SetProperty(ref _statusMessage, value);
		}

		public BookSearchResult SelectedResult
		{
			get => _selectedResult;
			set
			{
				if (SetProperty(ref _selectedResult, value) && value != null)
					_ = LoadBookAsync(value);
			}
		}

		public Book CurrentBook
		{
			get => _currentBook;
			private set
			{
				if (!SetProperty(ref _currentBook, value)) return;

				if (value == null)
				{
					_currentChapter = null;
					_chapterIndex = -1;
					OnPropertyChanged(nameof(CurrentChapter));
				}

				OnPropertyChanged(nameof(HasBook));
				OnPropertyChanged(nameof(HasNoBook));
				OnPropertyChanged(nameof(BookSubtitle));
				OnPropertyChanged(nameof(HasBookSubtitle));
				OnPropertyChanged(nameof(ChapterCountText));
				OnPropertyChanged(nameof(ChapterPositionText));
				OnPropertyChanged(nameof(CanGoPrev));
				OnPropertyChanged(nameof(CanGoNext));
			}
		}

		public bool HasBook => _currentBook != null;
		public bool HasNoBook => _currentBook == null;

		/// <summary>Текущая открытая глава. null, пока книга не загружена.</summary>
		public Chapter CurrentChapter
		{
			get => _currentChapter;
			set
			{
				// ListBox может прислать null при смене ItemsSource — не сбрасываем главу.
				if (value == null) return;
				if (ReferenceEquals(_currentChapter, value)) return;

				_currentChapter = value;

				var index = _currentBook?.Chapters != null
					? _currentBook.Chapters.IndexOf(value)
					: -1;
				_chapterIndex = index >= 0 ? index : value.Index;

				OnPropertyChanged();
				OnPropertyChanged(nameof(ChapterPositionText));
				OnPropertyChanged(nameof(CanGoPrev));
				OnPropertyChanged(nameof(CanGoNext));
			}
		}

		/// <summary>Индекс текущей главы с нуля; -1, если глава не выбрана.</summary>
		public int ChapterIndex => _chapterIndex;

		public bool CanGoPrev => _chapterIndex > 0;

		public bool CanGoNext =>
			_currentBook != null &&
			_currentBook.Chapters != null &&
			_chapterIndex >= 0 &&
			_chapterIndex < _currentBook.Chapters.Count - 1;

		public string ChapterPositionText
		{
			get
			{
				if (_currentBook == null || _currentBook.ChapterCount == 0 || _chapterIndex < 0)
					return "";

				return "Глава " + (_chapterIndex + 1) + " из " + _currentBook.ChapterCount;
			}
		}

		public string ChapterCountText =>
			_currentBook == null ? "" : "Глав: " + _currentBook.ChapterCount;

		/// <summary>Автор · год · лицензия — то, что удалось достать из источника.</summary>
		public string BookSubtitle
		{
			get
			{
				var book = _currentBook;
				if (book == null) return "";

				var parts = new List<string>();

				var author = !string.IsNullOrWhiteSpace(book.Author)
					? book.Author
					: book.Metadata?.Author;
				if (!string.IsNullOrWhiteSpace(author)) parts.Add(author);

				if (!string.IsNullOrWhiteSpace(book.Metadata?.Year))
					parts.Add(book.Metadata.Year);

				if (!string.IsNullOrWhiteSpace(book.Metadata?.License))
					parts.Add(book.Metadata.License);

				return string.Join(" · ", parts);
			}
		}

		public bool HasBookSubtitle => !string.IsNullOrWhiteSpace(BookSubtitle);

		/// <summary>
		/// Текст предупреждения о неполной загрузке книги (например, обрезанный список глав).
		/// null, если предупреждений нет.
		/// </summary>
		public string BookWarning
		{
			get => _bookWarning;
			private set
			{
				if (SetProperty(ref _bookWarning, value))
					OnPropertyChanged(nameof(HasBookWarning));
			}
		}

		public bool HasBookWarning => !string.IsNullOrEmpty(_bookWarning);

		public double FontSize
		{
			get => _settings.FontSize;
			set
			{
				if (Math.Abs(_settings.FontSize - value) < 0.01) return;
				_settings.FontSize = value;
				OnPropertyChanged();
				_settings.Save();
			}
		}

		public string FontFamily
		{
			get => _settings.FontFamily;
			set
			{
				if (_settings.FontFamily == value) return;
				_settings.FontFamily = value;
				OnPropertyChanged();
				_settings.Save();
			}
		}

		public string Theme
		{
			get => _settings.Theme;
			set
			{
				if (_settings.Theme == value) return;
				_settings.Theme = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsThemeLight));
				OnPropertyChanged(nameof(IsThemeSepia));
				OnPropertyChanged(nameof(IsThemeDark));
				_settings.Save();
			}
		}

		public bool IsThemeLight { get => Theme == "Light"; set { if (value) Theme = "Light"; } }
		public bool IsThemeSepia { get => Theme == "Sepia"; set { if (value) Theme = "Sepia"; } }
		public bool IsThemeDark { get => Theme == "Dark"; set { if (value) Theme = "Dark"; } }

		public RelayCommand SearchCommand { get; }
		public RelayCommand CancelCommand { get; }
		public RelayCommand ReloadCommand { get; }
		public RelayCommand PrevChapterCommand { get; }
		public RelayCommand NextChapterCommand { get; }
		public RelayCommand ShowSearchCommand { get; }
		public RelayCommand ShowTocCommand { get; }
		public RelayCommand LoadEpubCommand { get; }
		public RelayCommand OpenEpubCommand { get; }

		public ReaderViewModel()
		{
			_settings = ReadingSettings.Load();
			var provider = new WikisourceProvider();
			var cache = new FileBookCache();
			_service = new ReadingService(provider, cache);
			_epubProvider = new EpubProvider();
			_epubLibrary = new EpubLibraryStore();

			RefreshEpubBooks();

			SearchCommand = new RelayCommand(
				_ => _ = SearchAsync(),
				_ => !string.IsNullOrWhiteSpace(SearchQuery) && !IsSearching);

			CancelCommand = new RelayCommand(
				_ => _currentOp?.Cancel(),
				_ => IsSearching || IsLoadingBook);

			ReloadCommand = new RelayCommand(
				_ => _ = ReloadAsync(),
				_ => CurrentBook != null && !IsLoadingBook);

			PrevChapterCommand = new RelayCommand(
				_ => GoToChapter(_chapterIndex - 1),
				_ => CanGoPrev);

			NextChapterCommand = new RelayCommand(
				_ => GoToChapter(_chapterIndex + 1),
				_ => CanGoNext);

			ShowSearchCommand = new RelayCommand(_ => ShowSearch());

			ShowTocCommand = new RelayCommand(
				_ => IsSearchMode = false,
				_ => HasBook);

			LoadEpubCommand = new RelayCommand(
				_ => _ = LoadEpubFromDialogAsync(),
				_ => !IsLoadingBook);

			OpenEpubCommand = new RelayCommand(
				parameter =>
				{
					var entry = parameter as EpubLibraryEntry;
					if (entry != null) _ = OpenEpubEntryAsync(entry);
				});
		}

		/// <summary>Из карточки книги: подставить «Автор Название» и сразу искать.</summary>
		public void PreloadSearch(string query)
		{
			SearchQuery = query ?? "";
			_ = SearchAsync();
		}

		private void ShowSearch()
		{
			// Сбрасываем выбор, чтобы по тому же результату можно было кликнуть ещё раз.
			SelectedResult = null;
			IsSearchMode = true;
		}

		// ============================================================
		// EPUB
		// ============================================================

		/// <summary>Перечитывает историю EPUB и показывает свежие книги сверху.</summary>
		private void RefreshEpubBooks()
		{
			EpubBooks.Clear();

			foreach (var entry in _epubLibrary.Entries
				.OrderByDescending(e => e.LastOpenedAt)
				.ToList())
			{
				EpubBooks.Add(entry);
			}

			OnPropertyChanged(nameof(HasEpubBooks));
			OnPropertyChanged(nameof(HasNoEpubBooks));
		}

		private async Task LoadEpubFromDialogAsync()
		{
			var dialog = new OpenFileDialog
			{
				Title = "Открыть книгу EPUB",
				Filter = "EPUB (*.epub)|*.epub|Все файлы (*.*)|*.*",
				CheckFileExists = true,
				Multiselect = false
			};

			if (dialog.ShowDialog() != true) return;

			await LoadEpubFileAsync(dialog.FileName, false, 0).ConfigureAwait(true);
		}

		/// <summary>
		/// Общая загрузка EPUB: разбор/кэш, запись в историю, показ книги.
		/// </summary>
		private async Task LoadEpubFileAsync(string path, bool forceRefresh, int chapterIndex)
		{
			if (string.IsNullOrWhiteSpace(path)) return;

			_currentOp?.Cancel();
			_currentOp = new CancellationTokenSource();
			var ct = _currentOp.Token;

			IsLoadingBook = true;
			StatusMessage = "Открываем EPUB…";
			BookWarning = null;

			try
			{
				var book = await _epubProvider
					.LoadFromFileAsync(path, forceRefresh, ct)
					.ConfigureAwait(true);

				_epubLibrary.AddOrUpdate(book, path);
				ApplyBook(book, chapterIndex);
				RefreshEpubBooks();
			}
			catch (OperationCanceledException) { StatusMessage = null; }
			catch (Exception ex)
			{
				ReadingLog.Warn("EPUB: не удалось открыть " + path, ex);
				StatusMessage = "Ошибка EPUB: " + ex.Message;
			}
			finally
			{
				if (!ct.IsCancellationRequested) IsLoadingBook = false;
			}
		}

		private async Task OpenEpubEntryAsync(EpubLibraryEntry entry)
		{
			if (entry == null) return;

			// Уже открыта эта книга — просто возвращаемся к её оглавлению.
			if (CurrentBook != null &&
				string.Equals(CurrentBook.ProviderId, EpubProvider.ProviderKey, StringComparison.OrdinalIgnoreCase) &&
				string.Equals(CurrentBook.Id, entry.Id, StringComparison.OrdinalIgnoreCase))
			{
				IsSearchMode = false;
				return;
			}

			_currentOp?.Cancel();
			_currentOp = new CancellationTokenSource();
			var ct = _currentOp.Token;

			IsLoadingBook = true;
			StatusMessage = "Открываем «" + entry.Title + "»…";
			BookWarning = null;

			try
			{
				Book book = null;

				try
				{
					book = await _epubProvider.GetBookContentAsync(entry.Id, ct).ConfigureAwait(true);
				}
				catch (FileNotFoundException)
				{
					book = null;
				}

				if (book == null)
				{
					if (string.IsNullOrWhiteSpace(entry.FilePath) || !File.Exists(entry.FilePath))
						throw new FileNotFoundException(
							"Файл EPUB больше не найден: " + entry.FilePath);

					book = await _epubProvider
						.LoadFromFileAsync(entry.FilePath, true, ct)
						.ConfigureAwait(true);
				}

				_epubLibrary.Touch(entry.Id);
				ApplyBook(book, 0);
				RefreshEpubBooks();
			}
			catch (OperationCanceledException) { StatusMessage = null; }
			catch (Exception ex)
			{
				ReadingLog.Warn("EPUB: не удалось открыть «" + entry.Title + "»", ex);
				StatusMessage = "Ошибка EPUB: " + ex.Message;
			}
			finally
			{
				if (!ct.IsCancellationRequested) IsLoadingBook = false;
			}
		}

		/// <summary>Показывает загруженную книгу, по возможности сохраняя позицию.</summary>
		private void ApplyBook(Book book, int chapterIndex)
		{
			if (book == null) return;

			CurrentBook = book;
			IsSearchMode = false;
			BookWarning = book.ChaptersTruncated
				? "Произведение содержит больше глав, чем разрешено загружать. Показаны первые "
				  + book.ChapterCount + " глав."
				: null;

			if (book.ChapterCount > 0)
			{
				var target = chapterIndex < 0 ? 0 : chapterIndex;
				if (target >= book.ChapterCount) target = book.ChapterCount - 1;
				CurrentChapter = book.Chapters[target];
			}

			StatusMessage = null;
		}

		private void GoToChapter(int index)
		{
			if (_currentBook?.Chapters == null) return;
			if (index < 0 || index >= _currentBook.Chapters.Count) return;

			CurrentChapter = _currentBook.Chapters[index];
		}

		private async Task SearchAsync()
		{
			if (string.IsNullOrWhiteSpace(SearchQuery)) return;

			_currentOp?.Cancel();
			_currentOp = new CancellationTokenSource();
			var ct = _currentOp.Token;

			IsSearching = true;
			StatusMessage = "Поиск в Викитеке…";
			SearchResults.Clear();

			try
			{
				var results = await _service.SearchAsync(SearchQuery, ct).ConfigureAwait(true);
				foreach (var r in results) SearchResults.Add(r);

				StatusMessage = results.Count == 0
					? "Ничего не найдено. Попробуйте другое название."
					: $"Найдено: {results.Count}";
			}
			catch (OperationCanceledException) { StatusMessage = null; }
			catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
			finally
			{
				if (!ct.IsCancellationRequested) IsSearching = false;
			}
		}

		private async Task LoadBookAsync(BookSearchResult result)
		{
			_currentOp?.Cancel();
			_currentOp = new CancellationTokenSource();
			var ct = _currentOp.Token;

			IsLoadingBook = true;
			StatusMessage = $"Загрузка «{result.Title}»…";
			BookWarning = null;
			CurrentBook = null;

			try
			{
				var book = await _service.LoadBookAsync(result.PageId, false, ct).ConfigureAwait(true);
				ApplyBook(book, 0);
			}
			catch (OperationCanceledException) { StatusMessage = null; }
			catch (Exception ex) { StatusMessage = $"Ошибка загрузки: {ex.Message}"; }
			finally
			{
				if (!ct.IsCancellationRequested) IsLoadingBook = false;
			}
		}

		private async Task ReloadAsync()
		{
			if (CurrentBook == null) return;

			if (string.Equals(CurrentBook.ProviderId, EpubProvider.ProviderKey,
				StringComparison.OrdinalIgnoreCase))
			{
				await ReloadEpubAsync().ConfigureAwait(true);
				return;
			}

			var bookId = CurrentBook.Id;
			var previousIndex = _chapterIndex;

			IsLoadingBook = true;
			StatusMessage = "Перезагрузка с сервера…";
			BookWarning = null;

			try
			{
				var fresh = await _service.LoadBookAsync(bookId, true, CancellationToken.None).ConfigureAwait(true);
				ApplyBook(fresh, previousIndex);
			}
			catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
			finally { IsLoadingBook = false; }
		}

		/// <summary>Перечитывает EPUB с диска, сохраняя текущую главу.</summary>
		private async Task ReloadEpubAsync()
		{
			var entry = _epubLibrary.Find(CurrentBook.Id);
			var path = entry != null ? entry.FilePath : null;

			if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
			{
				StatusMessage = "Файл EPUB не найден — загрузите его заново.";
				return;
			}

			await LoadEpubFileAsync(path, true, _chapterIndex).ConfigureAwait(true);
		}
	}
}

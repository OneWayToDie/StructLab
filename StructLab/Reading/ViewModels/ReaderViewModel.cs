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
		private enum LeftPanelMode
		{
			CatalogSearch,
			Toc,
			Bookmarks,
			BookSearch
		}

		private readonly ReadingService _service;
		private readonly ReadingSettings _settings;
		private readonly EpubProvider _epubProvider;
		private readonly EpubLibraryStore _epubLibrary;
		private readonly ReadingProgressStore _progress;
		private readonly BookmarkStore _bookmarkStore;
		private CancellationTokenSource _currentOp;

		private string _searchQuery = "";
		private bool _isSearching;
		private bool _isLoadingBook;
		private LeftPanelMode _leftMode = LeftPanelMode.CatalogSearch;
		private string _statusMessage;
		private string _bookWarning;
		private BookSearchResult _selectedResult;
		private Book _currentBook;
		private Chapter _currentChapter;
		private int _chapterIndex = -1;
		private int _maxChapterIndex;
		private double _currentScrollOffset;
		private double? _pendingScrollOffset;
		private double? _pendingScrollRatio;
		private string _bookSearchQuery = "";
		private string _bookSearchSummary;

		public ObservableCollection<BookSearchResult> SearchResults { get; } = new ObservableCollection<BookSearchResult>();

		/// <summary>История загруженных EPUB-книг (свежие сверху).</summary>
		public ObservableCollection<EpubLibraryEntry> EpubBooks { get; } = new ObservableCollection<EpubLibraryEntry>();

		/// <summary>Закладки текущей книги.</summary>
		public ObservableCollection<Bookmark> Bookmarks { get; } = new ObservableCollection<Bookmark>();

		/// <summary>Результаты поиска по тексту текущей книги.</summary>
		public ObservableCollection<BookSearchHit> BookSearchResults { get; } = new ObservableCollection<BookSearchHit>();

		public bool HasEpubBooks => EpubBooks.Count > 0;
		public bool HasNoEpubBooks => EpubBooks.Count == 0;
		public bool HasBookmarks => Bookmarks.Count > 0;
		public bool HasNoBookmarks => Bookmarks.Count == 0;
		public bool HasBookSearchResults => BookSearchResults.Count > 0;
		public bool HasNoBookSearchResults => BookSearchResults.Count == 0;

		/// <summary>Прокрутка восстанавливается после перестройки документа.</summary>
		public event EventHandler ScrollRestoreRequested;

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

		public bool IsSearchMode => _leftMode == LeftPanelMode.CatalogSearch;
		public bool IsTocMode => _leftMode == LeftPanelMode.Toc;
		public bool IsBookmarksMode => _leftMode == LeftPanelMode.Bookmarks;
		public bool IsBookSearchMode => _leftMode == LeftPanelMode.BookSearch;

		/// <summary>Шапка книги (название, прогресс, кнопки) видна во всех режимах, кроме поиска по каталогу.</summary>
		public bool IsBookHeaderVisible => HasBook && !IsSearchMode;

		private void SetLeftMode(LeftPanelMode mode)
		{
			if (_leftMode == mode) return;

			_leftMode = mode;
			OnPropertyChanged(nameof(IsSearchMode));
			OnPropertyChanged(nameof(IsTocMode));
			OnPropertyChanged(nameof(IsBookmarksMode));
			OnPropertyChanged(nameof(IsBookSearchMode));
			OnPropertyChanged(nameof(IsBookHeaderVisible));
		}

		public string BookSearchQuery
		{
			get => _bookSearchQuery;
			set => SetProperty(ref _bookSearchQuery, value);
		}

		public string BookSearchSummary
		{
			get => _bookSearchSummary;
			private set
			{
				if (SetProperty(ref _bookSearchSummary, value))
					OnPropertyChanged(nameof(HasBookSearchSummary));
			}
		}

		public bool HasBookSearchSummary => !string.IsNullOrEmpty(_bookSearchSummary);

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
				OnPropertyChanged(nameof(IsBookHeaderVisible));
				OnPropertyChanged(nameof(BookSubtitle));
				OnPropertyChanged(nameof(HasBookSubtitle));
				OnPropertyChanged(nameof(ChapterCountText));
				OnPropertyChanged(nameof(ChapterPositionText));
				OnPropertyChanged(nameof(ProgressText));
				OnPropertyChanged(nameof(HasProgressText));
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

				if (_chapterIndex > _maxChapterIndex)
					_maxChapterIndex = _chapterIndex;

				UpdateChapterStatuses();

				OnPropertyChanged();
				OnPropertyChanged(nameof(ChapterPositionText));
				OnPropertyChanged(nameof(ProgressText));
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

		/// <summary>«Прочитано 7 из 23 глав (30%)» — по самой дальней достигнутой главе.</summary>
		public string ProgressText
		{
			get
			{
				var total = _currentBook?.ChapterCount ?? 0;
				if (total == 0) return "";

				var read = _maxChapterIndex;
				if (read < 0) read = 0;
				if (read > total) read = total;

				var percent = (int)Math.Round(read * 100.0 / total);
				if (percent > 100) percent = 100;

				return "Прочитано " + read + " из " + total + " глав (" + percent + "%)";
			}
		}

		public bool HasProgressText => _currentBook != null && _currentBook.ChapterCount > 0;

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
		public RelayCommand ShowBookmarksCommand { get; }
		public RelayCommand AddBookmarkCommand { get; }
		public RelayCommand DeleteBookmarkCommand { get; }
		public RelayCommand OpenBookmarkCommand { get; }
		public RelayCommand SearchInBookCommand { get; }
		public RelayCommand OpenBookSearchHitCommand { get; }
		public RelayCommand LoadEpubCommand { get; }
		public RelayCommand OpenEpubCommand { get; }

		/// <summary>
		/// Конструктор по умолчанию — для дизайнера и тестов.
		/// В приложении зависимости приходят из DI (см. App.xaml.cs).
		/// </summary>
		public ReaderViewModel()
			: this(
				new ReadingService(new WikisourceProvider(), new FileBookCache()),
				ReadingSettings.Load(),
				new EpubProvider(),
				new EpubLibraryStore(),
				new ReadingProgressStore(),
				new BookmarkStore())
		{
		}

		public ReaderViewModel(
			ReadingService service,
			ReadingSettings settings,
			EpubProvider epubProvider,
			EpubLibraryStore epubLibrary,
			ReadingProgressStore progress,
			BookmarkStore bookmarkStore)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));

			_service = service;
			_settings = settings != null ? settings : ReadingSettings.Load();
			_epubProvider = epubProvider != null ? epubProvider : new EpubProvider();
			_epubLibrary = epubLibrary != null ? epubLibrary : new EpubLibraryStore();
			_progress = progress != null ? progress : new ReadingProgressStore();
			_bookmarkStore = bookmarkStore != null ? bookmarkStore : new BookmarkStore();

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
				_ => SetLeftMode(LeftPanelMode.Toc),
				_ => HasBook);

			ShowBookmarksCommand = new RelayCommand(
				_ => SetLeftMode(LeftPanelMode.Bookmarks),
				_ => HasBook);

			AddBookmarkCommand = new RelayCommand(
				_ => AddBookmark(),
				_ => HasBook && _chapterIndex >= 0);

			DeleteBookmarkCommand = new RelayCommand(
				parameter =>
				{
					var bookmark = parameter as Bookmark;
					if (bookmark != null) DeleteBookmark(bookmark);
				},
				parameter => parameter is Bookmark);

			OpenBookmarkCommand = new RelayCommand(
				parameter =>
				{
					var bookmark = parameter as Bookmark;
					if (bookmark != null) OpenBookmark(bookmark);
				},
				parameter => parameter is Bookmark);

			SearchInBookCommand = new RelayCommand(
				_ => SearchInBook(),
				_ => HasBook && !string.IsNullOrWhiteSpace(BookSearchQuery));

			OpenBookSearchHitCommand = new RelayCommand(
				parameter =>
				{
					var hit = parameter as BookSearchHit;
					if (hit != null) OpenBookSearchHit(hit);
				},
				parameter => parameter is BookSearchHit);

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
			SetLeftMode(LeftPanelMode.CatalogSearch);
		}

		// ============================================================
		// ПРОГРЕСС И ПРОКРУТКА
		// ============================================================

		/// <summary>Текущее смещение прокрутки — приходит из окна.</summary>
		public void UpdateScrollOffset(double offset)
		{
			if (double.IsNaN(offset) || double.IsInfinity(offset)) return;
			_currentScrollOffset = offset < 0 ? 0 : offset;
		}

		/// <summary>Сохраняет позицию текущей книги (вызывается при закрытии окна).</summary>
		public void SaveProgress()
		{
			if (_currentBook == null || _chapterIndex < 0) return;

			_progress.Save(new ReadingProgress
			{
				BookId = _currentBook.FullId,
				ChapterIndex = _chapterIndex,
				ScrollOffset = _currentScrollOffset,
				MaxChapterIndex = _maxChapterIndex,
				UpdatedAt = DateTime.Now
			});
		}

		/// <summary>Окно забирает отложенную прокрутку после перестройки документа.</summary>
		public bool TryTakePendingScroll(out double? offset, out double? ratio)
		{
			offset = _pendingScrollOffset;
			ratio = _pendingScrollRatio;
			_pendingScrollOffset = null;
			_pendingScrollRatio = null;

			return offset.HasValue || ratio.HasValue;
		}

		/// <summary>Вернуть отложенную прокрутку (окно повторит попытку после layout).</summary>
		public void SetPendingScroll(double? offset, double? ratio)
		{
			_pendingScrollOffset = offset;
			_pendingScrollRatio = ratio;
		}

		/// <summary>Запрос на восстановление прокрутки (окно подпишется и выполнит).</summary>
		public void RequestScrollRestore()
			=> ScrollRestoreRequested?.Invoke(this, EventArgs.Empty);

		private void UpdateChapterStatuses()
		{
			var book = _currentBook;
			if (book?.Chapters == null) return;

			for (var i = 0; i < book.Chapters.Count; i++)
			{
				var chapter = book.Chapters[i];
				var isCurrent = i == _chapterIndex;
				chapter.IsCurrent = isCurrent;
				chapter.IsRead = i != _chapterIndex && i < _maxChapterIndex;
			}
		}

		// ============================================================
		// ЗАКЛАДКИ
		// ============================================================

		private void RefreshBookmarks()
		{
			Bookmarks.Clear();

			if (_currentBook != null)
			{
				foreach (var bookmark in _bookmarkStore.ForBook(_currentBook.FullId))
					Bookmarks.Add(bookmark);
			}

			OnPropertyChanged(nameof(HasBookmarks));
			OnPropertyChanged(nameof(HasNoBookmarks));
		}

		private void AddBookmark()
		{
			if (_currentBook == null || _currentChapter == null) return;

			var offset = _currentScrollOffset;

			// Повторное нажатие на почти той же позиции снимает закладку.
			foreach (var existing in Bookmarks)
			{
				if (existing.ChapterIndex == _chapterIndex &&
					Math.Abs(existing.ScrollOffset - offset) < 60)
				{
					_bookmarkStore.Remove(existing.Id);
					RefreshBookmarks();
					StatusMessage = "Закладка удалена";
					return;
				}
			}

			_bookmarkStore.Add(new Bookmark
			{
				Id = Guid.NewGuid().ToString("N"),
				BookId = _currentBook.FullId,
				ChapterIndex = _chapterIndex,
				ChapterTitle = _currentChapter.Title,
				ScrollOffset = offset,
				CreatedAt = DateTime.Now
			});

			RefreshBookmarks();
			StatusMessage = "Закладка добавлена";
		}

		private void DeleteBookmark(Bookmark bookmark)
		{
			if (bookmark == null) return;
			_bookmarkStore.Remove(bookmark.Id);
			RefreshBookmarks();
		}

		private void OpenBookmark(Bookmark bookmark)
		{
			if (bookmark == null || _currentBook?.Chapters == null) return;

			var index = bookmark.ChapterIndex;
			if (index < 0 || index >= _currentBook.Chapters.Count) return;

			SetLeftMode(LeftPanelMode.Toc);
			GoToChapter(index);

			_pendingScrollOffset = bookmark.ScrollOffset > 0 ? bookmark.ScrollOffset : (double?)null;
			RequestScrollRestore();
		}

		// ============================================================
		// ПОИСК ПО ТЕКСТУ КНИГИ
		// ============================================================

		private void SearchInBook()
		{
			var book = _currentBook;
			var query = (BookSearchQuery ?? "").Trim();

			BookSearchResults.Clear();
			BookSearchSummary = null;

			if (book?.Chapters == null || query.Length == 0) return;

			const int maxPerChapter = 5;
			const int maxTotal = 100;
			const int snippetRadius = 45;

			var total = 0;

			for (var i = 0; i < book.Chapters.Count && total < maxTotal; i++)
			{
				var chapter = book.Chapters[i];
				if (string.IsNullOrEmpty(chapter.Text)) continue;

				var found = 0;
				var start = 0;

				while (found < maxPerChapter && total < maxTotal)
				{
					var match = chapter.Text.IndexOf(query, start,
						StringComparison.CurrentCultureIgnoreCase);
					if (match < 0) break;

					var from = Math.Max(0, match - snippetRadius);
					var length = Math.Min(chapter.Text.Length - from,
						query.Length + snippetRadius * 2);
					var snippet = CollapseWhitespace(chapter.Text.Substring(from, length));

					BookSearchResults.Add(new BookSearchHit
					{
						ChapterIndex = i,
						ChapterTitle = chapter.Title,
						Snippet = snippet,
						PositionRatio = chapter.Text.Length == 0
							? 0
							: (double)match / chapter.Text.Length
					});

					found++;
					total++;
					start = match + query.Length;
					if (start >= chapter.Text.Length) break;
				}
			}

			BookSearchSummary = total == 0
				? "Ничего не найдено"
				: "Найдено: " + total
				  + (total == maxTotal ? " (показаны первые " + maxTotal + ")" : "");

			OnPropertyChanged(nameof(HasBookSearchResults));
			OnPropertyChanged(nameof(HasNoBookSearchResults));
			SetLeftMode(LeftPanelMode.BookSearch);
		}

		private void OpenBookSearchHit(BookSearchHit hit)
		{
			if (hit == null || _currentBook?.Chapters == null) return;

			var index = hit.ChapterIndex;
			if (index < 0 || index >= _currentBook.Chapters.Count) return;

			GoToChapter(index);

			_pendingScrollRatio = hit.PositionRatio < 0 ? 0
				: (hit.PositionRatio > 1 ? 1 : hit.PositionRatio);
			RequestScrollRestore();
		}

		private static string CollapseWhitespace(string text)
		{
			if (string.IsNullOrEmpty(text)) return "";

			var builder = new System.Text.StringBuilder(text.Length);
			var space = false;

			foreach (var ch in text)
			{
				if (char.IsWhiteSpace(ch))
				{
					space = true;
					continue;
				}

				if (space && builder.Length > 0) builder.Append(' ');
				space = false;
				builder.Append(ch);
			}

			return builder.ToString().Trim();
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

			await LoadEpubFileAsync(dialog.FileName, false, null).ConfigureAwait(true);
		}

		/// <summary>
		/// Общая загрузка EPUB: разбор/кэш, запись в историю, показ книги.
		/// chapterIndex == null — восстановить сохранённую позицию.
		/// </summary>
		private async Task LoadEpubFileAsync(string path, bool forceRefresh, int? chapterIndex)
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
				SetLeftMode(LeftPanelMode.Toc);
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
				ApplyBook(book, null);
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

		/// <summary>
		/// Показывает загруженную книгу.
		/// chapterIndex == null — восстановить сохранённую позицию; иначе открыть указанную главу.
		/// </summary>
		private void ApplyBook(Book book, int? chapterIndex)
		{
			if (book == null) return;

			CurrentBook = book;
			SetLeftMode(LeftPanelMode.Toc);
			BookWarning = book.ChaptersTruncated
				? "Произведение содержит больше глав, чем разрешено загружать. Показаны первые "
				  + book.ChapterCount + " глав."
				: null;

			_currentScrollOffset = 0;
			_pendingScrollOffset = null;
			_pendingScrollRatio = null;

			int target;

			if (chapterIndex.HasValue)
			{
				target = chapterIndex.Value;
			}
			else
			{
				var saved = _progress.Find(book.FullId);
				_maxChapterIndex = saved != null ? saved.MaxChapterIndex : 0;
				target = saved != null ? saved.ChapterIndex : 0;

				if (saved != null && saved.ScrollOffset > 0)
					_pendingScrollOffset = saved.ScrollOffset;
			}

			if (book.ChapterCount > 0)
			{
				if (target < 0) target = 0;
				if (target >= book.ChapterCount) target = book.ChapterCount - 1;

				CurrentChapter = book.Chapters[target];
			}

			RefreshBookmarks();
			RefreshBookSearchResults();

			OnPropertyChanged(nameof(ProgressText));
			OnPropertyChanged(nameof(HasProgressText));

			if (_pendingScrollOffset.HasValue)
				RequestScrollRestore();

			StatusMessage = null;
		}

		private void RefreshBookSearchResults()
		{
			BookSearchResults.Clear();
			BookSearchSummary = null;
			OnPropertyChanged(nameof(HasBookSearchResults));
			OnPropertyChanged(nameof(HasNoBookSearchResults));
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
				ApplyBook(book, null);
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using StructLab.Reading.Models;
using StructLab.Reading.Services;
using StructLab.ViewModels;

namespace StructLab.Reading.ViewModels
{
	public class ReaderViewModel : ViewModelBase
	{
		private readonly ReadingService _service;
		private readonly ReadingSettings _settings;
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

		public ReaderViewModel()
		{
			_settings = ReadingSettings.Load();
			var provider = new WikisourceProvider();
			var cache = new FileBookCache();
			_service = new ReadingService(provider, cache);

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

				CurrentBook = book;
				IsSearchMode = false;
				BookWarning = book.ChaptersTruncated
					? "Произведение содержит больше глав, чем разрешено загружать. Показаны первые "
					  + book.ChapterCount + " глав."
					: null;

				if (book.ChapterCount > 0)
					CurrentChapter = book.Chapters[0];

				StatusMessage = null;
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

			var bookId = CurrentBook.Id;
			var previousIndex = _chapterIndex;

			IsLoadingBook = true;
			StatusMessage = "Перезагрузка с сервера…";
			BookWarning = null;

			try
			{
				var fresh = await _service.LoadBookAsync(bookId, true, CancellationToken.None).ConfigureAwait(true);

				CurrentBook = fresh;
				BookWarning = fresh.ChaptersTruncated
					? "Произведение содержит больше глав, чем разрешено загружать. Показаны первые "
					  + fresh.ChapterCount + " глав."
					: null;

				if (fresh.ChapterCount > 0)
				{
					var target = previousIndex < 0 ? 0 : previousIndex;
					if (target >= fresh.ChapterCount) target = fresh.ChapterCount - 1;
					CurrentChapter = fresh.Chapters[target];
				}

				StatusMessage = null;
			}
			catch (Exception ex) { StatusMessage = $"Ошибка: {ex.Message}"; }
			finally { IsLoadingBook = false; }
		}
	}
}

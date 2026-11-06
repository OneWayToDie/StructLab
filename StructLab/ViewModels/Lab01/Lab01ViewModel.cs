using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using MaterialDesignThemes.Wpf;
using StructLab.Models;
using StructLab.Models.Lab01;

namespace StructLab.ViewModels.Lab01
{
	public class Lab01ViewModel : ViewModelBase
	{
		private static int _nextId = 1;

		private Book _selectedBook;
		private bool _isAddDialogOpen;
		private Book _newBook = new Book();
		private string _newBookError;

		public ObservableCollection<Book> Books { get; }

		public Book SelectedBook
		{
			get => _selectedBook;
			set => SetProperty(ref _selectedBook, value);
		}

		// ---- Диалог добавления ----
		public bool IsAddDialogOpen
		{
			get => _isAddDialogOpen;
			set => SetProperty(ref _isAddDialogOpen, value);
		}

		public Book NewBook
		{
			get => _newBook;
			set => SetProperty(ref _newBook, value);
		}

		public string NewBookError
		{
			get => _newBookError;
			set => SetProperty(ref _newBookError, value);
		}

		// ---- Команды ----
		public RelayCommand OpenAddDialogCommand { get; }
		public RelayCommand ConfirmAddCommand { get; }
		public RelayCommand CancelAddCommand { get; }
		public RelayCommand DeleteSelectedCommand { get; }
		public RelayCommand AddFirstCommand { get; }
		public RelayCommand AddSortedCommand { get; }
		public RelayCommand ClearAllCommand { get; }

		public Lab01ViewModel()
		{
			Books = new ObservableCollection<Book>();
			SeedDemoData();

			OpenAddDialogCommand = new RelayCommand(_ => OpenAddDialog());
			ConfirmAddCommand = new RelayCommand(_ => ConfirmAdd(), _ => NewBook != null);
			CancelAddCommand = new RelayCommand(_ => IsAddDialogOpen = false);
			DeleteSelectedCommand = new RelayCommand(_ => DeleteSelected(), _ => SelectedBook != null);
			AddFirstCommand = new RelayCommand(_ => AddFirst());
			AddSortedCommand = new RelayCommand(_ => AddSorted());
			ClearAllCommand = new RelayCommand(_ => ClearAll(), _ => Books.Count > 0);
		}

		// ---- Демо-данные, чтобы интерфейс не был пустым при первом запуске ----
		private void SeedDemoData()
		{
			AddBookInternal(new Book
			{
				Author = "Л. Н. Толстой",
				Title = "Война и мир",
				Genre = "Роман",
				Year = 1869,
				Pages = 1225,
				Copies = 3
			});
			AddBookInternal(new Book
			{
				Author = "Ф. М. Достоевский",
				Title = "Преступление и наказание",
				Genre = "Роман",
				Year = 1866,
				Pages = 671,
				Copies = 2
			});
			AddBookInternal(new Book
			{
				Author = "А. С. Пушкин",
				Title = "Евгений Онегин",
				Genre = "Поэма",
				Year = 1833,
				Pages = 224,
				Copies = 5
			});
			AddBookInternal(new Book
			{
				Author = "М. А. Булгаков",
				Title = "Мастер и Маргарита",
				Genre = "Роман",
				Year = 1967,
				Pages = 480,
				Copies = 1
			});
			AddBookInternal(new Book
			{
				Author = "А. П. Чехов",
				Title = "Вишнёвый сад",
				Genre = "Пьеса",
				Year = 1904,
				Pages = 96,
				Copies = 4
			});
		}

		// ---- Внутренняя логика ----
		private void OpenAddDialog()
		{
			NewBook = new Book { Year = 2024, Pages = 300, Copies = 1 };
			NewBookError = null;
			IsAddDialogOpen = true;
		}

		private void ConfirmAdd()
		{
			// Валидация
			if (string.IsNullOrWhiteSpace(NewBook.Author))
			{
				NewBookError = "Укажите автора.";
				return;
			}
			if (string.IsNullOrWhiteSpace(NewBook.Title))
			{
				NewBookError = "Укажите название.";
				return;
			}
			if (string.IsNullOrWhiteSpace(NewBook.Genre))
			{
				NewBookError = "Укажите жанр.";
				return;
			}
			if (NewBook.Year < 1000 || NewBook.Year > 2100)
			{
				NewBookError = "Год должен быть в диапазоне 1000–2100.";
				return;
			}
			if (NewBook.Pages <= 0)
			{
				NewBookError = "Количество страниц должно быть положительным.";
				return;
			}
			if (NewBook.Copies < 0)
			{
				NewBookError = "Количество экземпляров не может быть отрицательным.";
				return;
			}

			AddBookInternal(NewBook.Clone());
			IsAddDialogOpen = false;
		}

		private void AddBookInternal(Book book)
		{
			book.Id = _nextId++;
			Books.Add(book);
		}

		private void DeleteSelected()
		{
			if (SelectedBook == null) return;
			Books.Remove(SelectedBook);
			SelectedBook = null;
		}

		private void AddFirst()
		{
			OpenAddDialog(); // для простоты — открываем диалог, а «в начало» вставим в Б2 через C++
		}

		private void AddSorted()
		{
			// В Б2 заменим на вызов C++ DLL, пока — заглушка
			var sorted = Books.OrderBy(b => b.Title).ToList();
			Books.Clear();
			foreach (var b in sorted)
				Books.Add(b);
		}

		private void ClearAll()
		{
			Books.Clear();
			SelectedBook = null;
		}
	}
}

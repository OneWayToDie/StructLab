namespace StructLab.Models.Lab01
{
	public class Book : ObservableModel
	{
		private int _id;
		private string _author;
		private string _title;
		private string _genre;
		private int _year;
		private int _pages;
		private int _copies;
		private bool _isNew;

		public int Id { get => _id; set => SetProperty(ref _id, value); }
		public string Author { get => _author; set => SetProperty(ref _author, value); }
		public string Title { get => _title; set => SetProperty(ref _title, value); }
		public string Genre { get => _genre; set => SetProperty(ref _genre, value); }
		public int Year { get => _year; set => SetProperty(ref _year, value); }
		public int Pages { get => _pages; set => SetProperty(ref _pages, value); }
		public int Copies { get => _copies; set => SetProperty(ref _copies, value); }

		/// <summary>
		/// Транзиентный флаг для подсветки только что добавленной строки.
		/// Не сериализуется (в CBook его нет).
		/// </summary>
		public bool IsNew { get => _isNew; set => SetProperty(ref _isNew, value); }

		public Book Clone()
		{
			return new Book
			{
				Id = Id,
				Author = Author,
				Title = Title,
				Genre = Genre,
				Year = Year,
				Pages = Pages,
				Copies = Copies
			};
		}

		/// <summary>
		/// Обновляет текущий объект данными из снапшота, не пересоздавая строку в UI.
		/// </summary>
		public void CopyFrom(Book other)
		{
			Id = other.Id;
			Author = other.Author;
			Title = other.Title;
			Genre = other.Genre;
			Year = other.Year;
			Pages = other.Pages;
			Copies = other.Copies;
		}
	}
}

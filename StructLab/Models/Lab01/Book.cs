namespace StructLab.Models.Lab01
{
	public class Book
	{
		public int Id { get; set; }
		public string Author { get; set; }
		public string Title { get; set; }
		public string Genre { get; set; }
		public int Year { get; set; }
		public int Pages { get; set; }
		public int Copies { get; set; }

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
	}
}

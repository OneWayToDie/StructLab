using System;

namespace StructLab.Models.Lab01
{
	public class Loan : ObservableModel
	{
		private int _id;
		private int _bookId;
		private int _readerId;
		private string _issueDate;
		private string _dueDate;
		private string _returnDate;
		private string _bookTitle;
		private string _readerName;

		public int Id { get => _id; set => SetProperty(ref _id, value); }
		public int BookId { get => _bookId; set => SetProperty(ref _bookId, value); }
		public int ReaderId { get => _readerId; set => SetProperty(ref _readerId, value); }

		public string IssueDate
		{
			get => _issueDate;
			set { if (SetProperty(ref _issueDate, value)) NotifyComputed(); }
		}

		public string DueDate
		{
			get => _dueDate;
			set { if (SetProperty(ref _dueDate, value)) NotifyComputed(); }
		}

		public string ReturnDate
		{
			get => _returnDate;
			set { if (SetProperty(ref _returnDate, value)) NotifyComputed(); }
		}

		// Вычисляемые для UI (нужны для отображения, но не для хранения)
		public string BookTitle { get => _bookTitle; set => SetProperty(ref _bookTitle, value); }
		public string ReaderName { get => _readerName; set => SetProperty(ref _readerName, value); }

		public bool IsReturned => !string.IsNullOrEmpty(ReturnDate);

		public bool IsOverdue
		{
			get
			{
				if (IsReturned) return false;
				if (DateTime.TryParse(DueDate, out var due))
					return due.Date < DateTime.Today;
				return false;
			}
		}

		public string Status
		{
			get
			{
				if (IsReturned) return "Возвращена";
				if (IsOverdue) return "ПРОСРОЧЕНА";
				return "В срок";
			}
		}

		/// <summary>Декоративный штраф для анимации «монетки в копилку».</summary>
		public int Penalty
		{
			get
			{
				if (!IsOverdue) return 0;
				if (DateTime.TryParse(DueDate, out var due))
					return 50 + Math.Max(0, (DateTime.Today - due.Date).Days) * 10;
				return 50;
			}
		}

		public void CopyFrom(Loan other)
		{
			Id = other.Id;
			BookId = other.BookId;
			ReaderId = other.ReaderId;
			IssueDate = other.IssueDate;
			DueDate = other.DueDate;
			ReturnDate = other.ReturnDate;
			BookTitle = other.BookTitle;
			ReaderName = other.ReaderName;
		}

		private void NotifyComputed()
		{
			OnPropertyChanged(nameof(IsReturned));
			OnPropertyChanged(nameof(IsOverdue));
			OnPropertyChanged(nameof(Status));
			OnPropertyChanged(nameof(Penalty));
		}
	}
}

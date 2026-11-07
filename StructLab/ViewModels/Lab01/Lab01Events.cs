using System;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using StructLab.Models.Lab01;

namespace StructLab.ViewModels.Lab01
{
	public sealed class PageChangedEventArgs : EventArgs
	{
		public PageChangedEventArgs(int oldPage, int newPage)
		{
			OldPage = oldPage;
			NewPage = newPage;
		}

		public int OldPage { get; }
		public int NewPage { get; }
	}

	public sealed class BookAddedEventArgs : EventArgs
	{
		public BookAddedEventArgs(Book book) => Book = book;
		public Book Book { get; }
	}

	public sealed class BookDeletingEventArgs : EventArgs
	{
		public BookDeletingEventArgs(Book book)
		{
			Book = book;
			Completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		}

		public Book Book { get; }

		/// <summary>
		/// View сообщает, что анимация удаления завершена.
		/// </summary>
		public TaskCompletionSource<bool> Completion { get; }
	}

	public sealed class MessageShownEventArgs : EventArgs
	{
		public MessageShownEventArgs(PackIconKind icon, string title)
		{
			Icon = icon;
			Title = title;
		}

		public PackIconKind Icon { get; }
		public string Title { get; }
	}

	public sealed class LoanReturnedEventArgs : EventArgs
	{
		public LoanReturnedEventArgs(Loan loan, bool wasOverdue, int penalty)
		{
			Loan = loan;
			WasOverdue = wasOverdue;
			Penalty = penalty;
		}

		public Loan Loan { get; }
		public bool WasOverdue { get; }
		public int Penalty { get; }
	}
}

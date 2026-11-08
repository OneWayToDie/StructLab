using System.Windows;
using System.Windows.Controls;

namespace StructLab.Views.Lab01
{
	public partial class Lab01View : UserControl
	{
		public Lab01View()
		{
			InitializeComponent();

			Loaded += OnViewLoaded;
			Unloaded += OnViewUnloaded;
			DataContextChanged += OnViewDataContextChanged;
		}

		private void OpenReader_Click(object sender, RoutedEventArgs e)
		{
			var vm = DataContext as StructLab.ViewModels.Lab01.Lab01ViewModel;
			var book = vm?.DetailsBook;
			if (book == null) return;

			vm.IsBookDetailsDialogOpen = false;

			var readerVm = new StructLab.Reading.ViewModels.ReaderViewModel();
			readerVm.PreloadSearch($"{book.Author} {book.Title}");

			var window = new StructLab.Reading.Views.ReaderWindow
			{
				DataContext = readerVm,
				Owner = Window.GetWindow(this)
			};
			window.ShowDialog();
		}
	}
}

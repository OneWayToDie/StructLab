using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using StructLab.Reading.Helpers;
using StructLab.Reading.ViewModels;

namespace StructLab.Reading.Views
{
	public partial class ReaderWindow : Window
	{
		private ReaderViewModel _viewModel;

		public ReaderWindow()
		{
			InitializeComponent();
			DataContextChanged += OnDataContextChanged;
		}

		private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (_viewModel != null)
				_viewModel.PropertyChanged -= OnViewModelPropertyChanged;

			_viewModel = e.NewValue as ReaderViewModel;

			if (_viewModel != null)
				_viewModel.PropertyChanged += OnViewModelPropertyChanged;

			RebuildDocument();
		}

		private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ReaderViewModel.CurrentChapter) ||
				e.PropertyName == nameof(ReaderViewModel.CurrentBook) ||
				e.PropertyName == nameof(ReaderViewModel.FontSize) ||
				e.PropertyName == nameof(ReaderViewModel.FontFamily) ||
				e.PropertyName == nameof(ReaderViewModel.Theme))
			{
				RebuildDocument();
			}
		}

		private void RebuildDocument()
		{
			var vm = _viewModel;
			var chapter = vm?.CurrentChapter;

			if (chapter == null)
			{
				Reader.Document = new System.Windows.Documents.FlowDocument();
				return;
			}

			Reader.Document = FlowDocumentConverter.FromPlainText(
				chapter.Title,
				chapter.Text,
				vm.FontSize,
				vm.FontFamily,
				vm.Theme);

			Title = vm.CurrentBook != null
				? vm.CurrentBook.Title + " — StructLab"
				: "Читалка — StructLab";
		}

		private void SearchBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && _viewModel?.SearchCommand.CanExecute(null) == true)
				_viewModel.SearchCommand.Execute(null);
		}
	}
}

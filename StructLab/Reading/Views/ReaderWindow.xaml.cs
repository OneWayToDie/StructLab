using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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
			Closing += OnClosing;

			// Прокрутка FlowDocumentReader всплывает как обычное routed-событие.
			Reader.AddHandler(ScrollViewer.ScrollChangedEvent,
				new ScrollChangedEventHandler(OnReaderScrollChanged));
		}

		private void OnClosing(object sender, CancelEventArgs e)
		{
			_viewModel?.SaveProgress();
		}

		private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (_viewModel != null)
			{
				_viewModel.PropertyChanged -= OnViewModelPropertyChanged;
				_viewModel.ScrollRestoreRequested -= OnScrollRestoreRequested;
			}

			_viewModel = e.NewValue as ReaderViewModel;

			if (_viewModel != null)
			{
				_viewModel.PropertyChanged += OnViewModelPropertyChanged;
				_viewModel.ScrollRestoreRequested += OnScrollRestoreRequested;
			}

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

		private void OnReaderScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if (e.OriginalSource is ScrollViewer)
				_viewModel?.UpdateScrollOffset(e.VerticalOffset);
		}

		private void OnScrollRestoreRequested(object sender, EventArgs e)
		{
			Dispatcher.BeginInvoke(new Action(() => RestoreScroll(false)),
				DispatcherPriority.Background);
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

		private void RestoreScroll(bool isRetry)
		{
			double? offset;
			double? ratio;

			if (_viewModel == null) return;
			if (!_viewModel.TryTakePendingScroll(out offset, out ratio)) return;

			var scrollViewer = FindScrollViewer(Reader);
			if (scrollViewer == null)
			{
				if (!isRetry)
				{
					_viewModel.SetPendingScroll(offset, ratio);
					Dispatcher.BeginInvoke(new Action(() => RestoreScroll(true)),
						DispatcherPriority.ApplicationIdle);
				}
				return;
			}

			// Документ ещё не разложен — повторяем после прохода layout.
			if (scrollViewer.ScrollableHeight <= 0 && !isRetry)
			{
				_viewModel.SetPendingScroll(offset, ratio);
				Dispatcher.BeginInvoke(new Action(() => RestoreScroll(true)),
					DispatcherPriority.ApplicationIdle);
				return;
			}

			double target;
			if (offset.HasValue) target = offset.Value;
			else if (ratio.HasValue) target = Math.Max(0, ratio.Value * scrollViewer.ScrollableHeight);
			else return;

			scrollViewer.ScrollToVerticalOffset(target);
			_viewModel.UpdateScrollOffset(target);
		}

		private static ScrollViewer FindScrollViewer(DependencyObject root)
		{
			if (root == null) return null;

			var count = VisualTreeHelper.GetChildrenCount(root);
			for (var i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(root, i);
				var viewer = child as ScrollViewer;
				if (viewer != null) return viewer;

				var nested = FindScrollViewer(child);
				if (nested != null) return nested;
			}

			return null;
		}

		private void SearchBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && _viewModel?.SearchCommand.CanExecute(null) == true)
				_viewModel.SearchCommand.Execute(null);
		}

		private void BookSearchBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && _viewModel?.SearchInBookCommand.CanExecute(null) == true)
				_viewModel.SearchInBookCommand.Execute(null);
		}
	}
}

using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;


namespace StructLab.Views.Notifications
{
	public partial class ToastWindow : Window
	{
		private DispatcherTimer _autoCloseTimer;

		public ToastWindow(string message)
		{
			InitializeComponent();

			DataContext = new
			{
				Message = message,
				TimeText = DateTime.Now.ToString("HH:mm")
			};

			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			var workArea = SystemParameters.WorkArea;
			Left = workArea.Right - ActualWidth - 8;
			Top = workArea.Bottom - ActualHeight - 8;

			// Анимация: slide-in + fade-in
			var transform = new System.Windows.Media.TranslateTransform(60, 0);

			// ВАЖНО: применяем трансформацию к RootBorder, а не к Window
			RootBorder.RenderTransform = transform;

			// Прозрачность можно оставить на окне (или тоже перенести на RootBorder)
			Opacity = 0;

			var slideIn = new DoubleAnimation(60, 0, TimeSpan.FromMilliseconds(350))
			{
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
			};
			var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));

			transform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slideIn);
			BeginAnimation(OpacityProperty, fadeIn);

			// Авто-закрытие через 6 секунд
			_autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
			_autoCloseTimer.Tick += (s, a) => CloseWithAnimation();
			_autoCloseTimer.Start();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			_autoCloseTimer?.Stop();
			CloseWithAnimation();
		}

		private void CloseWithAnimation()
		{
			if (IsClosing) return;
			IsClosing = true;

			// Берем трансформацию у RootBorder
			var transform = RootBorder.RenderTransform as System.Windows.Media.TranslateTransform
							?? new System.Windows.Media.TranslateTransform();
			RootBorder.RenderTransform = transform;

			var slideOut = new DoubleAnimation(0, 60, TimeSpan.FromMilliseconds(250))
			{
				// ИСПРАВЛЕНО: EasingMode.EaseIn вместо EasingMode.In
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
			};
			var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
			fadeOut.Completed += (s, a) => Close();

			transform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slideOut);
			BeginAnimation(OpacityProperty, fadeOut);
		}

		private bool IsClosing { get; set; }
	}
}
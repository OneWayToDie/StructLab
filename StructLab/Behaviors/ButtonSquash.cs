using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace StructLab.Behaviors
{
	/// <summary>
	/// «Пружинящие» кнопки без переопределения визуальных стилей MaterialDesign:
	/// на PreviewMouseDown кнопка слегка сжимается, на отпускании — пружинит обратно.
	/// Поведение вешается на корневой элемент и работает для всех кнопок внутри.
	/// </summary>
	public static class ButtonSquash
	{
		public static readonly DependencyProperty IsEnabledProperty =
			DependencyProperty.RegisterAttached(
				"IsEnabled",
				typeof(bool),
				typeof(ButtonSquash),
				new PropertyMetadata(false, OnIsEnabledChanged));

		public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
		public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

		private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is UIElement root)) return;

			if ((bool)e.NewValue)
			{
				root.PreviewMouseDown += Root_PreviewMouseDown;
				root.PreviewMouseUp += Root_PreviewMouseUp;
				root.LostMouseCapture += Root_LostMouseCapture;
			}
			else
			{
				root.PreviewMouseDown -= Root_PreviewMouseDown;
				root.PreviewMouseUp -= Root_PreviewMouseUp;
				root.LostMouseCapture -= Root_LostMouseCapture;
			}
		}

		private static void Root_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			var button = FindButton(e.OriginalSource as DependencyObject);
			if (button == null) return;

			ScaleTo(button, 1.05, 0.88, 70, null);
		}

		private static void Root_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			var button = FindButton(e.OriginalSource as DependencyObject);
			if (button == null) return;

			ScaleTo(button, 1, 1, 220, new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 });
		}

		private static void Root_LostMouseCapture(object sender, MouseEventArgs e)
		{
			var button = FindButton(e.OriginalSource as DependencyObject);
			if (button == null) return;

			ScaleTo(button, 1, 1, 180, null);
		}

		private static Button FindButton(DependencyObject source)
		{
			while (source != null)
			{
				if (source is Button button) return button;
				if (!(source is Visual)) return null;
				source = VisualTreeHelper.GetParent(source);
			}
			return null;
		}

		private static void ScaleTo(Button button, double scaleX, double scaleY, double durationMs, IEasingFunction easing)
		{
			if (!Properties.Settings.Default.EnableFunAnimations) return;

			var scale = button.RenderTransform as ScaleTransform;
			if (scale == null)
			{
				if (button.RenderTransform != null) return;
				scale = new ScaleTransform(1, 1);
				button.RenderTransformOrigin = new Point(0.5, 0.5);
				button.RenderTransform = scale;
			}

			var duration = TimeSpan.FromMilliseconds(durationMs);
			var x = new DoubleAnimation(scaleX, duration) { EasingFunction = easing };
			var y = new DoubleAnimation(scaleY, duration) { EasingFunction = easing };

			scale.BeginAnimation(ScaleTransform.ScaleXProperty, x);
			scale.BeginAnimation(ScaleTransform.ScaleYProperty, y);
		}
	}
}

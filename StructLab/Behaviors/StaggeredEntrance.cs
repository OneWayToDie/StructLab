using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace StructLab.Behaviors
{
	/// <summary>
	/// Каскадное появление детей Panel при первом показе / возврате на вкладку.
	/// Не трогает RenderTransform, чтобы не конфликтовать с squash-анимацией кнопок.
	/// </summary>
	public static class StaggeredEntrance
	{
		public static readonly DependencyProperty IsEnabledProperty =
			DependencyProperty.RegisterAttached(
				"IsEnabled",
				typeof(bool),
				typeof(StaggeredEntrance),
				new PropertyMetadata(false, OnIsEnabledChanged));

		public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
		public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

		private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is Panel panel)) return;

			if ((bool)e.NewValue)
				panel.IsVisibleChanged += Panel_IsVisibleChanged;
			else
				panel.IsVisibleChanged -= Panel_IsVisibleChanged;
		}

		private static void Panel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (!(bool)e.NewValue) return;
			Play((Panel)sender);
		}

		private static void Play(Panel panel)
		{
			bool enabled = Properties.Settings.Default.EnableFunAnimations;

			for (int i = 0; i < panel.Children.Count; i++)
			{
				if (!(panel.Children[i] is UIElement child)) continue;

				if (!enabled)
				{
					child.BeginAnimation(UIElement.OpacityProperty, null);
					child.Opacity = 1;
					continue;
				}

				TimeSpan delay = TimeSpan.FromMilliseconds(Math.Min(i, 12) * 45);

				child.Opacity = 0;
				child.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240))
				{
					BeginTime = delay,
					EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
				});
			}
		}
	}
}

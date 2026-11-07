using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace StructLab.Controls
{
	/// <summary>
	/// TextBlock с анимированным числовым значением.
	/// Позволяет анимировать счётчики без таймеров и конвертеров.
	/// </summary>
	public class AnimatedNumberTextBlock : TextBlock
	{
		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register(nameof(Value), typeof(double), typeof(AnimatedNumberTextBlock),
				new PropertyMetadata(0d, OnValueChanged));

		public static readonly DependencyProperty FormatProperty =
			DependencyProperty.Register(nameof(Format), typeof(string), typeof(AnimatedNumberTextBlock),
				new PropertyMetadata("{0:F0}"));

		public static readonly DependencyProperty AnimationDurationProperty =
			DependencyProperty.Register(nameof(AnimationDuration), typeof(Duration), typeof(AnimatedNumberTextBlock),
				new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(450))));

		public static readonly DependencyProperty PulseOnChangeProperty =
			DependencyProperty.Register(nameof(PulseOnChange), typeof(bool), typeof(AnimatedNumberTextBlock),
				new PropertyMetadata(true));

		private static readonly DependencyProperty AnimatedValueProperty =
			DependencyProperty.Register("AnimatedValue", typeof(double), typeof(AnimatedNumberTextBlock),
				new PropertyMetadata(0d, OnAnimatedValueChanged));

		public double Value
		{
			get => (double)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		public string Format
		{
			get => (string)GetValue(FormatProperty);
			set => SetValue(FormatProperty, value);
		}

		public Duration AnimationDuration
		{
			get => (Duration)GetValue(AnimationDurationProperty);
			set => SetValue(AnimationDurationProperty, value);
		}

		public bool PulseOnChange
		{
			get => (bool)GetValue(PulseOnChangeProperty);
			set => SetValue(PulseOnChangeProperty, value);
		}

		private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var control = (AnimatedNumberTextBlock)d;
			double to = (double)e.NewValue;
			double from = (double)control.GetValue(AnimatedValueProperty);

			var animation = new DoubleAnimation(from, to, control.AnimationDuration)
			{
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
				FillBehavior = FillBehavior.HoldEnd
			};
			control.BeginAnimation(AnimatedValueProperty, animation);

			if (control.PulseOnChange && control.IsLoaded)
				control.PlayPulse();
		}

		private static void OnAnimatedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var control = (AnimatedNumberTextBlock)d;
			double value = (double)e.NewValue;
			if (string.IsNullOrEmpty(control.Format))
				control.Text = value.ToString("F0");
			else
				control.Text = string.Format(control.Format, value);
		}

		private void PlayPulse()
		{
			if (!(RenderTransform is ScaleTransform))
			{
				RenderTransform = new ScaleTransform(1, 1);
				RenderTransformOrigin = new Point(0.5, 0.5);
			}

			var scale = (ScaleTransform)RenderTransform;
			var animation = new DoubleAnimationUsingKeyFrames
			{
				Duration = TimeSpan.FromMilliseconds(260)
			};
			animation.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
			animation.KeyFrames.Add(new EasingDoubleKeyFrame(1.12, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(90))));
			animation.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(260))));

			scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
			scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
		}
	}
}

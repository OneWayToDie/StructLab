using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using StructLab.Models.Lab02;

namespace StructLab.Views.Lab02
{
	public partial class Lab02View : UserControl
	{
		public Lab02View()
		{
			InitializeComponent();
		}

		/// <summary>
		/// При «появлении» узла:
		/// 1) Каскадный рост 0 → 1.12 → 1.0 за 600 мс, сдвиг 70 мс на узел (визуально «расцветает»).
		/// 2) После появления — «дыхание»: opacity 1.0 ↔ 0.55 и scale 1.0 ↔ 1.08.
		///    Частота и фаза у каждого узла свои → создаётся «живой» эффект.
		/// </summary>
		private void Ellipse_Loaded(object sender, RoutedEventArgs e)
		{
			if (!(sender is Ellipse ellipse) || !(ellipse.DataContext is TreeNodeView node))
				return;

			if (!Properties.Settings.Default.EnableFunAnimations)
				return;

			ellipse.RenderTransformOrigin = new Point(0.5, 0.5);

			// ---- 1. ПОЯВЛЕНИЕ (каскад) ----
			// Две независимые шкалы: scaleAppear для появления, scaleBreath для пульсации
			var scaleAppear = new ScaleTransform(0, 0);
			var scaleBreath = new ScaleTransform(1, 1);
			var group = new TransformGroup();
			group.Children.Add(scaleAppear);
			group.Children.Add(scaleBreath);
			ellipse.RenderTransform = group;

			// Первые 20 узлов — каскадом по 70 мс, остальные появляются сразу после
			// (иначе 100+ узлов растянутся в 7 секунд).
			int perNodeDelay = 100;
			int cascadeDelayMs = Math.Min(node.Index, 20) * perNodeDelay;

			// Ключевые кадры: 0 → 1.12 (перелёт) → 1.0 (оседание)
			var appearAnim = new DoubleAnimationUsingKeyFrames
			{
				BeginTime = TimeSpan.FromMilliseconds(cascadeDelayMs),
				Duration = TimeSpan.FromMilliseconds(600)
			};
			appearAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
			appearAnim.KeyFrames.Add(new EasingDoubleKeyFrame(1.12,
				KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400)),
				new CubicEase { EasingMode = EasingMode.EaseOut }));
			appearAnim.KeyFrames.Add(new EasingDoubleKeyFrame(1.0,
				KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(600)),
				new CubicEase { EasingMode = EasingMode.EaseIn }));

			scaleAppear.BeginAnimation(ScaleTransform.ScaleXProperty, appearAnim);
			scaleAppear.BeginAnimation(ScaleTransform.ScaleYProperty, appearAnim);

			// ---- 2. ПУЛЬСАЦИЯ (после появления) ----
			// Каждый узел начинает пульсировать со своей фазой, чтобы не мигали синхронно.
			var pulseBegin = TimeSpan.FromMilliseconds(
				cascadeDelayMs + 600 + 150 + node.PulseDelayMs);

			// 2.1. Прозрачность: 1.0 ↔ 0.55 — заметный «вдох/выдох»
			var pulseOpacity = new DoubleAnimation
			{
				From = 1.0,
				To = 0.35,
				Duration = TimeSpan.FromMilliseconds(2000),
				AutoReverse = true,
				RepeatBehavior = RepeatBehavior.Forever,
				BeginTime = pulseBegin,
				EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
			};
			ellipse.BeginAnimation(UIElement.OpacityProperty, pulseOpacity);

			// 2.2. Масштаб: 1.0 ↔ 1.08 — «дыхание» кружка
			var pulseScale = new DoubleAnimation
			{
				From = 1.0,
				To = 1.15,
				Duration = TimeSpan.FromMilliseconds(1400),
				AutoReverse = true,
				RepeatBehavior = RepeatBehavior.Forever,
				BeginTime = pulseBegin,
				EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
			};
			scaleBreath.BeginAnimation(ScaleTransform.ScaleXProperty, pulseScale);
			scaleBreath.BeginAnimation(ScaleTransform.ScaleYProperty, pulseScale);
		}
	}
}
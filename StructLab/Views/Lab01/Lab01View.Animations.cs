using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using StructLab.Models.Lab01;
using StructLab.ViewModels.Lab01;

namespace StructLab.Views.Lab01
{
	/// <summary>
	/// Вся «сценическая» часть анимаций: полёты, частицы, маскот, flip-диалоги.
	/// Разметка остаётся в XAML, здесь — только то, что требует доступа к визуальному дереву.
	/// </summary>
	public partial class Lab01View
	{
		private static readonly Brush ConfettiGold = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
		private static readonly Brush ConfettiPink = new SolidColorBrush(Color.FromRgb(0xF4, 0x72, 0xB6));
		private static readonly Brush ConfettiCyan = new SolidColorBrush(Color.FromRgb(0x22, 0xD3, 0xEE));
		private static readonly Brush PageBrush = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
		private static readonly Brush DustBrush = new SolidColorBrush(Color.FromRgb(0xAE, 0xB9, 0xC9));
		private static readonly Brush CoinBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0xB5, 0x2B));

		private readonly Random _random = new Random();
		private bool _subscribed;
		private bool _loadedOnce;
		private Lab01ViewModel _attachedViewModel;

		private Lab01ViewModel ViewModel => DataContext as Lab01ViewModel;

		private bool AnimationsEnabled => Properties.Settings.Default.EnableFunAnimations;

		// ============================================================
		// ПОДПИСКИ
		// ============================================================
		private void OnViewLoaded(object sender, RoutedEventArgs e)
		{
			AttachViewModel();
			UpdateShelfProgress(false);
			_loadedOnce = true;
		}

		private void OnViewUnloaded(object sender, RoutedEventArgs e)
		{
			DetachViewModel();
			_loadedOnce = false;
		}

		private void OnViewDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			DetachViewModel();
			if (IsLoaded) AttachViewModel();
		}

		private void AttachViewModel()
		{
			if (_subscribed) return;
			var vm = ViewModel;
			if (vm == null) return;

			vm.PageChanged += Vm_PageChanged;
			vm.BookAdded += Vm_BookAdded;
			vm.BookDeleting += Vm_BookDeleting;
			vm.MessageShown += Vm_MessageShown;
			vm.LoanReturned += Vm_LoanReturned;
			vm.PropertyChanged += Vm_PropertyChanged;
			_attachedViewModel = vm;
			_subscribed = true;

			UpdateShelfProgress(false);
		}

		private void DetachViewModel()
		{
			if (!_subscribed) return;
			var vm = _attachedViewModel;
			if (vm != null)
			{
				vm.PageChanged -= Vm_PageChanged;
				vm.BookAdded -= Vm_BookAdded;
				vm.BookDeleting -= Vm_BookDeleting;
				vm.MessageShown -= Vm_MessageShown;
				vm.LoanReturned -= Vm_LoanReturned;
				vm.PropertyChanged -= Vm_PropertyChanged;
			}
			_attachedViewModel = null;
			_subscribed = false;
		}

		private void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var vm = (Lab01ViewModel)sender;

			switch (e.PropertyName)
			{
				case nameof(Lab01ViewModel.IsDoubly):
					PlayTogglePulse();
					break;
				case nameof(Lab01ViewModel.ShelfFillPercentage):
					UpdateShelfProgress(true);
					break;
				case nameof(Lab01ViewModel.IsBookDetailsDialogOpen):
					if (vm.IsBookDetailsDialogOpen) PlayBookFlip();
					break;
			}
		}

		// ============================================================
		// 2.3 ПЕРЕХОД МЕЖДУ СТРАНИЦАМИ
		// ============================================================
		private void Vm_PageChanged(object sender, PageChangedEventArgs e)
		{
			if (!AnimationsEnabled || PageTranslate == null) return;

			int direction = e.NewPage >= e.OldPage ? 1 : -1;
			var slide = new DoubleAnimation(direction * 36, 0, TimeSpan.FromMilliseconds(280))
			{
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
			};
			PageTranslate.BeginAnimation(TranslateTransform.XProperty, slide);

			var fade = new DoubleAnimation(0.15, 1, TimeSpan.FromMilliseconds(260));
			BooksGridHost.BeginAnimation(UIElement.OpacityProperty, fade);
		}

		// ============================================================
		// 2.5 ПУЛЬСАЦИЯ ПЕРЕКЛЮЧАТЕЛЯ
		// ============================================================
		private void PlayTogglePulse()
		{
			if (!AnimationsEnabled || ListTypeScale == null) return;

			var pulse = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(320) };
			pulse.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
			pulse.KeyFrames.Add(new EasingDoubleKeyFrame(1.06, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(110))));
			pulse.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(320))));

			ListTypeScale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
			ListTypeScale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);

			if (ListTypeLabel != null && ListTypeLabel.Foreground is SolidColorBrush brush && !brush.IsFrozen)
			{
				var flash = new ColorAnimation(brush.Color, Colors.White, TimeSpan.FromMilliseconds(120))
				{
					AutoReverse = true,
					BeginTime = TimeSpan.FromMilliseconds(80)
				};
				brush.BeginAnimation(SolidColorBrush.ColorProperty, flash);
			}
		}

		// ============================================================
		// 3.4 ПРОГРЕСС «ЗАПОЛНЕННОСТЬ ПОЛКИ»
		// ============================================================
		private void UpdateShelfProgress(bool animate)
		{
			var vm = ViewModel;
			if (vm == null || ShelfProgress == null) return;

			double target = vm.ShelfFillPercentage;
			if (!animate || !AnimationsEnabled)
			{
				ShelfProgress.BeginAnimation(ProgressBar.ValueProperty, null);
				ShelfProgress.Value = target;
				return;
			}

			var animation = new DoubleAnimation(ShelfProgress.Value, target, TimeSpan.FromMilliseconds(500))
			{
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
			};
			ShelfProgress.BeginAnimation(ProgressBar.ValueProperty, animation);
		}

		// ============================================================
		// 2.1 + 4.1 + 4.7 ДОБАВЛЕНИЕ КНИГИ
		// ============================================================
		private void Vm_BookAdded(object sender, BookAddedEventArgs e)
		{
			if (!AnimationsEnabled) return;

			Dispatcher.BeginInvoke(new Action(() =>
			{
				PlayBookFlight(e.Book);
				PlayShelfRock();
				OwlHop();
			}), DispatcherPriority.Loaded);
		}

		/// <summary>Подсветка новой строки (вызывается из LoadingRow).</summary>
		private void HighlightNewRow(DataGridRow row)
		{
			if (row == null) return;

			var brush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE7, 0xFF));
			row.Background = brush;

			var fade = new ColorAnimation(Color.FromRgb(0xE0, 0xE7, 0xFF), Colors.Transparent,
				TimeSpan.FromMilliseconds(900))
			{
				BeginTime = TimeSpan.FromMilliseconds(600),
				FillBehavior = FillBehavior.HoldEnd
			};
			brush.BeginAnimation(SolidColorBrush.ColorProperty, fade);
		}

		private void PlayBookFlight(Book book)
		{
			if (OverlayCanvas == null || BooksGrid == null) return;
			if (OverlayCanvas.ActualWidth <= 0 || OverlayCanvas.ActualHeight <= 0) return;

			var from = new Point(OverlayCanvas.ActualWidth / 2, OverlayCanvas.ActualHeight / 2);
			var to = BooksGrid.TranslatePoint(
				new Point(BooksGrid.ActualWidth / 2, Math.Min(BooksGrid.ActualHeight, 220) / 2),
				OverlayCanvas);

			FlyIcon(from, to, PackIconKind.BookOpenPageVariant, PrimaryBrush(), 28, 680, true, null);
		}

		private void PlayShelfRock()
		{
			if (ShelfRotate == null) return;

			var rock = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(520) };
			rock.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
			rock.KeyFrames.Add(new EasingDoubleKeyFrame(0.5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120))));
			rock.KeyFrames.Add(new EasingDoubleKeyFrame(-0.5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(280))));
			rock.KeyFrames.Add(new EasingDoubleKeyFrame(0.2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400))));
			rock.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(520))));

			ShelfRotate.BeginAnimation(RotateTransform.AngleProperty, rock);
		}

		// ============================================================
		// 2.4 + 4.12 ПОЯВЛЕНИЕ / ВОЛНА СТРОК
		// ============================================================
		private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!_loadedOnce) return;
			if (e.OriginalSource != MainTabs) return;

			if (MainTabs.SelectedIndex == 0)
				PlayRowsWave();
		}

		private void PlayRowsWave()
		{
			if (!AnimationsEnabled || BooksGrid == null) return;

			Dispatcher.BeginInvoke(new Action(() =>
			{
				int index = 0;
				foreach (var item in BooksGrid.Items)
				{
					if (!(BooksGrid.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow row))
						continue;

					row.Opacity = 0;
					var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
					{
						BeginTime = TimeSpan.FromMilliseconds(Math.Min(index, 12) * 35)
					};
					row.BeginAnimation(UIElement.OpacityProperty, fade);
					index++;
				}
			}), DispatcherPriority.Loaded);
		}

		private void BooksGrid_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			if (e.Row.Item is Book book && book.IsNew)
				HighlightNewRow(e.Row);
		}

		private void BooksGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var vm = ViewModel;
			if (vm?.SelectedBook != null)
				vm.ShowBookDetails(vm.SelectedBook);
		}

		// ============================================================
		// 3.1 + 4.6 + 4.11 УДАЛЕНИЕ СТРОКИ
		// ============================================================
		private void Vm_BookDeleting(object sender, BookDeletingEventArgs e)
		{
			if (!AnimationsEnabled || BooksGrid == null || OverlayCanvas == null)
			{
				e.Completion.TrySetResult(true);
				return;
			}

			var row = BooksGrid.ItemContainerGenerator.ContainerFromItem(e.Book) as DataGridRow;
			if (row == null)
			{
				e.Completion.TrySetResult(true);
				return;
			}

			var origin = row.TranslatePoint(
				new Point(row.ActualWidth / 2, row.ActualHeight / 2), OverlayCanvas);

			// 4.11 — пыль для старых книг.
			if (e.Book.Year < 1925)
				SpawnDust(origin);

			// 4.6 — разлетающиеся страницы.
			SpawnParticles(origin, 6, PackIconKind.FileDocument, PageBrush, 70, 55, 700, 18, true);

			double height = row.ActualHeight;
			row.MaxHeight = height;

			var collapse = new DoubleAnimation(height, 0, TimeSpan.FromMilliseconds(300))
			{
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
			};
			collapse.Completed += (s, a) => e.Completion.TrySetResult(true);
			row.BeginAnimation(FrameworkElement.MaxHeightProperty, collapse);
			row.BeginAnimation(UIElement.OpacityProperty,
				new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(280)));
		}

		// ============================================================
		// 4.3 САЛЮТ + 4.5 РЕАКЦИИ СОВЫ
		// ============================================================
		private void Vm_MessageShown(object sender, MessageShownEventArgs e)
		{
			if (!AnimationsEnabled) return;

			bool success = e.Icon == PackIconKind.CheckCircleOutline
				|| e.Icon == PackIconKind.BookCheck
				|| e.Icon == PackIconKind.BookCheckOutline;

			if (success)
			{
				PlayConfetti();
				OwlHop();
			}
			else
			{
				OwlShake();
			}
		}

		private void PlayConfetti()
		{
			if (OverlayCanvas == null || OverlayCanvas.ActualWidth <= 0) return;

			var origin = new Point(OverlayCanvas.ActualWidth / 2, OverlayCanvas.ActualHeight / 2);
			SpawnParticles(origin, 8, PackIconKind.Star, ConfettiGold, 120, 70, 1000, 16, true);
			SpawnParticles(origin, 7, PackIconKind.Star, ConfettiPink, 110, 65, 950, 14, true);
			SpawnParticles(origin, 7, PackIconKind.Star, ConfettiCyan, 125, 75, 1050, 13, true);
		}

		private void OwlHop()
		{
			if (OwlHopScale == null) return;

			var hop = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(420) };
			hop.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
			hop.KeyFrames.Add(new EasingDoubleKeyFrame(1.18, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(140))));
			hop.KeyFrames.Add(new EasingDoubleKeyFrame(0.94, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250))));
			hop.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(420))));

			OwlHopScale.BeginAnimation(ScaleTransform.ScaleXProperty, hop);
			OwlHopScale.BeginAnimation(ScaleTransform.ScaleYProperty, hop);
		}

		private void OwlShake()
		{
			if (OwlRotate == null) return;

			var shake = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(460) };
			shake.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
			shake.KeyFrames.Add(new EasingDoubleKeyFrame(9, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(90))));
			shake.KeyFrames.Add(new EasingDoubleKeyFrame(-9, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200))));
			shake.KeyFrames.Add(new EasingDoubleKeyFrame(6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(310))));
			shake.KeyFrames.Add(new EasingDoubleKeyFrame(-4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(390))));
			shake.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(460))));

			OwlRotate.BeginAnimation(RotateTransform.AngleProperty, shake);
		}

		// ============================================================
		// 4.8 + 4.9 ВОЗВРАТ КНИГИ
		// ============================================================
		private void Vm_LoanReturned(object sender, LoanReturnedEventArgs e)
		{
			if (!AnimationsEnabled || OverlayCanvas == null || OverlayCanvas.ActualWidth <= 0) return;

			Dispatcher.BeginInvoke(new Action(() =>
			{
				Point from = new Point(OverlayCanvas.ActualWidth / 2, OverlayCanvas.ActualHeight / 2);

				if (LoansGrid != null && e.Loan != null)
				{
					var row = LoansGrid.ItemContainerGenerator.ContainerFromItem(e.Loan) as DataGridRow;
					if (row != null)
						from = row.TranslatePoint(new Point(row.ActualWidth / 2, row.ActualHeight / 2), OverlayCanvas);
				}

				if (TotalBooksCounter != null)
				{
					var to = TotalBooksCounter.TranslatePoint(
						new Point(TotalBooksCounter.ActualWidth / 2, TotalBooksCounter.ActualHeight / 2),
						OverlayCanvas);
					FlyIcon(from, to, PackIconKind.BookArrowLeft, SuccessBrush(), 22, 560, true, null);
				}

				OwlHop();

				if (e.WasOverdue && e.Penalty > 0)
					PlayCoins(from, e.Penalty);
			}), DispatcherPriority.Loaded);
		}

		private void PlayCoins(Point from, int penalty)
		{
			if (PiggyBank == null || OverlayCanvas == null) return;

			var to = PiggyBank.TranslatePoint(
				new Point(PiggyBank.ActualWidth / 2, PiggyBank.ActualHeight / 2), OverlayCanvas);

			for (int i = 0; i < 4; i++)
			{
				var start = new Point(from.X + _random.Next(-18, 18), from.Y + _random.Next(-10, 10));
				FlyIcon(start, to, PackIconKind.Circle, CoinBrush, 12 + _random.Next(0, 5),
					520 + i * 70, true, i == 3 ? (Action)(() => BouncePiggy(penalty)) : null);
			}
		}

		private void BouncePiggy(int penalty)
		{
			if (PiggyScale != null)
			{
				var bounce = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(420) };
				bounce.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
				bounce.KeyFrames.Add(new EasingDoubleKeyFrame(1.22, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(140))));
				bounce.KeyFrames.Add(new EasingDoubleKeyFrame(0.94, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(260))));
				bounce.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(420))));
				PiggyScale.BeginAnimation(ScaleTransform.ScaleXProperty, bounce);
				PiggyScale.BeginAnimation(ScaleTransform.ScaleYProperty, bounce);
			}

			ShowPenalty(penalty);
		}

		private void ShowPenalty(int penalty)
		{
			if (PenaltyText == null) return;

			PenaltyText.Text = $"+{penalty} ₽";
			PenaltyText.Opacity = 0;
			PenaltyText.RenderTransform = new TranslateTransform();

			var opacity = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(1300) };
			opacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
			opacity.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120))));
			opacity.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(800))));
			opacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1300))));

			var move = new DoubleAnimation(0, -26, TimeSpan.FromMilliseconds(1300))
			{
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
			};

			PenaltyText.BeginAnimation(UIElement.OpacityProperty, opacity);
			((TranslateTransform)PenaltyText.RenderTransform)
				.BeginAnimation(TranslateTransform.YProperty, move);
		}

		// ============================================================
		// 3.2 + 4.10 FLIP-ДИАЛОГ
		// ============================================================
		private void PlayBookFlip()
		{
			if (DetailsFlipScale == null || DetailsFront == null || DetailsBack == null) return;

			Dispatcher.BeginInvoke(new Action(() =>
			{
				double half = 280;

				var flip = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(half * 2) };
				flip.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
				flip.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(half))));
				flip.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(half + 1))));
				flip.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(half * 2))));
				DetailsFlipScale.BeginAnimation(ScaleTransform.ScaleXProperty, flip);

				var front = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(half * 2) };
				front.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
				front.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(half - 1))));
				front.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(half))));
				DetailsFront.BeginAnimation(UIElement.OpacityProperty, front);

				var back = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(half * 2) };
				back.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(half - 1))));
				back.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(half))));
				DetailsBack.BeginAnimation(UIElement.OpacityProperty, back);
			}), DispatcherPriority.Loaded);
		}

		// ============================================================
		// 4.4 ИНДИКАТОР ЗАГРУЗКИ
		// ============================================================
		private void BusyBall_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			// Индикатор декоративный и не должен перехватывать ввод.
			e.Handled = true;
		}

		// ============================================================
		// ОБЩИЕ ХЕЛПЕРЫ
		// ============================================================
		private static Brush PrimaryBrush()
		{
			var brush = Application.Current?.TryFindResource("PrimaryBrush") as Brush;
			return brush ?? new SolidColorBrush(Color.FromRgb(0x4F, 0x46, 0xE5));
		}

		private static Brush SuccessBrush()
		{
			var brush = Application.Current?.TryFindResource("SuccessBrush") as Brush;
			return brush ?? new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
		}

		private void SpawnParticles(Point origin, int count, PackIconKind kind, Brush brush,
			double distance, double gravity, double lifeMs, double size, bool rotate)
		{
			for (int i = 0; i < count; i++)
			{
				var particle = new PackIcon
				{
					Kind = kind,
					Width = size,
					Height = size,
					Foreground = brush,
					IsHitTestVisible = false,
					RenderTransformOrigin = new Point(0.5, 0.5)
				};

				AnimateParticle(particle, origin, distance, gravity, lifeMs, rotate);
			}
		}

		private void SpawnDust(Point origin)
		{
			for (int i = 0; i < 9; i++)
			{
				double size = 4 + _random.Next(0, 9);
				var particle = new Ellipse
				{
					Width = size,
					Height = size,
					Fill = DustBrush,
					IsHitTestVisible = false,
					RenderTransformOrigin = new Point(0.5, 0.5)
				};

				AnimateParticle(particle, origin, 32, -18, 950, true);
			}
		}

		private void AnimateParticle(FrameworkElement particle, Point origin,
			double distance, double gravity, double lifeMs, bool rotate)
		{
			var translate = new TranslateTransform();
			var rotateTransform = new RotateTransform();
			var group = new TransformGroup();
			group.Children.Add(translate);
			group.Children.Add(rotateTransform);
			particle.RenderTransform = group;

			double size = Math.Max(particle.Width, 8);
			Canvas.SetLeft(particle, origin.X - size / 2);
			Canvas.SetTop(particle, origin.Y - size / 2);
			OverlayCanvas.Children.Add(particle);

			double angle = _random.NextDouble() * Math.PI * 2;
			double travel = distance * (0.55 + _random.NextDouble() * 0.9);
			double dx = Math.Cos(angle) * travel;
			double dy = Math.Sin(angle) * travel + gravity;

			var xAnimation = new DoubleAnimation(0, dx, TimeSpan.FromMilliseconds(lifeMs))
			{
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
			};

			var yAnimation = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(lifeMs) };
			yAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
			yAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(dy * 0.55,
				KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(lifeMs * 0.45)),
				new QuadraticEase { EasingMode = EasingMode.EaseOut }));
			yAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(dy,
				KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(lifeMs)),
				new QuadraticEase { EasingMode = EasingMode.EaseIn }));

			var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(lifeMs * 0.55))
			{
				BeginTime = TimeSpan.FromMilliseconds(lifeMs * 0.45),
				FillBehavior = FillBehavior.HoldEnd
			};
			fade.Completed += (s, e) => OverlayCanvas.Children.Remove(particle);

			translate.BeginAnimation(TranslateTransform.XProperty, xAnimation);
			translate.BeginAnimation(TranslateTransform.YProperty, yAnimation);
			particle.BeginAnimation(UIElement.OpacityProperty, fade);

			if (rotate)
			{
				double angleTo = _random.Next(-220, 220);
				rotateTransform.BeginAnimation(RotateTransform.AngleProperty,
					new DoubleAnimation(0, angleTo, TimeSpan.FromMilliseconds(lifeMs)));
			}
		}

		private void FlyIcon(Point from, Point to, PackIconKind kind, Brush brush,
			double size, double durationMs, bool arc, Action onCompleted)
		{
			if (OverlayCanvas == null) return;

			var icon = new PackIcon
			{
				Kind = kind,
				Width = size,
				Height = size,
				Foreground = brush,
				IsHitTestVisible = false,
				RenderTransformOrigin = new Point(0.5, 0.5)
			};

			var translate = new TranslateTransform();
			var scale = new ScaleTransform(1, 1);
			var group = new TransformGroup();
			group.Children.Add(translate);
			group.Children.Add(scale);
			icon.RenderTransform = group;

			Canvas.SetLeft(icon, from.X - size / 2);
			Canvas.SetTop(icon, from.Y - size / 2);
			OverlayCanvas.Children.Add(icon);

			double dx = to.X - from.X;
			double dy = to.Y - from.Y;

			var path = new PathGeometry();
			var figure = new PathFigure { StartPoint = new Point(0, 0), IsClosed = false };
			double arch = arc ? Math.Max(70, Math.Abs(dx) * 0.18) : 0;
			figure.Segments.Add(new QuadraticBezierSegment(
				new Point(dx * 0.5, dy * 0.5 - arch),
				new Point(dx, dy),
				true));
			path.Figures.Add(figure);

			var duration = TimeSpan.FromMilliseconds(durationMs);
			var xAnimation = new DoubleAnimationUsingPath
			{
				PathGeometry = path,
				Source = PathAnimationSource.X,
				Duration = duration
			};
			var yAnimation = new DoubleAnimationUsingPath
			{
				PathGeometry = path,
				Source = PathAnimationSource.Y,
				Duration = duration
			};

			yAnimation.Completed += (s, e) =>
			{
				var bounce = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(280) };
				bounce.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
				bounce.KeyFrames.Add(new EasingDoubleKeyFrame(1.24, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(90))));
				bounce.KeyFrames.Add(new EasingDoubleKeyFrame(0.9, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180))));
				bounce.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(280))));
				bounce.Completed += (s2, e2) =>
				{
					OverlayCanvas.Children.Remove(icon);
					onCompleted?.Invoke();
				};

				scale.BeginAnimation(ScaleTransform.ScaleXProperty, bounce);
				scale.BeginAnimation(ScaleTransform.ScaleYProperty, bounce);
			};

			translate.BeginAnimation(TranslateTransform.XProperty, xAnimation);
			translate.BeginAnimation(TranslateTransform.YProperty, yAnimation);
		}
	}
}

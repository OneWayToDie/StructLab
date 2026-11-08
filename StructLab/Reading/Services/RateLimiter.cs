using System;
using System.Threading;
using System.Threading.Tasks;

namespace StructLab.Reading.Services
{
	/// <summary>
	/// Глобальный ограничитель запросов к внешним API.
	/// Не более двух запросов в секунду на всё приложение.
	/// </summary>
	public sealed class RateLimiter
	{
		private static readonly Lazy<RateLimiter> LazyInstance =
			new Lazy<RateLimiter>(() => new RateLimiter(), LazyThreadSafetyMode.ExecutionAndPublication);

		private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
		private readonly TimeSpan _minInterval;
		private DateTime _lastRequestUtc = DateTime.MinValue;

		public static RateLimiter Instance => LazyInstance.Value;

		public RateLimiter() : this(TimeSpan.FromMilliseconds(500))
		{
		}

		public RateLimiter(TimeSpan minInterval)
		{
			_minInterval = minInterval;
		}

		/// <summary>
		/// Ждёт, пока можно будет выполнить следующий запрос.
		/// </summary>
		public async Task WaitAsync(CancellationToken cancellationToken)
		{
			await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				var elapsed = DateTime.UtcNow - _lastRequestUtc;
				if (elapsed < _minInterval)
				{
					var delay = _minInterval - elapsed;
					await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
				}

				_lastRequestUtc = DateTime.UtcNow;
			}
			finally
			{
				_gate.Release();
			}
		}
	}
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using SlidingConcurrentCache.Interface;

namespace SlidingConcurrentCache.Library
{
// ReSharper disable once UnusedMember.Global
	public class SlidingConcurrentCache<TKey, TValue> : ConcurrentDictionary<TKey, TValue>, ISlidingConcurrentCache<TKey, TValue>
	{
		private static readonly ConcurrentDictionary<TKey, KeyValuePair<DateTimeOffset, TValue>> __cache = new ConcurrentDictionary<TKey, KeyValuePair<DateTimeOffset, TValue>>();

		private readonly Timer _timer;
		private const double DEFAULT_INTERVAL_IN_MINUTE = 1 * 60 * 1000;

		public SlidingConcurrentCache(double intervalInMilliseconds = DEFAULT_INTERVAL_IN_MINUTE)
		{
			_timer = new Timer(intervalInMilliseconds);
			_timer.Elapsed += TimerElapsed;
			_timer.Start();
		}

		public async Task<TValue> GetOrAdd(TKey key, Func<TKey, Task<TValue>> valueFactory, ulong expireDurationInSeconds = 0ul, ulong slideDurationInSeconds = 0ul)
		{
			DateTimeOffset now = DateTimeOffset.Now;
			KeyValuePair<DateTimeOffset, TValue> value;
			bool cacheHit = __cache.TryGetValue(key, out value);

			if (!cacheHit && __cache.Keys.Any(cacheKey => cacheKey.Equals(key)))
			{
				value = __cache.Where(cacheItem => cacheItem.Key.Equals(key)).Select(cacheItem => cacheItem.Value).FirstOrDefault();
				cacheHit = !value.Value.Equals(default(TValue));
			}

			return cacheHit && value.Key > now
				? (slideDurationInSeconds > default(ulong)
					? GetOrAdd(key, value.Value, expireDurationInSeconds, slideDurationInSeconds)
					: value.Value)
				: GetOrAdd(key, await valueFactory.Invoke(key).ConfigureAwait(false), expireDurationInSeconds, slideDurationInSeconds);
		}

		private static TValue GetOrAdd(TKey key, TValue value, ulong expireDurationInSeconds = 0, ulong slideDurationInSeconds = 0)
		{
			DateTimeOffset now = DateTimeOffset.Now;
			DateTimeOffset expireTime = expireDurationInSeconds > default(ulong) ? now.AddSeconds(expireDurationInSeconds) : DateTimeOffset.MaxValue;
			KeyValuePair<DateTimeOffset, TValue> expireTimeValuePair = __cache.GetOrAdd(key, new KeyValuePair<DateTimeOffset, TValue>(expireTime, value));

			if (expireTimeValuePair.Key < now)
			{
				__cache[key] = new KeyValuePair<DateTimeOffset, TValue>(expireTime, value);
			}
			else if (slideDurationInSeconds > default(uint))
			{
				__cache[key] = new KeyValuePair<DateTimeOffset, TValue>(expireTimeValuePair.Key.AddSeconds(slideDurationInSeconds), expireTimeValuePair.Value);
			}

			return __cache[key].Value;
		}

		private void TimerElapsed(object source, ElapsedEventArgs e)
		{
			_timer.Stop();
			DateTimeOffset now = DateTimeOffset.Now;
			KeyValuePair<DateTimeOffset, TValue> tempPair;

			__cache.Where(cacheItem => cacheItem.Value.Key <= now).ToList().ForEach(cacheItem => __cache.TryRemove(cacheItem.Key, out  tempPair));
			_timer.Start();
		}
	}
}

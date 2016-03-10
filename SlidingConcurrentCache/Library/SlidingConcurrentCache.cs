using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SlidingConcurrentCache.Interface;

namespace SlidingConcurrentCache.Library
{
    // ReSharper disable once UnusedMember.Global
    public sealed class SlidingConcurrentCache<TKey, TValue> : ConcurrentDictionary<TKey, TValue>, ISlidingConcurrentCache<TKey, TValue> where TKey : IComparable<TKey>
    {
        private const int DEFAULT_INTERVAL_IN_MINUTE = 1*60*1000;
        private static readonly ConcurrentDictionary<TKey, KeyValuePair<DateTimeOffset, TValue>> __cache = new ConcurrentDictionary<TKey, KeyValuePair<DateTimeOffset, TValue>>();
        private readonly Timer _timer;

        private bool _isDisposed;

        public SlidingConcurrentCache(int intervalInMilliseconds = DEFAULT_INTERVAL_IN_MINUTE)
        {
            _timer = new Timer(TimerElapsed, null, intervalInMilliseconds, Timeout.Infinite);
        }

        public bool IsDisposed
        {
            get
            {
                if (!_isDisposed)
                {
                    return _isDisposed;
                }

                throw new ObjectDisposedException("SlidingConcurrentCache");
            }
        }

        public int CachedItemCount { get; } = __cache.Count;

        public async Task<TValue> GetOrAdd(TKey key, Func<TKey, Task<TValue>> valueFactory, ulong expireDurationInSeconds = 0ul, ulong slideDurationInSeconds = 0ul)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            KeyValuePair<DateTimeOffset, TValue> value;

            //if (!cacheHit && __cache.Keys.Any(cacheKey => cacheKey.Equals(key)))
            //{
            //	value = __cache.Where(cacheItem => cacheItem.Key.Equals(key)).Select(cacheItem => cacheItem.Value).FirstOrDefault();
            //	cacheHit = !value.Value.Equals(default(TValue));
            //}

            return __cache.TryGetValue(key, out value) && (value.Key > now)
                ? (slideDurationInSeconds > default(ulong)
                    ? GetOrAdd(key, value.Value, now, expireDurationInSeconds, slideDurationInSeconds)
                    : value.Value)
                : GetOrAdd(key, await valueFactory.Invoke(key).ConfigureAwait(false), now, expireDurationInSeconds, slideDurationInSeconds);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    //Dispose of managed resources.
                    _timer.Dispose();
                    __cache.Clear();
                }
                //Dispose of unmanaged resources.
                _isDisposed = true;
            }
        }

        private static TValue GetOrAdd(TKey key, TValue value, DateTimeOffset now, ulong expireDurationInSeconds = 0ul, ulong slideDurationInSeconds = 0ul)
        {
            DateTimeOffset expireTime = expireDurationInSeconds > default(ulong) ? now.AddSeconds(expireDurationInSeconds) : DateTimeOffset.MaxValue.Subtract(now.Offset);
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

        private void TimerElapsed(object state)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            KeyValuePair<DateTimeOffset, TValue> tempPair;

            Parallel.ForEach(__cache, async cacheItem =>
                                            {
                                                if (cacheItem.Value.Key <= now)
                                                {
                                                    byte tryCount = 10;
                                                    while (!__cache.TryRemove(cacheItem.Key, out tempPair) && (tryCount-- > 0))
                                                    {
                                                        await Task.Delay(TimeSpan.FromSeconds(1));
                                                    }
                                                }
                                            });

            _timer.Change(DEFAULT_INTERVAL_IN_MINUTE, Timeout.Infinite);
        }
    }
}
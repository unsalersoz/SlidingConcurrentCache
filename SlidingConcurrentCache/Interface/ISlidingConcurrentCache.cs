using System;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global

namespace SlidingConcurrentCache.Interface
{
	//v1.6 by Ünsal Ersöz (initial github release)
	public interface ISlidingConcurrentCache<TKey, TValue>
	{
		Task<TValue> GetOrAdd(TKey key, Func<TKey, Task<TValue>> valueFactory, ulong expireDurationInSeconds = 0ul, ulong slideDurationInSeconds = 0ul);
	}
}
using System;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global

namespace SlidingConcurrentCache.Interface
{
    /*
        v1.8 by Ünsal Ersöz
            - Added CachedItemCount property.
            - Profiling memory with DotMemory.
        v1.7 by Ünsal Ersöz
            - %100 .net native and .net core compatibility.
            - TKey now must be a IComparable<TKey> in order to remove unsafe search code for non-working .TryGetValue method
            - ISlidingConcurrentCache now implements IDisposable
            - Added unit tests with %100 code coverage.
        v1.6 by Ünsal Ersöz
            - (initial github release)
    */
    public interface ISlidingConcurrentCache<TKey, TValue> : IDisposable where TKey : IComparable<TKey>
    {
        bool IsDisposed { get; }
        int CachedItemCount { get; }
        Task<TValue> GetOrAdd(TKey key, Func<TKey, Task<TValue>> valueFactory, ulong expireDurationInSeconds = 0ul, ulong slideDurationInSeconds = 0ul);
    }
}
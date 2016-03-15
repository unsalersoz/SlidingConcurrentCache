using System;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global

namespace SlidingConcurrentCache.Interface
{
    public interface ISlidingConcurrentCache<TKey, TValue> : IDisposable where TKey : IComparable<TKey>
    {
        bool IsDisposed { get; }
        int CachedItemCount { get; }
        Task<TValue> GetOrAdd(TKey key, Func<TKey, Task<TValue>> valueFactory, ulong expireDurationInSeconds = 0ul, ulong slideDurationInSeconds = 0ul);
    }
}
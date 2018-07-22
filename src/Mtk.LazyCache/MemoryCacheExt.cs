using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Mtk.LazyCache
{
    public static class MemoryCacheExt
    {
        private static readonly KeyedSemaphoreSlim Lock = new KeyedSemaphoreSlim();

        public static T GetOrAdd<T>(this IMemoryCache cache, object key, Func<T> factory, TimeSpan ttl)
        {
            throw new NotImplementedException();
        }

        public static T GetOrAdd<T>(this IMemoryCache cache, object key, Func<T> factory, DateTime expiresIn)
        {
            throw new NotImplementedException();
        }

        public static Task<T> GetOrAddAsync<T>(this IMemoryCache cache, object key, Func<Task<T>> factory, TimeSpan ttl)
        {
            throw new NotImplementedException();
        }

        public static Task<T> GetOrAddAsync<T>(this IMemoryCache cache, object key, Func<Task<T>> factory, DateTime expiresIn)
        {
            throw new NotImplementedException();
        }
    }
}

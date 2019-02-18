using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Mtk.CacheOnce
{
    public static class MemoryCacheOnceExtensions
    {
        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Factory delegate should throw exception in case of fail to be invalidated in cache
        /// </summary>
        public static Task<T> IssueOnceAsync<T>(this IMemoryCache cache, object key, Func<Task<T>> factory, TimeSpan ttl, T invalidValue = default(T)) =>
            GetOrCreateOnceTaskAsync(cache, key, factory, ttl, invalidValue);

        /// <summary>
        /// Factory delegate should throw exception in case of fail to be invalidated in cache
        /// </summary>
        public static Task<T> IssueOnceAsync<T>(this IMemoryCache cache, object key, Func<Task<T>> factory, Func<T, TimeSpan> ttlGet, T invalidValue = default(T))
        {
            return GetOrCreateOnceTaskAsync(cache, key, async () =>
            {
                var value = await factory.Invoke().ConfigureAwait(false);
                await cache.Set(key, Task.FromResult(value), ttlGet.Invoke(value)); //replace existing cache item with new item using new calculated ttl
                return value;
            }, TimeSpan.FromHours(1), //default ttl because cached task is not completed
                invalidValue);
        }

        private static async Task<T> GetOrCreateOnceTaskAsync<T>(this IMemoryCache cache, object key, Func<Task<T>> factory, TimeSpan ttl, T invalidValue = default(T))
        {
            var comparer = EqualityComparer<T>.Default;

            if (!cache.TryGetValue(key, out Task<T> task)
                || task.IsFaulted
                || (task.IsCompleted && !comparer.Equals(invalidValue, default(T)) && comparer.Equals(task.Result, invalidValue)))
            {
                await Semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (!cache.TryGetValue(key, out task)
                        || task.IsFaulted
                        || (task.IsCompleted && !comparer.Equals(invalidValue, default(T)) && comparer.Equals(task.Result, invalidValue)))
                    {
                        task = cache.Set(key, factory.Invoke(), ttl);
                    }
                }
                finally
                {
                    Semaphore.Release();
                }
            }

            try
            {
                return await task.ConfigureAwait(false);
            }
            catch
            {
                return default(T);
            }
        }
    }
}

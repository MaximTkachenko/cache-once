using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Mtk.CacheOnce
{
    public static class MemoryCacheOnceExtensions
    {
        private static readonly SemaphoreSlim SyncSemaphore = new SemaphoreSlim(1, 1);
        private static readonly SemaphoreSlim AsyncSemaphore = new SemaphoreSlim(1, 1);

        public static void DeleteOnce(this IMemoryCache cache, object key) => cache.Remove(key);

        public static T GetOrCreateOnce<T>(this IMemoryCache cache, object key, Func<T> factory, TimeSpan ttl) =>
            GetOrCreateOnceLazy(cache, key, factory, ttl);

        public static T GetOrCreateOnce<T>(this IMemoryCache cache, object key, Func<T> factory, Func<T, TimeSpan> ttlGet)
        {
            return GetOrCreateOnceLazy(cache, key, () =>
            {
                var value = factory.Invoke();
                cache.Set(key, new Lazy<T>(() => value), ttlGet.Invoke(value));
                return value;
            }, TimeSpan.FromHours(1));
        }

        public static Task DeleteOnceAsync(this IMemoryCache cache, object key)
        {
            var task = cache.Get<Task>(key);
            if (task == null)
            {
                return Task.CompletedTask;
            }

            return task.ContinueWith(prev => cache.Remove(key));
        }

        public static Task<T> GetOrCreateOnceAsync<T>(this IMemoryCache cache, object key, Func<Task<T>> factory, TimeSpan ttl) =>
            GetOrCreateOnceTaskAsync(cache, key, factory, ttl);

        public static Task<T> GetOrCreateOnceAsync<T>(this IMemoryCache cache, object key, Func<Task<T>> factory, Func<T, TimeSpan> ttlGet)
        {
            return GetOrCreateOnceTaskAsync(cache, key, async () =>
            {
                var value = await factory.Invoke().ConfigureAwait(false);
                await cache.Set(key, Task.FromResult(value), ttlGet.Invoke(value));//replace existing cache item with new item using calculated ttl
                return value;
            }, TimeSpan.FromHours(1));
        }

        private static T GetOrCreateOnceLazy<T>(IMemoryCache cache, object key, Func<T> factory, TimeSpan ttl)
        {
            if (!cache.TryGetValue(key, out Lazy<T> lazyValue))
            {
                SyncSemaphore.Wait();
                try
                {
                    if (!cache.TryGetValue(key, out lazyValue))
                    {
                        cache.Set(key, new Lazy<T>(factory.Invoke), ttl);
                    }
                    else
                    {
                        return lazyValue.Value;
                    }
                }
                finally
                {
                    SyncSemaphore.Release();
                }
            }

            try
            {
                return lazyValue.Value;
            }
            catch
            {
                cache.Remove(key);
                return default(T);
            }
        }

        private static async Task<T> GetOrCreateOnceTaskAsync<T>(this IMemoryCache cache, object key, Func<Task<T>> factory, TimeSpan ttl)
        {
            if (!cache.TryGetValue(key, out Task<T> task))
            {
                await AsyncSemaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (!cache.TryGetValue(key, out task))
                    {
                        task = cache.Set(key, factory.Invoke(), ttl);
                    }
                }
                finally
                {
                    AsyncSemaphore.Release();
                }
            }

            try
            {
                return await task.ConfigureAwait(false);
            }
            catch
            {
                cache.Remove(key);
                return default(T);
            }
        }
    }
}

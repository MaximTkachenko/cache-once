using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Mtk.LazyCache
{
    public class LazyCache
    {
        private readonly KeyedSemaphoreSlim _keyedLock = new KeyedSemaphoreSlim();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly IMemoryCache _cache;
        private readonly bool _lockPerKey;

        public LazyCache(IMemoryCache cache, bool lockPerKey)
        {
            _cache = cache;
            _lockPerKey = lockPerKey;
        }

        public T LazyGetOrCreate<T>(object key, Func<T> factory, TimeSpan ttl)
        {
            Lazy<T> value;
            Func<Lazy<T>> action = () =>
            {
                return _cache.GetOrCreate(key, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = ttl;
                    return new Lazy<T>(factory.Invoke);
                });
            };

            if (_lockPerKey)
            {
                using (_keyedLock.Lock(key))
                {
                    value = action.Invoke();
                }
            }
            else
            {
                _lock.Wait();
                try
                {
                    value = action.Invoke();
                }
                finally
                {
                    _lock.Release();
                }
            }

            try
            {
                return value.Value;
            }
            catch
            {
                _cache.Remove(key);
                throw;
            }
        }

        public async Task<T> LazyGetOrCreateAsync<T>(object key, Func<Task<T>> factory, TimeSpan ttl)
        {
            AsyncLazy<T> value;
            Func<AsyncLazy<T>> action = () =>
            {
                return _cache.GetOrCreate(key, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = ttl;
                    return new AsyncLazy<T>(() => factory.Invoke());
                });
            };


            if (_lockPerKey)
            {
                using (_keyedLock.Lock(key))
                {
                    value = action.Invoke();
                }
            }
            else
            {
                _lock.Wait();
                try
                {
                    value = action.Invoke();
                }
                finally
                {
                    _lock.Release();
                }
            }

            try
            {
                return await value.Value.ConfigureAwait(false);
            }
            catch
            {
                _cache.Remove(key);
                throw;
            }
        }
    }
}

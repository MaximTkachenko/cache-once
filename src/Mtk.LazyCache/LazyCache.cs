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
            return LazyGetOrCreate(key, factory, entry => entry.AbsoluteExpirationRelativeToNow = ttl);
        }

        public async Task<T> LazyGetOrCreateAsync<T>(object key, Func<Task<T>> factory, TimeSpan ttl)
        {
            return await LazyGetOrCreateAsync(key, factory, entry => entry.AbsoluteExpirationRelativeToNow = ttl);
        }

        public T LazyGetOrCreate<T>(object key, Func<T> factory, DateTimeOffset expiresIn)
        {
            return LazyGetOrCreate(key, factory, entry => entry.AbsoluteExpiration = expiresIn);
        }

        public async Task<T> LazyGetOrCreateAsync<T>(object key, Func<Task<T>> factory, DateTimeOffset expiresIn)
        {
            return await LazyGetOrCreateAsync(key, factory, entry => entry.AbsoluteExpiration = expiresIn);
        }

        private T LazyGetOrCreate<T>(object key, Func<T> factory, Action<ICacheEntry> setExpiration)
        {
            Lazy<T> value;
            Func<Lazy<T>> action = () =>
            {
                return _cache.GetOrCreate(key, entry =>
                {
                    setExpiration(entry);
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

        public async Task<T> LazyGetOrCreateAsync<T>(object key, Func<Task<T>> factory, Action<ICacheEntry> setExpiration)
        {
            AsyncLazy<T> value;
            Func<AsyncLazy<T>> action = () =>
            {
                return _cache.GetOrCreate(key, entry =>
                {
                    setExpiration(entry);
                    return new AsyncLazy<T>(() => factory.Invoke());
                });
            };

            if (_lockPerKey)
            {
                using (await _keyedLock.LockAsync(key))
                {
                    value = action.Invoke();
                }
            }
            else
            {
                await _lock.WaitAsync().ConfigureAwait(false);
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

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Mtk.CacheOnce
{
    public sealed class LocalCacheOnce : ICacheOnce
    {
        private readonly KeyedSemaphoreSlim _keyedLock = new KeyedSemaphoreSlim();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly IMemoryCache _cache;
        private readonly bool _lockPerKey;

        public LocalCacheOnce(IMemoryCache cache, bool lockPerKey)
        {
            _cache = cache;
            _lockPerKey = lockPerKey;
        }

        public T GetOrCreate<T>(int key, Func<T> factory, TimeSpan ttl) =>
            GetOrCreate(key, factory, entry => entry.AbsoluteExpirationRelativeToNow = ttl);

        public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan ttl) =>
            GetOrCreate(key, factory, entry => entry.AbsoluteExpirationRelativeToNow = ttl);

        public async Task<T> GetOrCreateAsync<T>(int key, Func<Task<T>> factory, TimeSpan ttl) =>
            await GetOrCreateAsync(key, factory, entry => entry.AbsoluteExpirationRelativeToNow = ttl);

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl) =>
            await GetOrCreateAsync(key, factory, entry => entry.AbsoluteExpirationRelativeToNow = ttl);

        public T GetOrCreate<T>(int key, Func<T> factory, DateTimeOffset expiresIn) =>
            GetOrCreate(key, factory, entry => entry.AbsoluteExpiration = expiresIn);

        public T GetOrCreate<T>(string key, Func<T> factory, DateTimeOffset expiresIn) =>
            GetOrCreate(key, factory, entry => entry.AbsoluteExpiration = expiresIn);

        public async Task<T> GetOrCreateAsync<T>(int key, Func<Task<T>> factory, DateTimeOffset expiresIn) =>
            await GetOrCreateAsync(key, factory, entry => entry.AbsoluteExpiration = expiresIn);

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, DateTimeOffset expiresIn) =>
            await GetOrCreateAsync(key, factory, entry => entry.AbsoluteExpiration = expiresIn);

        public void Delete(int key) => _cache.Remove(key);

        public void Delete(string key) => _cache.Remove(key);

        private T GetOrCreate<T>(object key, Func<T> factory, Action<ICacheEntry> setExpiration)
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
                return default(T);
            }
        }

        private async Task<T> GetOrCreateAsync<T>(object key, Func<Task<T>> factory, Action<ICacheEntry> setExpiration)
        {
            Task<T> value;
            Func<Task<T>> action = () =>
            {
                return _cache.GetOrCreate(key, entry =>
                {
                    setExpiration(entry);
                    return factory.Invoke();
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
                return await value.ConfigureAwait(false);
            }
            catch
            {
                _cache.Remove(key);
                return default(T);
            }
        }

        public void Dispose()
        {
            _lock?.Dispose();
        }
    }
}

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
            GetOrCreate(key, factory, ttl, null);

        public T GetOrCreate<T>(int key, Func<T> factory, Func<T, TimeSpan> ttlGet) =>
            GetOrCreate(key, factory, null, ttlGet);

        public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan ttl) =>
            GetOrCreate(key, factory, ttl, null);

        public T GetOrCreate<T>(string key, Func<T> factory, Func<T, TimeSpan> ttlGet) =>
            GetOrCreate(key, factory, null, ttlGet);

        public async Task<T> GetOrCreateAsync<T>(int key, Func<Task<T>> factory, TimeSpan ttl) =>
            await GetOrCreateAsync(key, factory, ttl, null);

        public async Task<T> GetOrCreateAsync<T>(int key, Func<Task<T>> factory, Func<T, TimeSpan> ttlGet) =>
            await GetOrCreateAsync(key, factory, null, ttlGet);

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl) =>
            await GetOrCreateAsync(key, factory, ttl, null);

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, Func<T, TimeSpan> ttlGet) =>
            await GetOrCreateAsync(key, factory, null, ttlGet);

        public void Delete(int key) => _cache.Remove(key);

        public void Delete(string key) => _cache.Remove(key);

        private T GetOrCreate<T>(object key, Func<T> factory, TimeSpan? ttl, Func<T, TimeSpan> ttlGet)
        {
            Lazy<T> lazyValue;
            Func<Lazy<T>> action = () =>
            {
                return _cache.GetOrCreate(key, entry =>
                {
                    if (ttl.HasValue)
                    {
                        entry.AbsoluteExpirationRelativeToNow = ttl.Value;
                    }

                    return new Lazy<T>(() =>
                    {
                        var value = factory.Invoke();
                        if (ttlGet != null)
                        {
                            var ttlGetResult = ttlGet.Invoke(value);
                            _cache.Set(key, new Lazy<T>(() => value), ttlGetResult);
                        }

                        return value;
                    });
                });
            };

            if (_lockPerKey)
            {
                using (_keyedLock.Lock(key))
                {
                    lazyValue = action.Invoke();
                }
            }
            else
            {
                _lock.Wait();
                try
                {
                    lazyValue = action.Invoke();
                }
                finally
                {
                    _lock.Release();
                }
            }

            try
            {
                return lazyValue.Value;
            }
            catch
            {
                _cache.Remove(key);
                return default(T);
            }
        }

        private async Task<T> GetOrCreateAsync<T>(object key, Func<Task<T>> factory, TimeSpan? ttl, Func<T, TimeSpan> ttlGet)
        {
            Task<T> awaitableValue;

            var originalFactory = factory;
            factory = async () =>
            {
                var value = await originalFactory.Invoke().ConfigureAwait(false);
                if (ttlGet != null)
                {
                    var ttlGetResult = ttlGet.Invoke(value);
                    await _cache.Set(key, Task.FromResult(value), ttlGetResult);
                }

                return value;
            };

            Func<Task<T>> action = () =>
            {
                return _cache.GetOrCreate(key, entry =>
                {
                    if (ttl.HasValue)
                    {
                        entry.AbsoluteExpirationRelativeToNow = ttl.Value;
                    }

                    return factory.Invoke();
                });
            };

            if (_lockPerKey)
            {
                using (await _keyedLock.LockAsync(key))
                {
                    awaitableValue = action.Invoke();
                }
            }
            else
            {
                await _lock.WaitAsync().ConfigureAwait(false);
                try
                {
                    awaitableValue = action.Invoke();
                }
                finally
                {
                    _lock.Release();
                }
            }

            try
            {
                return await awaitableValue.ConfigureAwait(false);
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

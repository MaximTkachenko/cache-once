using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using ServiceStack.Redis;

namespace Mtk.CacheOnce.TwoLayer
{
    public sealed class TwoLayerCacheOnce : ICacheOnce
    {
        private const string InvalidationChannel = "InvalidationChannel";

        private readonly IRedisClientsManager _redis;
        private readonly ICacheOnce _localCache;
        private readonly IMemoryCache _originalLocalCache;
        private readonly IRedisPubSubServer _invalidationPubSub;
        private readonly ConcurrentDictionary<string, int> _changeLog = new ConcurrentDictionary<string, int>();

        public TwoLayerCacheOnce(IRedisClientsManager redis, IMemoryCache localCache)
        {
            _redis = redis;
            _originalLocalCache = localCache;
            _localCache = new LocalCacheOnce(localCache, false);

            //todo optional?
            _invalidationPubSub = _redis.CreatePubSubServer(InvalidationChannel);
            _invalidationPubSub.OnMessage += (channel, key) =>
            {
                if (!_changeLog.TryRemove(key, out _))
                {
                    _localCache.Delete(key);
                }
            };
            _invalidationPubSub.Start();
        }

        public T GetOrCreate<T>(int key, Func<T> factory, TimeSpan ttl) =>
            GetOrCreateInternal(key.ToString(), factory, ttl, null);

        public T GetOrCreate<T>(int key, Func<T> factory, Func<T, TimeSpan> ttlGet) =>
            GetOrCreateInternal(key.ToString(), factory, null, ttlGet);

        public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan ttl) =>
            GetOrCreateInternal(key, factory, ttl, null);

        public T GetOrCreate<T>(string key, Func<T> factory, Func<T, TimeSpan> ttlGet) =>
            GetOrCreateInternal(key, factory, null, ttlGet);

        public Task<T> GetOrCreateAsync<T>(int key, Func<Task<T>> factory, TimeSpan ttl) =>
            GetOrCreateInternalAsync(key.ToString(), factory, ttl, null);

        public Task<T> GetOrCreateAsync<T>(int key, Func<Task<T>> factory, Func<T, TimeSpan> ttlGet) =>
            GetOrCreateInternalAsync(key.ToString(), factory, null, ttlGet);

        public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl) =>
            GetOrCreateInternalAsync(key, factory, ttl, null);

        public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, Func<T, TimeSpan> ttlGet) =>
            GetOrCreateInternalAsync(key, factory, null, ttlGet);

        public void Delete(int key)
        {
            _localCache.Delete(key);
        }

        public void Delete(string key)
        {
            _localCache.Delete(key);
        }

        public void Dispose()
        {
            _localCache?.Dispose();
            _invalidationPubSub?.Dispose();
        }

        private T GetOrCreateInternal<T>(string key, Func<T> factory, TimeSpan? ttl, Func<T, TimeSpan> ttlGet)
        {
            return _localCache.GetOrCreate(key,
                () =>
                {
                    T value;
                    using (var redis = _redis.GetClient())
                    using (redis.AcquireLock(key + ":lock", TimeSpan.FromSeconds(3)))
                    {
                        value = redis.Get<T>(key);
                        if (value.Equals(default(T)))
                        {
                            value = factory.Invoke();
                            if (!value.Equals(default(T)))
                            {
                                if (ttlGet != null)
                                {
                                    ttl = ttlGet.Invoke(value);
                                }

                                redis.Set(key, value, ttl ?? TimeSpan.FromDays(100));
                                _changeLog[key] = 1;
                                _originalLocalCache.Set(key, new Lazy<T>(() => value), ttl ?? TimeSpan.FromDays(100));
                                redis.PublishMessage(InvalidationChannel, key);
                            }
                        }
                        else
                        {
                            ttl = redis.GetTimeToLive(key);
                            //todo is ok?
                            _originalLocalCache.Set(key, new Lazy<T>(() => value), ttl ?? TimeSpan.FromDays(100));
                        }
                    }

                    return value;
                },
                ttl ?? TimeSpan.FromHours(1));
        }

        private Task<T> GetOrCreateInternalAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl, Func<T, TimeSpan> ttlGet)
        {
            return _localCache.GetOrCreateAsync(key,
                async () =>
                {
                    T value;
                    using (var redis = _redis.GetClient())
                    using (redis.AcquireLock(key + ":lock", TimeSpan.FromSeconds(3)))
                    {
                        value = redis.Get<T>(key);
                        if (value.Equals(default(T)))
                        {
                            value = await factory.Invoke();
                            if (!value.Equals(default(T)))
                            {
                                if (ttlGet != null)
                                {
                                    ttl = ttlGet.Invoke(value);
                                }

                                redis.Set(key, value, ttl ?? TimeSpan.FromDays(100));
                                _changeLog[key] = 1;
                                await _originalLocalCache.Set(key, Task.FromResult(value), ttl ?? TimeSpan.FromDays(100));
                                redis.PublishMessage(InvalidationChannel, key);
                            }
                        }
                        else
                        {
                            ttl = redis.GetTimeToLive(key);
                            //todo is ok?
                            await _originalLocalCache.Set(key, Task.FromResult(value), ttl ?? TimeSpan.FromDays(100));
                        }
                    }

                    return value;
                },
                ttl ?? TimeSpan.FromHours(1));
        }
    }
}

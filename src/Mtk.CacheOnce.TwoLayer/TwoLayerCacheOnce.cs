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
        private readonly IRedisPubSubServer _invalidationPubSub;
        private readonly ConcurrentDictionary<string, int> _changeLog = new ConcurrentDictionary<string, int>();

        public TwoLayerCacheOnce(IRedisClientsManager redis, IMemoryCache localCache)
        {
            _redis = redis;
            _localCache = new LocalCacheOnce(localCache, false);

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

        public T GetOrCreate<T>(int key, Func<T> factory, TimeSpan ttl)
        {
            throw new NotImplementedException();
        }

        public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan ttl)
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
                                redis.Set(key, value, ttl);
                                _changeLog[key] = 1;
                                redis.PublishMessage(InvalidationChannel, key);
                            }
                        }
                    }

                    return value;
                },
                ttl);
        }

        public Task<T> GetOrCreateAsync<T>(int key, Func<Task<T>> factory, TimeSpan ttl)
        {
            throw new NotImplementedException();
        }

        public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
        {
            throw new NotImplementedException();
        }

        public T GetOrCreate<T>(int key, Func<T> factory, DateTimeOffset expiresIn)
        {
            throw new NotImplementedException();
        }

        public T GetOrCreate<T>(string key, Func<T> factory, DateTimeOffset expiresIn)
        {
            throw new NotImplementedException();
        }

        public Task<T> GetOrCreateAsync<T>(int key, Func<Task<T>> factory, DateTimeOffset expiresIn)
        {
            throw new NotImplementedException();
        }

        public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, DateTimeOffset expiresIn)
        {
            throw new NotImplementedException();
        }

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
    }
}

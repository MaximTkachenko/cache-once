using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using ServiceStack.Redis;

namespace Mtk.CacheOnce.TwoLayer
{
    //todo cache transitions
    public sealed class TwoLayerCacheOnce : ICacheOnce
    {
        private const string InvalidationChannel = "InvalidationChannel";
        private static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(1);
        private static readonly TimeSpan DistributedLockTimeout = TimeSpan.FromSeconds(20);

        private readonly IRedisClientsManager _redis;
        private readonly ICacheOnce _localCache;
        private readonly IMemoryCache _originalLocalCache;
        private readonly bool _notifyAboutChanges;
        private readonly IRedisPubSubServer _invalidationPubSub;
        private readonly ConcurrentDictionary<string, int> _changeLog = new ConcurrentDictionary<string, int>();

        public TwoLayerCacheOnce(IRedisClientsManager redis, IMemoryCache localCache, bool notifyAboutChanges = false)
        {
            _redis = redis;
            _originalLocalCache = localCache;
            _localCache = new LocalCacheOnce(localCache, false);
            _notifyAboutChanges = notifyAboutChanges;

            if (notifyAboutChanges)
            {
                _invalidationPubSub = _redis.CreatePubSubServer(InvalidationChannel);
                _invalidationPubSub.OnMessage += (channel, key) =>
                {
                    if (!_changeLog.TryRemove(key, out _))
                    {
                        _originalLocalCache.Remove(key);
                    }
                };
                _invalidationPubSub.Start();
            }
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
                    using (redis.AcquireLock(key + ":lock", DistributedLockTimeout))
                    {
                        value = redis.Get<T>(key);
                        if (value == null || value.Equals(default(T)))
                        {
                            value = factory.Invoke();
                            if (ttlGet != null)
                            {
                                ttl = ttlGet.Invoke(value);
                            }

                            ttl = ttl ?? DefaultTtl;

                            redis.Set(key, value, ttl.Value);
                            if (_notifyAboutChanges)
                            {
                                _changeLog[key] = 1;
                            }

                            if (ttlGet != null)
                            {
                                _originalLocalCache.Set(key, new Lazy<T>(() => value), ttl.Value);
                            }

                            if (_notifyAboutChanges)
                            {
                                redis.PublishMessage(InvalidationChannel, key);
                            }
                        }
                        else
                        {
                            ttl = redis.GetTimeToLive(key);
                            if (ttl.HasValue)
                            {
                                _originalLocalCache.Set(key, new Lazy<T>(() => value), ttl.Value);
                            }
                            else
                            {
                                _originalLocalCache.Remove(key);
                            }
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
                    using (redis.AcquireLock(key + ":lock", DistributedLockTimeout))
                    {
                        value = redis.Get<T>(key);
                        if (value == null || value.Equals(default(T)))
                        {
                            value = await factory.Invoke();
                            if (ttlGet != null)
                            {
                                ttl = ttlGet.Invoke(value);
                            }

                            ttl = ttl ?? DefaultTtl;

                            redis.Set(key, value, ttl.Value);
                            if (_notifyAboutChanges)
                            {
                                _changeLog[key] = 1;
                            }
                            
                            if (ttlGet != null)
                            {
                                await _originalLocalCache.Set(key, Task.FromResult(value), ttl.Value);
                            }

                            if (_notifyAboutChanges)
                            {
                                redis.PublishMessage(InvalidationChannel, key);
                            }
                        }
                        else
                        {
                            ttl = redis.GetTimeToLive(key);
                            if (ttl.HasValue)
                            { 
                                await _originalLocalCache.Set(key, Task.FromResult(value), ttl.Value);
                            }
                            else
                            {
                                _originalLocalCache.Remove(key);
                            }
                        }
                    }

                    return value;
                },
                ttl ?? DefaultTtl);
        }
    }
}

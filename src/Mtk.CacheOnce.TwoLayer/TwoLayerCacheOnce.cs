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
        private static readonly string InvalidationChannel = "2layercacheonce:invalidationchannel";
        private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);
        private static readonly TimeSpan DistributedLockTimeout = TimeSpan.FromSeconds(20);

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

            _invalidationPubSub = _redis.CreatePubSubServer(InvalidationChannel);
            _invalidationPubSub.OnMessage += (channel, key) =>
            {
#if DEBUG
                Console.WriteLine("notified");
#endif
                if (!_changeLog.TryRemove(key, out _))
                {
#if DEBUG
                    Console.WriteLine("removed from local");
#endif
                    _originalLocalCache.Remove(key);
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
            Delete(key.ToString());
        }

        public void Delete(string key) 
        {
            using (var redis = _redis.GetClient())
            {
                redis.Remove(key);
                redis.PublishMessage(InvalidationChannel, key);
            }
        }

        public void Dispose()
        {
            _localCache?.Dispose();
            _invalidationPubSub?.Dispose();
        }

        private T GetOrCreateInternal<T>(string key, Func<T> factory, TimeSpan? ttl, Func<T, TimeSpan> ttlGet)
        {
            if (ttlGet == null && ttl.IsEmpty())
            {
                throw new ArgumentException(nameof(ttl));
            }

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

                            if (ttl.IsEmpty())
                            {
                                throw new ArgumentException(nameof(ttl));
                            }

                            redis.Set(key, value, ttl.Value);
                            _changeLog[key] = 1;
#if DEBUG
                            Console.WriteLine("set in redis");
#endif
                            if (ttlGet != null)
                            {
                                _originalLocalCache.Set(key, new Lazy<T>(() => value), ttl.Value);
                            }

                            redis.PublishMessage(InvalidationChannel, key);
                        }
                        else
                        {
                            ttl = redis.GetTimeToLive(key);
#if DEBUG
                            Console.WriteLine("get from redis");
#endif
                            if (ttl.IsEmpty())
                            {
                                _originalLocalCache.Remove(key);
                            }
                            else
                            {
                                _originalLocalCache.Set(key, new Lazy<T>(() => value), ttl.Value);
                            }
                        }
                    }

                    return value;
                },
                ttl ?? DefaultTtl);
        }

        private Task<T> GetOrCreateInternalAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl, Func<T, TimeSpan> ttlGet)
        {
            if (ttlGet == null && ttl.IsEmpty())
            {
                throw new ArgumentException(nameof(ttl));
            }

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

                            if (ttl.IsEmpty())
                            {
                                throw new ArgumentException(nameof(ttl));
                            }

                            redis.Set(key, value, ttl.Value);
#if DEBUG
                            Console.WriteLine("set in redis");
#endif
                            _changeLog[key] = 1;
                            
                            if (ttlGet != null)
                            {
                                await _originalLocalCache.Set(key, Task.FromResult(value), ttl.Value);
                            }

                            redis.PublishMessage(InvalidationChannel, key);
                        }
                        else
                        {
                            ttl = redis.GetTimeToLive(key);
#if DEBUG
                            Console.WriteLine("get from redis");
#endif
                            if (ttl.IsEmpty())
                            {
                                _originalLocalCache.Remove(key);
                            }
                            else
                            {
                                await _originalLocalCache.Set(key, Task.FromResult(value), ttl.Value);
                            }
                        }
                    }

                    return value;
                },
                ttl ?? DefaultTtl);
        }
    }
}

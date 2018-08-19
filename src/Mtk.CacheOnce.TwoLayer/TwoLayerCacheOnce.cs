using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using ServiceStack.Redis;

namespace Mtk.CacheOnce.TwoLayer
{
    //todo redis lock
    //todo listen for keys updates and invalidate them in invalid cache
    public sealed class TwoLayerCacheOnce : ICacheOnce
    {
        private readonly IRedisClientsManager _redis;
        private readonly ICacheOnce _localCache;

        public TwoLayerCacheOnce(IRedisClientsManager redis, IMemoryCache localCache)
        {
            _redis = redis;
            _localCache = new CacheOnce(localCache, false);
        }

        public T GetOrCreate<T>(object key, Func<T> factory, TimeSpan ttl)
        {
            throw new NotImplementedException();
        }

        public Task<T> GetOrCreateAsync<T>(object key, Func<Task<T>> factory, TimeSpan ttl)
        {
            throw new NotImplementedException();
        }

        public T GetOrCreate<T>(object key, Func<T> factory, DateTimeOffset expiresIn)
        {
            throw new NotImplementedException();
        }

        public Task<T> GetOrCreateAsync<T>(object key, Func<Task<T>> factory, DateTimeOffset expiresIn)
        {
            throw new NotImplementedException();
        }
    }
}

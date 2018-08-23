using System;
using System.Threading.Tasks;

namespace Mtk.CacheOnce
{
    public interface ICacheOnce : IDisposable
    {
        T GetOrCreate<T>(int key, Func<T> factory, TimeSpan ttl);
        T GetOrCreate<T>(int key, Func<T> factory, Func<T, TimeSpan> ttlGet);
        T GetOrCreate<T>(string key, Func<T> factory, TimeSpan ttl);
        T GetOrCreate<T>(string key, Func<T> factory, Func<T, TimeSpan> ttlGet);
        Task<T> GetOrCreateAsync<T>(int key, Func<Task<T>> factory, TimeSpan ttl);
        Task<T> GetOrCreateAsync<T>(int key, Func<Task<T>> factory, Func<T, TimeSpan> ttlGet);
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl);
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, Func<T, TimeSpan> ttlGet);
        void Delete(int key);
        void Delete(string key);
    }
}

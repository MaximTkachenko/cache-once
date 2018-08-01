using System;
using System.Threading.Tasks;

namespace Mtk.LazyCache
{
    public interface ILazyCache
    {
        T GetOrCreate<T>(object key, Func<T> factory, TimeSpan ttl);
        Task<T> GetOrCreateAsync<T>(object key, Func<Task<T>> factory, TimeSpan ttl);
        T GetOrCreate<T>(object key, Func<T> factory, DateTimeOffset expiresIn);
        Task<T> GetOrCreateAsync<T>(object key, Func<Task<T>> factory, DateTimeOffset expiresIn);
    }
}

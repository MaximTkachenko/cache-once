using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Mtk.LazyCache.Tests
{
    public class LazyCacheTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task LazyGetOrCreate_MultipleThreads_InitOnceAndAllValuesTheSame(bool perKey)
        {
            var cnt = 20;
            var cache = new LazyCache(new MemoryCache(new MemoryCacheOptions()), perKey);
            var service = new TestInitializationService();
            var bag = new ConcurrentBag<int>();

            var tasks = new Task[cnt];
            for (int i = 0; i < cnt; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    int val = cache.GetOrCreate(cnt, () => service.Init(), TimeSpan.FromDays(1));
                    bag.Add(val);
                });
            }
            await Task.WhenAll(tasks);

            var results = bag.ToArray();
            Assert.True(results.All(x => x == results[0]));
            Assert.Equal(1, service.CountOfInitializations);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task LazyGetOrCreateAsync_MultipleThreads_InitOnceAndAllValuesTheSame(bool perKey)
        {
            var cnt = 20;
            var cache = new LazyCache(new MemoryCache(new MemoryCacheOptions()), perKey);
            var service = new TestInitializationService();
            var bag = new ConcurrentBag<int>();

            var tasks = new Task[cnt];
            for (int i = 0; i < cnt; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    int val = await cache.GetOrCreateAsync(cnt, () => service.InitAsync(), TimeSpan.FromDays(1));
                    bag.Add(val);
                });
            }
            await Task.WhenAll(tasks);

            var results = bag.ToArray();
            Assert.True(results.All(x => x == results[0]));
            Assert.Equal(1, service.CountOfInitializations);
        }
    }
}

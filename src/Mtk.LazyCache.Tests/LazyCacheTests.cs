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
        public async Task GetOrCreate_MultipleThreads_InitOnceAndAllValuesTheSame(bool perKey)
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
        public async Task GetOrCreateAsync_MultipleThreads_InitOnceAndAllValuesTheSame(bool perKey)
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetOrCreateAsync_MultipleThreadsCallFailedMethod_FailedKeyRemovedThenInitAgain(bool perKey)
        {
            var cnt = 10;
            var cache = new LazyCache(new MemoryCache(new MemoryCacheOptions()), perKey);
            var service = new TestInitializationService();
            var bag = new ConcurrentBag<int>();

            var tasks = new Task[cnt];
            for (int i = 0; i < cnt; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    int val = await cache.GetOrCreateAsync(cnt, async () => await service.FailedHttpAsync(), TimeSpan.FromDays(1));
                    bag.Add(val);
                });
            }
            await Task.WhenAll(tasks);
            await Task.Run(async () =>
            {
                int val = await cache.GetOrCreateAsync(cnt, async () => await service.FailedHttpAsync(), TimeSpan.FromDays(1));
                bag.Add(val);
            });

            var results = bag.ToArray();
            Assert.True(results.All(x => x == default(int)));
            Assert.Equal(2, service.CountOfInitializations);
        }
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetOrCreateAsync_MultipleThreadsCallUnstableHttpWithRetryAsync_ValueFinallyInitiated(bool perKey)
        {
            var cnt = 10;
            var cache = new LazyCache(new MemoryCache(new MemoryCacheOptions()), perKey);
            var service = new TestInitializationService();
            var bag = new ConcurrentBag<int>();
            int successAfterIteration = 4;

            var tasks = new Task[cnt];
            for (int i = 0; i < cnt; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    int val = await cache.GetOrCreateAsync(cnt, async () => await service.UnstableHttpWithRetryAsync(successAfterIteration), TimeSpan.FromDays(1));
                    bag.Add(val);
                });
            }
            await Task.WhenAll(tasks);
            await Task.Run(async () =>
            {
                int val = await cache.GetOrCreateAsync(cnt, async () => await service.UnstableHttpWithRetryAsync(successAfterIteration), TimeSpan.FromDays(1));
                bag.Add(val);
            });

            var results = bag.ToArray();
            Assert.True(results.All(x => x == 200));
            Assert.Equal(successAfterIteration, service.CountOfInitializations);
        }
    }
}

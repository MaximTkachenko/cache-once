using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Mtk.CacheOnce.Tests
{
    public class MemoryCacheOnceExtensionsTests
    {
        [Fact]
        public async Task GetOrCreateOnceAsync_WithTtlWhithMulipleThreads_InitOnce()
        {
            var threadsCount = 10;
            var cache = new MemoryCache(new MemoryCacheOptions());
            int result = 0;

            var tasks = new Task<int>[threadsCount];
            int calls = 0;
            for (int i = 0; i < threadsCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    return cache.GetOrCreateOnceAsync("bla",
                        async () =>
                        {
                            await Task.Delay(200);
                            Interlocked.Increment(ref calls);
                            return Interlocked.Increment(ref result);
                        },
                        TimeSpan.FromHours(1));
                });
            }

            await Task.WhenAll(tasks);
            tasks.Select(t => t.Result).All(v => v == tasks[0].Result).Should().BeTrue();
            calls.Should().Be(1);
            cache.TryGetValue("bla", out _).Should().BeTrue();
        }

        [Fact]
        public async Task GetOrCreateOnceAsync_WithTtlGetWhithMulipleThreads_InitOnce()
        {
            var threadsCount = 10;
            var cache = new MemoryCache(new MemoryCacheOptions());
            int result = 0;

            var tasks = new Task<(int Value, TimeSpan Ttl)>[threadsCount];
            int calls = 0;
            for (int i = 0; i < threadsCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    return cache.GetOrCreateOnceAsync("bla",
                        async () =>
                        {
                            await Task.Delay(200);
                            Interlocked.Increment(ref calls);
                            return (Interlocked.Increment(ref result), TimeSpan.FromMilliseconds(100));
                        },
                        d => d.Item2);
                });
            }

            await Task.WhenAll(tasks);
            tasks.Select(t => t.Result).All(v => v.Value == tasks[0].Result.Item1).Should().BeTrue();
            calls.Should().Be(1);

            await Task.Delay(TimeSpan.FromMilliseconds(200));
            cache.TryGetValue("bla", out _).Should().BeFalse();
        }

        [Fact]
        public async Task GetOrCreateOnceAsync_ExceptionWhithMulipleThreads_CacheItemRemoved()
        {
            var threadsCount = 10;
            var cache = new MemoryCache(new MemoryCacheOptions());

            var tasks = new Task[threadsCount];
            int calls = 0;
            for (int i = 0; i < threadsCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    Func<Task> action = async () =>
                        await cache.GetOrCreateOnceAsync("bla",
                            async () =>
                            {
                                await Task.Delay(200);
                                Interlocked.Increment(ref calls);
                                throw new DivideByZeroException("TEST");
#pragma warning disable 162
                                return 0;
#pragma warning restore 162
                            },
                            TimeSpan.FromHours(1));

                    action.Should().NotThrow();
                });
            }

            await Task.WhenAll(tasks);
            tasks.Count(t => t.IsCompleted).Should().Be(threadsCount);
            cache.TryGetValue("bla", out _).Should().BeFalse();
            calls.Should().Be(1);
        }

        [Fact]
        public void DeleteOnceAsync_NoItem_NoException()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());

            Func<Task> action = () => cache.DeleteOnceAsync("bla");

            action.Should().NotThrow();
        }

        [Fact]
        public async Task DeleteOnceAsync_WithTtl_Removed()
        {
            var threadsCount = 10;
            var cache = new MemoryCache(new MemoryCacheOptions());

            var tasks = new Task[threadsCount];
            for (int i = 0; i < threadsCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    return cache.GetOrCreateOnceAsync("bla",
                        async () =>
                        {
                            await Task.Delay(200);
                            return (1, TimeSpan.FromHours(100));
                        },
                        d => d.Item2);
                });
            }

            await Task.WhenAll(tasks);
            await Task.Run(() => cache.DeleteOnceAsync("bla"));

            cache.TryGetValue("bla", out _).Should().BeFalse();
        }

        [Fact]
        public async Task GetOrCreateOnceAsync_NestedCachUsage_InitOnceNoDeadlock()
        {
            var threadsCount = 10;
            var cache = new MemoryCache(new MemoryCacheOptions());

            var tasks = new Task<int>[threadsCount];
            int calls = 0;
            for (int i = 0; i < threadsCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    return cache.GetOrCreateOnceAsync("bla",
                        async () =>
                        {
                            await Task.Delay(1000);
                            Interlocked.Increment(ref calls);
                            return await GetIntCached(cache);
                        },
                        TimeSpan.FromHours(1));
                });
            }

            await Task.WhenAll(tasks);
            tasks.Select(t => t.Result).All(v => v == 1).Should().BeTrue();
            calls.Should().Be(1);
            cache.TryGetValue("bla", out _).Should().BeTrue();
        }

        private Task<int> GetIntCached(MemoryCache cache)
        {
            return cache.GetOrCreateOnceAsync("result", GetInt, TimeSpan.FromHours(1));
        }

        private async Task<int> GetInt()
        {
            await Task.Delay(500);
            return 1;
        }
    }
}

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
        public async Task IssueOnceAsync_WithTtlWhithMulipleThreads_InitOnce()
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
                    return cache.IssueOnceAsync("bla",
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
        public async Task IssueOnceAsync_WithTtlGetWhithMulipleThreads_InitOnce()
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
                    return cache.IssueOnceAsync("bla",
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
        public async Task IssueOnceAsync_ExceptionWhithMulipleThreads_CacheItemRemoved()
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
                        await cache.IssueOnceAsync("bla",
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
        public async Task IssueOnceAsync_NestedCachUsage_InitOnceNoDeadlock()
        {
            var threadsCount = 10;
            var cache = new MemoryCache(new MemoryCacheOptions());

            var tasks = new Task<int>[threadsCount];
            int calls = 0;
            for (int i = 0; i < threadsCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    return cache.IssueOnceAsync("bla",
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

        [Fact]
        public async Task IssueOnceAsync_WithInvalidValue_RenewOnceNoDeadlock()
        {
            var threadsCount = 5;
            var cache = new MemoryCache(new MemoryCacheOptions());
            var firstValue = "firstValue";
            var secondValue = "secondValue";

            await cache.IssueOnceAsync("bla",
                () => Task.FromResult(firstValue),
                TimeSpan.FromHours(1));

            var tasks = new Task<string>[threadsCount];
            int calls = 0;
            for (int i = 0; i < threadsCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    return cache.IssueOnceAsync("bla",
                        async () =>
                        {
                            await Task.Delay(100);
                            Interlocked.Increment(ref calls);
                            return await Task.FromResult(secondValue);
                        },
                        TimeSpan.FromHours(1), firstValue);
                });
            }

            await Task.WhenAll(tasks);
            tasks.Select(t => t.Result).All(v => v == secondValue).Should().BeTrue();
            calls.Should().Be(1);
            cache.TryGetValue("bla", out _).Should().BeTrue();
        }

        private Task<int> GetIntCached(IMemoryCache cache)
        {
            return cache.IssueOnceAsync("result", GetInt, TimeSpan.FromHours(1));
        }

        private async Task<int> GetInt()
        {
            await Task.Delay(500);
            return 1;
        }
    }
}

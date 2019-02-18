using System;
using System.Collections.Concurrent;
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
        private const int TaskCount = 10;

        [Fact]
        public async Task IssueOnceAsync_WithTtlWithMultipleThreads_InitOnce()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            int result = 0;

            var tasks = new Task<int>[TaskCount];
            int calls = 0;
            for (int i = 0; i < TaskCount; i++)
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
        public async Task IssueOnceAsync_WithTtlGetWithMultipleThreads_InitOnce()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            int result = 0;

            var tasks = new Task<(int Value, TimeSpan Ttl)>[TaskCount];
            int calls = 0;
            for (int i = 0; i < TaskCount; i++)
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
        public async Task IssueOnceAsync_ConcurrentlyExceptionThenCorrectResult_FailedItemRemovedThenCorrectItemAdded()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var result = 88;
            var results = new ConcurrentBag<int>();

            var tasks = new Task[TaskCount];
            var maxIndexToFail = TaskCount / 2;
            int calls = 0;
            for (int i = 0; i < TaskCount; i++)
            {
                bool fail = i < maxIndexToFail;
                tasks[i] = Task.Run(async () =>
                {
                    if (!fail)
                    {
                        await Task.Delay(100);
                    }

                    var resultFromCache = await cache.IssueOnceAsync("bla",
                        async () =>
                        {
                            await Task.Delay(50);
                            Interlocked.Increment(ref calls);

                            if (fail)
                            {
                                throw new DivideByZeroException("TEST");
                            }

                            return result;
                        },
                        TimeSpan.FromHours(1));

                    results.Add(resultFromCache);
                });
            }

            await Task.WhenAll(tasks);
            tasks.Count(t => t.IsCompleted).Should().Be(TaskCount);
            cache.TryGetValue("bla", out _).Should().BeTrue();
            results.Count(x => x == result).Should().Be(TaskCount - maxIndexToFail);
            results.Count(x => x == default(int)).Should().Be(maxIndexToFail);
            calls.Should().Be(2);
        }

        [Fact]
        public async Task IssueOnceAsync_NestedCacheUsage_InitOnceNoDeadlock()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());

            var tasks = new Task<int>[TaskCount];
            int calls = 0;
            for (int i = 0; i < TaskCount; i++)
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
            var cache = new MemoryCache(new MemoryCacheOptions());
            var firstValue = "firstValue";
            var secondValue = "secondValue";

            await cache.IssueOnceAsync("bla",
                () => Task.FromResult(firstValue),
                TimeSpan.FromHours(1));

            var tasks = new Task<string>[TaskCount];
            int calls = 0;
            for (int i = 0; i < TaskCount; i++)
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

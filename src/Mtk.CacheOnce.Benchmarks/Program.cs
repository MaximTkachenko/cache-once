using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Caching.Memory;

namespace Mtk.CacheOnce.Benchmarks
{
    class Program
    {
        static void Main()
        {
            var summary = BenchmarkRunner.Run<CacheOnceBenchmarks>();
        }
    }

    [ConcurrencyVisualizerProfiler]
    public class CacheOnceBenchmarks
    {
        private const int ConcurrentCount = 10;
        private readonly IMemoryCache _cache;

        public CacheOnceBenchmarks()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        [Benchmark]
        public void CacheOnce_OneKey()
        {
            var tasks = new Task[ConcurrentCount];
            for (int i = 0; i < ConcurrentCount; i++)
            {
                tasks[i] = Task.Run(() => {
                    return _cache.IssueOnceAsync("bla",
                        async () =>
                        {
                            await Task.Delay(200);
                            return 10;
                        },
                        TimeSpan.FromHours(1));
                });
            }

            Task.WhenAll(tasks).GetAwaiter().GetResult();
        }

        [Benchmark]
        public void CacheOnce_MultipleKeys()
        {
            var tasks = new Task[ConcurrentCount * ConcurrentCount];
            for (int i = 0; i < ConcurrentCount; i++)
            {
                for (int j = 0; j < ConcurrentCount; j++)
                {
                    var local = j;
                    tasks[i * ConcurrentCount + local] = Task.Run(() => {
                        return _cache.IssueOnceAsync($"bla_{local}",
                            async () =>
                            {
                                await Task.Delay(200);
                                return 10;
                            },
                            TimeSpan.FromHours(1));
                    });
                }
            }

            Task.WhenAll(tasks).GetAwaiter().GetResult();
        }
    }
}

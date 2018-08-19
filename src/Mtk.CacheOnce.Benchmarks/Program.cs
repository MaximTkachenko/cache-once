using System;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Caching.Memory;

namespace Mtk.CacheOnce.Benchmarks
{
    public class LazyCacheForHttpBenchmark
    {
        private readonly HttpClient _httpClient;

        [Params(10, 20)]
        public int DegreeOfParallelism { get; set; }

        [Params("http://mtkachenko.me")]//, "http://mtkachenko.me|https://ya.ru", "http://mtkachenko.me|https://ya.ru|https://google.com")]
        public string Urls { get; set; }

        public LazyCacheForHttpBenchmark()
        {
            _httpClient = new HttpClient();
        }

        [Benchmark]
        public async Task<string> CacheNoLock()
        {
            var urls = Urls.Split('|');
            var cache = new MemoryCache(new MemoryCacheOptions());
            var tasks = new Task<string>[DegreeOfParallelism];
            for (int i = 0; i < DegreeOfParallelism; i++)
            {
                foreach (var url in urls)
                {
                    tasks[i] = Task.Run(async () =>
                    {
                        return await cache.GetOrCreateAsync(url,
                            async entry =>
                            {
                                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                                var response = await _httpClient.GetAsync(url);
                                return await response.Content.ReadAsStringAsync();
                            });
                    });
                }
            }

            await Task.WhenAll(tasks);
            return tasks[0].Result;
        }

        [Benchmark]
        public async Task<string> CacheGlobalLock()
        {
            var urls = Urls.Split('|');
            var cache = new Mtk.CacheOnce.CacheOnce(new MemoryCache(new MemoryCacheOptions()), false);
            var tasks = new Task<string>[DegreeOfParallelism];
            for (int i = 0; i < DegreeOfParallelism; i++)
            {
                foreach (var url in urls)
                {
                    tasks[i] = Task.Run(async () =>
                    {
                        return await cache.GetOrCreateAsync(url,
                            async () =>
                            {
                                var response = await _httpClient.GetAsync(url);
                                return await response.Content.ReadAsStringAsync();
                            },
                            TimeSpan.FromHours(1));
                    });
                }
            }

            await Task.WhenAll(tasks);
            return tasks[0].Result;
        }

        [Benchmark]
        public async Task<string> CacheLockPerKey()
        {
            var urls = Urls.Split('|');
            var cache = new Mtk.CacheOnce.CacheOnce(new MemoryCache(new MemoryCacheOptions()), true);
            var tasks = new Task<string>[DegreeOfParallelism];
            for (int i = 0; i < DegreeOfParallelism; i++)
            {
                foreach (var url in urls)
                {
                    tasks[i] = Task.Run(async () =>
                    {
                        return await cache.GetOrCreateAsync(url,
                            async () =>
                            {
                                var response = await _httpClient.GetAsync(url);
                                return await response.Content.ReadAsStringAsync();
                            },
                            TimeSpan.FromHours(1));
                    });
                }
            }

            await Task.WhenAll(tasks);
            return tasks[0].Result;
        }

        //https://benchmarkdotnet.org/articles/configs/diagnosers.html
    }

    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<LazyCacheForHttpBenchmark>();
        }
    }
}

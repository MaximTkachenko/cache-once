using System;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Caching.Memory;

namespace Mtk.LazyCache.Benchmarks
{
    public class LazyCacheForHttpBenchmark
    {
        private const int DegreeOfParallelism = 20;
        private const string Url = "http://mtkachenko.me";
        private readonly HttpClient _httpClient;

        public LazyCacheForHttpBenchmark()
        {
            _httpClient = new HttpClient();
        }

        [Benchmark]
        public async Task<string> NoChache()
        {
            var tasks = new Task<string>[DegreeOfParallelism];
            for (int i = 0; i < DegreeOfParallelism; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    var response =  await _httpClient.GetAsync(Url);
                    return await response.Content.ReadAsStringAsync();
                });
            }

            await Task.WhenAll(tasks);
            return tasks[0].Result;
        }

        [Benchmark]
        public async Task<string> LazyCacheGlobalLock()
        {
            var cache = new LazyCache(new MemoryCache(new MemoryCacheOptions()), false);
            var tasks = new Task<string>[DegreeOfParallelism];
            for (int i = 0; i < DegreeOfParallelism; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    return await cache.GetOrCreateAsync(Url, 
                        async () =>
                        {
                            var response = await _httpClient.GetAsync(Url);
                            return await response.Content.ReadAsStringAsync();
                        }, 
                        TimeSpan.FromHours(1));
                });
            }

            await Task.WhenAll(tasks);
            return tasks[0].Result;
        }

        [Benchmark]
        public async Task<string> LazyCacheLockPerKey()
        {
            var cache = new LazyCache(new MemoryCache(new MemoryCacheOptions()), true);
            var tasks = new Task<string>[DegreeOfParallelism];
            for (int i = 0; i < DegreeOfParallelism; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    return await cache.GetOrCreateAsync(Url,
                        async () =>
                        {
                            var response = await _httpClient.GetAsync(Url);
                            return await response.Content.ReadAsStringAsync();
                        },
                        TimeSpan.FromHours(1));
                });
            }

            await Task.WhenAll(tasks);
            return tasks[0].Result;
        }

        //todo add benchmark for different keys
    }

    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<LazyCacheForHttpBenchmark>();
        }
    }
}

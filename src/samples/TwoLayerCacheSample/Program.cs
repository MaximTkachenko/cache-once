using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Mtk.CacheOnce;
using Mtk.CacheOnce.TwoLayer;
using ServiceStack.Redis;

namespace TwoLayerCacheSample
{
    class Program
    {
        private static readonly IRedisClientsManager ClientsManager = new RedisManagerPool("localhost:6379?db=1");
        private static readonly ICacheOnce Cache = new TwoLayerCacheOnce(ClientsManager, new MemoryCache(new MemoryCacheOptions()));
        private static readonly string AppId = Guid.NewGuid().ToString();
        private static readonly HttpClient HttpClient = new HttpClient();

        static void Main(string[] args)
        {
            Console.WriteLine(AppId);
            Console.WriteLine();

            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            var cnt = 4;

            while (true)
            {
                var tasks = new Task[cnt];
                for (int i = 0; i < cnt; i++)
                {
                    tasks[i] = Task.Run(async () =>
                    {
                        string code = await Cache.GetOrCreateAsync("2layercache:data", async () =>
                            {
                                await Task.Delay(1000);
                                var result = AppId;
                                Console.WriteLine($"IN LOCK: {result}");
                                return result;
                            },
                            TimeSpan.FromSeconds(20));

                        Console.WriteLine(code);
                    });
                }

                await Task.WhenAll(tasks);
                Console.WriteLine("----------------------");
                await Task.Delay(5000);
            }
        }
    }
}

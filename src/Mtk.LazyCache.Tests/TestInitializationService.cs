using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mtk.LazyCache.Tests
{
    public class TestInitializationService
    {
        private readonly HttpClient _client = new HttpClient{ Timeout = TimeSpan.FromMilliseconds(100) };

        public int CountOfInitializations;

        public int Init()
        {
            Interlocked.Increment(ref CountOfInitializations);
            Thread.Sleep(100);
            return Thread.CurrentThread.ManagedThreadId;
        }

        public async Task<int> InitAsync()
        {
            Interlocked.Increment(ref CountOfInitializations);
            await Task.Delay(100);
            return Thread.CurrentThread.ManagedThreadId;
        }

        public async Task<int> FailedHttpAsync()
        {
            Interlocked.Increment(ref CountOfInitializations);
            await Task.Delay(100);
            return (int)(await _client.GetAsync("http://localhost:1")).StatusCode;
        }

        public async Task<int> UnstableHttpWithRetryAsync(int successAfterIteration)
        {
            int retryCount = 5;
            int retryIteration = 0;
            var toWait = TimeSpan.FromMilliseconds(20);

            while (true)
            {
                try
                {
                    return await UnstableHttpAsync(successAfterIteration);
                }
                catch
                {
                    retryIteration++;
                    if (retryCount == retryIteration)
                    {
                        throw;
                    }

                    await Task.Delay(toWait);
                }
            }
        }

        private async Task<int> UnstableHttpAsync(int successAfterIteration)
        {
            var value = Interlocked.Increment(ref CountOfInitializations);
            return (int)(await _client.GetAsync(value == successAfterIteration ? "http://mtkachenko.me" : "http://localhost:1")).StatusCode;
        }
    }
}

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mtk.LazyCache.Tests
{
    public class TestInitializationService
    {
        private readonly HttpClient _client = new HttpClient{ Timeout = TimeSpan.FromMilliseconds(10) };

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
    }
}

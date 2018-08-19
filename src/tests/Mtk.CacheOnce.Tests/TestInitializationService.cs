using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mtk.CacheOnce.Tests
{
    public class TestInitializationService
    {
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

        public async Task<int> FailedAsync()
        {
            Interlocked.Increment(ref CountOfInitializations);
            await Task.Delay(100);
            throw new Exception("doesn't work");
        }

        public async Task<int> UnstablepWithRetryAsync(int successAfter)
        {
            int retryCount = 5;
            int retryIteration = 0;
            var toWait = TimeSpan.FromMilliseconds(20);

            while (true)
            {
                try
                {
                    return await UnstableHttpAsync(successAfter);
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

        private async Task<int> UnstableHttpAsync(int successAfter)
        {
            var value = Interlocked.Increment(ref CountOfInitializations);
            await Task.Delay(10);
            if (value == successAfter)
            {
                return 200;
            }
            throw new Exception("doesn't work");
        }
    }
}

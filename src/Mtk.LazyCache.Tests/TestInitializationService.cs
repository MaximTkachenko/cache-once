using System.Threading;
using System.Threading.Tasks;

namespace Mtk.LazyCache.Tests
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
    }
}

using System.Threading;
using System.Threading.Tasks;

namespace Mtk.LazyCache.Tests
{
    public class TestInitializationService
    {
        private int _count;

        public int CountOfInitializations => Interlocked.CompareExchange(ref _count, 0, 0);

        public int Init()
        {
            Interlocked.Increment(ref _count);
            Thread.Sleep(100);
            return Thread.CurrentThread.ManagedThreadId;
        }

        public async Task<int> InitAsync()
        {
            Interlocked.Increment(ref _count);
            await Task.Delay(100);
            return Thread.CurrentThread.ManagedThreadId;
        }
    }
}

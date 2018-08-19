using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mtk.CacheOnce
{
    /// <summary>
    /// Slightly modified https://stackoverflow.com/questions/31138179/asynchronous-locking-based-on-a-key
    /// </summary>
    internal sealed class KeyedSemaphoreSlim
    {
        private sealed class RefCounter
        {
            public RefCounter(SemaphoreSlim value)
            {
                RefCount = 1;
                Value = value;
            }

            public int RefCount { get; set; }
            public SemaphoreSlim Value { get; }
        }

        private static readonly Dictionary<object, RefCounter> SemaphoreSlims
            = new Dictionary<object, RefCounter>();

        private SemaphoreSlim GetOrCreate(object key)
        {
            RefCounter item;
            lock (SemaphoreSlims)
            {
                if (SemaphoreSlims.TryGetValue(key, out item))
                {
                    ++item.RefCount;
                }
                else
                {
                    item = new RefCounter(new SemaphoreSlim(1, 1));
                    SemaphoreSlims[key] = item;
                }
            }
            return item.Value;
        }

        public IDisposable Lock(object key)
        {
            GetOrCreate(key).Wait();
            return new Releaser(key);
        }

        public async Task<IDisposable> LockAsync(object key)
        {
            await GetOrCreate(key).WaitAsync().ConfigureAwait(false);
            return new Releaser(key);
        }

        private sealed class Releaser : IDisposable
        {
            private readonly object _key;

            public Releaser(object key)
            {
                _key = key;
            }

            public void Dispose()
            {
                RefCounter item;
                lock (SemaphoreSlims)
                {
                    item = SemaphoreSlims[_key];
                    --item.RefCount;
                    if (item.RefCount == 0)
                    {
                        SemaphoreSlims.Remove(_key);
                    }
                }
                item.Value.Release();
            }
        }

    }
}

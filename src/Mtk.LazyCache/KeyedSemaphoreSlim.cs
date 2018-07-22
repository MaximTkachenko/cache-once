using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mtk.LazyCache
{
    /// <summary>
    /// Slightly modified https://stackoverflow.com/questions/31138179/asynchronous-locking-based-on-a-key
    /// </summary>
    internal sealed class KeyedSemaphoreSlim
    {
        private sealed class RefCounted
        {
            public RefCounted(SemaphoreSlim value)
            {
                RefCount = 1;
                Value = value;
            }

            public int RefCount { get; set; }
            public SemaphoreSlim Value { get; }
        }

        private static readonly Dictionary<object, RefCounted> SemaphoreSlims
            = new Dictionary<object, RefCounted>();

        private SemaphoreSlim GetOrCreate(object key)
        {
            RefCounted item;
            lock (SemaphoreSlims)
            {
                if (SemaphoreSlims.TryGetValue(key, out item))
                {
                    ++item.RefCount;
                }
                else
                {
                    item = new RefCounted(new SemaphoreSlim(1, 1));
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
                RefCounted item;
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

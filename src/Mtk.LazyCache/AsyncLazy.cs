using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Mtk.LazyCache
{
    /// <summary>
    /// See https://blogs.msdn.microsoft.com/pfxteam/2011/01/15/asynclazyt/
    /// </summary>
    internal sealed class AsyncLazy<T> : Lazy<Task<T>>
    {
        public AsyncLazy(Func<T> valueFactory) :
            base(() => Task.Factory.StartNew(valueFactory))
        {
        }

        public AsyncLazy(Func<Task<T>> taskFactory) :
            base(() => Task.Factory.StartNew(taskFactory).Unwrap())
        {
        }

        public TaskAwaiter<T> GetAwaiter()
        {
            return Value.GetAwaiter();
        }
    }
}

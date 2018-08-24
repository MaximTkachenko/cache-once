using System;

namespace Mtk.CacheOnce
{
    public static class TimeSpanExt
    {
        public static bool IsEmpty(this TimeSpan? ts) => !ts.HasValue || ts.Value == TimeSpan.Zero;

        public static bool IsEmpty(this TimeSpan ts) => ts == TimeSpan.Zero;
    }
}

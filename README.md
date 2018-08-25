# cache-once

Cache results of HTTP requests, db queries etc. in a thread safe manner and exactly once.

[LocalCacheOnce](https://github.com/MaximTkachenko/cache-once/blob/master/src/Mtk.CacheOnce/LocalCacheOnce.cs) is an in-memory cache, it internally uses [IMemoryCache](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.imemorycache?view=aspnetcore-2.1).

[TwoLayerCacheOnce](https://github.com/MaximTkachenko/cache-once/blob/master/src/Mtk.CacheOnce.TwoLayer/TwoLayerCacheOnce.cs) is a two-layer cache, it uses [IMemoryCache](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.imemorycache?view=aspnetcore-2.1) and Redis ([ServiceStack.Redis](https://github.com/ServiceStack/ServiceStack.Redis)). Try [sample project](https://github.com/MaximTkachenko/cache-once/tree/master/src/samples/TwoLayerCacheSample).

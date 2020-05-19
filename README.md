![.NET Core](https://github.com/MaximTkachenko/cache-once/workflows/.NET%20Core/badge.svg)

# cache-once

Cache results of HTTP requests, db queries etc. calling initialization logic exactly once in a thread safe manner. It's supposed to be used for in-memory cache only. If you have `do-it-once` requirement across several processes you need something different like [distributed lock](https://martin.kleppmann.com/2016/02/08/how-to-do-distributed-locking.html) or [leader election](https://en.wikipedia.org/wiki/Leader_election).

It's implemented as a set of extension methods for [IMemoryCache](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.imemorycache?view=aspnetcore-2.2).

## Tasks
- fix benchmarks
- add this https://adamsitnik.com/ConcurrencyVisualizer-Profiler/
- read https://developers.google.com/api-client-library/dotnet/guide/aaa_oauth for inspiration
- try fine grained lock approach - https://github.com/khalidsalomao/SimpleHelpers.Net/blob/master/SimpleHelpers/NamedLock.cs

![.NET Core](https://github.com/MaximTkachenko/cache-once/workflows/.NET%20Core/badge.svg)

# cache-once

Cache results of HTTP requests, db queries etc. calling initialization logic exactly once in a thread safe manner. It's implemented as a set of extension methods for [IMemoryCache](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.imemorycache?view=aspnetcore-2.2).

## Tasks
- fix benchmarks
- add this https://adamsitnik.com/ConcurrencyVisualizer-Profiler/
- read https://developers.google.com/api-client-library/dotnet/guide/aaa_oauth for inspiration

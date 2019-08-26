[![Build Status](https://dev.azure.com/mtkorg/oss-projects/_apis/build/status/MaximTkachenko.cache-once%20(1)?branchName=master)](https://dev.azure.com/mtkorg/oss-projects/_build/latest?definitionId=9&branchName=master)

# cache-once

Cache results of HTTP requests, db queries etc. calling initialization logic exactly once in a thread safe manner. It's implemented as a set of extension methods for [IMemoryCache](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.imemorycache?view=aspnetcore-2.2).

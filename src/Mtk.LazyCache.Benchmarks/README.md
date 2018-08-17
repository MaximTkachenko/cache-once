``` ini

BenchmarkDotNet=v0.11.0, OS=Windows 10.0.17134.228 (1803/April2018Update/Redstone4)
Intel Core i7-7500U CPU 2.70GHz (Kaby Lake), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=2.1.400
  [Host]     : .NET Core 2.0.9 (CoreCLR 4.6.26614.01, CoreFX 4.6.26614.01), 64bit RyuJIT
  DefaultJob : .NET Core 2.0.9 (CoreCLR 4.6.26614.01, CoreFX 4.6.26614.01), 64bit RyuJIT


```
|          Method | DegreeOfParallelism |                 Urls |      Mean |     Error |    StdDev |
|---------------- |-------------------- |--------------------- |----------:|----------:|----------:|
|    **ChacheNoLock** |                  **10** | **http://mtkachenko.me** | **221.14 ms** |  **8.235 ms** | **24.281 ms** |
| CacheGlobalLock |                  10 | http://mtkachenko.me |  34.40 ms |  1.084 ms |  3.076 ms |
| CacheLockPerKey |                  10 | http://mtkachenko.me |  34.92 ms |  1.255 ms |  3.681 ms |
|    **ChacheNoLock** |                  **20** | **http://mtkachenko.me** | **434.56 ms** | **16.383 ms** | **48.048 ms** |
| CacheGlobalLock |                  20 | http://mtkachenko.me |  32.37 ms |  1.091 ms |  3.149 ms |
| CacheLockPerKey |                  20 | http://mtkachenko.me |  30.04 ms |  1.007 ms |  2.968 ms |

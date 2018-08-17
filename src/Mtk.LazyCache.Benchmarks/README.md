``` ini

BenchmarkDotNet=v0.11.0, OS=Windows 10.0.17134.228 (1803/April2018Update/Redstone4)
Intel Core i7-7500U CPU 2.70GHz (Kaby Lake), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=2.1.400
  [Host]     : .NET Core 2.0.9 (CoreCLR 4.6.26614.01, CoreFX 4.6.26614.01), 64bit RyuJIT
  DefaultJob : .NET Core 2.0.9 (CoreCLR 4.6.26614.01, CoreFX 4.6.26614.01), 64bit RyuJIT


```
|              Method | DegreeOfParallelism |                 Urls |      Mean |     Error |    StdDev |
|-------------------- |-------------------- |--------------------- |----------:|----------:|----------:|
|            **NoChache** |                  **10** | **http://mtkachenko.me** | **213.98 ms** | **9.4556 ms** | **27.732 ms** |
| LazyCacheGlobalLock |                  10 | http://mtkachenko.me |  32.50 ms | 1.3933 ms |  4.108 ms |
| LazyCacheLockPerKey |                  10 | http://mtkachenko.me |  32.79 ms | 0.9984 ms |  2.928 ms |
|            **NoChache** |                  **20** | **http://mtkachenko.me** |        **NA** |        **NA** |        **NA** |
| LazyCacheGlobalLock |                  20 | http://mtkachenko.me |  35.75 ms | 1.6573 ms |  4.808 ms |
| LazyCacheLockPerKey |                  20 | http://mtkachenko.me |  36.30 ms | 1.3498 ms |  3.980 ms |

Benchmarks with issues:
  LazyCacheForHttpBenchmark.NoChache: DefaultJob [DegreeOfParallelism=20, Urls=http://mtkachenko.me]

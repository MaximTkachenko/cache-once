BenchmarkDotNet=v0.11.0, OS=Windows 10.0.17134.165 (1803/April2018Update/Redstone4)
Intel Core i7-7500U CPU 2.70GHz (Kaby Lake), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=2.1.202
[Host]     : .NET Core 2.0.9 (CoreCLR 4.6.26614.01, CoreFX 4.6.26614.01), 64bit RyuJIT
DefaultJob : .NET Core 2.0.9 (CoreCLR 4.6.26614.01, CoreFX 4.6.26614.01), 64bit RyuJIT


```
              Method |      Mean |     Error |    StdDev |
-------------------- |----------:|----------:|----------:|
            NoChache | 404.35 ms | 11.991 ms | 34.788 ms |
 LazyCacheGlobalLock |  36.34 ms |  1.039 ms |  3.046 ms |
 LazyCacheLockPerKey |  36.62 ms |  1.068 ms |  3.114 ms |
 
```

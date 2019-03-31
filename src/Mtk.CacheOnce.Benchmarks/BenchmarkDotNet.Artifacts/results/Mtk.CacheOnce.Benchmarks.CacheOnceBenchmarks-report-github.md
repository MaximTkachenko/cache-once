``` ini

BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17134.648 (1803/April2018Update/Redstone4)
Intel Core i7-8550U CPU 1.80GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.2.101
  [Host]     : .NET Core 2.2.0 (CoreCLR 4.6.27110.04, CoreFX 4.6.27110.04), 64bit RyuJIT
  DefaultJob : .NET Core 2.2.0 (CoreCLR 4.6.27110.04, CoreFX 4.6.27110.04), 64bit RyuJIT


```
|                 Method |     Mean |     Error |    StdDev |
|----------------------- |---------:|----------:|----------:|
|       CacheOnce_OneKey | 111.5 us |  2.183 us |  3.648 us |
| CacheOnce_MultipleKeys | 950.8 us | 13.435 us | 12.567 us |

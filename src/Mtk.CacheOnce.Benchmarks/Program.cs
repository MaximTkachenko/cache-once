using System;
using BenchmarkDotNet.Diagnostics.Windows.Configs;

namespace Mtk.CacheOnce.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }

    [ConcurrencyVisualizerProfiler]
    public class Benchmarks
    {

    }
}

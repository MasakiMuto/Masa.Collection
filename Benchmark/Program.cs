using System;
using System.Collections.Concurrent;
using System.Linq;
using BenchmarkDotNet.Running;
using Masa.Collection;

namespace Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<CollectionBenchmark>();
        }
    }
}
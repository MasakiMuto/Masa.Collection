using System;
using System.Collections.Concurrent;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Masa.Collection;

namespace Benchmark
{
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [MemoryDiagnoser]
    public class CollectionBenchmark
    {
        private readonly ConcurrentDictionary<Guid, byte> _ConcurrentDictionary;
        private readonly BinarySearchTree<Guid> _BinarySearchTree;

        private Guid[] _Data;

        [Params(1000, 10000, 100000)]
        public int N;
        
        public CollectionBenchmark()
        {
            _ConcurrentDictionary = new ConcurrentDictionary<Guid, byte>();
            _BinarySearchTree = new BinarySearchTree<Guid>(128);
         }

        [GlobalSetup]
        public void Setup()
        {
            _Data = Enumerable
                .Range(0, N)
                .Select(x => Guid.NewGuid())
                .ToArray();

        }
        
        [Benchmark]
        public void AddConcurrentDictionary()
        {
            foreach (var guid in _Data)
            {
                _ConcurrentDictionary[guid] = default;
            }
        }

        [Benchmark]
        public void AddBinarySearchTree()
        {
            foreach (var guid in _Data)
            {
                _BinarySearchTree.Add(guid);
            }
        }
    }
}
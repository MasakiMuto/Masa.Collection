using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Masa.Collection.Tests
{
    [TestFixture]
    public class BinarySearchTreeTests
    {
        [Test]
        [TestCase(3, 4, 5)]
        [TestCase(0, 1, 2)]
        [TestCase(0, 2, 1)]
        [TestCase(1, 0, 2)]
        [TestCase(1, 2, 0)]
        [TestCase(2, 1, 0)]
        [TestCase(2, 0, 1)]
        public void AddTest(int a, int b, int c)
        {
            var tree = new BinarySearchTree<int>();
            tree.Add(a).Should().Be(true);
            tree.Add(a).Should().Be(false);
            tree.Add(b).Should().Be(true);
            tree.Add(b).Should().Be(false);
            tree.Add(c).Should().Be(true);
            tree.Add(c).Should().Be(false);
            tree.Add(a).Should().Be(false);
            tree.Add(b).Should().Be(false);
        }

        [Test]
        public void ParallelAddTest()
        {
            var tree = new BinarySearchTree<Guid>();
            var sources = Enumerable.Range(0, 100)
                .Select(_ => Guid.NewGuid())
                .ToArray();
            Parallel.ForEach(sources, x => tree.Add(x));

            foreach (var source in sources)
            {
                tree.Contains(source).Should().Be(true);
            }

            tree.Contains(Guid.NewGuid()).Should().Be(false);
        }
    }
}
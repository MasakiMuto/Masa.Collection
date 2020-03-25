using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Masa.Collection.Tests
{
    [TestOf(typeof(ConcurrentLinkedList<>))]
    public class ConcurrentLinkedListTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void AddFirstTest()
        {
            var list = new ConcurrentLinkedList<Guid>();
            var array = Enumerable.Range(0, 4).Select(x => Guid.NewGuid()).ToArray();
            foreach (var value in array.Reverse())
            {
                list.AddFirst(value);
            }
            list.ToArray().Should().Equal(array);
        }

        [Test]
        public void AddParallelTest()
        {
            var list = new ConcurrentLinkedList<Guid>();
            var array = Enumerable.Range(0, 1024).Select(_ => Guid.NewGuid()).ToArray();
            Parallel.ForEach(array, () => -1, (guid, state, index) =>
            {
                if (index == -1)
                {
                    return list.AddFirst(guid);
                }
                else
                {
                    return list.AddAfter(index, guid);
                }
            }, i => { });
            list.ToArray().Should().BeEquivalentTo(array);
        }
    }
}
using System;
using System.Linq;
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
    }
}
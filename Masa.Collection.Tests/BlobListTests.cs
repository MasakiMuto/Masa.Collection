using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Masa.Collection.Tests
{
    [TestFixture]
    public class BlobListTests
    {
        [TestCase(8, 4)]
        [TestCase(8, 32)]
        [TestCase(1024, 512)]
        public void AllocateTest(int blobSize, int itemCount)
        {
            var addressList = new ConcurrentStack<int>();
            var blobs = new BlobList<Guid>(blobSize);
            var items = Enumerable.Range(0, itemCount).Select(_ => Guid.NewGuid()).ToArray();
            Parallel.ForEach(items, x =>
            {
                var address = blobs.Allocate();
                blobs.Write(address, x);
                addressList.Push(address);
            });

            var values = addressList.Select(x => blobs.Read(x)).ToArray();

            items.Should().BeEquivalentTo(values);
        }
    }
}
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

        [TestCase(8, 4)]
        [TestCase(8, 12)]
        [TestCase(7, 32)]
        [TestCase(128, 300)]
        [TestCase(64, 300)]
        public void FreeTest(int blobSize, int itemCount)
        {
            var addressList = new ConcurrentStack<int>();
            var blobs = new BlobList<int>(blobSize);
            
            for (var i = 0; i < itemCount; i++)
            {
                var address = blobs.Allocate();
                addressList.Push(address);
            }

            blobs.LivingCount.Should().Be(itemCount);

            foreach (var address in addressList)
            {
                blobs.Free(address);
            }

            blobs.LivingCount.Should().Be(0);

            for (int i = 0; i < itemCount * 2; i++)
            {
                blobs.Allocate();
                blobs.LivingCount.Should().Be(i + 1);
            }
        }
    }
}
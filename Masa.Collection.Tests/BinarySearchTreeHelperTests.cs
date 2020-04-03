using FluentAssertions;
using NUnit.Framework;

namespace Masa.Collection.Tests
{
    [TestFixture]
    public class BinarySearchTreeHelperTests
    {
        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(255)]
        public void SetLockTest(int original)
        {
            BinarySearchTreeHelper.GetAddress(original).Should().Be(original);
            
            var locked = BinarySearchTreeHelper.SetLock(original);
            BinarySearchTreeHelper.IsLocked(locked).Should().Be(true);
            BinarySearchTreeHelper.GetAddress(locked).Should().Be(original);
            
            var unlocked = BinarySearchTreeHelper.SetUnlock(locked);
            BinarySearchTreeHelper.IsLocked(unlocked).Should().Be(false);
            unlocked.Should().Be(original);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(255)]
        public void SetNullTest(int original)
        {
            var nullValue = BinarySearchTreeHelper.SetNull(original);
            BinarySearchTreeHelper.IsNull(nullValue).Should().Be(true);
            BinarySearchTreeHelper.GetAddress(nullValue).Should().Be(original);
        }
    }
}
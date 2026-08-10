using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

using StackUnsafeAPI = EncosyTower.Collections.Extensions.Unsafe.StackUnsafeExtensionsUnsafe;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class StackUnsafeExtensionsUnsafeTests
    {
        [Test]
        public void OwnerAndReadOnlyAccessors_AliasStackStorage()
        {
            using var stack = new StackUnsafe<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var readOnly = stack.AsReadOnly();

            // SAFETY: The stack owns the shared storage and both raw indices are valid for the block.
            unsafe
            {
                var buffer = StackUnsafeAPI.GetBufferUnsafe(in stack);
                ref var second = ref StackUnsafeAPI.GetItemAtUnsafe(in stack, 1);
                var readOnlyBuffer = StackUnsafeAPI.GetBufferUnsafe(in readOnly);
                ref readonly var readOnlySecond = ref StackUnsafeAPI.GetItemAtUnsafe(
                      in readOnly
                    , 1
                );

                buffer[0] = 10;
                second = 20;

                Assert.AreEqual(10, readOnlyBuffer[0]);
                Assert.AreEqual(20, readOnlySecond);
            }

            CollectionAssert.AreEqual(new[] { 3, 20, 10 }, stack.ToArray());
        }
    }
}

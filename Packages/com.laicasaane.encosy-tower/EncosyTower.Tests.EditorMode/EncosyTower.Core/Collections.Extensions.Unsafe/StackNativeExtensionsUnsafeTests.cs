// Adapted from System.Collections.Generic.Stack<T> storage semantics.

using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

using StackNativeUnsafeAPI = EncosyTower.Collections.Extensions.Unsafe.StackNativeExtensionsUnsafe;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class StackNativeExtensionsUnsafeTests
    {
        [Test]
        public void RawAccessors_AliasStackStorage()
        {
            using var stack = new StackNative<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var readOnly = stack.AsReadOnly();

            // SAFETY: The stack owns the aliased buffer for the block and both indices are valid.
            unsafe
            {
                var buffer = StackNativeUnsafeAPI.GetBufferUnsafe(in stack);
                ref var second = ref StackNativeUnsafeAPI.GetItemAtUnsafe(in stack, 1);
                var readOnlyBuffer = StackNativeUnsafeAPI.GetBufferUnsafe(in readOnly);
                ref readonly var readOnlySecond = ref StackNativeUnsafeAPI.GetItemAtUnsafe(
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

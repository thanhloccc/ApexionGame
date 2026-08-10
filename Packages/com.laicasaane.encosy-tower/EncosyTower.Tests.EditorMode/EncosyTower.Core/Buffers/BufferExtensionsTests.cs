using System;
using EncosyTower.Buffers;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Buffers
{
    public partial class BufferExtensionsTests
    {
        private static readonly int[] s_seed = { 0, 1, 2, 3, 4, 5 };

        [Test]
        public void Managed_ShiftLeftAndRight_CoverOverlapAndNoOpBoundaries()
        {
            var buffer = new BufferManaged<int>((int[])s_seed.Clone());

            BufferManagedExtensions.ShiftLeft(buffer, 1, 4);
            CollectionAssert.AreEqual(new[] { 0, 2, 3, 4, 4, 5 }, buffer.AsSpan().ToArray());

            buffer.CopyFrom(s_seed);
            BufferManagedExtensions.ShiftRight(buffer, 1, 4);
            CollectionAssert.AreEqual(new[] { 0, 1, 1, 2, 3, 5 }, buffer.AsSpan().ToArray());

            buffer.CopyFrom(s_seed);
            BufferManagedExtensions.ShiftLeft(buffer, 0, 0);
            BufferManagedExtensions.ShiftRight(buffer, 0, 0);
            BufferManagedExtensions.ShiftLeft(buffer, 5, 5);
            BufferManagedExtensions.ShiftRight(buffer, 5, 5);
            CollectionAssert.AreEqual(s_seed, buffer.AsSpan().ToArray());

            BufferManagedExtensions.ShiftLeft(buffer, 0, 5);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 5 }, buffer.AsSpan().ToArray());

            buffer.CopyFrom(s_seed);
            BufferManagedExtensions.ShiftRight(buffer, 0, 5);
            CollectionAssert.AreEqual(new[] { 0, 0, 1, 2, 3, 4 }, buffer.AsSpan().ToArray());
        }

        [Test]
        public void Native_ShiftLeftAndRight_CoverOverlapAndNoOpBoundaries()
        {
            using var buffer = new BufferNative<int>(6, Allocator.Temp);
            buffer.CopyFrom(s_seed);

            BufferNativeExtensions.ShiftLeft(buffer, 1, 4);
            CollectionAssert.AreEqual(new[] { 0, 2, 3, 4, 4, 5 }, buffer.AsSpan().ToArray());

            buffer.CopyFrom(s_seed);
            BufferNativeExtensions.ShiftRight(buffer, 1, 4);
            CollectionAssert.AreEqual(new[] { 0, 1, 1, 2, 3, 5 }, buffer.AsSpan().ToArray());

            buffer.CopyFrom(s_seed);
            BufferNativeExtensions.ShiftLeft(buffer, 0, 0);
            BufferNativeExtensions.ShiftRight(buffer, 0, 0);
            BufferNativeExtensions.ShiftLeft(buffer, 5, 5);
            BufferNativeExtensions.ShiftRight(buffer, 5, 5);
            CollectionAssert.AreEqual(s_seed, buffer.AsSpan().ToArray());

            BufferNativeExtensions.ShiftLeft(buffer, 0, 5);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 5 }, buffer.AsSpan().ToArray());

            buffer.CopyFrom(s_seed);
            BufferNativeExtensions.ShiftRight(buffer, 0, 5);
            CollectionAssert.AreEqual(new[] { 0, 0, 1, 2, 3, 4 }, buffer.AsSpan().ToArray());
        }

        [Test]
        public void Unsafe_ShiftLeftAndRight_CoverOverlapAndNoOpBoundaries()
        {
            var buffer = new BufferUnsafe<int>(6, Allocator.Temp);

            try
            {
                buffer.CopyFrom(s_seed);

                BufferUnsafeExtensions.ShiftLeft(ref buffer, 1, 4);
                CollectionAssert.AreEqual(new[] { 0, 2, 3, 4, 4, 5 }, buffer.AsSpan().ToArray());

                buffer.CopyFrom(s_seed);
                BufferUnsafeExtensions.ShiftRight(ref buffer, 1, 4);
                CollectionAssert.AreEqual(new[] { 0, 1, 1, 2, 3, 5 }, buffer.AsSpan().ToArray());

                buffer.CopyFrom(s_seed);
                BufferUnsafeExtensions.ShiftLeft(ref buffer, 0, 0);
                BufferUnsafeExtensions.ShiftRight(ref buffer, 0, 0);
                BufferUnsafeExtensions.ShiftLeft(ref buffer, 5, 5);
                BufferUnsafeExtensions.ShiftRight(ref buffer, 5, 5);
                CollectionAssert.AreEqual(s_seed, buffer.AsSpan().ToArray());

                BufferUnsafeExtensions.ShiftLeft(ref buffer, 0, 5);
                CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 5 }, buffer.AsSpan().ToArray());

                buffer.CopyFrom(s_seed);
                BufferUnsafeExtensions.ShiftRight(ref buffer, 0, 5);
                CollectionAssert.AreEqual(new[] { 0, 0, 1, 2, 3, 4 }, buffer.AsSpan().ToArray());
            }
            finally
            {
                buffer.Dispose();
            }
        }

        [Test]
        public void Managed_ShiftLeftAndRight_NegativeArgumentsThrow()
        {
            // Regression: the shift guards compared signed ints, so a negative index or
            // count passed "value < Capacity" and reached the copy with a negative offset.
            var buffer = new BufferManaged<int>((int[])s_seed.Clone());

            Assert.Throws<InvalidOperationException>(() => BufferManagedExtensions.ShiftLeft(buffer, -1, 4));
            Assert.Throws<InvalidOperationException>(() => BufferManagedExtensions.ShiftRight(buffer, -1, 4));
            Assert.Throws<InvalidOperationException>(() => BufferManagedExtensions.ShiftLeft(buffer, 0, -1));
            Assert.Throws<InvalidOperationException>(() => BufferManagedExtensions.ShiftRight(buffer, 0, -1));

            CollectionAssert.AreEqual(s_seed, buffer.AsSpan().ToArray());
        }

        [Test]
        public void Native_ShiftLeftAndRight_NegativeArgumentsThrow()
        {
            // Regression: with the signed guards a negative index reached
            // Buffer.MemoryCopy at an offset before the buffer start.
            using var buffer = new BufferNative<int>(6, Allocator.Temp);
            buffer.CopyFrom(s_seed);

            Assert.Throws<InvalidOperationException>(() => BufferNativeExtensions.ShiftLeft(buffer, -1, 4));
            Assert.Throws<InvalidOperationException>(() => BufferNativeExtensions.ShiftRight(buffer, -1, 4));
            Assert.Throws<InvalidOperationException>(() => BufferNativeExtensions.ShiftLeft(buffer, 0, -1));
            Assert.Throws<InvalidOperationException>(() => BufferNativeExtensions.ShiftRight(buffer, 0, -1));

            CollectionAssert.AreEqual(s_seed, buffer.AsSpan().ToArray());
        }

        [Test]
        public void Unsafe_ShiftLeftAndRight_NegativeArgumentsThrow()
        {
            // Regression: with the signed guards a negative index reached
            // Buffer.MemoryCopy at an offset before the buffer start.
            var buffer = new BufferUnsafe<int>(6, Allocator.Temp);

            try
            {
                buffer.CopyFrom(s_seed);

                Assert.Throws<InvalidOperationException>(() => BufferUnsafeExtensions.ShiftLeft(ref buffer, -1, 4));
                Assert.Throws<InvalidOperationException>(() => BufferUnsafeExtensions.ShiftRight(ref buffer, -1, 4));
                Assert.Throws<InvalidOperationException>(() => BufferUnsafeExtensions.ShiftLeft(ref buffer, 0, -1));
                Assert.Throws<InvalidOperationException>(() => BufferUnsafeExtensions.ShiftRight(ref buffer, 0, -1));

                CollectionAssert.AreEqual(s_seed, buffer.AsSpan().ToArray());
            }
            finally
            {
                buffer.Dispose();
            }
        }
    }
}

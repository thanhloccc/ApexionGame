using System;
using EncosyTower.Buffers;
using EncosyTower.Tests.Core.Collections;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Buffers
{
    // Adapted from Unity.Collections.Tests/NativeListTests.cs (fixed-capacity subset)
    // against the Native forwarder.
    public partial class BufferNativeTests
    {
        [Test]
        public void Constructor_CreatesValidBuffer()
        {
            using var buffer = new BufferNative<int>(8, Allocator.Temp);

            Assert.IsTrue(buffer.IsCreated);
            Assert.AreEqual(8, buffer.Capacity);
        }

        [Test]
        public void DefaultBuffer_IsNotCreated()
        {
            BufferNative<int> buffer = default;

            Assert.IsFalse(buffer.IsCreated);
        }

        [Test]
        public void Constructor_ClearsToZero()
        {
            using var buffer = new BufferNative<int>(4, Allocator.Temp);

            for (var i = 0; i < buffer.Capacity; i++)
            {
                Assert.AreEqual(0, buffer[i]);
            }
        }

        [Test]
        public void Indexer_GetSet()
        {
            using var buffer = new BufferNative<int>(4, Allocator.Temp);

            buffer[0] = 10;
            buffer[3] = 40;

            Assert.AreEqual(10, buffer[0]);
            Assert.AreEqual(40, buffer[3]);
        }

        [Test]
        public void Resize_Grow_PreservesContent()
        {
            using var buffer = new BufferNative<int>(4, Allocator.Temp);
            buffer[0] = 1;
            buffer[1] = 2;
            buffer[2] = 3;
            buffer[3] = 4;

            buffer.Resize(16, copyContent: true);

            Assert.GreaterOrEqual(buffer.Capacity, 16);
            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(2, buffer[1]);
            Assert.AreEqual(3, buffer[2]);
            Assert.AreEqual(4, buffer[3]);
        }

        [Test]
        public void Clear_ZeroesContent()
        {
            using var buffer = new BufferNative<int>(4, Allocator.Temp);
            buffer[0] = 1;
            buffer[1] = 2;

            buffer.Clear();

            Assert.AreEqual(0, buffer[0]);
            Assert.AreEqual(0, buffer[1]);
        }

        [Test]
        public void CopyFrom_And_CopyTo_Span()
        {
            using var buffer = new BufferNative<int>(4, Allocator.Temp);

            buffer.CopyFrom(new[] { 5, 6, 7, 8 });

            var dst = new int[4];
            buffer.CopyTo(dst);

            CollectionAssert.AreEqual(new[] { 5, 6, 7, 8 }, dst);
        }

        [Test]
        public void AsSpan_ReflectsContent()
        {
            using var buffer = new BufferNative<int>(4, Allocator.Temp);
            buffer[0] = 11;
            buffer[1] = 22;

            var span = buffer.AsSpan();

            Assert.AreEqual(4, span.Length);
            Assert.AreEqual(11, span[0]);
            Assert.AreEqual(22, span[1]);
        }

        [Test]
        public void AsNativeSlice_ReflectsContent()
        {
            using var buffer = new BufferNative<int>(4, Allocator.Temp);
            buffer[0] = 11;
            buffer[1] = 22;

            var slice = buffer.AsNativeSlice();

            Assert.AreEqual(4, slice.Length);
            Assert.AreEqual(11, slice[0]);
            Assert.AreEqual(22, slice[1]);
        }

        [Test]
        public void Reinterpret_ReusesMemory()
        {
            using var buffer = new BufferNative<int>(4, Allocator.Temp);
            buffer[0] = 7;

            var reinterpreted = buffer.Reinterpret<uint>();

            Assert.AreEqual(buffer.Capacity, reinterpreted.Capacity);
            Assert.AreEqual(7u, reinterpreted[0]);
        }

        [Test]
        public void Dispose_MarksNotCreated()
        {
            var buffer = new BufferNative<int>(4, Allocator.Temp);
            Assert.IsTrue(buffer.IsCreated);

            buffer.Dispose();

            Assert.IsFalse(buffer.IsCreated);
        }

        [Test]
        public void DisposeJob_CompletesAndMarksNotCreated()
        {
            var buffer = new BufferNative<int>(4, Allocator.TempJob);
            buffer[0] = 1;

            var handle = buffer.Dispose(default);

            Assert.IsFalse(buffer.IsCreated);
            Assert.DoesNotThrow(() => handle.Complete());
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void DoubleDispose_Throws()
        {
            // Matches Unity NativeArray/NativeList semantics: a second Dispose with
            // safety checks enabled throws (the handle was released, not zeroed).
            var buffer = new BufferNative<int>(4, Allocator.Temp);
            buffer.Dispose();

            Assert.Throws<ObjectDisposedException>(() => buffer.Dispose());
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void UseAfterDispose_Throws()
        {
            var buffer = new BufferNative<int>(4, Allocator.Temp);
            buffer[0] = 1;
            buffer.Dispose();

            Assert.Catch(() => _ = buffer[0]);
        }

        [Test]
        public void Alloc_CreatesDefaultBufferAndClearsStorage()
        {
            BufferNative<int> buffer = default;

            try
            {
                buffer.Alloc(3, new AllocatorStrategy(Allocator.Temp), memClear: true);

                Assert.IsTrue(buffer.IsCreated);
                Assert.AreEqual(3, buffer.Capacity);
                CollectionAssert.AreEqual(new[] { 0, 0, 0 }, buffer.AsSpan().ToArray());
            }
            finally
            {
                if (buffer.IsCreated)
                {
                    buffer.Dispose();
                }
            }
        }

        [Test]
        public void Resize_RemainingOverloadsPreserveOrDiscardContent()
        {
            using var buffer = new BufferNative<int>(3, Allocator.Temp);
            buffer.CopyFrom(new[] { 1, 2, 3 });

            buffer.Resize(5);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 0, 0 }, buffer.AsSpan().ToArray());

            buffer.Resize(4, copyContent: false, memClear: true);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, buffer.AsSpan().ToArray());
        }

        [Test]
        public void FastClear_DoesNotWriteUnmanagedStorage()
        {
            using var buffer = new BufferNative<int>(3, Allocator.Temp);
            buffer.CopyFrom(new[] { 1, 2, 3 });

            buffer.FastClear();

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, buffer.AsSpan().ToArray());
        }

        [Test]
        public void CopyFromAndTryCopyFrom_AllOverloadsUseRequestedWindow()
        {
            using var buffer = new BufferNative<int>(6, Allocator.Temp);
            var source = new[] { 1, 2, 3, 4 };

            buffer.CopyFrom(source);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 0, 0 }, buffer.AsSpan().ToArray());
            buffer.Clear();
            buffer.CopyFrom(source, 2);
            CollectionAssert.AreEqual(new[] { 1, 2, 0, 0, 0, 0 }, buffer.AsSpan().ToArray());
            buffer.Clear();
            buffer.CopyFrom(1, source);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 0 }, buffer.AsSpan().ToArray());
            buffer.Clear();
            buffer.CopyFrom(2, source, 3);
            CollectionAssert.AreEqual(new[] { 0, 0, 1, 2, 3, 0 }, buffer.AsSpan().ToArray());

            Assert.IsTrue(buffer.TryCopyFrom(source));
            Assert.IsTrue(buffer.TryCopyFrom(source, 2));
            Assert.IsTrue(buffer.TryCopyFrom(1, source));
            Assert.IsTrue(buffer.TryCopyFrom(2, source, 3));

            var snapshot = buffer.AsSpan().ToArray();
            Assert.IsFalse(buffer.TryCopyFrom(5, source));
            Assert.IsFalse(buffer.TryCopyFrom(5, source, 2));
            CollectionAssert.AreEqual(snapshot, buffer.AsSpan().ToArray());
        }

        [Test]
        public void CopyToAndTryCopyTo_AllOverloadsUseRequestedWindow()
        {
            using var buffer = new BufferNative<int>(4, Allocator.Temp);
            buffer.CopyFrom(new[] { 1, 2, 3, 4 });
            var destination = new int[4];

            buffer.CopyTo(destination);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, destination);
            Array.Clear(destination, 0, destination.Length);
            buffer.CopyTo(destination, 2);
            CollectionAssert.AreEqual(new[] { 1, 2, 0, 0 }, destination);
            destination = new int[3];
            buffer.CopyTo(1, destination);
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, destination);
            destination = new int[4];
            buffer.CopyTo(1, destination, 2);
            CollectionAssert.AreEqual(new[] { 2, 3, 0, 0 }, destination);
            Assert.IsTrue(buffer.TryCopyTo(destination));
            Assert.IsTrue(buffer.TryCopyTo(destination, 2));
            Assert.IsTrue(buffer.TryCopyTo(1, new int[3]));
            Assert.IsTrue(buffer.TryCopyTo(1, destination, 2));

            destination = new[] { 9, 9, 9, 9 };
            Assert.IsFalse(buffer.TryCopyTo(3, destination));
            Assert.IsFalse(buffer.TryCopyTo(3, destination, 2));
            CollectionAssert.AreEqual(new[] { 9, 9, 9, 9 }, destination);
        }

        [Test]
        public void ReadOnlyViews_ReflectOwnerWrites()
        {
            using var buffer = new BufferNative<int>(4, Allocator.Temp);
            buffer.CopyFrom(new[] { 1, 2, 3, 4 });

            var slice = buffer.AsNativeSliceReadOnly();
            var span = buffer.AsReadOnlySpan();
            var readOnly = buffer.AsReadOnly();
            BufferNative<int>.ReadOnly converted = buffer;

            buffer[0] = 10;
            Assert.AreEqual(10, slice[0]);
            Assert.AreEqual(10, span[0]);
            Assert.AreEqual(10, readOnly[0]);
            Assert.AreEqual(10, converted[0]);
        }

        [Test]
        public void ReadOnly_AllCopyViewAndReinterpretMembersWork()
        {
            using var buffer = new BufferNative<int>(4, Allocator.Temp);
            buffer.CopyFrom(new[] { 1, 2, 3, 4 });
            var readOnly = buffer.AsReadOnly();

            Assert.IsTrue(readOnly.IsCreated);
            Assert.AreEqual(4, readOnly.Capacity);
            Assert.AreEqual(2, readOnly[1]);

            var destination = new int[4];
            readOnly.CopyTo(destination);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, destination);
            Array.Clear(destination, 0, destination.Length);
            readOnly.CopyTo(destination, 2);
            CollectionAssert.AreEqual(new[] { 1, 2, 0, 0 }, destination);
            destination = new int[3];
            readOnly.CopyTo(1, destination);
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, destination);
            destination = new int[4];
            readOnly.CopyTo(1, destination, 2);
            CollectionAssert.AreEqual(new[] { 2, 3, 0, 0 }, destination);
            Assert.IsTrue(readOnly.TryCopyTo(destination));
            Assert.IsTrue(readOnly.TryCopyTo(destination, 2));
            Assert.IsTrue(readOnly.TryCopyTo(1, new int[3]));
            Assert.IsTrue(readOnly.TryCopyTo(1, destination, 2));
            Assert.IsFalse(readOnly.TryCopyTo(3, destination));
            Assert.IsFalse(readOnly.TryCopyTo(3, destination, 2));

            Assert.AreEqual(4, readOnly.AsNativeSliceReadOnly().Length);
            Assert.AreEqual(4, readOnly.AsReadOnlySpan().Length);
            Assert.AreEqual(1u, readOnly.Reinterpret<uint>()[0]);
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void ReadOnly_AfterOwnerDispose_RejectsAccess()
        {
            var buffer = new BufferNative<int>(1, Allocator.Temp);
            var readOnly = buffer.AsReadOnly();
            buffer.Dispose();

            Assert.Catch(() => _ = readOnly[0]);
        }
    }
}

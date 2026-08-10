using System;
using EncosyTower.Buffers;
using EncosyTower.Tests.Core.Collections;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Buffers
{
    public partial class BufferManagedTests
    {
        [Test]
        public void Constructors_ExposeCapacityCreationAndAliasingIndexer()
        {
            BufferManaged<int> empty = default;
            var allocated = new BufferManaged<int>(3);
            var source = new[] { 1, 2, 3 };
            var aliased = new BufferManaged<int>(source);

            Assert.IsFalse(empty.IsCreated);
            Assert.IsTrue(allocated.IsCreated);
            Assert.AreEqual(3, allocated.Capacity);

            ref var item = ref aliased[1];
            item = 20;
            Assert.AreEqual(20, source[1]);

            source[2] = 30;
            Assert.AreEqual(30, aliased[2]);
        }

        [Test]
        public void Alloc_OverloadsReplaceStorage()
        {
            var buffer = new BufferManaged<int>(1);
            buffer[0] = 9;

            buffer.Alloc(2);
            Assert.AreEqual(2, buffer.Capacity);
            CollectionAssert.AreEqual(new[] { 0, 0 }, buffer.AsSpan().ToArray());

            buffer.Alloc(3, new AllocatorStrategy(Allocator.Temp), memClear: false);
            Assert.AreEqual(3, buffer.Capacity);
            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, buffer.AsSpan().ToArray());
        }

        [Test]
        public void Resize_OverloadsPreserveDiscardAndClearAsRequested()
        {
            var buffer = new BufferManaged<int>(new[] { 1, 2, 3 });

            buffer.Resize(5);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 0, 0 }, buffer.AsSpan().ToArray());

            buffer.Resize(2, copyContent: false);
            CollectionAssert.AreEqual(new[] { 0, 0 }, buffer.AsSpan().ToArray());

            buffer[0] = 7;
            buffer[1] = 8;
            buffer.Resize(4, copyContent: true, memClear: true);
            CollectionAssert.AreEqual(new[] { 7, 8, 0, 0 }, buffer.AsSpan().ToArray());

            buffer.Resize(3, copyContent: false, memClear: false);
            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, buffer.AsSpan().ToArray());
        }

        [Test]
        public void FastClear_FollowsValueAndReferenceTypeContract()
        {
            var values = new BufferManaged<int>(new[] { 1, 2 });
            values.FastClear();
            CollectionAssert.AreEqual(new[] { 1, 2 }, values.AsSpan().ToArray());

            var references = new BufferManaged<string>(new[] { "a", "b" });
            references.FastClear();
            CollectionAssert.AreEqual(new string[] { null, null }, references.AsSpan().ToArray());
        }

        [Test]
        public void Clear_ResetsEveryCell()
        {
            var buffer = new BufferManaged<int>(new[] { 1, 2, 3 });

            buffer.Clear();

            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, buffer.AsSpan().ToArray());
        }

        [Test]
        public void CopyFrom_AllOverloadsUseRequestedWindow()
        {
            var source = new[] { 1, 2, 3, 4 };
            var buffer = new BufferManaged<int>(6);

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
        }

        [Test]
        public void TryCopyFrom_AllOverloadsReportSuccessAndFailureWithoutPartialWrite()
        {
            var source = new[] { 1, 2, 3, 4 };
            var buffer = new BufferManaged<int>(6);

            Assert.IsTrue(buffer.TryCopyFrom(source));
            buffer.Clear();
            Assert.IsTrue(buffer.TryCopyFrom(source, 2));
            buffer.Clear();
            Assert.IsTrue(buffer.TryCopyFrom(1, source));
            buffer.Clear();
            Assert.IsTrue(buffer.TryCopyFrom(2, source, 3));

            var snapshot = buffer.AsSpan().ToArray();
            Assert.IsFalse(buffer.TryCopyFrom(5, source));
            Assert.IsFalse(buffer.TryCopyFrom(5, source, 2));
            CollectionAssert.AreEqual(snapshot, buffer.AsSpan().ToArray());
        }

        [Test]
        public void CopyTo_AllOverloadsUseRequestedWindow()
        {
            var buffer = new BufferManaged<int>(new[] { 1, 2, 3, 4 });
            var destination = new int[4];

            buffer.CopyTo(destination);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, destination);

            Array.Clear(destination, 0, destination.Length);
            buffer.CopyTo(destination, 2);
            CollectionAssert.AreEqual(new[] { 1, 2, 0, 0 }, destination);

            destination = new int[2];
            buffer.CopyTo(1, destination);
            CollectionAssert.AreEqual(new[] { 2, 3 }, destination);

            destination = new int[4];
            buffer.CopyTo(1, destination, 3);
            CollectionAssert.AreEqual(new[] { 2, 3, 4, 0 }, destination);
        }

        [Test]
        public void TryCopyTo_AllOverloadsReportSuccessAndFailureWithoutPartialWrite()
        {
            var buffer = new BufferManaged<int>(new[] { 1, 2, 3, 4 });

            Assert.IsTrue(buffer.TryCopyTo(new int[4]));
            Assert.IsTrue(buffer.TryCopyTo(new int[4], 2));
            Assert.IsTrue(buffer.TryCopyTo(1, new int[3]));
            Assert.IsTrue(buffer.TryCopyTo(1, new int[4], 3));

            var destination = new[] { 9, 9, 9, 9 };
            Assert.IsFalse(buffer.TryCopyTo(3, destination));
            Assert.IsFalse(buffer.TryCopyTo(3, destination, 2));
            CollectionAssert.AreEqual(new[] { 9, 9, 9, 9 }, destination);
        }

        [Test]
        public void MutableAndReadOnlyViewsAliasStorage()
        {
            var buffer = new BufferManaged<int>(new[] { 1, 2, 3 });
            var segment = buffer.AsArraySegment();
            var memory = buffer.AsMemory();
            var readOnlyMemory = buffer.AsReadOnlyMemory();
            var span = buffer.AsSpan();
            var readOnlySpan = buffer.AsReadOnlySpan();

            segment.Array[segment.Offset] = 10;
            memory.Span[1] = 20;
            span[2] = 30;

            Assert.AreEqual(10, buffer[0]);
            Assert.AreEqual(20, readOnlyMemory.Span[1]);
            Assert.AreEqual(30, readOnlySpan[2]);
        }

        [Test]
        public void ReadOnly_AllMembersReflectOwnerStorage()
        {
            var buffer = new BufferManaged<int>(new[] { 1, 2, 3, 4 });
            BufferManaged<int>.ReadOnly converted = buffer;
            var readOnly = buffer.AsReadOnly();

            Assert.IsTrue(readOnly.IsCreated);
            Assert.AreEqual(4, readOnly.Capacity);
            Assert.AreEqual(2, readOnly[1]);
            Assert.AreEqual(1, converted[0]);

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

            Assert.IsTrue(readOnly.TryCopyTo(new int[4]));
            Assert.IsTrue(readOnly.TryCopyTo(new int[4], 2));
            Assert.IsTrue(readOnly.TryCopyTo(1, new int[3]));
            Assert.IsTrue(readOnly.TryCopyTo(1, new int[4], 3));

            destination = new[] { 9, 9, 9, 9 };
            Assert.IsFalse(readOnly.TryCopyTo(3, destination));
            Assert.IsFalse(readOnly.TryCopyTo(3, destination, 2));
            CollectionAssert.AreEqual(new[] { 9, 9, 9, 9 }, destination);

            var memory = readOnly.AsReadOnlyMemory();
            var span = readOnly.AsReadOnlySpan();
            buffer[0] = 10;
            Assert.AreEqual(10, memory.Span[0]);
            Assert.AreEqual(10, span[0]);
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void OwnerAndReadOnlyIndexer_OutOfRangeThrow()
        {
            var buffer = new BufferManaged<int>(1);
            var readOnly = buffer.AsReadOnly();

            Assert.Throws<IndexOutOfRangeException>(() => _ = buffer[1]);
            Assert.Throws<IndexOutOfRangeException>(() => _ = readOnly[1]);
        }

        [Test]
        public void Dispose_IsDocumentedNoOp()
        {
            var buffer = new BufferManaged<int>(new[] { 7, 8 });

            buffer.Dispose();

            Assert.IsTrue(buffer.IsCreated);
            Assert.AreEqual(2, buffer.Capacity);
            Assert.AreEqual(7, buffer[0]);
        }
    }
}

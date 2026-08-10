using System;
using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections.Unsafe
{
    public partial class StackUnsafeTests
    {
        [Test]
        public void Constructors_CreateEmptyAndCopiedStacksIncludingZeroCapacity()
        {
            using var stack = new StackUnsafe<int>(4, new AllocatorStrategy(Allocator.Temp));
            using var zeroCapacity = new StackUnsafe<int>(0, Allocator.Temp);
            using var copied = new StackUnsafe<int>(new[] { 1, 2, 3 }, Allocator.Temp);

            Assert.IsTrue(stack.IsCreated);
            Assert.AreEqual(0, stack.Count);
            Assert.AreEqual(4, stack.Capacity);
            Assert.IsTrue(zeroCapacity.IsCreated);
            Assert.AreEqual(0, zeroCapacity.Count);
            Assert.AreEqual(0, zeroCapacity.Capacity);
            Assert.IsTrue(copied.IsCreated);
            Assert.AreEqual(3, copied.Count);
            Assert.AreEqual(3, copied.Capacity);
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, copied.ToArray());

            zeroCapacity.Push(7);

            Assert.AreEqual(1, zeroCapacity.Count);
            Assert.AreEqual(7, zeroCapacity.Peek());
        }

        [Test]
        public void Push_OverloadsGrowAndPreserveLifoOrder()
        {
            using var stack = new StackUnsafe<int>(1, Allocator.Temp);
            var second = 2;

            stack.Push(1);
            stack.Push(in second);

            for (var i = 3; i <= 20; i++)
            {
                stack.Push(i);
            }

            Assert.AreEqual(20, stack.Count);
            Assert.GreaterOrEqual(stack.Capacity, 20);

            for (var i = 20; i >= 1; i--)
            {
                Assert.AreEqual(i, stack.Pop());
            }

            Assert.AreEqual(0, stack.Count);
        }

        [Test]
        public void PushRange_AppendsWithLastItemOnTop()
        {
            using var stack = new StackUnsafe<int>(1, Allocator.Temp);
            stack.Push(1);

            stack.PushRange(new[] { 2, 3, 4 });
            stack.PushRange(ReadOnlySpan<int>.Empty);

            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, stack.ToArray());
        }

        [Test]
        public void PopPeekAndTryMethods_HandleSuccessAndFailure()
        {
            using var stack = new StackUnsafe<int>(2, Allocator.Temp);

            Assert.IsFalse(stack.TryPop(out var emptyPop));
            Assert.IsFalse(stack.TryPeek(out var emptyPeek));
            Assert.AreEqual(0, emptyPop);
            Assert.AreEqual(0, emptyPeek);

            stack.PushRange(new[] { 10, 20 });

            Assert.AreEqual(20, stack.Peek());
            Assert.IsTrue(stack.TryPeek(out var peeked));
            Assert.AreEqual(20, peeked);
            Assert.AreEqual(2, stack.Count);
            Assert.IsTrue(stack.TryPop(out var popped));
            Assert.AreEqual(20, popped);
            Assert.AreEqual(10, stack.Pop());
            Assert.IsFalse(stack.TryPop(out emptyPop));
            Assert.AreEqual(0, emptyPop);
        }

        [Test]
        public void Clear_ResetsCountAndAllowsReuse()
        {
            using var stack = new StackUnsafe<int>(4, Allocator.Temp);
            stack.PushRange(new[] { 1, 2, 3 });

            stack.Clear();

            Assert.AreEqual(0, stack.Count);
            Assert.IsFalse(stack.TryPeek(out _));

            stack.PushRange(new[] { 4, 5 });

            CollectionAssert.AreEqual(new[] { 5, 4 }, stack.ToArray());
        }

        [Test]
        public void CopyAndTryCopy_AllOverloadsUseTopFirstOrder()
        {
            using var stack = new StackUnsafe<int>(new[] { 1, 2, 3, 4 }, Allocator.Temp);
            var full = new int[4];
            var partial = new int[3];
            var offset = new int[2];
            var range = new int[3];

            stack.CopyTo(full);
            stack.CopyTo(partial, 2);
            stack.CopyTo(1, offset);
            stack.CopyTo(1, range, 2);

            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, stack.ToArray());
            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, full);
            CollectionAssert.AreEqual(new[] { 4, 3, 0 }, partial);
            CollectionAssert.AreEqual(new[] { 3, 2 }, offset);
            CollectionAssert.AreEqual(new[] { 3, 2, 0 }, range);
            Assert.IsTrue(stack.TryCopyTo(new int[4]));
            Assert.IsTrue(stack.TryCopyTo(new int[3], 2));
            Assert.IsTrue(stack.TryCopyTo(1, new int[3]));
            Assert.IsTrue(stack.TryCopyTo(1, new int[3], 2));

            var failedDestination = new[] { 9, 9 };
            Assert.IsFalse(stack.TryCopyTo(3, failedDestination));
            Assert.IsFalse(stack.TryCopyTo(3, failedDestination, 2));
            CollectionAssert.AreEqual(new[] { 9, 9 }, failedDestination);
        }

        [Test]
        public void CapacityMethodsGrowShrinkAndTrimWithoutChangingOrder()
        {
            using var stack = new StackUnsafe<int>(new[] { 1, 2, 3 }, Allocator.Temp);

            stack.EnsureCapacity(10);
            Assert.GreaterOrEqual(stack.Capacity, 10);
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, stack.ToArray());

            var afterEnsure = stack.Capacity;
            stack.IncreaseCapacityBy(5);
            Assert.AreEqual(afterEnsure + 5, stack.Capacity);
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, stack.ToArray());

            stack.IncreaseCapacityTo(stack.Count);
            Assert.AreEqual(stack.Count, stack.Capacity);
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, stack.ToArray());

            stack.IncreaseCapacityTo(8);
            stack.Trim();

            Assert.AreEqual(stack.Count, stack.Capacity);
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, stack.ToArray());
        }

        [Test]
        public void ReadOnly_AllPropertiesPeekCopyAndConversionMembersWork()
        {
            using var stack = new StackUnsafe<int>(new[] { 1, 2, 3, 4 }, Allocator.Temp);
            var readOnly = stack.AsReadOnly();
            StackUnsafe<int>.ReadOnly converted = stack;
            var full = new int[4];
            var partial = new int[3];
            var offset = new int[2];
            var range = new int[3];

            Assert.IsTrue(readOnly.IsCreated);
            Assert.AreEqual(4, readOnly.Count);
            Assert.AreEqual(4, readOnly.Capacity);
            Assert.AreEqual(4, readOnly.Peek());
            Assert.IsTrue(readOnly.TryPeek(out var peeked));
            Assert.AreEqual(4, peeked);
            Assert.AreEqual(4, converted.Peek());

            readOnly.CopyTo(full);
            readOnly.CopyTo(partial, 2);
            readOnly.CopyTo(1, offset);
            readOnly.CopyTo(1, range, 2);

            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, readOnly.ToArray());
            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, full);
            CollectionAssert.AreEqual(new[] { 4, 3, 0 }, partial);
            CollectionAssert.AreEqual(new[] { 3, 2 }, offset);
            CollectionAssert.AreEqual(new[] { 3, 2, 0 }, range);
            Assert.IsTrue(readOnly.TryCopyTo(new int[4]));
            Assert.IsTrue(readOnly.TryCopyTo(new int[3], 2));
            Assert.IsTrue(readOnly.TryCopyTo(1, new int[3]));
            Assert.IsTrue(readOnly.TryCopyTo(1, new int[3], 2));
            Assert.IsFalse(readOnly.TryCopyTo(3, new int[2]));
            Assert.IsFalse(readOnly.TryCopyTo(3, new int[2], 2));
        }

        [Test]
        public void ReadOnlyEmpty_TryPeekReturnsFalse()
        {
            using var stack = new StackUnsafe<int>(0, Allocator.Temp);
            var readOnly = stack.AsReadOnly();

            Assert.IsFalse(readOnly.TryPeek(out var value));
            Assert.AreEqual(0, value);
        }

        [Test]
        public void OwnerAndReadOnlyEnumerators_DirectMembersVisitTopFirstOrderAndReset()
        {
            using var stack = new StackUnsafe<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var ownerEnumerator = stack.GetEnumerator();
            var readOnlyEnumerator = stack.AsReadOnly().GetEnumerator();

            Assert.IsTrue(ownerEnumerator.MoveNext());
            Assert.AreEqual(3, ownerEnumerator.Current);
            Assert.IsTrue(ownerEnumerator.MoveNext());
            Assert.AreEqual(2, ownerEnumerator.Current);
            ownerEnumerator.Reset();
            Assert.IsTrue(ownerEnumerator.MoveNext());
            Assert.AreEqual(3, ownerEnumerator.Current);
            ownerEnumerator.Dispose();

            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            Assert.AreEqual(3, readOnlyEnumerator.Current);
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            Assert.AreEqual(2, readOnlyEnumerator.Current);
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            Assert.AreEqual(1, readOnlyEnumerator.Current);
            Assert.IsFalse(readOnlyEnumerator.MoveNext());
            Assert.AreEqual(0, readOnlyEnumerator.Current);
            readOnlyEnumerator.Reset();
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            Assert.AreEqual(3, readOnlyEnumerator.Current);
            readOnlyEnumerator.Dispose();
        }

        [Test]
        public void Enumerator_DoesNotInvalidateOnMutationWithinCapacity()
        {
            using var stack = new StackUnsafe<int>(8, Allocator.Temp);
            stack.PushRange(new[] { 1, 2, 3 });
            var enumerator = stack.GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(3, enumerator.Current);

            stack.Push(4);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(2, enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current);
            Assert.IsFalse(enumerator.MoveNext());
            enumerator.Dispose();
        }

        [Test]
        public void Dispose_MarksNotCreatedAndDoubleDisposeIsNoOp()
        {
            var stack = new StackUnsafe<int>(4, Allocator.Temp);
            stack.Push(1);

            stack.Dispose();

            Assert.IsFalse(stack.IsCreated);
            Assert.AreEqual(0, stack.Count);
            Assert.AreEqual(0, stack.Capacity);
            Assert.DoesNotThrow(() => stack.Dispose());
        }

        [Test]
        public void DisposeJob_CompletesAndMarksNotCreated()
        {
            var stack = new StackUnsafe<int>(4, Allocator.TempJob);
            stack.Push(1);

            var handle = stack.Dispose(default);

            Assert.IsFalse(stack.IsCreated);
            Assert.AreEqual(0, stack.Count);
            Assert.AreEqual(0, stack.Capacity);
            Assert.DoesNotThrow(() => handle.Complete());
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void Validation_RejectsInvalidCapacityCopiesAndEmptyAccess()
        {
            Assert.Throws<InvalidOperationException>(
                static () => _ = new StackUnsafe<int>(-1, Allocator.Temp)
            );

            using var stack = new StackUnsafe<int>(new[] { 1, 2, 3 }, Allocator.Temp);

            Assert.Throws<InvalidOperationException>(() => stack.EnsureCapacity(-1));
            Assert.Throws<InvalidOperationException>(() => stack.IncreaseCapacityTo(2));
            Assert.Throws<InvalidOperationException>(() => stack.CopyTo(4, new int[1]));

            stack.Clear();
            var readOnly = stack.AsReadOnly();

            Assert.Throws<InvalidOperationException>(() => stack.Pop());
            Assert.Throws<InvalidOperationException>(() => stack.Peek());
            Assert.Throws<InvalidOperationException>(() => readOnly.Peek());
        }
    }
}

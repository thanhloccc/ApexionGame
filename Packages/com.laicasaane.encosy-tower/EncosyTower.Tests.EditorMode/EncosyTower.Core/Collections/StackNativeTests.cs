#if UNITY_COLLECTIONS

using System;
using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections
{
    // Adapted from System.Collections.Generic.Stack<T> semantics.
    public partial class StackNativeTests
    {
        [Test]
        public void ConstructorsAndInPush_CreateExpectedContent()
        {
            using var capacity = new StackNative<int>(4, Allocator.Temp);
            using var source = new StackNative<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var four = 4;

            source.Push(in four);

            Assert.IsTrue(capacity.IsCreated);
            Assert.AreEqual(0, capacity.Count);
            Assert.GreaterOrEqual(capacity.Capacity, 4);
            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, source.ToArray());
        }

        [Test]
        public void PushAndPop_PreserveLifoOrder()
        {
            using var stack = new StackNative<int>(4, Allocator.Temp);
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);

            Assert.AreEqual(3, stack.Pop());
            Assert.AreEqual(2, stack.Pop());
            Assert.AreEqual(1, stack.Pop());
            Assert.AreEqual(0, stack.Count);
        }

        [Test]
        public void EmptyStack_PopAndPeekThrowWhileTryMethodsReturnFalse()
        {
            using var stack = new StackNative<int>(0, Allocator.Temp);

            Assert.Catch(() => stack.Pop());
            Assert.Catch(() => stack.Peek());
            Assert.IsFalse(stack.TryPop(out var popped));
            Assert.IsFalse(stack.TryPeek(out var peeked));
            Assert.AreEqual(0, popped);
            Assert.AreEqual(0, peeked);
        }

        [Test]
        public void Push_GrowsPastInitialCapacity()
        {
            const int N = 200;
            using var stack = new StackNative<int>(2, Allocator.Temp);

            for (var i = 0; i < N; i++)
            {
                stack.Push(i);
            }

            Assert.AreEqual(N, stack.Count);

            for (var i = N - 1; i >= 0; i--)
            {
                Assert.AreEqual(i, stack.Pop());
            }
        }

        [Test]
        public void PushRange_AppendsValuesWithLastValueOnTop()
        {
            using var stack = new StackNative<int>(2, Allocator.Temp);
            stack.Push(1);

            stack.PushRange(new[] { 2, 3, 4 });

            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, stack.ToArray());
        }

        [Test]
        public void PeekAndTryOperations_ObserveTopWithoutRemovingIt()
        {
            using var stack = new StackNative<int>(2, Allocator.Temp);
            stack.Push(10);
            stack.Push(20);

            Assert.AreEqual(20, stack.Peek());
            Assert.IsTrue(stack.TryPeek(out var peeked));
            Assert.AreEqual(20, peeked);
            Assert.AreEqual(2, stack.Count);

            Assert.IsTrue(stack.TryPop(out var popped));
            Assert.AreEqual(20, popped);
            Assert.AreEqual(10, stack.Peek());
        }

        [Test]
        public void ToArrayAndCopyTo_ReturnTopFirstOrder()
        {
            using var stack = new StackNative<int>(new[] { 1, 2, 3, 4, 5 }, Allocator.Temp);
            var full = new int[5];
            var partial = new int[3];

            stack.CopyTo(full);
            stack.CopyTo(1, partial);

            CollectionAssert.AreEqual(new[] { 5, 4, 3, 2, 1 }, stack.ToArray());
            CollectionAssert.AreEqual(new[] { 5, 4, 3, 2, 1 }, full);
            CollectionAssert.AreEqual(new[] { 4, 3, 2 }, partial);
        }

        [Test]
        public void CopyToAndTryCopyTo_AllOverloadsUseTopFirstOrder()
        {
            using var stack = new StackNative<int>(new[] { 1, 2, 3, 4 }, Allocator.Temp);
            var full = new int[4];
            var partial = new int[2];
            var offset = new int[2];
            var explicitLength = new int[3];

            stack.CopyTo(full);
            stack.CopyTo(partial.AsSpan(), 2);
            stack.CopyTo(1, offset);
            stack.CopyTo(1, explicitLength, 2);

            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, full);
            CollectionAssert.AreEqual(new[] { 4, 3 }, partial);
            CollectionAssert.AreEqual(new[] { 3, 2 }, offset);
            CollectionAssert.AreEqual(new[] { 3, 2, 0 }, explicitLength);

            Assert.IsTrue(stack.TryCopyTo(full));
            Assert.IsTrue(stack.TryCopyTo(partial, 2));
            Assert.IsTrue(stack.TryCopyTo(1, offset));
            Assert.IsTrue(stack.TryCopyTo(1, explicitLength, 2));
            Assert.IsFalse(stack.TryCopyTo(3, partial));
        }

        [Test]
        public void ReadOnly_ConversionPropertiesPeekCopiesAndEnumerationExposeTopFirstValues()
        {
            using var stack = new StackNative<int>(new[] { 1, 2, 3, 4 }, Allocator.Temp);
            StackNative<int>.ReadOnly readOnly = stack;
            var direct = stack.AsReadOnly();
            var full = new int[4];
            var partial = new int[2];
            var offset = new int[2];
            var explicitLength = new int[3];

            Assert.IsTrue(readOnly.IsCreated);
            Assert.AreEqual(readOnly.Count, direct.Count);
            Assert.AreEqual(4, readOnly.Count);
            Assert.GreaterOrEqual(readOnly.Capacity, 4);
            Assert.AreEqual(4, readOnly.Peek());
            Assert.IsTrue(readOnly.TryPeek(out var peeked));
            Assert.AreEqual(4, peeked);
            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, readOnly.ToArray());

            readOnly.CopyTo(full);
            readOnly.CopyTo(partial.AsSpan(), 2);
            readOnly.CopyTo(1, offset);
            readOnly.CopyTo(1, explicitLength, 2);

            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, full);
            CollectionAssert.AreEqual(new[] { 4, 3 }, partial);
            CollectionAssert.AreEqual(new[] { 3, 2 }, offset);
            CollectionAssert.AreEqual(new[] { 3, 2, 0 }, explicitLength);
            Assert.IsTrue(readOnly.TryCopyTo(full));
            Assert.IsTrue(readOnly.TryCopyTo(partial, 2));
            Assert.IsTrue(readOnly.TryCopyTo(1, offset));
            Assert.IsTrue(readOnly.TryCopyTo(1, explicitLength, 2));
            Assert.IsFalse(readOnly.TryCopyTo(3, partial));

            var enumerator = readOnly.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(4, enumerator.Current);
            enumerator.Reset();
            Assert.IsTrue(enumerator.MoveNext());
            enumerator.Dispose();

            stack.Clear();
            Assert.IsFalse(readOnly.TryPeek(out peeked));
            Assert.AreEqual(0, peeked);
        }

        [Test]
        public void CapacityAndOwnerEnumeratorMembers_WorkDirectly()
        {
            using var stack = new StackNative<int>(new[] { 1, 2 }, Allocator.Temp);
            var initialCapacity = stack.Capacity;

            stack.EnsureCapacity(initialCapacity + 2);
            Assert.GreaterOrEqual(stack.Capacity, initialCapacity + 2);

            var ensured = stack.Capacity;
            stack.IncreaseCapacityBy(2);
            Assert.GreaterOrEqual(stack.Capacity, ensured + 2);

            var increased = stack.Capacity;
            stack.IncreaseCapacityTo(increased + 2);
            Assert.GreaterOrEqual(stack.Capacity, increased + 2);

            stack.Trim();
            Assert.AreEqual(stack.Count, stack.Capacity);

            var enumerator = stack.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(2, enumerator.Current);
            enumerator.Reset();
            Assert.IsTrue(enumerator.MoveNext());
            enumerator.Dispose();
        }

        [Test]
        public void Clear_ResetsCountAndAllowsReuse()
        {
            using var stack = new StackNative<int>(4, Allocator.Temp);
            stack.PushRange(new[] { 1, 2, 3 });

            stack.Clear();

            Assert.AreEqual(0, stack.Count);
            Assert.IsFalse(stack.TryPeek(out _));

            stack.Push(4);

            Assert.AreEqual(4, stack.Pop());
        }

        [Test]
        public void Enumerator_VisitsValuesFromTopToBottom()
        {
            using var stack = new StackNative<int>(new[] { 1, 2, 3, 4 }, Allocator.Temp);
            var visited = new int[stack.Count];
            var index = 0;

            foreach (var value in stack)
            {
                visited[index++] = value;
            }

            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, visited);
        }

        [Test]
        public void Enumerator_MutationDuringIteration_Throws()
        {
            using var stack = new StackNative<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var enumerator = stack.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());

            stack.Push(4);

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Test]
        public void Dispose_MarksNotCreated()
        {
            var stack = new StackNative<int>(4, Allocator.Temp);
            Assert.IsTrue(stack.IsCreated);

            stack.Dispose();

            Assert.IsFalse(stack.IsCreated);
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void UseAfterDispose_Throws()
        {
            var stack = new StackNative<int>(4, Allocator.Temp);
            stack.Push(1);
            stack.Dispose();

            Assert.Catch(() => _ = stack.Count);
        }

        [Test]
        public void DoubleDispose_DoesNotThrow()
        {
            var stack = new StackNative<int>(4, Allocator.Temp);
            stack.Dispose();

            Assert.DoesNotThrow(() => stack.Dispose());
        }

        [Test]
        public void DisposeJob_CompletesAndMarksNotCreated()
        {
            var stack = new StackNative<int>(4, Allocator.TempJob);
            stack.Push(1);

            var handle = stack.Dispose(default);

            Assert.IsFalse(stack.IsCreated);
            Assert.DoesNotThrow(() => handle.Complete());
        }
    }
}

#endif

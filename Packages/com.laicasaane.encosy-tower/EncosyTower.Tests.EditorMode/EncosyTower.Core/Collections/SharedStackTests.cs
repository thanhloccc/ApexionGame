using System;
using System.Collections.Generic;
using EncosyTower.Collections;
using NUnit.Framework;

namespace EncosyTower.Tests.Core.Collections
{
    // Adapted from System.Collections.Generic.Stack<T> semantics.
    public partial class SharedStackTests
    {
        [Test]
        public void Constructors_CreateExpectedContent()
        {
            using var empty = new SharedStack<int>();
            using var capacity = new SharedStack<int>(8);
            using var span = new SharedStack<int>(new[] { 1, 2, 3 }.AsSpan());
            using var collection = new SharedStack<int>(new List<int> { 4, 5, 6 });

            Assert.AreEqual(0, empty.Count);
            Assert.AreEqual(0, capacity.Count);
            Assert.GreaterOrEqual(capacity.Capacity, 8);
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, span.ToArray());
            CollectionAssert.AreEqual(new[] { 6, 5, 4 }, collection.ToArray());
        }

        [Test]
        public void GenericConstructors_CreateExpectedContent()
        {
            using var empty = new SharedStack<int, uint>();
            using var capacity = new SharedStack<int, uint>(8);
            using var span = new SharedStack<int, uint>(new[] { 1, 2, 3 }.AsSpan());
            using var collection = new SharedStack<int, uint>(new List<int> { 4, 5, 6 });

            Assert.IsTrue(empty.IsCreated);
            Assert.AreEqual(0, empty.Count);
            Assert.GreaterOrEqual(capacity.Capacity, 8);
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, span.ToArray());
            CollectionAssert.AreEqual(new[] { 6, 5, 4 }, collection.ToArray());
        }

        [Test]
        public void PushAndPop_GrowAndPreserveLifoOrder()
        {
            const int N = 200;
            using var stack = new SharedStack<int>(2);

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
        public void ValueInAndTryOperations_ReturnExpectedValues()
        {
            using var stack = new SharedStack<int, int>(4);
            var two = 2;

            stack.Push(1);
            stack.Push(in two);

            Assert.AreEqual(2, stack.Peek());
            Assert.IsTrue(stack.TryPeek(out var peeked));
            Assert.AreEqual(2, peeked);
            Assert.IsTrue(stack.TryPop(out var popped));
            Assert.AreEqual(2, popped);
            Assert.AreEqual(1, stack.Pop());
            Assert.IsFalse(stack.TryPeek(out peeked));
            Assert.IsFalse(stack.TryPop(out popped));
            Assert.AreEqual(0, peeked);
            Assert.AreEqual(0, popped);
        }

        [Test]
        public void PushRange_PlacesLastValueOnTop()
        {
            using var stack = new SharedStack<int>(2);
            stack.Push(1);

            stack.PushRange(new[] { 2, 3, 4 });

            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, stack.ToArray());
        }

        [Test]
        public void Clear_ResetsCountAndAllowsReuse()
        {
            using var stack = new SharedStack<int>(new[] { 1, 2, 3 }.AsSpan());

            stack.Clear();

            Assert.AreEqual(0, stack.Count);
            stack.Push(4);
            Assert.AreEqual(4, stack.Pop());
        }

        [Test]
        public void Trim_ShrinksCapacityToCountWithoutChangingOrder()
        {
            using var stack = new SharedStack<int>(16);
            stack.PushRange(new[] { 1, 2, 3 });

            stack.Trim();

            Assert.AreEqual(stack.Count, stack.Capacity);
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, stack.ToArray());
        }

        [Test]
        public void CopyToAndTryCopyTo_AllOwnerAndReadOnlyOverloadsUseTopFirstOrder()
        {
            using var stack = new SharedStack<int, int>(new[] { 1, 2, 3, 4 }.AsSpan());
            var ownerFull = new int[4];
            var ownerPartial = new int[2];
            var ownerOffset = new int[2];
            var ownerLength = new int[3];

            stack.CopyTo(ownerFull);
            stack.CopyTo(ownerPartial.AsSpan(), 2);
            stack.CopyTo(1, ownerOffset);
            stack.CopyTo(1, ownerLength, 2);

            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, ownerFull);
            CollectionAssert.AreEqual(new[] { 4, 3 }, ownerPartial);
            CollectionAssert.AreEqual(new[] { 3, 2 }, ownerOffset);
            CollectionAssert.AreEqual(new[] { 3, 2, 0 }, ownerLength);
            Assert.IsTrue(stack.TryCopyTo(ownerFull));
            Assert.IsTrue(stack.TryCopyTo(ownerPartial, 2));
            Assert.IsTrue(stack.TryCopyTo(1, ownerOffset));
            Assert.IsTrue(stack.TryCopyTo(1, ownerLength, 2));
            Assert.IsFalse(stack.TryCopyTo(3, ownerPartial));

            SharedStack<int, int>.ReadOnly readOnly = stack;
            var readOnlyFull = new int[4];
            var readOnlyPartial = new int[2];
            var readOnlyOffset = new int[2];
            var readOnlyLength = new int[3];

            Assert.IsTrue(readOnly.IsCreated);
            Assert.AreEqual(4, readOnly.Count);
            Assert.GreaterOrEqual(readOnly.Capacity, 4);
            Assert.AreEqual(4, readOnly.Peek());
            Assert.IsTrue(readOnly.TryPeek(out var peeked));
            Assert.AreEqual(4, peeked);
            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, readOnly.ToArray());

            readOnly.CopyTo(readOnlyFull);
            readOnly.CopyTo(readOnlyPartial.AsSpan(), 2);
            readOnly.CopyTo(1, readOnlyOffset);
            readOnly.CopyTo(1, readOnlyLength, 2);

            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, readOnlyFull);
            CollectionAssert.AreEqual(new[] { 4, 3 }, readOnlyPartial);
            CollectionAssert.AreEqual(new[] { 3, 2 }, readOnlyOffset);
            CollectionAssert.AreEqual(new[] { 3, 2, 0 }, readOnlyLength);
            Assert.IsTrue(readOnly.TryCopyTo(readOnlyFull));
            Assert.IsTrue(readOnly.TryCopyTo(readOnlyPartial, 2));
            Assert.IsTrue(readOnly.TryCopyTo(1, readOnlyOffset));
            Assert.IsTrue(readOnly.TryCopyTo(1, readOnlyLength, 2));
            Assert.IsFalse(readOnly.TryCopyTo(3, readOnlyPartial));

            var nativeReadOnly = readOnly.AsNative();
            Assert.AreEqual(4, nativeReadOnly.Count);

            stack.Clear();
            Assert.IsFalse(readOnly.TryPeek(out peeked));
            Assert.AreEqual(0, peeked);
        }

        [Test]
        public void CapacityConversionsEnumeratorsAndDispose_WorkDirectly()
        {
            var stack = new SharedStack<int, int>(new[] { 1, 2 }.AsSpan());

            try
            {
                var initialCapacity = stack.Capacity;
                stack.IncreaseCapacityBy(2);
                Assert.GreaterOrEqual(stack.Capacity, initialCapacity + 2);

                var increased = stack.Capacity;
                stack.IncreaseCapacityTo(increased + 2);
                Assert.GreaterOrEqual(stack.Capacity, increased + 2);

                SharedStackNative<int> native = stack;
                Assert.AreEqual(2, native.Count);

                var ownerEnumerator = stack.GetEnumerator();
                Assert.IsTrue(ownerEnumerator.MoveNext());
                Assert.AreEqual(2, ownerEnumerator.Current);
                ownerEnumerator.Reset();
                Assert.IsTrue(ownerEnumerator.MoveNext());
                ownerEnumerator.Dispose();

                var readOnlyEnumerator = stack.AsReadOnly().GetEnumerator();
                Assert.IsTrue(readOnlyEnumerator.MoveNext());
                Assert.AreEqual(2, readOnlyEnumerator.Current);
                readOnlyEnumerator.Reset();
                Assert.IsTrue(readOnlyEnumerator.MoveNext());
                readOnlyEnumerator.Dispose();
            }
            finally
            {
                stack.Dispose();
            }

            Assert.IsFalse(stack.IsCreated);
            Assert.DoesNotThrow(() => stack.Dispose());
        }
    }
}

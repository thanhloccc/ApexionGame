using System;
using EncosyTower.Collections;
using NUnit.Framework;

namespace EncosyTower.Tests.Core.Collections
{
    // Adapted from System.Collections.Generic.Stack<T> semantics against SharedStackNative<T>.
    public partial class SharedStackNativeTests
    {
        [Test]
        public void OwnerPush_AfterViewCreationIsVisible()
        {
            using var stack = new SharedStack<int>(8);
            stack.Push(1);
            var view = stack.AsNative();

            stack.Push(2);

            Assert.AreEqual(2, view.Count);
            CollectionAssert.AreEqual(new[] { 2, 1 }, view.ToArray());
        }

        [Test]
        public void DefaultImplicitConversionAndValueInOperations_ExposeSharedLifoState()
        {
            SharedStackNative<int> empty = default;
            using var stack = new SharedStack<int, int>(8);
            SharedStackNative<int> view = stack;
            var two = 2;

            view.Push(1);
            view.Push(in two);

            Assert.IsFalse(empty.IsCreated);
            Assert.IsTrue(view.IsCreated);
            Assert.AreEqual(2, view.Count);
            Assert.GreaterOrEqual(view.Capacity, 8);
            Assert.AreEqual(2, view.Peek());
            Assert.IsTrue(view.TryPeek(out var peeked));
            Assert.AreEqual(2, peeked);
            Assert.IsTrue(view.TryPop(out var popped));
            Assert.AreEqual(2, popped);
            Assert.AreEqual(1, view.Pop());
            Assert.IsFalse(view.TryPeek(out peeked));
            Assert.IsFalse(view.TryPop(out popped));
            Assert.AreEqual(0, peeked);
            Assert.AreEqual(0, popped);
        }

        [Test]
        public void CopyToAndTryCopyTo_NativeAndReadOnlyOverloadsUseTopFirstOrder()
        {
            using var stack = new SharedStack<int>(new[] { 1, 2, 3, 4 }.AsSpan());
            var view = stack.AsNative();
            var full = new int[4];
            var partial = new int[2];
            var offset = new int[2];
            var explicitLength = new int[3];

            view.CopyTo(full);
            view.CopyTo(partial.AsSpan(), 2);
            view.CopyTo(1, offset);
            view.CopyTo(1, explicitLength, 2);

            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, view.ToArray());
            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, full);
            CollectionAssert.AreEqual(new[] { 4, 3 }, partial);
            CollectionAssert.AreEqual(new[] { 3, 2 }, offset);
            CollectionAssert.AreEqual(new[] { 3, 2, 0 }, explicitLength);
            Assert.IsTrue(view.TryCopyTo(full));
            Assert.IsTrue(view.TryCopyTo(partial, 2));
            Assert.IsTrue(view.TryCopyTo(1, offset));
            Assert.IsTrue(view.TryCopyTo(1, explicitLength, 2));
            Assert.IsFalse(view.TryCopyTo(3, partial));

            var readOnly = view.AsReadOnly();
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

            view.Clear();
            Assert.IsFalse(readOnly.TryPeek(out peeked));
            Assert.AreEqual(0, peeked);
        }

        [Test]
        public void NativeAndReadOnlyEnumerators_MoveResetAndDisposeDirectly()
        {
            using var stack = new SharedStack<int>(new[] { 1, 2 }.AsSpan());
            var view = stack.AsNative();
            var ownerEnumerator = view.GetEnumerator();

            Assert.IsTrue(ownerEnumerator.MoveNext());
            Assert.AreEqual(2, ownerEnumerator.Current);
            ownerEnumerator.Reset();
            Assert.IsTrue(ownerEnumerator.MoveNext());
            ownerEnumerator.Dispose();

            var readOnlyEnumerator = view.AsReadOnly().GetEnumerator();
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            Assert.AreEqual(2, readOnlyEnumerator.Current);
            readOnlyEnumerator.Reset();
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            readOnlyEnumerator.Dispose();
        }

        [Test]
        public void Clear_ResetsSharedCountAndAllowsReuse()
        {
            using var stack = new SharedStack<int>(new[] { 1, 2 }.AsSpan());
            var view = stack.AsNative();

            view.Clear();
            view.Push(3);

            CollectionAssert.AreEqual(new[] { 3 }, stack.ToArray());
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void ViewCreatedBeforeOwnerGrow_ThrowsOnNextAccess()
        {
            using var stack = new SharedStack<int>(2);
            stack.Push(1);
            stack.Push(2);
            var staleView = stack.AsNative();

            stack.Push(3);

            Assert.Catch(() => _ = staleView.Count);
        }

        [Test]
        public void ViewCreatedAfterOwnerGrow_CanMutateOwner()
        {
            using var stack = new SharedStack<int>(2);
            stack.PushRange(new[] { 1, 2, 3 });
            var view = stack.AsNative();

            view.Push(4);

            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, stack.ToArray());
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void FullView_PushThrowsBecauseCapacityIsImmutable()
        {
            using var stack = new SharedStack<int>(new[] { 1, 2 }.AsSpan());
            var view = stack.AsNative();

            Assert.Throws<InvalidOperationException>(() => view.Push(3));
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void Dispose_ReleasesOwnerAndNativeHeader()
        {
            var stack = new SharedStack<int>(4);
            stack.Push(1);
            var view = stack.AsNative();

            stack.Dispose();

            Assert.IsFalse(stack.IsCreated);
            Assert.Catch(() => _ = view.Count);
            Assert.DoesNotThrow(() => stack.Dispose());
        }
    }
}

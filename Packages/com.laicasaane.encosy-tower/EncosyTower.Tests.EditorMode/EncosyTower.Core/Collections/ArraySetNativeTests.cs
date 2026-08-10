using System;
using System.Collections;
using System.Collections.Generic;
using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections
{
    // Adapted from Unity.Collections.Tests/NativeHashSetTests.cs against the Native forwarder.
    public partial class ArraySetNativeTests
    {
        [Test]
        public void Constructor_CreatesValidSet()
        {
            using var set = new ArraySetNative<int>(4, Allocator.Temp);

            Assert.IsTrue(set.IsCreated);
            Assert.AreEqual(0, set.Count);
            Assert.GreaterOrEqual(set.Capacity, 4);
        }

        [Test]
        public void DefaultSet_IsNotCreated()
        {
            ArraySetNative<int> set = default;

            Assert.IsFalse(set.IsCreated);
        }

        [Test]
        public void Add_StoresUniqueValues()
        {
            using var set = new ArraySetNative<int>(4, Allocator.Temp);

            Assert.IsTrue(set.Add(1));
            Assert.IsTrue(set.Add(2));
            Assert.IsFalse(set.Add(1));

            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(2));
            Assert.IsFalse(set.Contains(3));
        }

        [Test]
        public void Remove_RemovesValue()
        {
            using var set = new ArraySetNative<int>(4, Allocator.Temp);
            set.Add(1);
            set.Add(2);
            set.Add(3);

            Assert.IsTrue(set.Remove(2));
            Assert.IsFalse(set.Remove(99));

            Assert.AreEqual(2, set.Count);
            Assert.IsFalse(set.Contains(2));
            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(3));
        }

        [Test]
        public void Clear_ResetsCount()
        {
            using var set = new ArraySetNative<int>(4, Allocator.Temp);
            set.Add(1);
            set.Add(2);

            set.Clear();

            Assert.AreEqual(0, set.Count);
            Assert.IsFalse(set.Contains(1));

            set.Add(5);
            Assert.AreEqual(1, set.Count);
            Assert.IsTrue(set.Contains(5));
        }

        [Test]
        public void Grow_PastInitialCapacity_KeepsAllValues()
        {
            const int N = 200;
            using var set = new ArraySetNative<int>(4, Allocator.Temp);

            for (var i = 0; i < N; i++)
            {
                Assert.IsTrue(set.Add(i));
            }

            Assert.AreEqual(N, set.Count);

            for (var i = 0; i < N; i++)
            {
                Assert.IsTrue(set.Contains(i), $"missing {i}");
            }
        }

        [Test]
        public void EnsureCapacity_GrowsCapacity()
        {
            using var set = new ArraySetNative<int>(4, Allocator.Temp);
            var before = set.Capacity;

            set.EnsureCapacity(before + 100);

            Assert.Greater(set.Capacity, before);
        }

        [Test]
        public void Trim_ShrinksCapacityToCount()
        {
            using var set = new ArraySetNative<int>(64, Allocator.Temp);
            set.Add(1);
            set.Add(2);

            set.Trim();

            Assert.AreEqual(2, set.Capacity);
            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(2));
        }

        [Test]
        public void Enumerator_VisitsAllValues()
        {
            using var set = new ArraySetNative<int>(8, Allocator.Temp);

            for (var i = 0; i < 5; i++)
            {
                set.Add(i * 10);
            }

            var seen = new HashSet<int>();

            foreach (var value in set)
            {
                seen.Add(value);
            }

            CollectionAssert.AreEquivalent(new[] { 0, 10, 20, 30, 40 }, seen);
        }

        [Test]
        public void Enumerator_ModifyDuringIteration_Throws()
        {
            using var set = new ArraySetNative<int>(8, Allocator.Temp);
            set.Add(1);
            set.Add(2);

            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var value in set)
                {
                    set.Add(value + 100);
                }
            });
        }

        [Test]
        public void CopyConstructor_DeepCopiesContent()
        {
            using var source = new ArraySetNative<int>(8, Allocator.Temp);
            source.Add(1);
            source.Add(2);

            using var copy = new ArraySetNative<int>(source, Allocator.Temp);

            Assert.AreEqual(source.Count, copy.Count);
            Assert.IsTrue(copy.Contains(1));
            Assert.IsTrue(copy.Contains(2));

            copy.Add(3);
            Assert.IsFalse(source.Contains(3));
        }

        [Test]
        public void CopyConstructor_AfterBucketGrowth_AllValuesResolve()
        {
            const int N = 500;
            using var source = new ArraySetNative<int>(2, Allocator.Temp);

            for (var i = 0; i < N; i++)
            {
                source.Add(i);
            }

            using var copy = new ArraySetNative<int>(source, Allocator.Temp);

            Assert.AreEqual(N, copy.Count);

            for (var i = 0; i < N; i++)
            {
                Assert.IsTrue(copy.Contains(i), $"missing {i} after copy");
            }
        }

        [Test]
        public void Intersect_KeepsOnlyShared()
        {
            using var a = new ArraySetNative<int>(8, Allocator.Temp);
            using var b = new ArraySetNative<int>(8, Allocator.Temp);

            a.Add(1); a.Add(2); a.Add(3);
            b.Add(2); b.Add(3); b.Add(4);

            a.Intersect(in b);

            Assert.AreEqual(2, a.Count);
            Assert.IsFalse(a.Contains(1));
            Assert.IsTrue(a.Contains(2));
            Assert.IsTrue(a.Contains(3));
        }

        [Test]
        public void Exclude_RemovesShared()
        {
            using var a = new ArraySetNative<int>(8, Allocator.Temp);
            using var b = new ArraySetNative<int>(8, Allocator.Temp);

            a.Add(1); a.Add(2); a.Add(3);
            b.Add(2); b.Add(3);

            a.Exclude(in b);

            Assert.AreEqual(1, a.Count);
            Assert.IsTrue(a.Contains(1));
            Assert.IsFalse(a.Contains(2));
        }

        [Test]
        public void Union_MergesValues()
        {
            using var a = new ArraySetNative<int>(8, Allocator.Temp);
            using var b = new ArraySetNative<int>(8, Allocator.Temp);

            a.Add(1); a.Add(2);
            b.Add(2); b.Add(3);

            a.Union(in b);

            Assert.AreEqual(3, a.Count);
            Assert.IsTrue(a.Contains(1));
            Assert.IsTrue(a.Contains(2));
            Assert.IsTrue(a.Contains(3));
        }

        [Test]
        public void Reinterpret_ReusesUnderlyingValues()
        {
            using var set = new ArraySetNative<int>(8, Allocator.Temp);
            set.Add(1);

            var reinterpreted = set.Reinterpret<uint>();

            Assert.AreEqual(set.Count, reinterpreted.Count);
            Assert.IsTrue(reinterpreted.Contains(1u));
        }

        [Test]
        public void AsReadOnly_ReflectsContent()
        {
            using var set = new ArraySetNative<int>(8, Allocator.Temp);
            set.Add(1);
            set.Add(2);

            var readOnly = set.AsReadOnly();

            Assert.IsTrue(readOnly.IsCreated);
            Assert.AreEqual(2, readOnly.Count);

            var buffer = new int[readOnly.Count];
            readOnly.CopyTo(buffer, readOnly.Count);
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, buffer);
        }

        [Test]
        public void Dispose_MarksNotCreated()
        {
            var set = new ArraySetNative<int>(4, Allocator.Temp);
            Assert.IsTrue(set.IsCreated);

            set.Dispose();

            Assert.IsFalse(set.IsCreated);
        }

        [Test]
        public void DisposeJob_CompletesAndMarksNotCreated()
        {
            var set = new ArraySetNative<int>(4, Allocator.TempJob);
            set.Add(1);

            var handle = set.Dispose(default);

            Assert.IsFalse(set.IsCreated);
            Assert.DoesNotThrow(() => handle.Complete());
        }

        [Test]
        public void DoubleDispose_DoesNotThrow()
        {
            var set = new ArraySetNative<int>(4, Allocator.Temp);
            set.Dispose();

            Assert.DoesNotThrow(() => set.Dispose());
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void UseAfterDispose_Throws()
        {
            var set = new ArraySetNative<int>(4, Allocator.Temp);
            set.Add(1);
            set.Dispose();

            Assert.Catch(() => set.Contains(1));
        }

        [Test]
        public void Collisions_ManyValues_AllRetrievable()
        {
            const int N = 500;
            using var set = new ArraySetNative<int>(2, Allocator.Temp);

            for (var i = 0; i < N; i++)
            {
                set.Add(i);
            }

            for (var i = 0; i < N; i++)
            {
                Assert.IsTrue(set.Contains(i), $"missing {i}");
            }
        }

        [Test]
        public void ValueOverloadsAndCapacityOperations_MutateExpectedState()
        {
            using var set = new ArraySetNative<int>(4, Allocator.Temp);
            var two = 2;
            var missing = 99;

            Assert.IsTrue(set.Add(1));
            Assert.IsTrue(set.Add(in two));
            Assert.IsFalse(set.Add(in two));
            Assert.AreEqual(2, set.Items.Length);
            Assert.AreEqual(1, set.Items[0]);
            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(in two));
            Assert.IsFalse(set.Contains(in missing));
            Assert.IsTrue(set.Remove(1));
            Assert.IsTrue(set.Remove(in two));
            Assert.IsFalse(set.Remove(in missing));

            var initialCapacity = set.Capacity;
            set.EnsureCapacity(initialCapacity + 4);
            Assert.GreaterOrEqual(set.Capacity, initialCapacity + 4);

            var ensuredCapacity = set.Capacity;
            set.IncreaseCapacityBy(4);
            Assert.GreaterOrEqual(set.Capacity, ensuredCapacity + 4);

            var increasedCapacity = set.Capacity;
            set.IncreaseCapacityTo(increasedCapacity + 4);
            Assert.GreaterOrEqual(set.Capacity, increasedCapacity + 4);
        }

        [Test]
        public void ReadOnly_PropertiesContainsAndAllCopyOverloadsReflectOwner()
        {
            using var set = new ArraySetNative<int>(4, Allocator.Temp);
            set.Add(1);
            set.Add(2);
            set.Add(3);
            set.Add(4);
            var readOnly = set.AsReadOnly();
            var expected = new[] { 1, 2, 3, 4 };
            var destination = new int[set.Count];

            Assert.IsTrue(readOnly.IsCreated);
            Assert.AreEqual(set.Capacity, readOnly.Capacity);
            Assert.AreEqual(set.Count, readOnly.Count);
            Assert.AreEqual(4, readOnly.Items.Length);
            Assert.AreEqual(1, readOnly.Items[0]);
            Assert.IsTrue(readOnly.Contains(1));
            var two = 2;
            var missing = 99;
            Assert.IsTrue(readOnly.Contains(in two));
            Assert.IsFalse(readOnly.Contains(in missing));

            readOnly.CopyTo(destination.AsSpan());
            CollectionAssert.AreEqual(expected, destination);

            Array.Clear(destination, 0, destination.Length);
            readOnly.CopyTo(destination.AsSpan(), 2);
            readOnly.CopyTo(2, destination.AsSpan(2));
            CollectionAssert.AreEqual(expected, destination);

            Array.Clear(destination, 0, destination.Length);
            readOnly.CopyTo(1, destination.AsSpan(), 2);
            CollectionAssert.AreEqual(
                  expected.AsSpan(1, 2).ToArray()
                , destination.AsSpan(0, 2).ToArray()
            );

            Assert.IsTrue(readOnly.TryCopyTo(destination.AsSpan()));
            Assert.IsTrue(readOnly.TryCopyTo(destination.AsSpan(), 2));
            Assert.IsTrue(readOnly.TryCopyTo(1, destination.AsSpan(0, 3)));
            Assert.IsTrue(readOnly.TryCopyTo(1, destination.AsSpan(), 2));
            Assert.IsFalse(readOnly.TryCopyTo(new int[3].AsSpan(), set.Count));
            Assert.IsFalse(readOnly.TryCopyTo(3, new int[2].AsSpan()));
            Assert.IsFalse(default(ArraySetNative<int>.ReadOnly).IsCreated);
        }

        [Test]
        public void Enumerators_DirectConstructionMovementCurrentResetDisposeAndValidity()
        {
            using var set = new ArraySetNative<int>(4, Allocator.Temp);
            set.Add(7);
            var direct = new ArraySetNativeEnumerator<int>(in set);

            Assert.IsTrue(direct.IsValid);
            Assert.IsTrue(direct.MoveNext());
            Assert.AreEqual(7, direct.Current);
            Assert.IsFalse(direct.MoveNext());
            direct.Reset();
            Assert.IsTrue(direct.MoveNext());
            IEnumerator interfaceEnumerator = direct;
            Assert.AreEqual(7, interfaceEnumerator.Current);
            direct.Dispose();

            var ownerEnumerator = set.GetEnumerator();
            Assert.IsTrue(ownerEnumerator.MoveNext());
            ownerEnumerator.Dispose();

            var readOnly = set.AsReadOnly();
            var readOnlyDirect = new ArraySetNativeReadOnlyEnumerator<int>(in readOnly);
            Assert.IsTrue(readOnlyDirect.IsValid);
            Assert.IsTrue(readOnlyDirect.MoveNext());
            Assert.AreEqual(7, readOnlyDirect.Current);
            Assert.IsFalse(readOnlyDirect.MoveNext());
            readOnlyDirect.Reset();
            Assert.IsTrue(readOnlyDirect.MoveNext());
            IEnumerator readOnlyInterfaceEnumerator = readOnlyDirect;
            Assert.AreEqual(7, readOnlyInterfaceEnumerator.Current);
            readOnlyDirect.Dispose();

            var readOnlyEnumerator = readOnly.GetEnumerator();
            Assert.IsTrue(readOnlyEnumerator.MoveNext());
            readOnlyEnumerator.Dispose();

            Assert.IsFalse(default(ArraySetNativeEnumerator<int>).IsValid);
            Assert.IsFalse(default(ArraySetNativeReadOnlyEnumerator<int>).IsValid);
        }

        [Test]
        public void HashCollisionsAndRemovalReuse_PreserveRemainingValues()
        {
            using var set = new ArraySetNative<CollisionValue>(2, Allocator.Temp);
            var first = new CollisionValue(1);
            var second = new CollisionValue(2);
            var third = new CollisionValue(3);

            Assert.IsTrue(set.Add(first));
            Assert.IsTrue(set.Add(second));
            Assert.IsTrue(set.Add(third));
            Assert.IsFalse(set.Add(second));
            Assert.IsTrue(set.Remove(second));
            Assert.IsTrue(set.Add(new CollisionValue(4)));
            Assert.IsTrue(set.Contains(first));
            Assert.IsTrue(set.Contains(third));
            Assert.IsTrue(set.Contains(new CollisionValue(4)));
            Assert.IsFalse(set.Contains(second));
        }

        private readonly struct CollisionValue : IEquatable<CollisionValue>
        {
            private readonly int _value;

            public CollisionValue(int value)
            {
                _value = value;
            }

            public bool Equals(CollisionValue other)
                => _value == other._value;

            public override bool Equals(object obj)
                => obj is CollisionValue other && Equals(other);

            public override int GetHashCode()
                => 1;
        }
    }
}

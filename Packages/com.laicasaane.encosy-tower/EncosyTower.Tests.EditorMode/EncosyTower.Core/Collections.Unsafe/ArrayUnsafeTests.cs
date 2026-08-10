// Adapted from Unity.Collections.Tests/NativeArrayTests.cs.

using System;
using System.Collections;
using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class ArrayUnsafeTests
    {
        [Test]
        public void Constructor_DefaultClearsMemory()
        {
            using var array = new ArrayUnsafe<int>(4, Allocator.Temp);

            Assert.IsTrue(array.IsCreated);
            Assert.AreEqual(4, array.Length);

            for (var i = 0; i < array.Length; i++)
            {
                Assert.AreEqual(0, array[i]);
            }
        }

        [Test]
        public void Constructor_UninitializedMemoryCanBeWritten()
        {
            using var array = new ArrayUnsafe<int>(
                  3
                , Allocator.Temp
                , NativeArrayOptions.UninitializedMemory
            );

            array[0] = 10;
            array[1] = 20;
            array[2] = 30;

            CollectionAssert.AreEqual(new[] { 10, 20, 30 }, array.ToArray());
        }

        [Test]
        public void Constructor_AllocatorHandleCreatesWritableArray()
        {
            using var array = new ArrayUnsafe<int>(2, AllocatorManager.Persistent);

            array[0] = 10;
            array[1] = 20;

            CollectionAssert.AreEqual(new[] { 10, 20 }, array.ToArray());
        }

        [Test]
        public void Constructor_ZeroLengthCreatesDisposableArray()
        {
            var array = new ArrayUnsafe<int>(0, Allocator.Temp);

            Assert.IsTrue(array.IsCreated);
            Assert.AreEqual(0, array.Length);
            Assert.AreEqual(0, array.AsSpan().Length);

            array.Dispose();

            Assert.IsFalse(array.IsCreated);
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void Constructor_NonOwningAllocatorStrategiesThrow()
        {
            Assert.Throws<ArgumentException>(
                () => _ = new ArrayUnsafe<int>(1, default(AllocatorStrategy))
            );
            Assert.Throws<ArgumentException>(
                () => _ = new ArrayUnsafe<int>(1, Allocator.None)
            );
            Assert.Throws<ArgumentException>(
                () => _ = new ArrayUnsafe<int>(1, Allocator.Invalid)
            );
        }

        [Test]
        public void Indexer_ReadsAndWritesValues()
        {
            using var array = new ArrayUnsafe<int>(3, Allocator.Temp);

            array[0] = 10;
            array[2] = 30;

            Assert.AreEqual(10, array[0]);
            Assert.AreEqual(30, array[2]);
        }

        [Test]
        [TestRequiresCollectionChecks]
        public void Indexer_OutOfRange_Throws()
        {
            using var array = new ArrayUnsafe<int>(3, Allocator.Temp);

            Assert.Catch(() => _ = array[-1]);
            Assert.Catch(() => array[3] = 10);
        }

        [Test]
        public void CopyFromAndTryCopyFrom_SpanFamiliesCopyOrReturnFalse()
        {
            using var array = new ArrayUnsafe<int>(6, Allocator.Temp);

            array.CopyFrom(new[] { 1, 2, 3 });
            array.CopyFrom(new[] { 4, 5, 6 }, 2);
            array.CopyFrom(3, new[] { 7, 8 });
            array.CopyFrom(5, new[] { 9, 10 }, 1);

            CollectionAssert.AreEqual(new[] { 4, 5, 3, 7, 8, 9 }, array.ToArray());
            Assert.IsTrue(array.TryCopyFrom(new[] { 11, 12 }));
            Assert.IsTrue(array.TryCopyFrom(new[] { 13, 14 }, 1));
            Assert.IsTrue(array.TryCopyFrom(2, new[] { 15, 16 }));
            Assert.IsTrue(array.TryCopyFrom(4, new[] { 17, 18 }, 2));
            Assert.IsFalse(array.TryCopyFrom(5, new[] { 19, 20 }));
        }

        [Test]
        public void CopyToAndTryCopyTo_SpanFamiliesCopyOrReturnFalse()
        {
            using var array = new ArrayUnsafe<int>(6, Allocator.Temp);
            array.CopyFrom(new[] { 1, 2, 3, 4, 5, 6 });

            var all = new int[6];
            var firstThree = new int[3];
            var fromIndex = new int[2];
            var range = new int[2];

            array.CopyTo(all);
            array.CopyTo(firstThree, 3);
            array.CopyTo(2, fromIndex);
            array.CopyTo(4, range, 2);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, all);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, firstThree);
            CollectionAssert.AreEqual(new[] { 3, 4 }, fromIndex);
            CollectionAssert.AreEqual(new[] { 5, 6 }, range);
            Assert.IsTrue(array.TryCopyTo(new int[6]));
            Assert.IsTrue(array.TryCopyTo(new int[3], 3));
            Assert.IsTrue(array.TryCopyTo(2, new int[2]));
            Assert.IsTrue(array.TryCopyTo(4, new int[2], 2));
            Assert.IsFalse(array.TryCopyTo(new int[1], 2));
        }

        [Test]
        public void ToArrayAndSpans_ReflectContentAndWrites()
        {
            using var array = new ArrayUnsafe<int>(3, Allocator.Temp);
            array.CopyFrom(new[] { 1, 2, 3 });

            var span = array.AsSpan();
            var readOnlySpan = array.AsReadOnlySpan();

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, array.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, readOnlySpan.ToArray());

            span[1] = 20;

            Assert.AreEqual(20, array[1]);
        }

        [Test]
        public void Reinterpret_SharesMemory()
        {
            using var array = new ArrayUnsafe<int>(2, Allocator.Temp);
            array[0] = 42;

            var reinterpreted = array.Reinterpret<uint>();

            Assert.AreEqual(array.Length, reinterpreted.Length);
            Assert.AreEqual(42u, reinterpreted[0]);

            reinterpreted[1] = 99u;

            Assert.AreEqual(99, array[1]);
            reinterpreted.Dispose();
        }

        [Test]
        public void EqualsAndGetHashCode_UseBufferIdentity()
        {
            var array = new ArrayUnsafe<int>(2, Allocator.Temp);
            var alias = array;
            using var other = new ArrayUnsafe<int>(2, Allocator.Temp);

            Assert.IsTrue(array.Equals(alias));
            Assert.IsTrue(array == alias);
            Assert.IsFalse(array != alias);
            Assert.AreEqual(array.GetHashCode(), alias.GetHashCode());
            Assert.IsFalse(array.Equals(other));
            Assert.IsTrue(array.Equals((object)alias));
            Assert.IsFalse(array.Equals((object)other));
            Assert.IsTrue(array != other);

            array.Dispose();
        }

        [Test]
        public void Enumerator_VisitsEveryElement()
        {
            using var array = new ArrayUnsafe<int>(3, Allocator.Temp);
            array.CopyFrom(new[] { 1, 2, 3 });
            var values = new int[3];
            var index = 0;

            foreach (var value in array)
            {
                values[index++] = value;
            }

            Assert.AreEqual(3, index);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, values);
        }

        [Test]
        public void Dispose_MarksNotCreated()
        {
            var array = new ArrayUnsafe<int>(3, Allocator.Temp);

            array.Dispose();

            Assert.IsFalse(array.IsCreated);
        }

        [Test]
        public void DoubleDispose_Throws()
        {
            var array = new ArrayUnsafe<int>(3, Allocator.Temp);
            array.Dispose();

            Assert.Throws<ObjectDisposedException>(() => array.Dispose());
        }

        [Test]
        public void DisposeJob_CompletesAndMarksNotCreated()
        {
            var array = new ArrayUnsafe<int>(3, AllocatorManager.Persistent);

            array.Dispose(default(JobHandle)).Complete();

            Assert.IsFalse(array.IsCreated);
        }

        [Test]
        public void GetUnsafePtr_MutatesOwnedStorage()
        {
            using var array = new ArrayUnsafe<int>(2, Allocator.Temp);

            // SAFETY: The pointer is bounded by the live two-element allocation and does not escape.
            unsafe
            {
                var pointer = (int*)array.GetUnsafePtr();

                Assert.AreNotEqual(IntPtr.Zero, (IntPtr)pointer);

                pointer[0] = 10;
                pointer[1] = 20;
            }

            CollectionAssert.AreEqual(new[] { 10, 20 }, array.ToArray());
        }

        [Test]
        public void Enumerator_DirectConstructionResetDisposeAndInterfacesWork()
        {
            var array = new ArrayUnsafe<int>(2, Allocator.Temp);

            try
            {
                array.CopyFrom(new[] { 7, 8 });
                var alias = array;
                var direct = new ArrayUnsafe<int>.Enumerator(ref alias);

                Assert.IsTrue(direct.MoveNext());
                Assert.AreEqual(7, direct.Current);
                Assert.IsTrue(direct.MoveNext());
                Assert.AreEqual(8, direct.Current);
                Assert.IsFalse(direct.MoveNext());
                direct.Reset();
                Assert.IsTrue(direct.MoveNext());
                IEnumerator interfaceEnumerator = direct;
                Assert.AreEqual(7, interfaceEnumerator.Current);
                direct.Dispose();

                var ownerEnumerator = array.GetEnumerator();
                Assert.IsTrue(ownerEnumerator.MoveNext());
                ownerEnumerator.Dispose();
            }
            finally
            {
                array.Dispose();
            }
        }
    }
}

// Adapted from Unity.Collections.Tests/NativeArrayTests.cs slice cases.

using System;
using System.Collections;
using System.Collections.Generic;
using EncosyTower.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class NativeSliceReadOnlyTests
    {
        [Test]
        public void Constructors_CreateExpectedWindows()
        {
            using var array = new NativeArray<int>(new[] { 1, 2, 3, 4, 5 }, Allocator.Temp);
            var nativeSlice = new NativeSlice<int>(array, 1, 3);
            var readOnlyArray = array.AsReadOnly();
            var empty = new NativeSliceReadOnly<int>();

            var fromSlice = new NativeSliceReadOnly<int>(nativeSlice);
            var fromSliceStart = new NativeSliceReadOnly<int>(nativeSlice, 1);
            var fromSliceRange = new NativeSliceReadOnly<int>(nativeSlice, 1, 1);
            var fromArray = new NativeSliceReadOnly<int>(array);
            var fromArrayStart = new NativeSliceReadOnly<int>(array, 2);
            var fromArrayRange = new NativeSliceReadOnly<int>(array, 1, 2);
            var fromReadOnly = new NativeSliceReadOnly<int>(readOnlyArray);
            var fromReadOnlyStart = new NativeSliceReadOnly<int>(readOnlyArray, 2);
            var fromReadOnlyRange = new NativeSliceReadOnly<int>(readOnlyArray, 1, 2);

            Assert.AreEqual(0, empty.Length);
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, fromSlice.ToArray());
            CollectionAssert.AreEqual(new[] { 3, 4 }, fromSliceStart.ToArray());
            CollectionAssert.AreEqual(new[] { 3 }, fromSliceRange.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, fromArray.ToArray());
            CollectionAssert.AreEqual(new[] { 3, 4, 5 }, fromArrayStart.ToArray());
            CollectionAssert.AreEqual(new[] { 2, 3 }, fromArrayRange.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, fromReadOnly.ToArray());
            CollectionAssert.AreEqual(new[] { 3, 4, 5 }, fromReadOnlyStart.ToArray());
            CollectionAssert.AreEqual(new[] { 2, 3 }, fromReadOnlyRange.ToArray());
        }

        [Test]
        public void SubSliceConstructors_CreateExpectedWindows()
        {
            using var array = new NativeArray<int>(new[] { 1, 2, 3, 4, 5 }, Allocator.Temp);
            var source = new NativeSliceReadOnly<int>(array);

            var copy = new NativeSliceReadOnly<int>(source);
            var fromStart = new NativeSliceReadOnly<int>(source, 2);
            var fromRange = new NativeSliceReadOnly<int>(source, 1, 3);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, copy.ToArray());
            CollectionAssert.AreEqual(new[] { 3, 4, 5 }, fromStart.ToArray());
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, fromRange.ToArray());
        }

        [Test]
        public void IndexerLengthAndStride_ReflectSource()
        {
            using var array = new NativeArray<int>(new[] { 10, 20, 30 }, Allocator.Temp);
            var slice = new NativeSliceReadOnly<int>(array);

            Assert.AreEqual(3, slice.Length);
            Assert.AreEqual(UnsafeUtility.SizeOf<int>(), slice.Stride);
            Assert.AreEqual(10, slice[0]);
            Assert.AreEqual(30, slice[2]);
        }

        [Test]
        public void ImplicitConversions_CreateExpectedViews()
        {
            using var array = new NativeArray<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var nativeSlice = new NativeSlice<int>(array, 1, 2);
            var readOnlyArray = array.AsReadOnly();

            NativeSliceReadOnly<int> fromSlice = nativeSlice;
            NativeSliceReadOnly<int> fromArray = array;
            NativeSliceReadOnly<int> fromReadOnly = readOnlyArray;

            CollectionAssert.AreEqual(new[] { 2, 3 }, fromSlice.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, fromArray.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, fromReadOnly.ToArray());
        }

        [Test]
        public void SliceConvert_ReinterpretsContiguousBytes()
        {
            using var array = new NativeArray<int>(new[] { 0x01020304 }, Allocator.Temp);
            var slice = new NativeSliceReadOnly<int>(array);

            var bytes = slice.SliceConvert<byte>();

            Assert.AreEqual(UnsafeUtility.SizeOf<int>(), bytes.Length);
            CollectionAssert.AreEqual(BitConverter.GetBytes(array[0]), bytes.ToArray());
        }

        [Test]
        public void SliceWithStride_SelectsFieldFromEveryElement()
        {
            using var array = new NativeArray<Pair>(
                  new[] {
                      new Pair { First = 1, Second = 2 },
                      new Pair { First = 3, Second = 4 },
                  }
                , Allocator.Temp
            );
            var slice = new NativeSliceReadOnly<Pair>(array);

            var secondValues = slice.SliceWithStride<int>(UnsafeUtility.SizeOf<int>());
            var firstValues = slice.SliceWithStride<int>();

            CollectionAssert.AreEqual(new[] { 2, 4 }, secondValues.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 3 }, firstValues.ToArray());
        }

        [Test]
        public void CopyToAndToArray_CopyEveryValue()
        {
            using var source = new NativeArray<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            using var nativeDestination = new NativeArray<int>(3, Allocator.Temp);
            var managedDestination = new int[3];
            var slice = new NativeSliceReadOnly<int>(source);

            slice.CopyTo(nativeDestination);
            slice.CopyTo(managedDestination);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, nativeDestination.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, managedDestination);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, slice.ToArray());
        }

        [Test]
        public void Enumerator_VisitsEveryValue()
        {
            using var array = new NativeArray<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var slice = new NativeSliceReadOnly<int>(array);
            var values = new List<int>();

            foreach (var value in slice)
            {
                values.Add(value);
            }

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, values);
        }

        [Test]
        public void AsSpanEqualityHashAndOperators_UseViewIdentityAndExposeContent()
        {
            using var array = new NativeArray<int>(new[] { 1, 2, 3 }, Allocator.Temp);
            var slice = new NativeSliceReadOnly<int>(array);
            var alias = new NativeSliceReadOnly<int>(slice);
            var different = new NativeSliceReadOnly<int>(slice, 1);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, slice.AsSpan().ToArray());
            Assert.IsTrue(slice.Equals(alias));
            Assert.IsTrue(slice.Equals((object)alias));
            Assert.AreEqual(slice.GetHashCode(), alias.GetHashCode());
            Assert.IsTrue(slice == alias);
            Assert.IsFalse(slice != alias);
            Assert.IsFalse(slice == different);
            Assert.IsTrue(slice != different);
        }

        [Test]
        public void Enumerator_DirectConstructionResetDisposeAndInterfacesWork()
        {
            using var array = new NativeArray<int>(new[] { 7, 8 }, Allocator.Temp);
            var slice = new NativeSliceReadOnly<int>(array);
            var direct = new NativeSliceReadOnly<int>.Enumerator(ref slice);

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

            var fromOwner = slice.GetEnumerator();
            Assert.IsTrue(fromOwner.MoveNext());
            fromOwner.Dispose();

            IEnumerable<int> genericEnumerable = slice;
            var genericEnumerator = genericEnumerable.GetEnumerator();
            Assert.IsTrue(genericEnumerator.MoveNext());
            genericEnumerator.Dispose();

            IEnumerable enumerable = slice;
            var enumerator = enumerable.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(7, enumerator.Current);
        }

        private struct Pair
        {
            public int First;
            public int Second;
        }
    }
}

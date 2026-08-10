// Adapted from Unity.Collections.Tests/NativeListTests.cs and FixedListTests.gen.cs.

using System;
using System.Collections.Generic;
using EncosyTower.Collections;
using EncosyTower.Common;
using NUnit.Framework;

namespace EncosyTower.Tests.Core.Collections
{
    public partial class ListFastTests
    {
        private static ListFast<int> NewList(params int[] values)
            => new(new List<int>(values));

        [Test]
        public void ConstructorsAndIdentityMembers_WrapExpectedList()
        {
            var empty = new ListFast<int>();
            var source = new List<int> { 1, 2, 3 };
            var wrapped = new ListFast<int>(source);
            var same = new ListFast<int>(source);
            var other = NewList(1, 2, 3);
            ListFast<int> notCreated = default;
            ListFast<int> convertedFromList = source;
            List<int> convertedToList = wrapped;
            ListFast<int>.ReadOnly readOnly = wrapped;

            Assert.IsTrue(empty.IsCreated);
            Assert.AreEqual(0, empty.Count);
            Assert.IsFalse(empty.IsReadOnly);
            Assert.IsTrue(wrapped.IsCreated);
            Assert.AreSame(source, wrapped.List);
            Assert.AreSame(source, convertedFromList.List);
            Assert.AreSame(source, convertedToList);
            CollectionAssert.AreEqual(source, wrapped.ToArray());
            Assert.IsFalse(notCreated.IsCreated);
            Assert.IsTrue(wrapped == same);
            Assert.IsFalse(wrapped != same);
            Assert.IsTrue(wrapped != other);
            Assert.IsTrue(wrapped.Equals(same));
            Assert.IsTrue(wrapped.Equals(readOnly));
            Assert.IsTrue(wrapped.Equals(source));
            Assert.IsFalse(wrapped.Equals(other));
            Assert.IsFalse(wrapped.Equals(null));
            Assert.AreEqual(source.GetHashCode(), wrapped.GetHashCode());
        }

        [Test]
        public void Add_GrowsAndKeepsAllValues()
        {
            const int N = 200;
            var list = new ListFast<int>(new List<int>(1));
            var first = 0;

            list.Add(in first);

            for (var i = 1; i < N; i++)
            {
                list.Add(i);
            }

            Assert.AreEqual(N, list.Count);

            for (var i = 0; i < N; i++)
            {
                Assert.AreEqual(i, list[i], $"missing {i}");
            }
        }

        [Test]
        public void Indexer_GetsSetsAndRejectsOutOfRangeIndexes()
        {
            var list = NewList(10, 20);

            list[1] = 30;

            Assert.AreEqual(10, list[0]);
            Assert.AreEqual(30, list[1]);
            Assert.Catch(() => { _ = list[-1]; });
            Assert.Catch(() => list[2] = 40);
        }

        [Test]
        public void InsertAndRemoveOperations_UpdateOrder()
        {
            var list = NewList(0, 3, 4);
            var two = 2;

            list.Insert(1, 1);
            list.Insert(2, in two);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, list.ToArray());

            list.RemoveAt(2);
            CollectionAssert.AreEqual(new[] { 0, 1, 3, 4 }, list.ToArray());

            list.RemoveRange(1, 2);
            CollectionAssert.AreEqual(new[] { 0, 4 }, list.ToArray());

            list.AddRange(new[] { 5, 6 });
            list.RemoveAtSwapBack(1);
            CollectionAssert.AreEqual(new[] { 0, 6, 5 }, list.ToArray());
        }

        [Test]
        public void Remove_ValueAndInValue_ReturnExpectedResult()
        {
            var list = NewList(1, 2, 2, 3);
            var two = 2;
            var missing = 9;

            Assert.IsTrue(list.Remove(2));
            Assert.IsTrue(list.Remove(in two));
            Assert.IsFalse(list.Remove(in missing));
            CollectionAssert.AreEqual(new[] { 1, 3 }, list.ToArray());
        }

        [Test]
        public void ElementAt_ReturnsWritableReference()
        {
            var list = NewList(1, 2, 3);

            ref var value = ref list.ElementAt(1);
            value = 20;

            Assert.AreEqual(20, list[1]);
        }

        [Test]
        public void IndexOfAndContains_FindExpectedValues()
        {
            var list = NewList(1, 2, 3, 2, 4);
            var two = 2;
            var missing = 9;

            Assert.AreEqual(1, list.IndexOf(2));
            Assert.AreEqual(3, list.IndexOf(2, 2));
            Assert.AreEqual(3, list.IndexOf(2, 2, 2));
            Assert.AreEqual(-1, list.IndexOf(9));
            Assert.AreEqual(-1, list.IndexOf(2, 4, 1));
            Assert.AreEqual(1, list.IndexOf(in two));
            Assert.AreEqual(3, list.IndexOf(in two, 2));
            Assert.AreEqual(3, list.IndexOf(in two, 2, 2));
            Assert.AreEqual(-1, list.IndexOf(in missing));
            Assert.IsTrue(list.Contains(2));
            Assert.IsTrue(list.Contains(in two));
            Assert.IsFalse(list.Contains(in missing));
        }

        [Test]
        public void FindOperations_SupportValueAndInPredicates()
        {
            var list = NewList(1, 2, 3, 4, 5, 4);
            Predicate<int> isEven = value => value % 2 == 0;
            PredicateIn<int> isEvenIn = (in int value) => value % 2 == 0;

            Assert.IsTrue(list.Exists(isEven));
            Assert.IsTrue(list.Exists(isEvenIn));
            Assert.IsFalse(list.Exists(value => value > 10));
            Assert.IsFalse(list.Exists((in int value) => value > 10));

            var found = list.Find(isEven);
            var foundIn = list.Find(isEvenIn);

            Assert.IsTrue(found.HasValue);
            Assert.AreEqual(2, found.GetValueOrThrow());
            Assert.IsTrue(foundIn.HasValue);
            Assert.AreEqual(2, foundIn.GetValueOrThrow());
            Assert.IsFalse(list.Find(value => value > 10).HasValue);
            Assert.IsFalse(list.Find((in int value) => value > 10).HasValue);

            Assert.AreEqual(1, list.FindIndex(isEven));
            Assert.AreEqual(3, list.FindIndex(2, isEven));
            Assert.AreEqual(3, list.FindIndex(2, 2, isEven));
            Assert.AreEqual(1, list.FindIndex(isEvenIn));
            Assert.AreEqual(3, list.FindIndex(2, isEvenIn));
            Assert.AreEqual(3, list.FindIndex(2, 2, isEvenIn));
            Assert.AreEqual(-1, list.FindIndex(value => value > 10));
            Assert.AreEqual(-1, list.FindIndex(2, value => value == 1));
            Assert.AreEqual(-1, list.FindIndex(0, 4, value => value == 5));
            Assert.AreEqual(-1, list.FindIndex((in int value) => value > 10));
            Assert.AreEqual(-1, list.FindIndex(2, (in int value) => value == 1));
            Assert.AreEqual(-1, list.FindIndex(0, 4, (in int value) => value == 5));

            Assert.AreEqual(5, list.FindLastIndex(isEven));
            Assert.AreEqual(3, list.FindLastIndex(4, isEven));
            Assert.AreEqual(3, list.FindLastIndex(4, 3, isEven));
            Assert.AreEqual(5, list.FindLastIndex(isEvenIn));
            Assert.AreEqual(3, list.FindLastIndex(4, isEvenIn));
            Assert.AreEqual(3, list.FindLastIndex(4, 3, isEvenIn));
            Assert.AreEqual(-1, list.FindLastIndex(value => value > 10));
            Assert.AreEqual(-1, list.FindLastIndex(2, value => value == 4));
            Assert.AreEqual(-1, list.FindLastIndex(4, 3, value => value == 1));
            Assert.AreEqual(-1, list.FindLastIndex((in int value) => value > 10));
            Assert.AreEqual(-1, list.FindLastIndex(2, (in int value) => value == 4));
            Assert.AreEqual(-1, list.FindLastIndex(4, 3, (in int value) => value == 1));
        }

        [Test]
        public void FindAll_ReturnsAndAppendsMatchingValues()
        {
            var list = NewList(1, 2, 3, 4, 5);
            Predicate<int> isOdd = value => value % 2 != 0;
            PredicateIn<int> isOddIn = (in int value) => value % 2 != 0;

            var returned = list.FindAll(isOdd);
            var returnedIn = list.FindAll(isOddIn);
            var valueResult = NewList();
            var inResult = NewList();
            ICollection<int> collectionValueResult = new List<int>();
            ICollection<int> collectionInResult = new List<int>();

            list.FindAll(isOdd, valueResult);
            list.FindAll(isOddIn, inResult);
            list.FindAll(isOdd, collectionValueResult);
            list.FindAll(isOddIn, collectionInResult);

            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, returned.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, returnedIn.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, valueResult.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, inResult.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, collectionValueResult);
            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, collectionInResult);
        }

        [Test]
        public void ForEach_SupportsValueInAndRefActions()
        {
            var list = NewList(1, 2, 3);
            var sum = 0;
            var inSum = 0;
            Action<int> add = value => sum += value;
            ActionIn<int> addIn = (in int value) => inSum += value;
            ActionRef<int> doubleValue = (ref int value) => value *= 2;

            list.ForEach(add);
            list.ForEach(addIn);
            list.ForEach(doubleValue);

            Assert.AreEqual(6, sum);
            Assert.AreEqual(6, inSum);
            CollectionAssert.AreEqual(new[] { 2, 4, 6 }, list.ToArray());
        }

        [Test]
        public void AddRange_AllOverloadsAppendExpectedValues()
        {
            var list = NewList();
            var array = new[] { 1, 2, 3 };
            ReadOnlySpan<int> span = new[] { 4, 5, 6 };
            IEnumerable<int> enumerable = new Queue<int>(new[] { 7, 8 });

            list.AddRange(array, 2);
            list.AddRange(array);
            list.AddRange(span, 2);
            list.AddRange(span);
            list.AddRange(enumerable);

            CollectionAssert.AreEqual(
                new[] { 1, 2, 1, 2, 3, 4, 5, 4, 5, 6, 7, 8 },
                list.ToArray()
            );
        }

        [Test]
        public void CopyFromAndTryCopyFrom_SpanFamiliesCopyOrReturnFalse()
        {
            var list = NewList(0, 0, 0, 0);
            ReadOnlySpan<int> source = new[] { 1, 2, 3, 4 };

            list.CopyFrom(source);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, list.ToArray());

            list.CopyFrom(new[] { 5, 6, 7 }, 2);
            list.CopyFrom(2, new[] { 8, 9 });
            list.CopyFrom(1, new[] { 10, 11, 12 }, 2);
            CollectionAssert.AreEqual(new[] { 5, 10, 11, 9 }, list.ToArray());

            Assert.IsTrue(list.TryCopyFrom(new[] { 1, 2, 3, 4 }));
            Assert.IsTrue(list.TryCopyFrom(new[] { 5, 6, 7 }, 2));
            Assert.IsTrue(list.TryCopyFrom(2, new[] { 8, 9 }));
            Assert.IsTrue(list.TryCopyFrom(1, new[] { 10, 11, 12 }, 2));
            Assert.IsFalse(list.TryCopyFrom(new[] { 1, 2, 3, 4, 5 }));
            Assert.IsFalse(list.TryCopyFrom(3, new[] { 1, 2 }));
        }

        [Test]
        public void CopyToAndTryCopyTo_SpanFamiliesCopyOrReturnFalse()
        {
            var list = NewList(1, 2, 3, 4);
            var destination = new int[4];

            list.CopyTo(destination.AsSpan());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, destination);

            Array.Clear(destination, 0, destination.Length);
            list.CopyTo(destination.AsSpan(), 2);
            list.CopyTo(2, destination.AsSpan(2));
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, destination);

            Array.Clear(destination, 0, destination.Length);
            list.CopyTo(1, destination.AsSpan(), 2);
            CollectionAssert.AreEqual(new[] { 2, 3, 0, 0 }, destination);

            Assert.IsTrue(list.TryCopyTo(destination.AsSpan()));
            Assert.IsTrue(list.TryCopyTo(destination.AsSpan(), 2));
            Assert.IsTrue(list.TryCopyTo(2, destination.AsSpan(0, 2)));
            Assert.IsTrue(list.TryCopyTo(1, destination.AsSpan(), 2));
            Assert.IsFalse(list.TryCopyTo(new int[3].AsSpan(), list.Count));
            Assert.IsFalse(list.TryCopyTo(3, new int[2].AsSpan()));

            var arrayDestination = new int[6];
            list.CopyTo(arrayDestination, 1);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 0 }, arrayDestination);
        }

        [Test]
        public void Clear_ResetsCountAndAllowsReuse()
        {
            var list = NewList(1, 2, 3);

            list.Clear();

            Assert.AreEqual(0, list.Count);
            list.Add(4);
            Assert.AreEqual(4, list[0]);
        }

        [Test]
        public void PeekPopPush_UseStackSemantics()
        {
            var list = NewList(1, 2);
            var three = 3;

            var index = list.Push(in three);
            var peeked = list.Peek();
            var popped = list.Pop();

            Assert.AreEqual(2, index);
            Assert.AreEqual(3, peeked);
            Assert.AreEqual(3, popped);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(2, list.Push(4));
        }

        [Test]
        public void Spans_ReflectListContentAndCount()
        {
            var list = NewList(1, 2, 3);

            var span = list.AsSpan();
            span[1] = 20;
            var readOnlySpan = list.AsReadOnlySpan();

            Assert.AreEqual(list.Count, span.Length);
            Assert.AreEqual(list.Count, readOnlySpan.Length);
            CollectionAssert.AreEqual(new[] { 1, 20, 3 }, readOnlySpan.ToArray());
        }

        [Test]
        public void CapacityOperations_GrowAndTrimToCount()
        {
            var backing = new List<int>(4) { 1, 2 };
            var list = new ListFast<int>(backing);
            var initialCapacity = list.Capacity;

            list.IncreaseCapacityBy(5);
            Assert.GreaterOrEqual(list.Capacity, initialCapacity + 5);

            list.IncreaseCapacityTo(20);
            Assert.GreaterOrEqual(list.Capacity, 20);

            list.Trim();
            Assert.AreEqual(list.Count, list.Capacity);
            CollectionAssert.AreEqual(new[] { 1, 2 }, list.ToArray());
        }

        [Test]
        public void AddReplicate_AllOverloadsReturnAppendedSpan()
        {
            var list = NewList();
            var created = 0;

            var defaults = list.AddReplicate(2);
            var values = list.AddReplicate(7, 3);
            var createdValues = list.AddReplicate(3, () => ++created);
            var noInit = list.AddReplicateNoInit(2);
            noInit.Fill(9);

            Assert.AreEqual(2, defaults.Length);
            CollectionAssert.AreEqual(new[] { 0, 0 }, defaults.ToArray());
            Assert.AreEqual(3, values.Length);
            CollectionAssert.AreEqual(new[] { 7, 7, 7 }, values.ToArray());
            Assert.AreEqual(3, createdValues.Length);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, createdValues.ToArray());
            Assert.AreEqual(2, noInit.Length);
            CollectionAssert.AreEqual(new[] { 0, 0, 7, 7, 7, 1, 2, 3, 9, 9 }, list.ToArray());
        }

        [Test]
        public void Sort_AllOverloadsOrderExpectedRanges()
        {
            var defaultSort = NewList(4, 1, 3, 2);
            var comparerSort = NewList(1, 3, 2, 4);
            var rangeSort = NewList(9, 4, 2, 3, 1, 8);
            var comparisonSort = NewList(1, 4, 2, 3);

            defaultSort.Sort();
            comparerSort.Sort(Comparer<int>.Create((left, right) => right.CompareTo(left)));
            rangeSort.Sort(1, 4, Comparer<int>.Default);
            comparisonSort.Sort((left, right) => right.CompareTo(left));

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, defaultSort.ToArray());
            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, comparerSort.ToArray());
            CollectionAssert.AreEqual(new[] { 9, 1, 2, 3, 4, 8 }, rangeSort.ToArray());
            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, comparisonSort.ToArray());
        }

        [Test]
        public void Enumerator_VisitsInOrder()
        {
            var list = NewList(1, 2, 3);
            var visited = new List<int>();

            foreach (var value in list)
            {
                visited.Add(value);
            }

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, visited);
        }

        [Test]
        public void Enumerator_MutationDuringIteration_Throws()
        {
            var list = NewList(1, 2, 3);

            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var value in list)
                {
                    list.Add(value);
                }
            });
        }

        [Test]
        public void Enumerator_DirectMembersMoveResetAndDispose()
        {
            var list = NewList(1, 2);
            ListFastEnumerator<int> enumerator = list.GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current);

            enumerator.Reset();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(2, enumerator.Current);
            Assert.IsFalse(enumerator.MoveNext());

            enumerator.Dispose();
        }

        [Test]
        public void ReadOnly_ConstructorsPropertiesConversionsEqualityAndCopiesReflectOwner()
        {
            var backing = new List<int> { 1, 2, 3, 4 };
            var list = new ListFast<int>(backing);
            var defaultConstructed = new ListFast<int>.ReadOnly();
            var constructed = new ListFast<int>.ReadOnly(list);
            var fromMethod = list.AsReadOnly();
            ListFast<int>.ReadOnly convertedFromOwner = list;
            ListFast<int>.ReadOnly convertedFromList = backing;
            ReadOnlySpan<int> span = convertedFromOwner;
            var arrayDestination = new int[6];
            var destination = new int[4];

            constructed.CopyTo(arrayDestination, 1);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 0 }, arrayDestination);

            constructed.CopyTo(destination.AsSpan());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, destination);

            Array.Clear(destination, 0, destination.Length);
            constructed.CopyTo(destination.AsSpan(), 2);
            constructed.CopyTo(2, destination.AsSpan(2));
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, destination);

            Array.Clear(destination, 0, destination.Length);
            constructed.CopyTo(1, destination.AsSpan(), 2);
            CollectionAssert.AreEqual(new[] { 2, 3, 0, 0 }, destination);

            Assert.IsTrue(constructed.TryCopyTo(destination.AsSpan()));
            Assert.IsTrue(constructed.TryCopyTo(destination.AsSpan(), 2));
            Assert.IsTrue(constructed.TryCopyTo(2, destination.AsSpan(0, 2)));
            Assert.IsTrue(constructed.TryCopyTo(1, destination.AsSpan(), 2));
            Assert.IsFalse(constructed.TryCopyTo(new int[3].AsSpan(), constructed.Count));
            Assert.IsFalse(constructed.TryCopyTo(3, new int[2].AsSpan()));
            Assert.IsTrue(constructed.IsCreated);
            Assert.IsTrue(constructed.IsReadOnly);
            Assert.AreEqual(4, constructed.Count);
            Assert.GreaterOrEqual(constructed.Capacity, constructed.Count);
            Assert.AreEqual(2, constructed[1]);
            Assert.AreEqual(4, fromMethod.AsReadOnlySpan().Length);
            Assert.AreEqual(0, defaultConstructed.Count);
            Assert.AreEqual(0, ListFast<int>.ReadOnly.Empty.Count);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, span.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, constructed.ToArray());
            Assert.IsTrue(constructed.Equals(convertedFromOwner));
            Assert.IsTrue(constructed.Equals(list));
            Assert.IsTrue(constructed.Equals(backing));
            Assert.IsFalse(constructed.Equals(NewList(1, 2, 3, 4)));
            Assert.AreEqual(convertedFromOwner.GetHashCode(), constructed.GetHashCode());

            list[1] = 20;

            Assert.AreEqual(20, convertedFromList[1]);
        }

        [Test]
        public void ReadOnly_SearchAndActionsCallEveryOverload()
        {
            var list = NewList(1, 2, 3, 2, 4);
            var readOnly = list.AsReadOnly();
            var two = 2;
            var missing = 9;
            var sum = 0;
            var inSum = 0;
            Predicate<int> isEven = value => value % 2 == 0;
            PredicateIn<int> isEvenIn = (in int value) => value % 2 == 0;
            Action<int> add = value => sum += value;
            ActionIn<int> addIn = (in int value) => inSum += value;
            ActionRef<int> doubleValue = (ref int value) => value *= 2;

            Assert.IsTrue(readOnly.Contains(2));
            Assert.IsTrue(readOnly.Contains(in two));
            Assert.IsFalse(readOnly.Contains(in missing));
            Assert.IsTrue(readOnly.Exists(isEven));
            Assert.IsTrue(readOnly.Exists(isEvenIn));
            Assert.IsFalse(readOnly.Exists(value => value > 10));
            Assert.IsFalse(readOnly.Exists((in int value) => value > 10));
            Assert.AreEqual(2, readOnly.Find(isEven).GetValueOrThrow());
            Assert.AreEqual(2, readOnly.Find(isEvenIn).GetValueOrThrow());
            Assert.IsFalse(readOnly.Find(value => value > 10).HasValue);
            Assert.IsFalse(readOnly.Find((in int value) => value > 10).HasValue);
            Assert.AreEqual(1, readOnly.FindIndex(isEven));
            Assert.AreEqual(3, readOnly.FindIndex(2, isEven));
            Assert.AreEqual(3, readOnly.FindIndex(2, 2, isEven));
            Assert.AreEqual(1, readOnly.FindIndex(isEvenIn));
            Assert.AreEqual(3, readOnly.FindIndex(2, isEvenIn));
            Assert.AreEqual(3, readOnly.FindIndex(2, 2, isEvenIn));
            Assert.AreEqual(-1, readOnly.FindIndex(value => value > 10));
            Assert.AreEqual(-1, readOnly.FindIndex(1, value => value == 1));
            Assert.AreEqual(-1, readOnly.FindIndex(0, 4, value => value == 4));
            Assert.AreEqual(-1, readOnly.FindIndex((in int value) => value > 10));
            Assert.AreEqual(-1, readOnly.FindIndex(1, (in int value) => value == 1));
            Assert.AreEqual(-1, readOnly.FindIndex(0, 4, (in int value) => value == 4));
            Assert.AreEqual(4, readOnly.FindLastIndex(isEven));
            Assert.AreEqual(3, readOnly.FindLastIndex(3, isEven));
            Assert.AreEqual(3, readOnly.FindLastIndex(3, 2, isEven));
            Assert.AreEqual(4, readOnly.FindLastIndex(isEvenIn));
            Assert.AreEqual(3, readOnly.FindLastIndex(3, isEvenIn));
            Assert.AreEqual(3, readOnly.FindLastIndex(3, 2, isEvenIn));
            Assert.AreEqual(-1, readOnly.FindLastIndex(value => value > 10));
            Assert.AreEqual(-1, readOnly.FindLastIndex(2, value => value == 4));
            Assert.AreEqual(-1, readOnly.FindLastIndex(3, 2, value => value == 1));
            Assert.AreEqual(-1, readOnly.FindLastIndex((in int value) => value > 10));
            Assert.AreEqual(-1, readOnly.FindLastIndex(2, (in int value) => value == 4));
            Assert.AreEqual(-1, readOnly.FindLastIndex(3, 2, (in int value) => value == 1));
            Assert.AreEqual(1, readOnly.IndexOf(2));
            Assert.AreEqual(3, readOnly.IndexOf(2, 2));
            Assert.AreEqual(3, readOnly.IndexOf(2, 2, 2));
            Assert.AreEqual(1, readOnly.IndexOf(in two));
            Assert.AreEqual(3, readOnly.IndexOf(in two, 2));
            Assert.AreEqual(3, readOnly.IndexOf(in two, 2, 2));
            Assert.AreEqual(-1, readOnly.IndexOf(9));
            Assert.AreEqual(-1, readOnly.IndexOf(2, 4));
            Assert.AreEqual(-1, readOnly.IndexOf(2, 0, 1));
            Assert.AreEqual(-1, readOnly.IndexOf(in missing));
            Assert.AreEqual(-1, readOnly.IndexOf(in two, 4));
            Assert.AreEqual(-1, readOnly.IndexOf(in two, 0, 1));

            readOnly.ForEach(add);
            readOnly.ForEach(addIn);
            readOnly.ForEach(doubleValue);

            Assert.AreEqual(12, sum);
            Assert.AreEqual(12, inSum);
            CollectionAssert.AreEqual(new[] { 2, 4, 6, 4, 8 }, list.ToArray());
        }

        [Test]
        public void ReadOnly_FindAllCallsEveryResultOverload()
        {
            var readOnly = NewList(1, 2, 3, 4, 5).AsReadOnly();
            Predicate<int> isOdd = value => value % 2 != 0;
            PredicateIn<int> isOddIn = (in int value) => value % 2 != 0;
            var returned = readOnly.FindAll(isOdd);
            var returnedIn = readOnly.FindAll(isOddIn);
            var valueResult = NewList();
            var inResult = NewList();
            ICollection<int> collectionValueResult = new List<int>();
            ICollection<int> collectionInResult = new List<int>();

            readOnly.FindAll(isOdd, valueResult);
            readOnly.FindAll(isOddIn, inResult);
            readOnly.FindAll(isOdd, collectionValueResult);
            readOnly.FindAll(isOddIn, collectionInResult);

            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, returned.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, returnedIn.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, valueResult.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, inResult.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, collectionValueResult);
            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, collectionInResult);
        }

        [Test]
        public void ReadOnly_EnumeratorDirectMembersMoveResetAndDispose()
        {
            var readOnly = NewList(1, 2).AsReadOnly();
            var enumerator = readOnly.GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current);

            enumerator.Reset();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(2, enumerator.Current);
            Assert.IsFalse(enumerator.MoveNext());

            enumerator.Dispose();
        }

        [Test]
        public void Prefill_AllFactoriesCreateExpectedContent()
        {
            var created = 0;
            var fromFactory = ListFast<int>.Prefill(3, () => ++created);
            var defaults = ListFast<int>.Prefill(3);
            var values = ListFast<int>.Prefill(5, 3);
            var noInit = ListFast<int>.PrefillNoInit(3);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, fromFactory.ToArray());
            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, defaults.ToArray());
            CollectionAssert.AreEqual(new[] { 5, 5, 5 }, values.ToArray());
            Assert.AreEqual(3, noInit.Count);
        }

        [Test]
        public void RemoveRange_ZeroLengthBoundaries_PinContract()
        {
            // Pins the family-wide contract: RemoveRange validates startIndex < Count before
            // looking at length, so a zero-length remove at Count (including on an empty list)
            // throws, unlike BCL List<T>.RemoveRange. Zero-length removes at a valid index
            // are no-ops.
            var empty = new ListFast<int>();

            Assert.Throws<InvalidOperationException>(() => empty.RemoveRange(0, 0));

            var list = new ListFast<int>(new List<int> { 1, 2, 3 });

            Assert.Throws<InvalidOperationException>(() => list.RemoveRange(3, 0));

            list.RemoveRange(0, 0);
            list.RemoveRange(2, 0);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list.ToArray());
        }
    }
}

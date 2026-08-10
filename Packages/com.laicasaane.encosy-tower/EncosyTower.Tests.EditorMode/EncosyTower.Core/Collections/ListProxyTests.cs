// Adapted from Unity.Collections.Tests/NativeListTests.cs and FixedListTests.gen.cs.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using EncosyTower.Collections;
using NUnit.Framework;

namespace EncosyTower.Tests.Core.Collections
{
    public class BufferProvider<T> : IBufferProvider<BufferManaged<T>, T>
    {
        private BufferManaged<T> _buffer = new(4);
        private int _count;
        private int _version;

        public ref BufferManaged<T> Buffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _buffer;
        }

        public ref int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _count;
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Capacity;
        }

        public ref int Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _version;
        }
    }

    public partial class ListProxyTests
    {
        [Test]
        public void Constructors_UseProviderAndGrowRequestedCapacity()
        {
            var provider = new BufferProvider<int>();
            var list = new ListProxy<BufferProvider<int>, BufferManaged<int>, int>(provider);
            var grownProvider = new BufferProvider<int>();
            var grown = new ListProxy<BufferProvider<int>, BufferManaged<int>, int>(grownProvider, 12);

            Assert.IsTrue(list.IsCreated);
            Assert.IsFalse(list.IsReadOnly);
            Assert.AreSame(provider, list.Provider);
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(4, list.Capacity);
            Assert.AreEqual(0, grown.Count);
            Assert.GreaterOrEqual(grown.Capacity, 12);
            Assert.AreEqual(grown.Capacity, grownProvider.Capacity);
        }

        [Test]
        public void Add_GrowsPastProviderCapacityAndKeepsValues()
        {
            const int N = 200;
            var list = NewList();

            for (var i = 0; i < N; i++)
            {
                list.Add(i);
            }

            Assert.AreEqual(N, list.Count);
            Assert.GreaterOrEqual(list.Capacity, N);

            for (var i = 0; i < N; i++)
            {
                Assert.AreEqual(i, list[i], $"missing {i}");
            }
        }

        [Test]
        public void IndexerAndElementAt_ReadAndWriteProviderBuffer()
        {
            var list = NewList();
            list.AddRange(new[] { 1, 2, 3 });

            list[1] = 20;
            ref var value = ref list.ElementAt(2);
            value = 30;

            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(20, list[1]);
            Assert.AreEqual(30, list[2]);
            Assert.Catch(() => { _ = list[3]; });
        }

        [Test]
        public void Insert_ShiftsValuesRight()
        {
            var list = NewList();
            list.AddRange(new[] { 0, 2, 3 });

            list.Insert(1, 1);

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, list.ToArray());
        }

        [Test]
        public void AddAndInsert_InOverloadsAppendAndShiftValues()
        {
            var list = NewList();
            var one = 1;
            var two = 2;

            list.Add(in two);
            list.Insert(0, in one);

            CollectionAssert.AreEqual(new[] { 1, 2 }, list.ToArray());
        }

        [Test]
        public void AddRange_ArraySpanAndEnumerable_AppendExpectedValues()
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
        public void Clear_ResetsCountAndAllowsReuse()
        {
            var list = NewList();
            list.AddRange(new[] { 1, 2, 3 });

            list.Clear();

            Assert.AreEqual(0, list.Count);
            list.Add(4);
            Assert.AreEqual(4, list[0]);
        }

        [Test]
        public void CopyFromAndTryCopyFrom_SpanFamiliesCopyOrReturnFalse()
        {
            var list = NewList();
            list.AddReplicate(4);
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
            var list = NewList();
            list.AddRange(new[] { 1, 2, 3, 4 });
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
        public void PeekPopPush_UseStackSemantics()
        {
            var list = NewList();
            list.AddRange(new[] { 1, 2 });
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
        public void RemoveRange_ShiftsRemainingValuesLeft()
        {
            var list = NewList();
            list.AddRange(new[] { 0, 1, 2, 3, 4 });

            list.RemoveRange(1, 2);

            CollectionAssert.AreEqual(new[] { 0, 3, 4 }, list.ToArray());
        }

        [Test]
        public void RemoveAtSwapBack_MovesLastValueIntoHole()
        {
            var list = NewList();
            list.AddRange(new[] { 0, 1, 2, 3 });

            list.RemoveAtSwapBack(1);

            CollectionAssert.AreEqual(new[] { 0, 3, 2 }, list.ToArray());
        }

        [Test]
        public void ToArrayAndSpans_ReflectLiveContent()
        {
            var list = NewList();
            list.AddRange(new[] { 1, 2, 3 });

            var span = list.AsSpan();
            span[1] = 20;
            var readOnlySpan = list.AsReadOnlySpan();

            Assert.AreEqual(list.Count, span.Length);
            Assert.AreEqual(list.Count, readOnlySpan.Length);
            CollectionAssert.AreEqual(new[] { 1, 20, 3 }, list.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 20, 3 }, readOnlySpan.ToArray());
        }

        [Test]
        public void CapacityOperations_GrowAndTrimToCount()
        {
            var list = NewList();
            list.AddRange(new[] { 1, 2 });
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

            var defaults = list.AddReplicate(2);
            CollectionAssert.AreEqual(new[] { 0, 0 }, defaults.ToArray());

            var values = list.AddReplicate(7, 3);
            CollectionAssert.AreEqual(new[] { 7, 7, 7 }, values.ToArray());

            var noInit = list.AddReplicateNoInit(2);
            noInit.Fill(9);

            Assert.AreEqual(2, defaults.Length);
            Assert.AreEqual(3, values.Length);
            Assert.AreEqual(2, noInit.Length);
            CollectionAssert.AreEqual(new[] { 0, 0, 7, 7, 7, 9, 9 }, list.ToArray());
        }

        [Test]
        public void Enumerator_VisitsValuesInOrder()
        {
            var list = NewList();
            list.AddRange(new[] { 1, 2, 3 });
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
            var list = NewList();
            list.AddRange(new[] { 1, 2, 3 });

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
            var list = NewList();
            list.AddRange(new[] { 1, 2 });
            BufferProviderEnumerator<BufferProvider<int>, BufferManaged<int>, int> enumerator
                = list.GetEnumerator();

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
        public void ReadOnly_ConstructorPropertiesConversionViewsAndCopiesReflectOwner()
        {
            var list = NewList();
            list.AddRange(new[] { 1, 2, 3, 4 });
            var constructed = new ListProxy<BufferProvider<int>, BufferManaged<int>, int>.ReadOnly(list);
            var fromMethod = list.AsReadOnly();
            ListProxy<BufferProvider<int>, BufferManaged<int>, int>.ReadOnly converted = list;
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
            Assert.AreEqual(4, constructed.Count);
            Assert.GreaterOrEqual(constructed.Capacity, constructed.Count);
            Assert.AreEqual(2, constructed[1]);
            Assert.AreEqual(4, fromMethod.AsReadOnlySpan().Length);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, converted.ToArray());

            list[1] = 20;

            Assert.AreEqual(20, constructed[1]);
        }

        [Test]
        public void ReadOnly_EnumeratorDirectMembersMoveResetAndDispose()
        {
            var list = NewList();
            list.AddRange(new[] { 1, 2 });
            var readOnly = list.AsReadOnly();
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
        public void Prefill_FactoriesCreateExpectedContent()
        {
            var defaultProvider = new BufferProvider<int>();
            var defaults = ListProxy<BufferProvider<int>, BufferManaged<int>, int>
                .Prefill(defaultProvider, 3);
            var valueProvider = new BufferProvider<int>();
            var values = ListProxy<BufferProvider<int>, BufferManaged<int>, int>
                .Prefill(valueProvider, 5, 3);
            var noInitProvider = new BufferProvider<int>();
            var noInit = ListProxy<BufferProvider<int>, BufferManaged<int>, int>
                .PrefillNoInit(noInitProvider, 3);

            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, defaults.ToArray());
            CollectionAssert.AreEqual(new[] { 5, 5, 5 }, values.ToArray());
            Assert.AreEqual(3, noInit.Count);
        }

        [Test]
        public void SharedProvider_ProxiesObserveSameCountValuesAndVersion()
        {
            var provider = new BufferProvider<int>();
            var first = new ListProxy<BufferProvider<int>, BufferManaged<int>, int>(provider);
            var second = new ListProxy<BufferProvider<int>, BufferManaged<int>, int>(provider);

            first.Add(1);
            first.Add(2);
            var enumerator = first.GetEnumerator();

            Assert.AreEqual(2, second.Count);
            CollectionAssert.AreEqual(new[] { 1, 2 }, second.ToArray());
            Assert.IsTrue(enumerator.MoveNext());

            second[0] = 10;

            Assert.AreEqual(10, first[0]);
            Assert.AreEqual(provider.Version, first.Provider.Version);
            Assert.AreEqual(provider.Version, second.Provider.Version);
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Test]
        public void RemoveRange_ZeroLengthBoundaries_PinContract()
        {
            // Pins the family-wide contract: RemoveRange validates startIndex < Count before
            // looking at length, so a zero-length remove at Count (including on an empty list)
            // throws, unlike BCL List<T>.RemoveRange. Zero-length removes at a valid index
            // are no-ops.
            var empty = NewList();

            Assert.Throws<InvalidOperationException>(() => empty.RemoveRange(0, 0));

            var list = NewList();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            Assert.Throws<InvalidOperationException>(() => list.RemoveRange(3, 0));

            list.RemoveRange(0, 0);
            list.RemoveRange(2, 0);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list.ToArray());
        }

        private static ListProxy<BufferProvider<int>, BufferManaged<int>, int> NewList()
            => new(new BufferProvider<int>());
    }
}

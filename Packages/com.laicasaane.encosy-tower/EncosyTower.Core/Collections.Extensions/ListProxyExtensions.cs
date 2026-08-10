using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections.Extensions
{
    public static class ListProxyExtensions
    {
        public static bool Contains<TProvider, TBuffer, T>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , T item
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where T : IEquatable<T>
        {
            var items = self.AsReadOnlySpan();
            var length = items.Length;

            for (var index = 0; index < length; index++)
            {
                ref readonly var item2 = ref items[index];

                if (item.Equals(item2))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool Contains<TProvider, TBuffer, T>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , in T item
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where T : IEquatable<T>
        {
            var items = self.AsReadOnlySpan();
            var length = items.Length;

            for (var index = 0; index < length; index++)
            {
                ref readonly var item2 = ref items[index];

                if (item.Equals(item2))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool Contains<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , T item
            , TComparer comparer
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where TComparer : IEqualityComparer<T>
        {
            var items = self.AsReadOnlySpan();
            var length = items.Length;

            for (var index = 0; index < length; index++)
            {
                ref readonly var item2 = ref items[index];

                if (comparer.Equals(item, item2))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool Contains<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , in T item
            , TComparer comparer
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where TComparer : IEqualityComparer<T>
        {
            var items = self.AsReadOnlySpan();
            var length = items.Length;

            for (var index = 0; index < length; index++)
            {
                ref readonly var item2 = ref items[index];

                if (comparer.Equals(item, item2))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool Remove<TProvider, TBuffer, T>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , T item
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where T : IEquatable<T>
        {
            self._version++;

            var index = IndexOf(ref self, item);

            if ((uint)index >= (uint)self._count)
            {
                return false;
            }

            if (index < --self._count)
            {
                self.CopyBuffer(index + 1, index, self._count - index);
            }

            var shouldClear = false;
            ListProxy<TProvider, TBuffer, T>.ShouldClear(ref shouldClear);

            if (shouldClear)
            {
                self.ClearBuffer(self._count, 1);
            }

            return true;
        }

        public static bool Remove<TProvider, TBuffer, T>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , in T item
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where T : IEquatable<T>
        {
            self._version++;

            var index = IndexOf(ref self, in item);

            if ((uint)index >= (uint)self._count)
            {
                return false;
            }

            if (index < --self._count)
            {
                self.CopyBuffer(index + 1, index, self._count - index);
            }

            var shouldClear = false;
            ListProxy<TProvider, TBuffer, T>.ShouldClear(ref shouldClear);

            if (shouldClear)
            {
                self.ClearBuffer(self._count, 1);
            }

            return true;
        }

        public static bool Remove<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , T item
            , TComparer comparer
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where TComparer : IEqualityComparer<T>
        {
            self._version++;

            var index = IndexOf(ref self, item, comparer);

            if ((uint)index >= (uint)self._count)
            {
                return false;
            }

            if (index < --self._count)
            {
                self.CopyBuffer(index + 1, index, self._count - index);
            }

            var shouldClear = false;
            ListProxy<TProvider, TBuffer, T>.ShouldClear(ref shouldClear);

            if (shouldClear)
            {
                self.ClearBuffer(self._count, 1);
            }

            return true;
        }

        public static bool Remove<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , in T item
            , TComparer comparer
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where TComparer : IEqualityComparer<T>
        {
            self._version++;

            var index = IndexOf(ref self, in item, comparer);

            if ((uint)index >= (uint)self._count)
            {
                return false;
            }

            if (index < --self._count)
            {
                self.CopyBuffer(index + 1, index, self._count - index);
            }

            var shouldClear = false;
            ListProxy<TProvider, TBuffer, T>.ShouldClear(ref shouldClear);

            if (shouldClear)
            {
                self.ClearBuffer(self._count, 1);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , T item
            , TComparer comparer
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where TComparer : IComparer<T>
        {
            return BinarySearch(ref self, 0, self._count, item, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , int index
            , int count
            , T item
            , TComparer comparer
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where TComparer : IComparer<T>
        {
            ThrowIfIndexIsNonNegative(index >= 0);
            ThrowIfCountIsNonNegative(count >= 0);
            ThrowIfRangeIsWithinList(self._count - index >= count);

            var result = MemoryExtensions.BinarySearch(
                  self.AsReadOnlySpan().Slice(index, count)
                , item
                , comparer
            );
            return result < 0 ? result : result + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , in T item
            , TComparer comparer
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where TComparer : IComparer<T>
        {
            return BinarySearch(ref self, 0, self._count, in item, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , int index
            , int count
            , in T item
            , TComparer comparer
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where TComparer : IComparer<T>
        {
            ThrowIfIndexIsNonNegative(index >= 0);
            ThrowIfCountIsNonNegative(count >= 0);
            ThrowIfRangeIsWithinList(self._count - index >= count);

            var result = MemoryExtensions.BinarySearch(
                  self.AsReadOnlySpan().Slice(index, count)
                , item
                , comparer
            );
            return result < 0 ? result : result + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<TProvider, TBuffer, T>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , T item
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where T : IEquatable<T>
        {
            return IndexOf(ref self, item, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<TProvider, TBuffer, T>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , T item
            , int index
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where T : IEquatable<T>
        {
            return IndexOf(ref self, item, index, self._count - index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<TProvider, TBuffer, T>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , T item
            , int index
            , int count
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where T : IEquatable<T>
        {
            ThrowIfIndexIsNonNegative(index >= 0);
            ThrowIfCountIsNonNegative(count >= 0);
            ThrowIfSectionIsWithinList(index + count <= self._count);

            var result = MemoryExtensions.IndexOf(self.AsReadOnlySpan().Slice(index, count), item);
            return result < 0 ? result : result + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<TProvider, TBuffer, T>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , in T item
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where T : IEquatable<T>
        {
            return IndexOf(ref self, in item, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<TProvider, TBuffer, T>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , in T item
            , int index
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where T : IEquatable<T>
        {
            return IndexOf(ref self, in item, index, self._count - index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<TProvider, TBuffer, T>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , in T item
            , int index
            , int count
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where T : IEquatable<T>
        {
            ThrowIfIndexIsNonNegative(index >= 0);
            ThrowIfCountIsNonNegative(count >= 0);
            ThrowIfSectionIsWithinList(index + count <= self._count);

            var result = MemoryExtensions.IndexOf(self.AsReadOnlySpan().Slice(index, count), item);
            return result < 0 ? result : result + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , T item
            , TComparer comparer
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where TComparer : IEqualityComparer<T>
        {
            return EncosyMemoryExtensions.IndexOf(self.AsReadOnlySpan(), item, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , in T item
            , TComparer comparer
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where TComparer : IEqualityComparer<T>
        {
            return EncosyMemoryExtensions.IndexOf(self.AsReadOnlySpan(), in item, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , TComparer comparer
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where TComparer : IComparer<T>
        {
            Sort(ref self, 0, self._count, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T> self
            , int index
            , int count
            , TComparer comparer
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where TComparer : IComparer<T>
        {
            ThrowIfIndexIsNonNegative(index >= 0);
            ThrowIfCountIsNonNegative(count >= 0);
            ThrowIfOffsetLengthIsValid(self._count - index >= count);

            self._version++;
            ArraySortHelper<T, TComparer>.Sort(self.AsSpan().Slice(index, count), comparer);
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfIndexIsNonNegative([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Index must be non-negative.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfCountIsNonNegative([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Count must be non-negative.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfRangeIsWithinList([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Index and count do not denote a valid range in ListProxy<TProvider, TBuffer, T>.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfSectionIsWithinList([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Index and count do not specify a valid section in ListProxy<TProvider, TBuffer, T>.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfOffsetLengthIsValid([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Offset and length do not specify a valid range.");
        }
    }
}

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
    public static class ListProxyReadOnlyExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<TProvider, TBuffer, T>(
              this ref ListProxy<TProvider, TBuffer, T>.ReadOnly self
            , T item
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where T : IEquatable<T>
        {
            var items = self.AsReadOnlySpan();
            var length = items.Length;
            return length > 0 && MemoryExtensions.IndexOf(items, item) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<TProvider, TBuffer, T>(
              this ref ListProxy<TProvider, TBuffer, T>.ReadOnly self
            , in T item
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where T : IEquatable<T>
        {
            var items = self.AsReadOnlySpan();
            var length = items.Length;
            return length > 0 && MemoryExtensions.IndexOf(items, item) >= 0;
        }

        public static bool Contains<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T>.ReadOnly self
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
              this ref ListProxy<TProvider, TBuffer, T>.ReadOnly self
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T>.ReadOnly self
            , T item
            , TComparer comparer
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where TComparer : IComparer<T>
        {
            return BinarySearch(ref self, 0, self.Count, item, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T>.ReadOnly self
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
            ThrowIfRangeIsWithinList(self.Count - index >= count);

            var result = MemoryExtensions.BinarySearch(
                  self.AsReadOnlySpan().Slice(index, count)
                , item
                , comparer
            );
            return result < 0 ? result : result + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T>.ReadOnly self
            , in T item
            , TComparer comparer
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where TComparer : IComparer<T>
        {
            return BinarySearch(ref self, 0, self.Count, in item, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T>.ReadOnly self
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
            ThrowIfRangeIsWithinList(self.Count - index >= count);

            var result = MemoryExtensions.BinarySearch(
                  self.AsReadOnlySpan().Slice(index, count)
                , item
                , comparer
            );
            return result < 0 ? result : result + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<TProvider, TBuffer, T>(
              this ref ListProxy<TProvider, TBuffer, T>.ReadOnly self
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
              this ref ListProxy<TProvider, TBuffer, T>.ReadOnly self
            , T item
            , int index
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where T : IEquatable<T>
        {
            return IndexOf(ref self, item, index, self.Count - index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<TProvider, TBuffer, T>(
              this ref ListProxy<TProvider, TBuffer, T>.ReadOnly self
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
            ThrowIfSectionIsWithinList(index + count <= self.Count);

            var result = MemoryExtensions.IndexOf(self.AsReadOnlySpan().Slice(index, count), item);
            return result < 0 ? result : result + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<TProvider, TBuffer, T>(
              this ref ListProxy<TProvider, TBuffer, T>.ReadOnly self
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
              this ref ListProxy<TProvider, TBuffer, T>.ReadOnly self
            , in T item
            , int index
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where T : IEquatable<T>
        {
            return IndexOf(ref self, in item, index, self.Count - index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<TProvider, TBuffer, T>(
              this ref ListProxy<TProvider, TBuffer, T>.ReadOnly self
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
            ThrowIfSectionIsWithinList(index + count <= self.Count);

            var result = MemoryExtensions.IndexOf(self.AsReadOnlySpan().Slice(index, count), item);
            return result < 0 ? result : result + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<TProvider, TBuffer, T, TComparer>(
              this ref ListProxy<TProvider, TBuffer, T>.ReadOnly self
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
              this ref ListProxy<TProvider, TBuffer, T>.ReadOnly self
            , in T item
            , TComparer comparer
        )
            where TProvider : IBufferProvider<TBuffer, T>
            where TBuffer : IBuffer<T>
            where TComparer : IEqualityComparer<T>
        {
            return EncosyMemoryExtensions.IndexOf(self.AsReadOnlySpan(), in item, comparer);
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
                => new("Index is less than 0.");
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
                => new("Count is less than 0.");
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
                => new("Index and count do not denote a valid range in the ListProxy<TProvider, TBuffer, T>.ReadOnly.");
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
                => new(
                    "Index and count do not specify a valid section in the "
                    + "ListProxy<TProvider, TBuffer, T>.ReadOnly."
                );
        }
    }
}

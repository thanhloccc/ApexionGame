using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections.Extensions
{
    public static class SharedListNativeReadOnlyExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(this in SharedListNative<T>.ReadOnly self, T item)
            where T : unmanaged, IEquatable<T>
        {
            var items = self.AsReadOnlySpan();
            var length = items.Length;
            return length > 0 && MemoryExtensions.IndexOf(items, item) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(this in SharedListNative<T>.ReadOnly self, in T item)
            where T : unmanaged, IEquatable<T>
        {
            var items = self.AsReadOnlySpan();
            var length = items.Length;
            return length > 0 && MemoryExtensions.IndexOf(items, item) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T, TComparer>(
              this in SharedListNative<T>.ReadOnly self
            , T item
            , TComparer comparer
        )
            where T : unmanaged
            where TComparer : unmanaged, IComparer<T>
        {
            return BinarySearch(self, 0, self.Count, item, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T, TComparer>(
              this in SharedListNative<T>.ReadOnly self
            , int index
            , int count
            , T item
            , TComparer comparer
        )
            where T : unmanaged
            where TComparer : unmanaged, IComparer<T>
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
        public static int BinarySearch<T, TComparer>(
              this in SharedListNative<T>.ReadOnly self
            , in T item
            , TComparer comparer
        )
            where T : unmanaged
            where TComparer : unmanaged, IComparer<T>
        {
            return BinarySearch(self, 0, self.Count, in item, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T, TComparer>(
              this in SharedListNative<T>.ReadOnly self
            , int index
            , int count
            , in T item
            , TComparer comparer
        )
            where T : unmanaged
            where TComparer : unmanaged, IComparer<T>
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
        public static int IndexOf<T>(this in SharedListNative<T>.ReadOnly self, T item)
            where T : unmanaged, IEquatable<T>
                => IndexOf(self, item, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this in SharedListNative<T>.ReadOnly self, T item, int index)
            where T : unmanaged, IEquatable<T>
                => IndexOf(self, item, index, self.Count - index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this in SharedListNative<T>.ReadOnly self, T item, int index, int count)
            where T : unmanaged, IEquatable<T>
        {
            ThrowIfIndexIsNonNegative(index >= 0);
            ThrowIfCountIsNonNegative(count >= 0);
            ThrowIfSectionIsWithinList(index + count <= self.Count);

            var result = MemoryExtensions.IndexOf(self.AsReadOnlySpan().Slice(index, count), item);
            return result < 0 ? result : result + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this in SharedListNative<T>.ReadOnly self, in T item)
            where T : unmanaged, IEquatable<T>
                => IndexOf(self, in item, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this in SharedListNative<T>.ReadOnly self, in T item, int index)
            where T : unmanaged, IEquatable<T>
                => IndexOf(self, in item, index, self.Count - index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this in SharedListNative<T>.ReadOnly self, in T item, int index, int count)
            where T : unmanaged, IEquatable<T>
        {
            ThrowIfIndexIsNonNegative(index >= 0);
            ThrowIfCountIsNonNegative(count >= 0);
            ThrowIfSectionIsWithinList(index + count <= self.Count);

            var result = MemoryExtensions.IndexOf(self.AsReadOnlySpan().Slice(index, count), item);
            return result < 0 ? result : result + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T, TComparer>(this in SharedListNative<T>.ReadOnly self, T item, TComparer comparer)
            where T : unmanaged
            where TComparer : unmanaged, IEqualityComparer<T>
                => EncosyMemoryExtensions.IndexOf(self.AsReadOnlySpan(), item, comparer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T, TComparer>(
              this in SharedListNative<T>.ReadOnly self
            , in T item
            , TComparer comparer
        )
            where T : unmanaged
            where TComparer : unmanaged, IEqualityComparer<T>
                => EncosyMemoryExtensions.IndexOf(self.AsReadOnlySpan(), in item, comparer);

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
                => new("Index and count do not denote a valid range in SharedListNative<T>.ReadOnly.");
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
                => new("Index and count do not specify a valid section in SharedListNative<T>.ReadOnly.");
        }
    }
}

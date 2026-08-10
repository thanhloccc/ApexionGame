using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections.Extensions
{
    public static class SharedListNativeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>([NotNull] this in SharedListNative<T> self, T item)
            where T : unmanaged, IEquatable<T>
        {
            var items = self.AsReadOnlySpan();
            var length = items.Length;
            return length > 0 && MemoryExtensions.IndexOf(items, item) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>([NotNull] this in SharedListNative<T> self, in T item)
            where T : unmanaged, IEquatable<T>
        {
            var items = self.AsReadOnlySpan();
            var length = items.Length;
            return length > 0 && MemoryExtensions.IndexOf(items, item) >= 0;
        }

        public static bool Remove<T>([NotNull] this in SharedListNative<T> self, T item)
            where T : unmanaged, IEquatable<T>
        {
            var index = IndexOf(self, item);

            if (index < 0)
            {
                return false;
            }

            self.RemoveAt(index);
            return true;
        }

        public static bool Remove<T>([NotNull] this in SharedListNative<T> self, in T item)
            where T : unmanaged, IEquatable<T>
        {
            var index = IndexOf(self, in item);

            if (index < 0)
            {
                return false;
            }

            self.RemoveAt(index);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T, TComparer>(
              [NotNull] this in SharedListNative<T> self
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
              [NotNull] this in SharedListNative<T> self
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

            var result = self.AsNativeSlice().Slice(index, count).BinarySearch(item, comparer);
            return result < 0 ? result : result + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T, TComparer>(
              [NotNull] this in SharedListNative<T> self
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
            [NotNull] this in SharedListNative<T> self
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

            var result = self.AsNativeSlice().Slice(index, count).BinarySearch(item, comparer);
            return result < 0 ? result : result + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>([NotNull] this in SharedListNative<T> self, T item)
            where T : unmanaged, IEquatable<T>
                => IndexOf(self, item, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>([NotNull] this in SharedListNative<T> self, T item, int index)
            where T : unmanaged, IEquatable<T>
                => IndexOf(self, item, index, self.Count - index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>([NotNull] this in SharedListNative<T> self, T item, int index, int count)
            where T : unmanaged, IEquatable<T>
        {
            ThrowIfIndexIsNonNegative(index >= 0);
            ThrowIfCountIsNonNegative(count >= 0);
            ThrowIfSectionIsWithinList(index + count <= self.Count);

            var result = MemoryExtensions.IndexOf(self.AsReadOnlySpan().Slice(index, count), item);
            return result < 0 ? result : result + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>([NotNull] this in SharedListNative<T> self, in T item)
            where T : unmanaged, IEquatable<T>
                => IndexOf(self, in item, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>([NotNull] this in SharedListNative<T> self, in T item, int index)
            where T : unmanaged, IEquatable<T>
                => IndexOf(self, in item, index, self.Count - index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>([NotNull] this in SharedListNative<T> self, in T item, int index, int count)
            where T : unmanaged, IEquatable<T>
        {
            ThrowIfIndexIsNonNegative(index >= 0);
            ThrowIfCountIsNonNegative(count >= 0);
            ThrowIfSectionIsWithinList(index + count <= self.Count);

            var result = MemoryExtensions.IndexOf(self.AsReadOnlySpan().Slice(index, count), item);
            return result < 0 ? result : result + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T, TComparer>([NotNull] this in SharedListNative<T> self, T item, TComparer comparer)
            where T : unmanaged
            where TComparer : unmanaged, IEqualityComparer<T>
                => EncosyMemoryExtensions.IndexOf(self.AsReadOnlySpan(), item, comparer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T, TComparer>(
              [NotNull] this in SharedListNative<T> self
            , in T item
            , TComparer comparer
        )
            where T : unmanaged
            where TComparer : unmanaged, IEqualityComparer<T>
                => EncosyMemoryExtensions.IndexOf(self.AsReadOnlySpan(), in item, comparer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<T, TComparer>([NotNull] this in SharedListNative<T> self, TComparer comparer)
            where T : unmanaged
            where TComparer : unmanaged, IComparer<T>
                => Sort(self, 0, self.Count, comparer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<T, TComparer>(
              [NotNull] this in SharedListNative<T> self
            , int index
            , int count
            , TComparer comparer
        )
            where T : unmanaged
            where TComparer : unmanaged, IComparer<T>
        {
            ThrowIfIndexIsNonNegative(index >= 0);
            ThrowIfCountIsNonNegative(count >= 0);
            ThrowIfOffsetLengthIsValid(self.Count - index >= count);

            self.MarkChanged();

            if (count > 1)
            {
                self.AsNativeSlice().Slice(index, count).Sort(comparer);
            }
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
                => new(
                    "Index and count do not denote a valid range in " +
                    "SharedListNative<T>."
                );
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
                    "Index and count do not specify a valid section in " +
                    "SharedListNative<T>."
                );
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

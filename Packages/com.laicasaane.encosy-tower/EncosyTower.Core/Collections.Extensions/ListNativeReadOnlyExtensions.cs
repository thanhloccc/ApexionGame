using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections.Extensions
{
    public static class ListNativeReadOnlyExtensions
    {
        public static bool Contains<T>(this in ListNative<T>.ReadOnly self, T item)
            where T : unmanaged, IEquatable<T>
        {
            var s = self.AsReadOnlySpan();
            for (var i = 0; i < s.Length; i++)
            {
                if (s[i].Equals(item))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(this in ListNative<T>.ReadOnly self, in T item)
            where T : unmanaged, IEquatable<T>
                => Contains(self, item);

        public static int IndexOf<T>(this in ListNative<T>.ReadOnly self, T item)
            where T : unmanaged, IEquatable<T>
        {
            var s = self.AsReadOnlySpan();
            for (var i = 0; i < s.Length; i++)
            {
                if (s[i].Equals(item))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this in ListNative<T>.ReadOnly self, T item, int index)
            where T : unmanaged, IEquatable<T>
                => IndexOf(self, item, index, self.Count - index);

        public static int IndexOf<T>(
              this in ListNative<T>.ReadOnly self
            , T item
            , int index
            , int count
        )
            where T : unmanaged, IEquatable<T>
        {
            ThrowIfRangeIsInvalid_Index(index >= 0);
            ThrowIfRangeIsInvalid_Count(count >= 0);
            ThrowIfRangeIsInvalid_Section(self.Count - index >= count);
            var s = self.AsReadOnlySpan().Slice(index, count);
            for (var i = 0; i < s.Length; i++)
            {
                if (s[i].Equals(item))
                {
                    return index + i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this in ListNative<T>.ReadOnly self, in T item)
            where T : unmanaged, IEquatable<T>
                => IndexOf(self, in item, 0, self.Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this in ListNative<T>.ReadOnly self, in T item, int index)
            where T : unmanaged, IEquatable<T>
                => IndexOf(self, in item, index, self.Count - index);

        public static int IndexOf<T>(
              this in ListNative<T>.ReadOnly self
            , in T item
            , int index
            , int count
        )
            where T : unmanaged, IEquatable<T>
        {
            ThrowIfRangeIsInvalid_Index(index >= 0);
            ThrowIfRangeIsInvalid_Count(count >= 0);
            ThrowIfRangeIsInvalid_Section(self.Count - index >= count);
            var s = self.AsReadOnlySpan().Slice(index, count);
            for (var i = 0; i < s.Length; i++)
            {
                if (s[i].Equals(item))
                {
                    return index + i;
                }
            }

            return -1;
        }

        public static int IndexOf<T, TComparer>(
              this in ListNative<T>.ReadOnly self
            , T item
            , TComparer comparer
        )
            where T : unmanaged
            where TComparer : unmanaged, IEqualityComparer<T>
        {
            var s = self.AsReadOnlySpan();
            for (var i = 0; i < s.Length; i++)
            {
                if (comparer.Equals(s[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int IndexOf<T, TComparer>(
              this in ListNative<T>.ReadOnly self
            , in T item
            , TComparer comparer
        )
            where T : unmanaged
            where TComparer : unmanaged, IEqualityComparer<T>
        {
            var s = self.AsReadOnlySpan();
            for (var i = 0; i < s.Length; i++)
            {
                if (comparer.Equals(s[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T, TComparer>(
              this in ListNative<T>.ReadOnly self
            , T item
            , TComparer comparer
        )
            where T : unmanaged
            where TComparer : unmanaged, IComparer<T>
                => Array.BinarySearch(self.ToArray(), item, comparer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T, TComparer>(
              this in ListNative<T>.ReadOnly self
            , int index
            , int count
            , T item
            , TComparer comparer
        )
            where T : unmanaged
            where TComparer : unmanaged, IComparer<T>
                => Array.BinarySearch(self.ToArray(), index, count, item, comparer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T, TComparer>(
              this in ListNative<T>.ReadOnly self
            , in T item
            , TComparer comparer
        )
            where T : unmanaged
            where TComparer : unmanaged, IComparer<T>
                => Array.BinarySearch(self.ToArray(), item, comparer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T, TComparer>(
              this in ListNative<T>.ReadOnly self
            , int index
            , int count
            , in T item
            , TComparer comparer
        )
            where T : unmanaged
            where TComparer : unmanaged, IComparer<T>
                => Array.BinarySearch(self.ToArray(), index, count, item, comparer);

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfRangeIsInvalid_Index([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentOutOfRangeException CreateException()
                => new("index", "Index must be non-negative.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfRangeIsInvalid_Count([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentOutOfRangeException CreateException()
                => new("count", "Count must be non-negative.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfRangeIsInvalid_Section([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentOutOfRangeException CreateException()
                => new("count", "Index and count do not specify a valid section in ListNative<T>.ReadOnly.");
        }
    }
}

#if UNITY_COLLECTIONS

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections
{
    public static class EncosyNativeListExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncreaseCapacityBy<T>(this NativeList<T> list, int amount)
            where T : unmanaged
        {
            ThrowHelper.ThrowIfAmountIsInvalid(amount > 0);
            return IncreaseCapacityTo(list, list.Capacity + amount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncreaseCapacityTo<T>(this NativeList<T> list, int newCapacity)
            where T : unmanaged
        {
            if (newCapacity > list.Capacity)
            {
                list.SetCapacity(newCapacity);
            }

            return list.Capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NewOrClear<T>(
              ref this NativeList<T> list
            , int capacity
            , AllocatorManager.AllocatorHandle allocator
        )
            where T : unmanaged
        {
            if (list.IsCreated)
            {
                list.Clear();
            }
            else
            {
                list = new(capacity, allocator);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ElementAtOrDefault<T>(this NativeList<T> list, int index)
            where T : unmanaged
        {
            return (uint)index < (uint)list.Length ? list[index] : default;
        }

        /// <summary>
        /// Dispose the collection then set it to default.
        /// </summary>
        /// <param name="list">The collection to dispose and unset</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeUnset<T>(ref this NativeList<T> list)
            where T : unmanaged
        {
            list.DisposeIfCreated();
            list = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeIfCreated<T>(this NativeList<T> list)
            where T : unmanaged
        {
            if (list.IsCreated)
            {
                list.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobHandle DisposeIfCreated<T>(this NativeList<T> list, JobHandle inputDeps)
            where T : unmanaged
        {
            if (list.IsCreated)
            {
                return list.Dispose(inputDeps);
            }

            return inputDeps;
        }

        /// <inheritdoc cref="NativeList{T}.AddRange(NativeArray{T})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRange<T>(this NativeList<T> list, ReadOnlySpan<T> items)
            where T : unmanaged
        {
            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                fixed (T* ptr = items)
                {
                    list.AddRange(ptr, items.Length);
                }
            }
        }

        /// <inheritdoc cref="NativeList{T}.InsertRange(int, int)"/>
        /// <returns>
        /// A <see cref="Span{T}"/> of the inserted range.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> InsertRangeSpan<T>(this NativeList<T> list, int index, int count)
            where T : unmanaged
        {
            list.InsertRange(index, count);
            return list.AsSpan().Slice(index, count);
        }

        /// <inheritdoc cref="NativeList{T}.InsertRangeWithBeginEnd(int, int)"/>
        /// <returns>
        /// A <see cref="Span{T}"/> of the inserted range.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> InsertRangeWithBeginEndSpan<T>(this NativeList<T> list, int begin, int end)
            where T : unmanaged
        {
            list.InsertRangeWithBeginEnd(begin, end);

            var count = end - begin;

            if (count > 0)
            {
                return list.AsSpan().Slice(begin, count);
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(this NativeList<T> list)
            where T : unmanaged
        {
            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                return new Span<T>(list.GetUnsafePtr(), list.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this NativeList<T> list)
            where T : unmanaged
        {
            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                return new ReadOnlySpan<T>(list.GetUnsafeReadOnlyPtr(), list.Length);
            }
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfAmountIsPositive([DoesNotReturnIf(false)] bool valid)
        {
            if (valid == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Amount must be greater than 0.");
        }
    }
}

#endif

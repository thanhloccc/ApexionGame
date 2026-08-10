#if UNITY_COLLECTIONS

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections
{
    public static class EncosyNativeBitArrayExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncreaseCapacityBy(this NativeBitArray array, int amount)
        {
            ThrowHelper.ThrowIfAmountIsInvalid(amount > 0);
            return IncreaseCapacityTo(array, array.Capacity + amount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncreaseCapacityTo(this NativeBitArray array, int newCapacity)
        {
            if (newCapacity > array.Capacity)
            {
                array.SetCapacity(newCapacity);
            }

            return array.Capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NewOrClear(
              ref this NativeBitArray array
            , int capacity
            , AllocatorManager.AllocatorHandle allocator
        )
        {
            if (array.IsCreated)
            {
                array.Clear();
            }
            else
            {
                array = new(capacity, allocator);
            }
        }

        /// <summary>
        /// Dispose the collection then set it to default.
        /// </summary>
        /// <param name="array">The collection to dispose and unset</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeUnset(ref this NativeBitArray array)
        {
            array.DisposeIfCreated();
            array = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeIfCreated(this NativeBitArray array)
        {
            if (array.IsCreated)
            {
                array.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobHandle DisposeIfCreated(this NativeBitArray array, JobHandle inputDeps)
        {
            if (array.IsCreated)
            {
                return array.Dispose(inputDeps);
            }

            return inputDeps;
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

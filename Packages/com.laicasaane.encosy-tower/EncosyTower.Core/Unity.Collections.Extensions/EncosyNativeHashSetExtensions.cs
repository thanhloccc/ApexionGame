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
    public static class EncosyNativeHashSetExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncreaseCapacityBy<T>(this NativeHashSet<T> set, int amount)
            where T : unmanaged, IEquatable<T>
        {
            ThrowHelper.ThrowIfAmountIsInvalid(amount > 0);
            return IncreaseCapacityTo(set, set.Capacity + amount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncreaseCapacityTo<T>(this NativeHashSet<T> set, int newCapacity)
            where T : unmanaged, IEquatable<T>
        {
            if (newCapacity > set.Capacity)
            {
                set.Capacity = newCapacity;
            }

            return set.Capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncreaseCapacityBy<T>(this NativeParallelHashSet<T> set, int amount)
            where T : unmanaged, IEquatable<T>
        {
            ThrowIfAmountIsPositive(amount > 0);
            return IncreaseCapacityTo(set, set.Capacity + amount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncreaseCapacityTo<T>(this NativeParallelHashSet<T> set, int newCapacity)
            where T : unmanaged, IEquatable<T>
        {
            if (newCapacity > set.Capacity)
            {
                set.Capacity = newCapacity;
            }

            return set.Capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref NativeHashSet<T> NewOrClear<T>(
              ref this NativeHashSet<T> set
            , int capacity
            , AllocatorManager.AllocatorHandle allocator
        )
            where T : unmanaged, IEquatable<T>
        {
            if (set.IsCreated)
            {
                set.Clear();
            }
            else
            {
                set = new(capacity, allocator);
            }

            return ref set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref NativeParallelHashSet<T> NewOrClear<T>(
              ref this NativeParallelHashSet<T> set
            , int capacity
            , AllocatorManager.AllocatorHandle allocator
        )
            where T : unmanaged, IEquatable<T>
        {
            if (set.IsCreated)
            {
                set.Clear();
            }
            else
            {
                set = new(capacity, allocator);
            }

            return ref set;
        }

        /// <summary>
        /// Dispose the collection then set it to default.
        /// </summary>
        /// <param name="set">The collection to dispose and unset</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeUnset<T>(ref this NativeHashSet<T> set)
            where T : unmanaged, IEquatable<T>
        {
            set.DisposeIfCreated();
            set = default;
        }

        /// <summary>
        /// Dispose the collection then set it to default.
        /// </summary>
        /// <param name="set">The collection to dispose and unset</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeUnset<T>(ref this NativeParallelHashSet<T> set)
            where T : unmanaged, IEquatable<T>
        {
            set.DisposeIfCreated();
            set = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeIfCreated<T>(this NativeHashSet<T> set)
            where T : unmanaged, IEquatable<T>
        {
            if (set.IsCreated)
            {
                set.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobHandle DisposeIfCreated<T>(this NativeHashSet<T> set, JobHandle inputDeps)
            where T : unmanaged, IEquatable<T>
        {
            if (set.IsCreated)
            {
                return set.Dispose(inputDeps);
            }

            return inputDeps;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeIfCreated<T>(this NativeParallelHashSet<T> set)
            where T : unmanaged, IEquatable<T>
        {
            if (set.IsCreated)
            {
                set.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobHandle DisposeIfCreated<T>(this NativeParallelHashSet<T> set, JobHandle inputDeps)
            where T : unmanaged, IEquatable<T>
        {
            if (set.IsCreated)
            {
                return set.Dispose(inputDeps);
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

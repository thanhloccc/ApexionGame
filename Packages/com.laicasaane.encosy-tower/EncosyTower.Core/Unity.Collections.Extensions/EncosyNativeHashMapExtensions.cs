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
    public static class EncosyNativeHashMapExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncreaseCapacityBy<TKey, TValue>(this NativeHashMap<TKey, TValue> map, int amount)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            ThrowHelper.ThrowIfAmountIsInvalid(amount > 0);
            return IncreaseCapacityTo(map, map.Capacity + amount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncreaseCapacityTo<TKey, TValue>(this NativeHashMap<TKey, TValue> map, int newCapacity)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (newCapacity > map.Capacity)
            {
                map.Capacity = newCapacity;
            }

            return map.Capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncreaseCapacityBy<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> map, int amount)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            ThrowIfAmountIsPositive(amount > 0);
            return IncreaseCapacityTo(map, map.Capacity + amount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncreaseCapacityTo<TKey, TValue>(
              this NativeParallelHashMap<TKey, TValue> map
            , int newCapacity
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (newCapacity > map.Capacity)
            {
                map.Capacity = newCapacity;
            }

            return map.Capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncreaseCapacityBy<TKey, TValue>(
              this NativeParallelMultiHashMap<TKey, TValue> map
            , int amount
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            ThrowIfAmountIsPositive(amount > 0);
            return IncreaseCapacityTo(map, map.Capacity + amount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncreaseCapacityTo<TKey, TValue>(
              this NativeParallelMultiHashMap<TKey, TValue> map
            , int newCapacity
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (newCapacity > map.Capacity)
            {
                map.Capacity = newCapacity;
            }

            return map.Capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref NativeHashMap<TKey, TValue> NewOrClear<TKey, TValue>(
              ref this NativeHashMap<TKey, TValue> map
            , int capacity
            , AllocatorManager.AllocatorHandle allocator
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (map.IsCreated)
            {
                map.Clear();
            }
            else
            {
                map = new(capacity, allocator);
            }

            return ref map;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref NativeParallelHashMap<TKey, TValue> NewOrClear<TKey, TValue>(
              ref this NativeParallelHashMap<TKey, TValue> map
            , int capacity
            , AllocatorManager.AllocatorHandle allocator
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (map.IsCreated)
            {
                map.Clear();
            }
            else
            {
                map = new(capacity, allocator);
            }

            return ref map;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref NativeParallelMultiHashMap<TKey, TValue> NewOrClear<TKey, TValue>(
              ref this NativeParallelMultiHashMap<TKey, TValue> map
            , int capacity
            , AllocatorManager.AllocatorHandle allocator
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (map.IsCreated)
            {
                map.Clear();
            }
            else
            {
                map = new(capacity, allocator);
            }

            return ref map;
        }

        /// <summary>
        /// Dispose the collection then set it to default.
        /// </summary>
        /// <param name="map">The collection to dispose and unset</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeUnset<TKey, TValue>(ref this NativeHashMap<TKey, TValue> map)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            map.DisposeIfCreated();
            map = default;
        }

        /// <summary>
        /// Dispose the collection then set it to default.
        /// </summary>
        /// <param name="map">The collection to dispose and unset</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeUnset<TKey, TValue>(ref this NativeParallelHashMap<TKey, TValue> map)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            map.DisposeIfCreated();
            map = default;
        }

        /// <summary>
        /// Dispose the collection then set it to default.
        /// </summary>
        /// <param name="map">The collection to dispose and unset</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeUnset<TKey, TValue>(ref this NativeParallelMultiHashMap<TKey, TValue> map)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            map.DisposeIfCreated();
            map = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeIfCreated<TKey, TValue>(this NativeHashMap<TKey, TValue> map)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (map.IsCreated)
            {
                map.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobHandle DisposeIfCreated<TKey, TValue>(
              this NativeHashMap<TKey, TValue> map
            , JobHandle inputDeps
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (map.IsCreated)
            {
                return map.Dispose(inputDeps);
            }

            return inputDeps;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeIfCreated<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> map)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (map.IsCreated)
            {
                map.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobHandle DisposeIfCreated<TKey, TValue>(
              this NativeParallelHashMap<TKey, TValue> map
            , JobHandle inputDeps
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (map.IsCreated)
            {
                return map.Dispose(inputDeps);
            }

            return inputDeps;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeIfCreated<TKey, TValue>(this NativeParallelMultiHashMap<TKey, TValue> map)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (map.IsCreated)
            {
                map.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobHandle DisposeIfCreated<TKey, TValue>(
              this NativeParallelMultiHashMap<TKey, TValue> map
            , JobHandle inputDeps
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            if (map.IsCreated)
            {
                return map.Dispose(inputDeps);
            }

            return inputDeps;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Deconstruct<TKey, TValue>(this KVPair<TKey, TValue> kv, out TKey key, out TValue value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            key = kv.Key;
            value = kv.Value;
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

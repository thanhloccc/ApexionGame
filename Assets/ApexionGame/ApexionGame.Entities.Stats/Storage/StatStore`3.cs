using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EncosyTower.Common;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

using static ApexionGame.Entities.Stats.Debugging.ValidationDefines;

namespace ApexionGame.Entities.Stats
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    public unsafe partial struct StatStore<TStat, TStatModifier, TStatObserver>
        : IDisposable, INativeDisposable, IIsCreated
        where TStat : unmanaged
        where TStatModifier : unmanaged
        where TStatObserver : unmanaged
    {
        public const int DEFAULT_STAT_CAPACITY = 4;
        public const int DEFAULT_MODIFIER_CAPACITY = 4;
        public const int DEFAULT_OBSERVER_CAPACITY = 4;

#pragma warning disable IDE1006 // Naming Styles
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeList<StatOwnerSlot>* m_Slots;

        [NativeDisableUnsafePtrRestriction]
        internal UnsafeList<StatBufferSlot<TStat>>* m_Stats;

        [NativeDisableUnsafePtrRestriction]
        internal UnsafeList<StatBufferSlot<TStatModifier>>* m_Modifiers;

        [NativeDisableUnsafePtrRestriction]
        internal UnsafeList<StatBufferSlot<TStatObserver>>* m_Observers;

        [NativeDisableUnsafePtrRestriction]
        internal UnsafeList<int>* m_FreeSlots;

        internal AllocatorManager.AllocatorHandle m_Allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;

#if UNITY_BURST
        private static readonly Unity.Burst.SharedStatic<int> s_SafetyId
            = Unity.Burst.SharedStatic<int>.GetOrCreate<StatStore<TStat, TStatModifier, TStatObserver>>();
#else
        private static int s_SafetyId;
#endif
#endif
#pragma warning restore IDE1006 // Naming Styles

        public StatStore(int initialOwnerCapacity, AllocatorManager.AllocatorHandle allocator) : this()
        {
            ThrowIfNegativeCapacity(initialOwnerCapacity >= 0, initialOwnerCapacity);

            m_Allocator = allocator;
            m_Slots = UnsafeList<StatOwnerSlot>.Create(initialOwnerCapacity, allocator);
            m_Stats = UnsafeList<StatBufferSlot<TStat>>.Create(initialOwnerCapacity, allocator);
            m_Modifiers = UnsafeList<StatBufferSlot<TStatModifier>>.Create(initialOwnerCapacity, allocator);
            m_Observers = UnsafeList<StatBufferSlot<TStatObserver>>.Create(initialOwnerCapacity, allocator);
            m_FreeSlots = UnsafeList<int>.Create(initialOwnerCapacity, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

#if UNITY_BURST
            CollectionHelper.SetStaticSafetyId<StatStore<TStat, TStatModifier, TStatObserver>>(
                  ref m_Safety
                , ref s_SafetyId.Data
            );
#else
            CollectionHelper.SetStaticSafetyId<StatStore<TStat, TStatModifier, TStatObserver>>(
                  ref m_Safety
                , ref s_SafetyId
            );
#endif

            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Slots != null;
        }

        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                ThrowIfNotCreated(IsCreated);

                // SAFETY: The checks above validate the live slot list before reading its length.
                return m_Slots->Length;
            }
        }

        public readonly int OwnerCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                ThrowIfNotCreated(IsCreated);

                // SAFETY: The checks above validate both live lists; every free slot is a slot
                // that was allocated once and is currently not owned by anybody.
                return m_Slots->Length - m_FreeSlots->Length;
            }
        }

        public StatOwnerHandle CreateOwner()
            => CreateOwner(DEFAULT_STAT_CAPACITY, DEFAULT_MODIFIER_CAPACITY, DEFAULT_OBSERVER_CAPACITY);

        public StatOwnerHandle CreateOwner(int statCapacity, int modifierCapacity, int observerCapacity)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            ThrowIfNotCreated(IsCreated);
            ThrowIfNegativeCapacity(statCapacity >= 0, statCapacity);
            ThrowIfNegativeCapacity(modifierCapacity >= 0, modifierCapacity);
            ThrowIfNegativeCapacity(observerCapacity >= 0, observerCapacity);

            // SAFETY: The checks above validate every live list. A recycled slot keeps its three
            // list headers, so their addresses stay stable for the lifetime of the store and
            // buffers handed out for the previous owner already fail on the version mismatch.
            var freeCount = m_FreeSlots->Length;

            if (freeCount > 0)
            {
                var index = m_FreeSlots->Ptr[freeCount - 1];
                m_FreeSlots->RemoveAt(freeCount - 1);

                ref var recycled = ref m_Slots->ElementAt(index);
                recycled.version = -recycled.version + 1;
                recycled.modifierIdCounter = 0;

                return new StatOwnerHandle(index, recycled.version);
            }

            var slot = new StatOwnerSlot {
                version = 1,
                modifierIdCounter = 0,
            };

            m_Slots->Add(in slot);

            m_Stats->Add(new StatBufferSlot<TStat> {
                list = UnsafeList<TStat>.Create(statCapacity, m_Allocator),
            });

            m_Modifiers->Add(new StatBufferSlot<TStatModifier> {
                list = UnsafeList<TStatModifier>.Create(modifierCapacity, m_Allocator),
            });

            m_Observers->Add(new StatBufferSlot<TStatObserver> {
                list = UnsafeList<TStatObserver>.Create(observerCapacity, m_Allocator),
            });

            return new StatOwnerHandle(m_Slots->Length - 1, slot.version);
        }

        public bool DestroyOwner(StatOwnerHandle owner)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            ThrowIfNotCreated(IsCreated);

            if (TryGetSlotIndex(owner, out var index) == false)
            {
                return false;
            }

            // SAFETY: TryGetSlotIndex validated the live slot and its index. The three list headers
            // stay allocated so the slot can be recycled later without reallocating them.
            m_Stats->Ptr[index].list->Clear();
            m_Modifiers->Ptr[index].list->Clear();
            m_Observers->Ptr[index].list->Clear();

            ref var slot = ref m_Slots->ElementAt(index);
            slot.modifierIdCounter = 0;
            slot.version = -slot.version;

            m_FreeSlots->Add(index);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Exists(StatOwnerHandle owner)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return TryGetSlotIndex(owner, out _);
        }

        /// <summary>
        /// Rebuilds the handle living in slot <paramref name="slotIndex"/>, if any.
        /// </summary>
        /// <remarks>
        /// Exists so tooling can walk <c>0 .. Capacity</c> and discover owners it was never handed.
        /// Gameplay code should keep the handle it was given rather than search for it: a slot
        /// index alone is not an identity, only index plus version is.
        /// </remarks>
        public readonly bool TryGetOwnerAt(int slotIndex, out StatOwnerHandle owner)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            if (IsCreated == false
                || (uint)slotIndex >= (uint)m_Slots->Length
            )
            {
                owner = default;
                return false;
            }

            // SAFETY: IsCreated plus the bounds check above make this element read valid.
            var slot = m_Slots->ElementAt(slotIndex);

            if (slot.IsAlive == false)
            {
                owner = default;
                return false;
            }

            owner = new StatOwnerHandle(slotIndex, slot.version);
            return true;
        }

        public void Dispose()
        {
            if (IsCreated == false)
            {
                return;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#endif
            // SAFETY: IsCreated guarantees every list is live. Each owner owns three separately
            // allocated list headers that must be freed before the index lists themselves.
            DestroyAll(m_Slots, m_Stats, m_Modifiers, m_Observers, m_FreeSlots);

            m_Slots = null;
            m_Stats = null;
            m_Modifiers = null;
            m_Observers = null;
            m_FreeSlots = null;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (IsCreated == false)
            {
                return inputDeps;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#endif
            var job = new DisposeJob {
                slots = m_Slots,
                stats = m_Stats,
                modifiers = m_Modifiers,
                observers = m_Observers,
                freeSlots = m_FreeSlots,
            };

            var handle = job.Schedule(inputDeps);

            m_Slots = null;
            m_Stats = null;
            m_Modifiers = null;
            m_Observers = null;
            m_FreeSlots = null;

            return handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly bool TryGetSlotIndex(StatOwnerHandle owner, out int index)
        {
            if (IsCreated == false || owner.IsValid == false)
            {
                index = default;
                return false;
            }

            // SAFETY: IsCreated guarantees the slot list is live; the index is range-checked
            // against its current length before the pointer is dereferenced.
            if ((uint)owner.Index >= (uint)m_Slots->Length)
            {
                index = default;
                return false;
            }

            if (m_Slots->Ptr[owner.Index].version != owner.Version)
            {
                index = default;
                return false;
            }

            index = owner.Index;
            return true;
        }

        private static void DestroyAll(
              UnsafeList<StatOwnerSlot>* slots
            , UnsafeList<StatBufferSlot<TStat>>* stats
            , UnsafeList<StatBufferSlot<TStatModifier>>* modifiers
            , UnsafeList<StatBufferSlot<TStatObserver>>* observers
            , UnsafeList<int>* freeSlots
        )
        {
            var length = slots->Length;

            for (var i = 0; i < length; i++)
            {
                ref var stat = ref stats->ElementAt(i);

                if (stat.list != null)
                {
                    UnsafeList<TStat>.Destroy(stat.list);
                    stat.list = null;
                }

                ref var modifier = ref modifiers->ElementAt(i);

                if (modifier.list != null)
                {
                    UnsafeList<TStatModifier>.Destroy(modifier.list);
                    modifier.list = null;
                }

                ref var observer = ref observers->ElementAt(i);

                if (observer.list != null)
                {
                    UnsafeList<TStatObserver>.Destroy(observer.list);
                    observer.list = null;
                }
            }

            UnsafeList<StatOwnerSlot>.Destroy(slots);
            UnsafeList<StatBufferSlot<TStat>>.Destroy(stats);
            UnsafeList<StatBufferSlot<TStatModifier>>.Destroy(modifiers);
            UnsafeList<StatBufferSlot<TStatObserver>>.Destroy(observers);
            UnsafeList<int>.Destroy(freeSlots);
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(STATS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfNotCreated([DoesNotReturnIf(false)] bool isCreated)
        {
            if (isCreated == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ObjectDisposedException CreateException()
                => new(
                      nameof(StatStore<TStat, TStatModifier, TStatObserver>)
                    , "The stat store is not created or has already been disposed."
                );
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(STATS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfNegativeCapacity([DoesNotReturnIf(false)] bool validCapacity, int capacity)
        {
            if (validCapacity == false)
            {
                throw CreateException(capacity);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentOutOfRangeException CreateException(int capacity)
                => new(nameof(capacity), $"Capacity must not be negative but is '{capacity}'.");
        }

        internal struct DisposeJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public UnsafeList<StatOwnerSlot>* slots;

            [NativeDisableUnsafePtrRestriction]
            public UnsafeList<StatBufferSlot<TStat>>* stats;

            [NativeDisableUnsafePtrRestriction]
            public UnsafeList<StatBufferSlot<TStatModifier>>* modifiers;

            [NativeDisableUnsafePtrRestriction]
            public UnsafeList<StatBufferSlot<TStatObserver>>* observers;

            [NativeDisableUnsafePtrRestriction]
            public UnsafeList<int>* freeSlots;

            public void Execute()
            {
                // SAFETY: The store handed over ownership of every list before scheduling and
                // cleared its own pointers, so this job is their only remaining owner.
                DestroyAll(slots, stats, modifiers, observers, freeSlots);
            }
        }
    }
}

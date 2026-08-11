using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace ApexionGame.Entities.Stats
{
    unsafe partial struct StatStore<TStat, TStatModifier, TStatObserver>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly StatBufferLookup<TStat> AsStatLookup()
            => MakeLookup(m_Stats);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly StatBufferLookup<TStatModifier> AsModifierLookup()
            => MakeLookup(m_Modifiers);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly StatBufferLookup<TStatObserver> AsObserverLookup()
            => MakeLookup(m_Observers);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetStats(StatOwnerHandle owner, out StatBuffer<TStat> buffer)
        {
            if (TryGetSlotIndex(owner, out var index) == false)
            {
                buffer = default;
                return false;
            }

            buffer = MakeBuffer(m_Stats->Ptr[index].list);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetModifiers(StatOwnerHandle owner, out StatBuffer<TStatModifier> buffer)
        {
            if (TryGetSlotIndex(owner, out var index) == false)
            {
                buffer = default;
                return false;
            }

            buffer = MakeBuffer(m_Modifiers->Ptr[index].list);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetObservers(StatOwnerHandle owner, out StatBuffer<TStatObserver> buffer)
        {
            if (TryGetSlotIndex(owner, out var index) == false)
            {
                buffer = default;
                return false;
            }

            buffer = MakeBuffer(m_Observers->Ptr[index].list);
            return true;
        }

        public readonly bool TryGetBuffers(
              StatOwnerHandle owner
            , out StatBuffer<TStat> stats
            , out StatBuffer<TStatModifier> modifiers
            , out StatBuffer<TStatObserver> observers
        )
        {
            if (TryGetSlotIndex(owner, out var index) == false)
            {
                stats = default;
                modifiers = default;
                observers = default;
                return false;
            }

            stats = MakeBuffer(m_Stats->Ptr[index].list);
            modifiers = MakeBuffer(m_Modifiers->Ptr[index].list);
            observers = MakeBuffer(m_Observers->Ptr[index].list);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetModifierIdCounter(StatOwnerHandle owner, out uint counter)
        {
            if (TryGetSlotIndex(owner, out var index) == false)
            {
                counter = default;
                return false;
            }

            // SAFETY: TryGetSlotIndex validated the live slot and its index.
            counter = m_Slots->Ptr[index].modifierIdCounter;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryIncrementModifierId(StatOwnerHandle owner, out uint modifierId)
        {
            if (TryGetSlotIndex(owner, out var index) == false)
            {
                modifierId = default;
                return false;
            }

            // SAFETY: TryGetSlotIndex validated the live slot before the increment.
            ref var slot = ref m_Slots->ElementAt(index);
            slot.modifierIdCounter++;
            modifierId = slot.modifierIdCounter;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly StatBuffer<T> MakeBuffer<T>(UnsafeList<T>* list)
            where T : unmanaged
        {
            var buffer = new StatBuffer<T>(list);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            buffer.m_Safety = m_Safety;
#endif
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly StatBufferLookup<T> MakeLookup<T>(UnsafeList<StatBufferSlot<T>>* buffers)
            where T : unmanaged
        {
            var lookup = new StatBufferLookup<T> {
                m_Slots = m_Slots,
                m_Buffers = buffers,
            };

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            lookup.m_Safety = m_Safety;
#endif
            return lookup;
        }
    }
}

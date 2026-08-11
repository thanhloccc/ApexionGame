using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ApexionGame.Entities.Stats
{
    unsafe partial struct StatStore<TStat, TStatModifier, TStatObserver>
    {
        /// <summary>
        /// Copies the full state of one owner out of the store: its three buffers and its modifier
        /// id counter. Everything is blittable, so the result can be written straight to disk.
        /// </summary>
        /// <remarks>
        /// Appends to the lists; it does not clear them.
        /// <para>
        /// The copied <see cref="StatHandle"/> values embed <see cref="StatOwnerHandle"/>.Index and
        /// Version, which are meaningless in a later session. Restoring therefore takes two passes:
        /// <see cref="TryRestoreOwner"/> for every owner first, then a remap pass over the handles.
        /// </para>
        /// </remarks>
        public readonly bool TryCopyOwnerTo(
              StatOwnerHandle owner
            , NativeList<TStat> stats
            , NativeList<TStatModifier> modifiers
            , NativeList<TStatObserver> observers
            , out uint modifierIdCounter
        )
        {
            if (TryGetSlotIndex(owner, out var index) == false)
            {
                modifierIdCounter = default;
                return false;
            }

            // SAFETY: TryGetSlotIndex validated the live slot and its index; every parallel list is
            // kept at the same length by CreateOwner.
            CopyInto(m_Stats->Ptr[index].list, stats);
            CopyInto(m_Modifiers->Ptr[index].list, modifiers);
            CopyInto(m_Observers->Ptr[index].list, observers);

            modifierIdCounter = m_Slots->Ptr[index].modifierIdCounter;
            return true;
        }

        /// <summary>
        /// Creates an owner and fills it with previously copied state.
        /// </summary>
        /// <remarks>
        /// <see cref="StatIndex"/> values stay valid because the stat buffer is restored in order.
        /// The handles stored inside observers and modifiers do not: pair every restored owner with
        /// its old handle in a <see cref="StatOwnerRemap"/>, then run the remap pass.
        /// </remarks>
        public bool TryRestoreOwner(
              ReadOnlySpan<TStat> stats
            , ReadOnlySpan<TStatModifier> modifiers
            , ReadOnlySpan<TStatObserver> observers
            , uint modifierIdCounter
            , out StatOwnerHandle owner
        )
        {
            ThrowIfNotCreated(IsCreated);

            owner = CreateOwner(
                  Math.Max(stats.Length, 1)
                , Math.Max(modifiers.Length, 1)
                , Math.Max(observers.Length, 1)
            );

            if (TryGetSlotIndex(owner, out var index) == false)
            {
                owner = StatOwnerHandle.Null;
                return false;
            }

            // SAFETY: the owner was just created by this call, so all four lists are live and the
            // index is in range. A recycled slot keeps buffers that CreateOwner already cleared.
            RestoreInto(m_Stats->Ptr[index].list, stats);
            RestoreInto(m_Modifiers->Ptr[index].list, modifiers);
            RestoreInto(m_Observers->Ptr[index].list, observers);

            m_Slots->ElementAt(index).modifierIdCounter = modifierIdCounter;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyInto<T>(UnsafeList<T>* source, NativeList<T> destination)
            where T : unmanaged
        {
            if (source == null || source->Length < 1)
            {
                return;
            }

            // SAFETY: the caller validated the live slot; the span aliases the list for the copy only.
            destination.AddRange(source->Ptr, source->Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RestoreInto<T>(UnsafeList<T>* destination, ReadOnlySpan<T> source)
            where T : unmanaged
        {
            destination->Clear();

            if (source.Length < 1)
            {
                return;
            }

            destination->SetCapacity(source.Length);

            // SAFETY: capacity was just grown to hold the whole span, and T is unmanaged.
            fixed (T* ptr = source)
            {
                destination->AddRange(ptr, source.Length);
            }
        }
    }
}

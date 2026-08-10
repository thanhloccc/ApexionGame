using System;

namespace ApexionGame.Entities.Stats
{
    partial struct StatAccessor<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver, TValuePairComposer>
    {
        /// <summary>
        /// Second pass of a restore: rewrites every stat handle stored on one owner so it points at
        /// the handle that owner was restored as.
        /// </summary>
        /// <remarks>
        /// Run this only after **every** owner in the save has been restored and recorded in
        /// <paramref name="remap"/>. Doing it per owner as you go would leave forward references
        /// pointing at owners that do not exist yet, and they would silently become
        /// <see cref="StatHandle.Null"/>.
        /// <para>
        /// The store cannot do this itself: its type parameters are plain <c>unmanaged</c>, so it
        /// can reach neither <see cref="IStatObserver.ObserverHandle"/> nor
        /// <see cref="IStatModifier{TValuePair, TStat, TStatModifierStack}.RemapObservedStats"/>.
        /// </para>
        /// </remarks>
        public readonly bool TryRemapOwner(StatOwnerHandle owner, in StatOwnerRemap remap)
        {
            if (remap.IsCreated == false
                || _store.TryGetModifiers(owner, out var modifiers) == false
                || _store.TryGetObservers(owner, out var observers) == false
            )
            {
                return false;
            }

            var observerCount = observers.Length;

            for (var i = 0; i < observerCount; i++)
            {
                ref var observer = ref observers.ElementAt(i);
                observer.ObserverHandle = remap.RemapOrNull(observer.ObserverHandle);
            }

            var modifierCount = modifiers.Length;

            for (var i = 0; i < modifierCount; i++)
            {
                // By ref: the modifier rewrites its own fields, exactly like it does in Apply.
                ref var modifier = ref modifiers.ElementAt(i);
                modifier.RemapObservedStats(in remap);
            }

            return true;
        }

        /// <summary>
        /// Runs <see cref="TryRemapOwner"/> for every restored owner.
        /// </summary>
        /// <returns>True when every owner was remapped; false if any was unreachable.</returns>
        public readonly bool TryRemapOwners(ReadOnlySpan<StatOwnerHandle> owners, in StatOwnerRemap remap)
        {
            var success = true;

            for (var i = 0; i < owners.Length; i++)
            {
                success &= TryRemapOwner(owners[i], in remap);
            }

            return success;
        }
    }
}

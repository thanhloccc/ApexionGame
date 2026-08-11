using System;
using ApexionGame.Entities.Stats.Debugging;
using EncosyTower.Collections;
using Unity.Collections;
using Unity.Jobs;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>
    /// The one and only type that touches the store, the accessor and the world data.
    /// </summary>
    /// <remarks>
    /// Everything else in the sample — spawning, combat, spells, diagnostics — goes through the small
    /// surface below. That is deliberate: the store is native memory with a strict disposal order and a
    /// couple of sharp edges (index 0 is the None stat, only the generated builder seeds it, nothing clears
    /// the event lists for you), and one owner is much easier to keep correct than seven.
    /// <para>
    /// The scratch sets are native and persistent, so draining events per tick allocates nothing.
    /// </para>
    /// </remarks>
    public sealed class RtsStatWorld : IDisposable
    {
        private StatStore<RtsStatSystem.Stat, RtsStatSystem.StatModifier, RtsStatSystem.StatObserver> _store;
        private RtsStatSystem.Accessor _accessor;
        private RtsStatSystem.WorldData _worldData;

        private StatStoreDebug<
            RtsStatSystem.ValuePair, RtsStatSystem.Stat
            , RtsStatSystem.StatModifier, RtsStatSystem.StatObserver> _debug;

        /// <summary>Distinct stats touched by the events drained in one pass.</summary>
        private ArraySetNative<StatHandle> _changedStats;

        /// <summary>
        /// Modifiers that reported a missing source, kept until something prunes them.
        /// </summary>
        /// <remarks>
        /// A set rather than a list: one modifier can raise its trigger several times in a single
        /// propagation, and removing an already-removed handle would inflate every count that follows.
        /// </remarks>
        private ArraySetNative<StatModifierHandle> _dangling;

        public RtsStatWorld(int ownerCapacity)
        {
            _store = new StatStore<RtsStatSystem.Stat, RtsStatSystem.StatModifier, RtsStatSystem.StatObserver>(
                initialOwnerCapacity: ownerCapacity, Allocator.Persistent);

            _accessor = new RtsStatSystem.Accessor(_store);
            _worldData = new RtsStatSystem.WorldData(ownerCapacity * 2, Allocator.Persistent);

            _changedStats = new ArraySetNative<StatHandle>(256, Allocator.Persistent);
            _dangling = new ArraySetNative<StatModifierHandle>(64, Allocator.Persistent);
        }

        /// <summary>
        /// Publishes this store to <c>ApexionGame ▸ Stats ▸ Stat Debugger</c>.
        /// </summary>
        /// <remarks>
        /// Separate from the constructor because the resolver that names stats needs the match to exist first —
        /// a bare <see cref="StatOwnerHandle"/> says nothing about which collection built it.
        /// </remarks>
        public void RegisterDebugView(string name, Func<StatOwnerHandle, int, string> statNameResolver)
        {
            if (_debug != null || string.IsNullOrEmpty(name))
            {
                return;
            }

            _debug = new(name, _store, statNameResolver);
            StatDebugRegistry.Register(_debug);
        }

        public bool IsCreated => _store.IsCreated;

        public int OwnerCount => _store.IsCreated ? _store.OwnerCount : 0;

        /// <remarks>
        /// Order matters and it is not obvious: unregister the debug view before the store it points at,
        /// then the world data (which owns the scratch lists the store's buffers are read into), then the
        /// store.
        /// </remarks>
        public void Dispose()
        {
            if (_debug != null)
            {
                StatDebugRegistry.Unregister(_debug);
                _debug = null;
            }

            if (_changedStats.IsCreated)
            {
                _changedStats.Dispose();
            }

            if (_dangling.IsCreated)
            {
                _dangling.Dispose();
            }

            if (_worldData.IsCreated)
            {
                _worldData.Dispose();
            }

            if (_store.IsCreated)
            {
                _store.Dispose();
            }

            _worldData = default;
            _store = default;
            _accessor = default;
        }

        // ---- owners ----------------------------------------------------------------------------

        /// <remarks>
        /// Through the generated builder rather than <c>store.CreateOwner()</c>, because the builder is what
        /// seeds the None stat at index 0. Handles do not work without it.
        /// </remarks>
        public TeamStats CreateTeamOwner(out StatOwnerHandle owner)
            => TeamStats.Builder
                .Build(ref _store, out owner)
                .CreateAllStats(produceChangeEvents: true)
                .ToStats();

        public UnitStats CreateUnitOwner(out StatOwnerHandle owner)
            => UnitStats.Builder
                .Build(ref _store, out owner)
                .CreateAllStats(produceChangeEvents: true)
                .ToStats();

        /// <remarks>
        /// Clears the owner's buffers and pools the slot; it does not reach into other owners. Whatever this
        /// owner put on <i>somebody else</i> has to be removed first — see <see cref="RtsDeathSystem"/>.
        /// </remarks>
        public void DestroyOwner(in StatOwnerHandle owner) => _store.DestroyOwner(owner);

        public bool Exists(in StatOwnerHandle owner) => _store.Exists(owner);

        // ---- values ----------------------------------------------------------------------------

        public float Read(in StatHandle handle)
            => _accessor.TryGetStatValue(handle, out var pair)
                ? pair.GetCurrentValueOrDefault(new StatVariant(0f)).Float
                : 0f;

        public float ReadBase(in StatHandle handle)
            => _accessor.TryGetStatValue(handle, out var pair)
                ? pair.GetBaseValueOrDefault().Float
                : 0f;

        /// <summary>
        /// Writes a base value, which recalculates the stat and propagates before returning.
        /// </summary>
        /// <remarks>There is no commit step and no deferred flush. That is the whole point of the library.</remarks>
        public bool Write(in StatHandle handle, float value)
            => _accessor.TrySetStatBaseValue(
                  handle
                , new RtsStatSystem.ValuePair(new StatVariant(value))
                , ref _worldData);

        /// <summary>
        /// Writes a unit's whole sheet in one batched call.
        /// </summary>
        /// <remarks>
        /// The batch groups by owner internally and propagates once, which is what you want when a wave of
        /// units spawns in a frame. Omitted options keep their current value rather than being zeroed.
        /// </remarks>
        public void WriteUnitSheet(
              in StatOwnerHandle owner
            , in UnitStats stats
            , in UnitStats.Options.Data values
        )
        {
            UnitStats.Accessor
                .Create(owner, stats, _accessor, _worldData)
                .TrySetBaseValueToStats(values);
        }

        // ---- modifiers -------------------------------------------------------------------------

        /// <summary>
        /// Adds a modifier. False means the affected stat is missing, an observed stat is missing, or the
        /// modifier would close a cycle — and in all three cases the store is untouched.
        /// </summary>
        public bool TryAddModifier(
              in StatHandle affected
            , in RtsStatSystem.StatModifier modifier
            , out StatModifierHandle handle
        )
            => _accessor.TryAddStatModifier(affected, modifier, out handle, ref _worldData);

        /// <remarks>
        /// Failure is normal, not an error: a modifier that sat on a unit which has since died went with its
        /// owner. Counting the successes is how the sample shows whether its own bookkeeping leaks.
        /// </remarks>
        public bool RemoveModifier(in StatModifierHandle handle)
            => _accessor.TryRemoveStatModifier(handle, ref _worldData);

        public int ModifierCount(in StatHandle handle)
            => _accessor.TryGetModifierCount(handle, out var count) ? count : 0;

        /// <summary>Reverse edges: who has to be recalculated when this stat changes.</summary>
        public int ObserverCount(in StatHandle handle)
            => _accessor.TryGetObserverCount(handle, out var count) ? count : 0;

        public bool TryReadModifiers(
              in StatHandle handle
            , NativeList<RtsStatSystem.StatModifierRecord> into
        )
            => _accessor.TryGetModifiersOfStat(handle, into);

        // ---- events ----------------------------------------------------------------------------

        public void ClearChangeEvents() => _worldData.ClearStatChangeEvents();

        /// <summary>
        /// How many distinct stats the accumulated events touched, then empties the stream.
        /// </summary>
        /// <remarks>
        /// Distinct, not the raw event count: one write can emit several events for the same stat where
        /// branches of unequal length meet, and that is correct behaviour rather than a bug to hide.
        /// </remarks>
        public int DrainChangedStats()
        {
            DrainChangeEvents(out var changedStats, out _);
            return changedStats;
        }

        public void DrainChangeEvents(out int changedStats, out int rawEvents)
        {
            var events = _worldData.GetStatChangeEvents(Allocator.Temp);

            _changedStats.Clear();

            for (var i = 0; i < events.Length; i++)
            {
                _changedStats.Add(events[i].statHandle);
            }

            rawEvents = events.Length;
            changedStats = _changedStats.Count;

            events.Dispose();
            _worldData.ClearStatChangeEvents();
        }

        /// <summary>Moves this tick's modifier triggers into the dangling set and empties the stream.</summary>
        public void DrainModifierTriggers()
        {
            var triggers = _worldData.GetModifierTriggerEvents(Allocator.Temp);

            for (var i = 0; i < triggers.Length; i++)
            {
                _dangling.Add(triggers[i].handle);
            }

            triggers.Dispose();
            _worldData.ClearModifierTriggerEvents();
        }

        public int DanglingCount => _dangling.IsCreated ? _dangling.Count : 0;

        /// <summary>
        /// Removes every modifier that reported a missing source, reporting the handles that actually went.
        /// </summary>
        /// <remarks>
        /// This cleans up modifiers that <i>read</i> a dead owner. It cannot clean up observer entries that a
        /// sloppy death left on a live node: those belong to modifiers that no longer exist, so no handle can
        /// reach them. There is no repair for that after the fact.
        /// </remarks>
        public int PruneDangling(FasterList<StatModifierHandle> removed)
        {
            removed.Clear();

            foreach (var handle in _dangling)
            {
                if (RemoveModifier(handle))
                {
                    removed.Add(handle);
                }
            }

            var reported = _dangling.Count;

            _dangling.Clear();

            return reported;
        }

        // ---- jobs ------------------------------------------------------------------------------

        /// <summary>
        /// Recalculates the given stats through the generated <c>[BurstCompile]</c> job.
        /// </summary>
        /// <remarks>
        /// <c>Run</c> rather than <c>Schedule</c>: there is nothing here to overlap with, and Schedule would
        /// need a Complete before anything could read the store again. The job clears the list when it
        /// finishes, so the count has to be taken first.
        /// </remarks>
        public int Recalculate(NativeList<StatHandle> handles)
        {
            var scheduled = handles.Length;

            new RtsStatSystem.DeferredUpdateStatListJob(_accessor, _worldData, handles).Run();

            return scheduled;
        }
    }
}

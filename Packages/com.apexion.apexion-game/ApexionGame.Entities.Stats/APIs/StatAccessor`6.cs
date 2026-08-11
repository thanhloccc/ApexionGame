// MIT License
//
// Copyright (c) 2023 Philippe St-Amand
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EncosyTower.Collections;
using Unity.Collections;
using UnityEngine;

using static ApexionGame.Entities.Stats.Debugging.ValidationDefines;

namespace ApexionGame.Entities.Stats
{
    public partial struct StatAccessor<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver, TValuePairComposer>
        where TValuePair : unmanaged, IStatValuePair, IEquatable<TValuePair>
        where TStat : unmanaged, IStat<TValuePair>
        where TStatModifier : unmanaged, IStatModifier<TValuePair, TStat, TStatModifierStack>
        where TStatModifierStack : unmanaged, IStatModifierStack<TValuePair, TStat>
        where TStatObserver : unmanaged, IStatObserver
        where TValuePairComposer : unmanaged, IStatValuePairComposer<TValuePair>
    {
        internal StatStore<TStat, TStatModifier, TStatObserver> _store;
        internal TValuePairComposer _valuePairComposer;
        internal TStat _nullStat;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatAccessor(StatStore<TStat, TStatModifier, TStatObserver> store)
            : this(store, default)
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatAccessor(
              StatStore<TStat, TStatModifier, TStatObserver> store
            , TValuePairComposer valuePairComposer
        )
        {
            _store = store;
            _valuePairComposer = valuePairComposer;
            _nullStat = default;
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _store.IsCreated;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCreateStatHandle<TStatData>(
              StatOwnerHandle owner
            , TValuePair valuePair
            , bool produceChangeEvents
            , uint userData
            , out StatHandle<TStatData> statHandle
            , out TStatData statData
        )
            where TStatData : unmanaged, IStatData
        {
            if (_store.TryGetStats(owner, out var statBuffer))
            {
                var modifierRangeStart = GetModifierBufferLength(owner);
                var observerRangeStart = GetObserverBufferLength(owner);

                statHandle = StatAPI.CreateStatHandle<TValuePair, TStat, TStatData, TValuePairComposer>(
                      owner
                    , valuePair
                    , produceChangeEvents
                    , userData
                    , statBuffer
                    , modifierRangeStart
                    , observerRangeStart
                    , out statData
                    , _valuePairComposer
                );

                return true;
            }

            statData = default;
            statHandle = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCreateStatHandle<TStatData>(
              StatOwnerHandle owner
            , TValuePair valuePair
            , bool produceChangeEvents
            , uint userData
            , out StatHandle<TStatData> statHandle
        )
            where TStatData : unmanaged, IStatData
        {
            if (_store.TryGetStats(owner, out var statBuffer))
            {
                var modifierRangeStart = GetModifierBufferLength(owner);
                var observerRangeStart = GetObserverBufferLength(owner);

                statHandle = StatAPI.CreateStatHandle<TValuePair, TStat, TStatData, TValuePairComposer>(
                      owner
                    , valuePair
                    , produceChangeEvents
                    , userData
                    , statBuffer
                    , modifierRangeStart
                    , observerRangeStart
                    , out _
                    , _valuePairComposer
                );

                return true;
            }

            statHandle = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCreateStatHandle<TStatData>(
              StatOwnerHandle owner
            , TStatData statData
            , bool produceChangeEvents
            , uint userData
            , out StatHandle<TStatData> statHandle
        )
            where TStatData : unmanaged, IStatData
        {
            if (_store.TryGetStats(owner, out var statBuffer))
            {
                var modifierRangeStart = GetModifierBufferLength(owner);
                var observerRangeStart = GetObserverBufferLength(owner);

                statHandle = StatAPI.CreateStatHandle<TValuePair, TStat, TStatData, TValuePairComposer>(
                      owner
                    , statData
                    , produceChangeEvents
                    , userData
                    , statBuffer
                    , modifierRangeStart
                    , observerRangeStart
                    , _valuePairComposer
                );

                return true;
            }

            statHandle = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCreateStatHandle(
              StatOwnerHandle owner
            , TValuePair valuePair
            , bool produceChangeEvents
            , uint userData
            , out StatHandle statHandle
        )
        {
            if (_store.TryGetStats(owner, out var statBuffer))
            {
                var modifierRangeStart = GetModifierBufferLength(owner);
                var observerRangeStart = GetObserverBufferLength(owner);

                statHandle = StatAPI.CreateStatHandle(
                      owner
                    , valuePair
                    , produceChangeEvents
                    , userData
                    , statBuffer
                    , modifierRangeStart
                    , observerRangeStart
                );

                return true;
            }

            statHandle = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetStatData<TStatData>(StatHandle<TStatData> statHandle, out TStatData statData)
            where TStatData : unmanaged, IStatData
        {
            return StatAPI.TryGetStatData<TValuePair, TStat, TStatData>(
                  statHandle
                , _store.AsStatLookup()
                , out statData
            );
        }

        /// <remarks>
        /// Assumes the "statBuffer" is on the owner of the statHandle
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetStatData<TStatData>(
              StatHandle<TStatData> statHandle
            , ReadOnlySpan<TStat> statBuffer
            , out TStatData statData
        )
            where TStatData : unmanaged, IStatData
        {
            return StatAPI.TryGetStatData<TValuePair, TStat, TStatData>(statHandle, statBuffer, out statData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetStat(StatHandle statHandle, out TStat stat)
        {
            return StatAPI.TryGetStat<TValuePair, TStat>(statHandle, _store.AsStatLookup(), out stat);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly NativeArray<TStat>.ReadOnly GetStats(StatOwnerHandle owner)
        {
            return StatAPI.GetStats<TValuePair, TStat>(owner, _store.AsStatLookup());
        }

        /// <remarks>
        /// Assumes the "statBuffer" is on the owner of the statHandle
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetStat(
              StatHandle statHandle
            , ReadOnlySpan<TStat> statBuffer
            , out TStat stat
        )
        {
            return StatAPI.TryGetStat<TValuePair, TStat>(statHandle, statBuffer, out stat);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetStatValue(StatHandle statHandle, out TValuePair valuePair)
        {
            return StatAPI.TryGetStatValue<TValuePair, TStat>(statHandle, _store.AsStatLookup(), out valuePair);
        }

        /// <remarks>
        /// Assumes the "statBuffer" is on the owner of the statHandle
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetStatValue(
              StatHandle statHandle
            , ReadOnlySpan<TStat> statBuffer
            , out TValuePair valuePair
        )
        {
            return StatAPI.TryGetStatValue<TValuePair, TStat>(statHandle, statBuffer, out valuePair);
        }

        public bool TrySetStatBaseValue(
              StatHandle statHandle
            , in TValuePair value
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  statHandle
                , _store.AsStatLookup()
                , out var statBuffer
                , out bool success
                , ref _nullStat
            );

            var owner = statHandle.owner;

            if (success == false
                || _store.TryGetModifiers(owner, out var modifierBuffer) == false
                || _store.TryGetObservers(owner, out var observerBuffer) == false
            )
            {
                return false;
            }

            var result = statRef.TrySetBaseValue(value.GetBaseValueOrDefault());

            if (result)
            {
                UpdateStatRef(
                      statHandle
                    , ref statRef
                    , statBuffer
                    , modifierBuffer
                    , observerBuffer
                    , ref worldData
                );
            }

            return result;
        }

        /// <remarks>
        /// Assumes the "statBuffer" is on the owner of the statHandle
        /// </remarks>
        public bool TrySetStatBaseValue(
              StatHandle statHandle
            , in TValuePair value
            , StatBuffer<TStat> statBuffer
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            var owner = statHandle.owner;

            ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  statHandle
                , statBuffer
                , out bool success
                , ref _nullStat
            );

            if (success == false
                || _store.TryGetModifiers(owner, out var modifierBuffer) == false
                || _store.TryGetObservers(owner, out var observerBuffer) == false
            )
            {
                return false;
            }

            var result = statRef.TrySetBaseValue(value.GetBaseValueOrDefault());

            if (result)
            {
                UpdateStatRef(
                      statHandle
                    , ref statRef
                    , statBuffer
                    , modifierBuffer
                    , observerBuffer
                    , ref worldData
                );
            }

            return result;
        }

        public bool TrySetStatCurrentValue(
              StatHandle statHandle
            , in TValuePair value
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  statHandle
                , _store.AsStatLookup()
                , out var statBuffer
                , out bool success
                , ref _nullStat
            );

            var owner = statHandle.owner;

            if (success == false
                || _store.TryGetModifiers(owner, out var modifierBuffer) == false
                || _store.TryGetObservers(owner, out var observerBuffer) == false
            )
            {
                return false;
            }

            var result = statRef.TrySetCurrentValue(value.GetCurrentValueOrDefault());

            if (result)
            {
                UpdateStatRef(
                      statHandle
                    , ref statRef
                    , statBuffer
                    , modifierBuffer
                    , observerBuffer
                    , ref worldData
                );
            }

            return result;
        }

        /// <remarks>
        /// Assumes the "statBuffer" is on the owner of the statHandle
        /// </remarks>
        public bool TrySetStatCurrentValue(
              StatHandle statHandle
            , in TValuePair value
            , StatBuffer<TStat> statBuffer
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            var owner = statHandle.owner;

            ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  statHandle
                , statBuffer
                , out bool success
                , ref _nullStat
            );

            if (success == false
                || _store.TryGetModifiers(owner, out var modifierBuffer) == false
                || _store.TryGetObservers(owner, out var observerBuffer) == false
            )
            {
                return false;
            }

            var result = statRef.TrySetCurrentValue(value.GetCurrentValueOrDefault());

            if (result)
            {
                UpdateStatRef(
                      statHandle
                    , ref statRef
                    , statBuffer
                    , modifierBuffer
                    , observerBuffer
                    , ref worldData
                );
            }

            return result;
        }

        public void TryUpdateAllStats(
              StatOwnerHandle owner
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            if (_store.TryGetStats(owner, out var statBuffer) == false)
            {
                return;
            }

            var length = statBuffer.Length;

            // Start from 1 because index 0 is the "None stat" (which is not a real stat).
            for (var i = 1; i < length; i++)
            {
                TryUpdateStat(new StatHandle(owner, i), ref worldData);
            }
        }

        /// <remarks>
        /// If the stat doesn't exist, it just does nothing (no error).
        /// <para>
        /// Deliberately has no visited set. A stat re-entering the worklist is not waste — on a DAG
        /// whose two branches into a join have different lengths, the second visit is what repairs
        /// the value computed while the longer branch was still stale. See DEC-005 / D-01.
        /// </para>
        /// </remarks>
        public void TryUpdateStat(
              StatHandle statHandle
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            ThrowHelper.ThrowIfStatWorldDataIsNotCreated(worldData.IsCreated);

            var lookupStats = _store.AsStatLookup();
            var valuePairComposer = _valuePairComposer;
            var statReader = new StatReader<TValuePair, TStat>(lookupStats);
            var tmpGlobalUpdatedStats = worldData._tmpGlobalUpdatedStats;
            var tmpSameOwnerUpdatedStats = worldData._tmpSameOwnerUpdatedStats;

            tmpGlobalUpdatedStats.Clear();
            tmpGlobalUpdatedStats.Add(statHandle);

            for (var i = 0; i < tmpGlobalUpdatedStats.Length; i++)
            {
                var newStatHandle = tmpGlobalUpdatedStats[i];
                var newOwner = newStatHandle.owner;

                ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                      newStatHandle
                    , lookupStats
                    , out var statBuffer
                    , out bool getStatSuccess
                    , ref _nullStat
                );

                if (getStatSuccess == false
                    || _store.TryGetModifiers(newOwner, out var modifierBuffer) == false
                    || _store.TryGetObservers(newOwner, out var observerBuffer) == false
                )
                {
                    continue;
                }

                tmpSameOwnerUpdatedStats.Clear();

                StatAPI.UpdateSingleStatCommon(
                      newStatHandle
                    , statReader
                    , ref statRef
                    , modifierBuffer
                    , observerBuffer.AsReadOnlySpan()
                    , ref worldData
                    , valuePairComposer
                );

                // Then update same-owner list
                for (var s = 0; s < tmpSameOwnerUpdatedStats.Length; s++)
                {
                    var sameOwnerStatHandle = tmpSameOwnerUpdatedStats[s];

                    ref TStat sameOwnerStatRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                          sameOwnerStatHandle
                        , statBuffer
                        , out _
                        , ref _nullStat
                    );

                    StatAPI.UpdateSingleStatCommon(
                          sameOwnerStatHandle
                        , statReader
                        , ref sameOwnerStatRef
                        , modifierBuffer
                        , observerBuffer.AsReadOnlySpan()
                        , ref worldData
                        , valuePairComposer
                    );
                }
            }
        }

        /// <remarks>
        /// No visited set, for the same reason as <see cref="TryUpdateStat"/> — see DEC-005 / D-01.
        /// </remarks>
        internal void TryUpdateStatAssumeSingleOwner(
              StatHandle statHandle
            , StatBuffer<TStat> statBuffer
            , StatBuffer<TStatModifier> modifierBuffer
            , StatBuffer<TStatObserver> observerBuffer
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            ThrowHelper.ThrowIfStatWorldDataIsNotCreated(worldData.IsCreated);

            var statReader = new StatReader<TValuePair, TStat>(statBuffer);
            var tmpGlobalUpdatedStats = worldData._tmpGlobalUpdatedStats;
            var tmpSameOwnerUpdatedStats = worldData._tmpSameOwnerUpdatedStats;
            var valuePairComposer = _valuePairComposer;

            tmpGlobalUpdatedStats.Clear();
            tmpSameOwnerUpdatedStats.Clear();

            ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  statHandle
                , statBuffer
                , out bool getStatSuccess
                , ref _nullStat
            );

            if (getStatSuccess)
            {
                StatAPI.UpdateSingleStatCommon(
                      statHandle
                    , statReader
                    , ref statRef
                    , modifierBuffer
                    , observerBuffer.AsReadOnlySpan()
                    , ref worldData
                    , valuePairComposer
                );

                // Then update same-owner list
                for (var s = 0; s < tmpSameOwnerUpdatedStats.Length; s++)
                {
                    var sameOwnerStatHandle = tmpSameOwnerUpdatedStats[s];

                    ref TStat sameOwnerStatRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                          sameOwnerStatHandle
                        , statBuffer
                        , out _
                        , ref _nullStat
                    );

                    StatAPI.UpdateSingleStatCommon(
                          sameOwnerStatHandle
                        , statReader
                        , ref sameOwnerStatRef
                        , modifierBuffer
                        , observerBuffer.AsReadOnlySpan()
                        , ref worldData
                        , valuePairComposer
                    );
                }
            }

            ThrowIfGlobalUpdateListIsNotEmpty(tmpGlobalUpdatedStats.Length == 0);
        }

        internal void UpdateStatRef(
              StatHandle statHandle
            , ref TStat initialStatRef
            , StatBuffer<TStat> initialStatBuffer
            , StatBuffer<TStatModifier> initialStatModifierBuffer
            , StatBuffer<TStatObserver> initialStatObserverBuffer
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            ThrowHelper.ThrowIfStatWorldDataIsNotCreated(worldData.IsCreated);

            var lookupStats = _store.AsStatLookup();
            var statReader = new StatReader<TValuePair, TStat>(lookupStats);
            var tmpGlobalUpdatedStats = worldData._tmpGlobalUpdatedStats;
            var tmpSameOwnerUpdatedStats = worldData._tmpSameOwnerUpdatedStats;
            var valuePairComposer = _valuePairComposer;

            tmpGlobalUpdatedStats.Clear();
            tmpSameOwnerUpdatedStats.Clear();

            // First update the current stat ref
            StatAPI.UpdateSingleStatCommon(
                  statHandle
                , statReader
                , ref initialStatRef
                , initialStatModifierBuffer
                , initialStatObserverBuffer.AsReadOnlySpan()
                , ref worldData
                , valuePairComposer
            );

            // Then update same-owner list
            for (var s = 0; s < tmpSameOwnerUpdatedStats.Length; s++)
            {
                var sameOwnerStatHandle = tmpSameOwnerUpdatedStats[s];

                ref TStat sameOwnerStatRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                      sameOwnerStatHandle
                    , initialStatBuffer
                    , out _
                    , ref _nullStat
                );

                StatAPI.UpdateSingleStatCommon(
                      sameOwnerStatHandle
                    , statReader
                    , ref sameOwnerStatRef
                    , initialStatModifierBuffer
                    , initialStatObserverBuffer.AsReadOnlySpan()
                    , ref worldData
                    , valuePairComposer
                );
            }

            // Then update following stats
            for (var i = 0; i < tmpGlobalUpdatedStats.Length; i++)
            {
                var newStatHandle = tmpGlobalUpdatedStats[i];
                var newOwner = newStatHandle.owner;

                ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                      newStatHandle
                    , lookupStats
                    , out var statBuffer
                    , out bool getStatSuccess
                    , ref _nullStat
                );

                if (getStatSuccess == false
                    || _store.TryGetModifiers(newOwner, out var modifierBuffer) == false
                    || _store.TryGetObservers(newOwner, out var observerBuffer) == false
                )
                {
                    continue;
                }

                tmpSameOwnerUpdatedStats.Clear();

                StatAPI.UpdateSingleStatCommon(
                      newStatHandle
                    , statReader
                    , ref statRef
                    , modifierBuffer
                    , observerBuffer.AsReadOnlySpan()
                    , ref worldData
                    , valuePairComposer
                );

                // Then update same-owner list
                for (var s = 0; s < tmpSameOwnerUpdatedStats.Length; s++)
                {
                    var sameOwnerStatHandle = tmpSameOwnerUpdatedStats[s];

                    ref TStat sameOwnerStatRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                          sameOwnerStatHandle
                        , statBuffer
                        , out _
                        , ref _nullStat
                    );

                    StatAPI.UpdateSingleStatCommon(
                          sameOwnerStatHandle
                        , statReader
                        , ref sameOwnerStatRef
                        , modifierBuffer
                        , observerBuffer.AsReadOnlySpan()
                        , ref worldData
                        , valuePairComposer
                    );
                }
            }
        }

        /// <remarks>
        /// Does not clear the list <paramref name="modifiers"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetModifiersOfStat(
              StatHandle statHandle
            , NativeList<StatModifierRecord<TValuePair, TStat, TStatModifier, TStatModifierStack>> modifiers
        )
        {
            return StatAPI.TryGetModifiersOfStat(
                  statHandle
                , _store.AsStatLookup()
                , _store.AsModifierLookup()
                , modifiers
            );
        }

        /// <remarks>
        /// Does not clear the list <paramref name="observers"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetObserversOfStat(StatHandle statHandle, NativeList<TStatObserver> observers)
        {
            return StatAPI.TryGetObserversOfStat<TValuePair, TStat, TStatObserver>(
                  statHandle
                , _store.AsStatLookup()
                , _store.AsObserverLookup()
                , observers
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetModifierCount(StatHandle statHandle, out int modifierCount)
        {
            return StatAPI.TryGetModifierCount<TValuePair, TStat>(
                  statHandle
                , _store.AsStatLookup()
                , out modifierCount
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetObserverCount(StatHandle statHandle, out int observerCount)
        {
            return StatAPI.TryGetObserverCount<TValuePair, TStat>(
                  statHandle
                , _store.AsStatLookup()
                , out observerCount
            );
        }

        /// <remarks>
        /// Does not clear the list <paramref name="observers"/>.
        /// <br/>
        /// Useful to store observers before destroying an owner, and then manually update all observers after destroy.
        /// <br/>
        /// An observers update isn't automatically called when a stats owner is destroyed.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetAllObservers(StatOwnerHandle owner, NativeList<TStatObserver> observers)
        {
            return StatAPI.TryGetAllObservers(owner, _store.AsObserverLookup(), observers);
        }

        public bool TryAddStatModifier(
              StatHandle affectedStatHandle
            , TStatModifier modifier
            , out StatModifierHandle statModifierHandle
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            var owner = affectedStatHandle.owner;

            ref TStat affectedStatRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  affectedStatHandle
                , _store.AsStatLookup()
                , out var statBuffer
                , out bool getStatSuccess
                , ref _nullStat
            );

            if (getStatSuccess == false
                || _store.Exists(owner) == false
                || _store.TryGetModifiers(owner, out var modifierBufferOnAffectedOwner) == false
                || _store.TryGetObservers(owner, out var observerBufferOnAffectedOwner) == false
            )
            {
                statModifierHandle = default;
                return false;
            }

            return TryAddStatModifier(
                  affectedStatHandle
                , modifier
                , ref affectedStatRef
                , statBuffer
                , modifierBufferOnAffectedOwner
                , observerBufferOnAffectedOwner
                , out statModifierHandle
                , ref worldData
                , isGuaranteedSingleOwner: false
                , recalculateStat: true
                , deferRangeShift: false
            );
        }

        public bool TryAddStatModifiersBatch(
              StatHandle affectedStatHandle
            , ReadOnlySpan<TStatModifier> modifiers
            , NativeList<StatModifierHandle> statModifierHandles
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            var owner = affectedStatHandle.owner;

            ref TStat affectedStatRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  affectedStatHandle
                , _store.AsStatLookup()
                , out var statBuffer
                , out bool getStatSuccess
                , ref _nullStat
            );

            if (getStatSuccess == false
                || _store.Exists(owner) == false
                || _store.TryGetModifiers(owner, out var modifierBufferOnAffectedOwner) == false
                || _store.TryGetObservers(owner, out var observerBufferOnAffectedOwner) == false
            )
            {
                return false;
            }

            bool success = true;

            statModifierHandles.IncreaseCapacityTo(statModifierHandles.Length + modifiers.Length);

            var statModifierHandlesLength = statModifierHandles.Length;

            for (var i = 0; i < modifiers.Length; i++)
            {
                // NOTE: deferRangeShift: true
                // While deferred, ModifierRange.startIndex of stats after the affected stat is stale during the batch.
                // This is safe only because nothing inside the batch reads other stats' modifier ranges,
                // loop detection walks observers only, and recalculateStat is false for every call in the batch.
                // Any future change that reads modifier ranges mid-batch must shift first.

                var result = TryAddStatModifier(
                      affectedStatHandle
                    , modifiers[i]
                    , ref affectedStatRef
                    , statBuffer
                    , modifierBufferOnAffectedOwner
                    , observerBufferOnAffectedOwner
                    , out StatModifierHandle statModifierHandle
                    , ref worldData
                    , isGuaranteedSingleOwner: false
                    , recalculateStat: false
                    , deferRangeShift: true
                );

                if (result)
                {
                    statModifierHandles.Add(statModifierHandle);
                }

                success &= result;
            }

            var acceptedCount = statModifierHandles.Length - statModifierHandlesLength;

            // Single range shift for all accepted modifiers of this batch.
            if (acceptedCount > 0)
            {
                for (var n = affectedStatHandle.index + 1; n < statBuffer.Length; n++)
                {
                    ref TStat nextStatRef = ref statBuffer.ElementAt(n);

                    var nextModifierRange = nextStatRef.ModifierRange;
                    nextModifierRange.startIndex += acceptedCount;
                    nextStatRef.ModifierRange = nextModifierRange;
                }
            }

            UpdateStatRef(
                  affectedStatHandle
                , ref affectedStatRef
                , statBuffer
                , modifierBufferOnAffectedOwner
                , observerBufferOnAffectedOwner
                , ref worldData
            );

            return success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryAddStatModifierSingleOwner(
              StatHandle affectedStatHandle
            , TStatModifier modifier
            , StatBuffer<TStat> statBufferOnAffectedOwner
            , StatBuffer<TStatModifier> modifierBufferOnAffectedOwner
            , StatBuffer<TStatObserver> observerBufferOnAffectedOwner
            , out StatModifierHandle statModifierHandle
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            ref TStat affectedStatRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  affectedStatHandle
                , statBufferOnAffectedOwner
                , out bool getStatSuccess
                , ref _nullStat
            );

            if (getStatSuccess)
            {
                return TryAddStatModifier(
                      affectedStatHandle
                    , modifier
                    , ref affectedStatRef
                    , statBufferOnAffectedOwner
                    , modifierBufferOnAffectedOwner
                    , observerBufferOnAffectedOwner
                    , out statModifierHandle
                    , ref worldData
                    , isGuaranteedSingleOwner: true
                    , recalculateStat: true
                    , deferRangeShift: false
                );
            }

            statModifierHandle = default;
            return false;
        }

        internal bool TryAddStatModifier(
              StatHandle affectedStatHandle
            , TStatModifier modifier
            , ref TStat affectedStatRef
            , StatBuffer<TStat> statBufferOnAffectedOwner
            , StatBuffer<TStatModifier> modifierBufferOnAffectedOwner
            , StatBuffer<TStatObserver> observerBufferOnAffectedOwner
            , out StatModifierHandle statModifierHandle
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
            , bool isGuaranteedSingleOwner
            , bool recalculateStat
            , bool deferRangeShift
        )
        {
            ThrowHelper.ThrowIfStatWorldDataIsNotCreated(worldData.IsCreated);

            var tmpStatObservers = worldData._tmpStatObservers;
            var tmpModifierObservedStats = worldData._tmpModifierObservedStats;
            var tmpVisitedObserverHandles = worldData._tmpVisitedObserverHandles;
            var lookupStats = _store.AsStatLookup();
            var lookupObservers = _store.AsObserverLookup();

            // Ensure lists are created and cleared
            tmpStatObservers.Clear();
            tmpModifierObservedStats.Clear();
            tmpVisitedObserverHandles.Clear();

            // Get observed stats of modifier
            modifier.AddObservedStatsToList(tmpModifierObservedStats);

            // Validate that every observed stat is reachable.
            // Otherwise the modifier would be added but never react to its observed stats — fail up front instead.
            for (var k = 0; k < tmpModifierObservedStats.Length; k++)
            {
                var observedStatHandle = tmpModifierObservedStats[k];

                if (isGuaranteedSingleOwner)
                {
                    if (observedStatHandle.owner != affectedStatHandle.owner
                        || observedStatHandle.index.IsValidInRange(statBufferOnAffectedOwner.Length) == false
                    )
                    {
                        statModifierHandle = default;
                        return false;
                    }
                }
                else if (lookupStats.TryGetBuffer(observedStatHandle.owner, out var observedStatBuffer) == false
                    || lookupObservers.TryGetBuffer(observedStatHandle.owner, out _) == false
                    || observedStatHandle.index.IsValidInRange(observedStatBuffer.Length) == false
                )
                {
                    statModifierHandle = default;
                    return false;
                }
            }

            // Increment modifier Id (local to owner)
            if (_store.TryIncrementModifierId(affectedStatHandle.owner, out var modifierId) == false)
            {
                statModifierHandle = default;
                return false;
            }

            modifier.Id = modifierId;

            statModifierHandle = new StatModifierHandle {
                affectedStatHandle = affectedStatHandle,
                modifierId = modifier.Id,
            };

            var modifierCanBeAdded = true;

            // If the modifier observes any stats, handle infinite observer loops detection
            if (tmpModifierObservedStats.Length > 0)
            {
                // Make sure the modifier wouldn't make the stat observe itself (would cause infinite loop)
                for (var k = 0; k < tmpModifierObservedStats.Length; k++)
                {
                    var modifierObservedStatHandle = tmpModifierObservedStats[k];

                    if (affectedStatHandle == modifierObservedStatHandle)
                    {
                        modifierCanBeAdded = false;
                        break;
                    }
                }

                // Don't allow infinite observer loops.
                // Follow the chain of stats that would react to this stat's changes if the modifier was added (follow the
                // observers chain). If we end up finding this stat anywhere in the chain, it would cause an infinite loop.
                if (modifierCanBeAdded)
                {
                    // Start by adding the affected stat's observers
                    StatAPI.AddObserversOfStatToList<TStatObserver>(
                          affectedStatRef.ObserverRange
                        , observerBufferOnAffectedOwner.AsReadOnlySpan()
                        , tmpStatObservers
                    );

                    for (var i = 0; i < tmpStatObservers.Length; i++)
                    {
                        var iteratedObserverStatHandle = tmpStatObservers[i].ObserverHandle;
                        var iteratedOwner = iteratedObserverStatHandle.owner;

                        // If we find the affected stat down the chain of stats that it observes,
                        // it would create an infinite loop. Prevent adding modifier.
                        if (iteratedObserverStatHandle == affectedStatHandle)
                        {
                            modifierCanBeAdded = false;
                            break;
                        }

                        // Each unique observer stat only needs to be expanded once;
                        // re-expanding duplicates can blow up the walk exponentially.
                        if (tmpVisitedObserverHandles.Add(iteratedObserverStatHandle) == false)
                        {
                            continue;
                        }

                        // Add the affected stat to the observers chain list if the iterated observer is
                        // an observed stat of the modifier. Because if we proceed with adding the modifier, the
                        // affected stat would be added as an observer of all modifier observed stats
                        for (var k = 0; k < tmpModifierObservedStats.Length; k++)
                        {
                            var modifierObservedStatHandle = tmpModifierObservedStats[k];

                            if (iteratedObserverStatHandle == modifierObservedStatHandle)
                            {
                                tmpStatObservers.Add(new TStatObserver {
                                    ObserverHandle = affectedStatHandle,
                                });
                            }
                        }

                        // Update buffers so they represent the ones on the observer owner
                        if (isGuaranteedSingleOwner)
                        {
                            if (StatAPI.TryGetStat<TValuePair, TStat>(
                                  iteratedObserverStatHandle
                                , statBufferOnAffectedOwner.AsReadOnlySpan()
                                , out TStat observerStat
                            ))
                            {
                                StatAPI.AddObserversOfStatToList(
                                      observerStat.ObserverRange
                                    , observerBufferOnAffectedOwner.AsReadOnlySpan()
                                    , tmpStatObservers
                                );
                            }
                        }
                        else if (lookupStats.TryGetBuffer(iteratedOwner, out var observerStatBuffer)
                            && lookupObservers.TryGetBuffer(iteratedOwner, out var observerStatObserverBuffer)
                            && StatAPI.TryGetStat<TValuePair, TStat>(
                                  iteratedObserverStatHandle
                                , observerStatBuffer.AsReadOnlySpan()
                                , out TStat observerStat
                            )
                        )
                        {
                            StatAPI.AddObserversOfStatToList(
                                  observerStat.ObserverRange
                                , observerStatObserverBuffer.AsReadOnlySpan()
                                , tmpStatObservers
                            );
                        }
                    }
                }
            }

            if (modifierCanBeAdded == false)
            {
                statModifierHandle = default;
                return false;
            }

            // Add modifier by inserting it at the end of the stat's modifiers sub-list
            {
                var modifierRange = affectedStatRef.ModifierRange;

                // IMPORTANT: modifiers must be sorted in affected stat order
                modifierBufferOnAffectedOwner.Insert(modifierRange.ExclusiveEnd, modifier);

                modifierRange.count++;
                affectedStatRef.ModifierRange = modifierRange;

                // update next stat start indexes
                // (skipped when the caller batches several adds and shifts once at the end)
                if (deferRangeShift == false)
                {
                    for (var n = affectedStatHandle.index + 1; n < statBufferOnAffectedOwner.Length; n++)
                    {
                        ref TStat nextStatRef = ref statBufferOnAffectedOwner.ElementAt(n);

                        var nextModifierRange = nextStatRef.ModifierRange;
                        nextModifierRange.startIndex++;
                        nextStatRef.ModifierRange = nextModifierRange;
                    }
                }
            }

            // Add affected stat as observer of all observed stats
            for (var i = 0; i < tmpModifierObservedStats.Length; i++)
            {
                var observedStatHandle = tmpModifierObservedStats[i];
                var observedOwner = observedStatHandle.owner;

                // Update buffers so they represent the ones on the observer owner
                if (isGuaranteedSingleOwner)
                {
                    StatAPI.AddStatAsObserverOfOtherStat<TValuePair, TStat, TStatObserver>(
                          affectedStatHandle
                        , observedStatHandle
                        , statBufferOnAffectedOwner
                        , observerBufferOnAffectedOwner
                    );
                }
                else if (lookupStats.TryGetBuffer(observedOwner, out var observedStatBuffer)
                    && lookupObservers.TryGetBuffer(observedOwner, out var observedStatObserverBuffer)
                )
                {
                    StatAPI.AddStatAsObserverOfOtherStat<TValuePair, TStat, TStatObserver>(
                          affectedStatHandle
                        , observedStatHandle
                        , observedStatBuffer
                        , observedStatObserverBuffer
                    );
                }
            }

            // Update stat following modifier add
            if (recalculateStat)
            {
                if (isGuaranteedSingleOwner)
                {
                    TryUpdateStatAssumeSingleOwner(
                          affectedStatHandle
                        , statBufferOnAffectedOwner
                        , modifierBufferOnAffectedOwner
                        , observerBufferOnAffectedOwner
                        , ref worldData
                    );
                }
                else
                {
                    TryUpdateStat(affectedStatHandle, ref worldData);
                }
            }

            return true;
        }

        public bool TryGetStatModifier(StatModifierHandle modifierHandle, out TStatModifier statModifier)
        {
            var handle = modifierHandle.affectedStatHandle;
            var owner = handle.owner;

            if (_store.TryGetStats(owner, out var statBuffer)
                && _store.TryGetModifiers(owner, out var modifierBuffer)
                && StatAPI.TryGetStat<TValuePair, TStat>(
                      handle
                    , statBuffer.AsReadOnlySpan()
                    , out TStat affectedStat
                )
                && affectedStat.ModifierRange.count > 0
            )
            {
                var modifierRange = affectedStat.ModifierRange;
                var end = modifierRange.ExclusiveEnd;

                for (var i = modifierRange.startIndex; i < end; i++)
                {
                    TStatModifier modifier = modifierBuffer[i];

                    if (modifier.Id == modifierHandle.modifierId)
                    {
                        statModifier = modifier;
                        return true;
                    }
                }
            }

            statModifier = default;
            return false;
        }

        public bool TryRemoveStatModifier(
              StatModifierHandle modifierHandle
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            ThrowHelper.ThrowIfStatWorldDataIsNotCreated(worldData.IsCreated);

            var affectedHandle = modifierHandle.affectedStatHandle;
            var affectedOwner = affectedHandle.owner;

            if (_store.TryGetStats(affectedOwner, out var statBufferOnAffectedOwner) == false
                || _store.TryGetModifiers(affectedOwner, out var modifierBufferOnAffectedOwner) == false
                || affectedHandle.index.IsValidInRange(statBufferOnAffectedOwner.Length) == false
            )
            {
                return false;
            }

            var tmpModifierObservedStats = worldData._tmpModifierObservedStats;
            ref TStat affectedStatRef = ref statBufferOnAffectedOwner.ElementAt(affectedHandle.index);
            var affectedModifierRange = affectedStatRef.ModifierRange;

            for (var i = affectedModifierRange.startIndex; i < affectedModifierRange.ExclusiveEnd; i++)
            {
                TStatModifier modifier = modifierBufferOnAffectedOwner[i];

                if (modifier.Id != modifierHandle.modifierId)
                {
                    continue;
                }

                // Remove modifier
                modifierBufferOnAffectedOwner.RemoveAt(i);

                // Update count
                affectedModifierRange.count--;
                affectedStatRef.ModifierRange = affectedModifierRange;

                ThrowIfNegativeRangeCount(affectedStatRef.ModifierRange.count >= 0);

                // Update start indexes of next stats
                for (var n = affectedHandle.index + 1; n < statBufferOnAffectedOwner.Length; n++)
                {
                    ref TStat nextStatRef = ref statBufferOnAffectedOwner.ElementAt(n);

                    var nextModifierRange = nextStatRef.ModifierRange;
                    nextModifierRange.startIndex--;
                    nextStatRef.ModifierRange = nextModifierRange;
                }

                // Remove the modifier's affected stat as an observer of the modifier's observed stats
                tmpModifierObservedStats.Clear();
                modifier.AddObservedStatsToList(tmpModifierObservedStats);

                for (var n = 0; n < tmpModifierObservedStats.Length; n++)
                {
                    var observedStatHandle = tmpModifierObservedStats[n];
                    var observedOwner = observedStatHandle.owner;

                    if (_store.TryGetStats(observedOwner, out var statBufferOnObservedOwner) == false
                        || _store.TryGetObservers(observedOwner, out var observerBufferOnObservedOwner) == false
                        || observedStatHandle.index.IsValidInRange(statBufferOnObservedOwner.Length) == false
                    )
                    {
                        continue;
                    }

                    ref TStat observedStatRef = ref statBufferOnObservedOwner.ElementAt(observedStatHandle.index);

                    var observerRange = observedStatRef.ObserverRange;

                    for (var k = observerRange.startIndex; k < observerRange.ExclusiveEnd; k++)
                    {
                        var observerOfObservedStat = observerBufferOnObservedOwner[k];

                        if (observerOfObservedStat.ObserverHandle != modifierHandle.affectedStatHandle)
                        {
                            continue;
                        }

                        // Remove
                        observerBufferOnObservedOwner.RemoveAt(k);

                        // Update counts
                        observerRange.count--;
                        observedStatRef.ObserverRange = observerRange;

                        ThrowIfNegativeRangeCount(observedStatRef.ObserverRange.count >= 0);

                        // Update start indexes of next stats
                        for (var t = observedStatHandle.index + 1; t < statBufferOnObservedOwner.Length; t++)
                        {
                            ref TStat nextStatRef = ref statBufferOnObservedOwner.ElementAt(t);

                            var nextObserverRange = nextStatRef.ObserverRange;
                            nextObserverRange.startIndex--;
                            nextStatRef.ObserverRange = nextObserverRange;
                        }

                        // Break so we don't remove all observer instances of this stat
                        break;
                    }
                }

                // Stat update following modifier remove
                TryUpdateStat(modifierHandle.affectedStatHandle, ref worldData);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes all modifiers of the stat, one at a time.
        /// </summary>
        /// <remarks>
        /// If an individual removal fails the method returns false immediately;
        /// modifiers already removed stay removed.
        /// With intact internal invariants an individual removal cannot fail
        /// once the stat and its buffers were resolved.
        /// </remarks>
        public bool TryRemoveModifiersOfStat(
              StatHandle statHandle
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  statHandle
                , _store.AsStatLookup()
                , out _
                , out bool getStatSuccess
                , ref _nullStat
            );

            if (getStatSuccess == false
                || _store.TryGetModifiers(statHandle.owner, out var modifierBuffer) == false
            )
            {
                return false;
            }

            while (statRef.ModifierRange.count > 0)
            {
                var modifiers = modifierBuffer.AsReadOnlySpan();
                TStatModifier modifier = modifiers[statRef.ModifierRange.startIndex];

                var handle = new StatModifierHandle {
                    affectedStatHandle = statHandle,
                    modifierId = modifier.Id,
                };

                if (TryRemoveStatModifier(handle, ref worldData) == false)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Destroys the owner, then recalculates every stat of *other* owners that depended on it.
        /// </summary>
        /// <remarks>
        /// <see cref="StatStore{TStat, TStatModifier, TStatObserver}.DestroyOwner"/> on its own does not
        /// touch observers — the upstream ECS version has the same gap. Stats left pointing at the
        /// destroyed owner resolve to nothing and are skipped during propagation, so this is a
        /// convenience, not a correctness requirement.
        /// </remarks>
        public bool DestroyOwnerAndUpdateObservers(
              StatOwnerHandle owner
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            if (_store.TryGetObservers(owner, out var observerBuffer) == false)
            {
                return false;
            }

            // Snapshot before destroying: the buffer is cleared by DestroyOwner.
            // A local list is used instead of worldData scratch because TryUpdateStat below reuses those.
            var dependentStats = new NativeList<StatHandle>(observerBuffer.Length, Allocator.Temp);

            StatAPI.GetOtherDependantStatsOfOwner<TStatObserver>(
                  owner
                , observerBuffer.AsReadOnlySpan()
                , dependentStats
            );

            if (_store.DestroyOwner(owner) == false)
            {
                dependentStats.Dispose();
                return false;
            }

            for (var i = 0; i < dependentStats.Length; i++)
            {
                TryUpdateStat(dependentStats[i], ref worldData);
            }

            dependentStats.Dispose();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly int GetModifierBufferLength(StatOwnerHandle owner)
            => _store.TryGetModifiers(owner, out var modifierBuffer) ? modifierBuffer.Length : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly int GetObserverBufferLength(StatOwnerHandle owner)
            => _store.TryGetObservers(owner, out var observerBuffer) ? observerBuffer.Length : 0;

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(STATS_CHECKS)]
        private static void ThrowIfNegativeRangeCount([DoesNotReturnIf(false)] bool nonNegative)
        {
            if (nonNegative == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new(
                    "A modifier or observer range count went negative. " +
                    "The stat buffers are out of sync with their ranges."
                );
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(STATS_CHECKS)]
        private static void ThrowIfGlobalUpdateListIsNotEmpty([DoesNotReturnIf(false)] bool isEmpty)
        {
            if (isEmpty == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new(
                    "A single-owner update produced cross-owner observers. " +
                    "isGuaranteedSingleOwner was set on a stat that observes another owner."
                );
        }
    }
}

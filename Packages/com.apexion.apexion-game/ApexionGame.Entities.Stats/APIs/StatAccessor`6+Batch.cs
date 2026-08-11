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
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace ApexionGame.Entities.Stats
{
    partial struct StatAccessor<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver, TValuePairComposer>
    {
        /// <remarks>
        /// When this method returns false, the stat is not modified in any way.
        /// ProduceChangeEvents and UserData are applied only if the value set succeeds.
        /// </remarks>
        public bool TrySetStatData<TStatData>(
              in StatDataParams<TStatData> statParams
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
            where TStatData : unmanaged, IStatData
        {
            if (statParams.IsCreated == false
                || statParams.Handle.TryGetValue(out var statHandle) == false
                || statParams.StatData.TryGetValue(out var statData) == false
            )
            {
                return false;
            }

            var owner = statHandle.owner;

            ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  statHandle
                , _store.AsStatLookup()
                , out var statBuffer
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

            var result = statData.IsValuePair
                ? statRef.TrySetValues(statData.BaseValue, statData.CurrentValue)
                : statRef.TrySetCurrentValue(statData.CurrentValue);

            if (result == false)
            {
                return false;
            }

            if (statParams.ProduceChangeEvents.TryGetValue(out var produceChangeEvents))
            {
                statRef.ProduceChangeEvents = produceChangeEvents;
            }

            if (statParams.UserData.TryGetValue(out var userdata))
            {
                statRef.UserData = userdata;
            }

            UpdateStatRef(
                  statHandle
                , ref statRef
                , statBuffer
                , modifierBuffer
                , observerBuffer
                , ref worldData
            );

            return result;
        }

        /// <remarks>
        /// When this method returns false, the stat is not modified in any way.
        /// ProduceChangeEvents and UserData are applied only if the value set succeeds.
        /// </remarks>
        public bool TrySetStatData(
              in StatValueParams<TValuePair> statParams
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            if (statParams.IsCreated == false
                || statParams.Handle.TryGetValue(out var statHandle) == false
                || statParams.StatValues.TryGetValue(out var statValues) == false
            )
            {
                return false;
            }

            var owner = statHandle.owner;

            ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  statHandle
                , _store.AsStatLookup()
                , out var statBuffer
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

            var result = statRef.TrySetValues(
                  statValues.GetBaseValueOrDefault()
                , statValues.GetCurrentValueOrDefault()
            );

            if (result == false)
            {
                return false;
            }

            if (statParams.ProduceChangeEvents.TryGetValue(out var produceChangeEvents))
            {
                statRef.ProduceChangeEvents = produceChangeEvents;
            }

            if (statParams.UserData.TryGetValue(out var userdata))
            {
                statRef.UserData = userdata;
            }

            UpdateStatRef(
                  statHandle
                , ref statRef
                , statBuffer
                , modifierBuffer
                , observerBuffer
                , ref worldData
            );

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetStatData<TStatData>(
              StatHandle<TStatData> statHandle
            , TStatData statData
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
            where TStatData : unmanaged, IStatData
        {
            var owner = statHandle.owner;

            ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  statHandle
                , _store.AsStatLookup()
                , out var statBuffer
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

            var result = statData.IsValuePair
                ? statRef.TrySetValues(statData.BaseValue, statData.CurrentValue)
                : statRef.TrySetCurrentValue(statData.CurrentValue);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetStatData<TStatData>(
              StatHandle<TStatData> statHandle
            , TStatData statData
            , StatBuffer<TStat> statBuffer
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
            where TStatData : unmanaged, IStatData
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

            var result = statData.IsValuePair
                ? statRef.TrySetValues(statData.BaseValue, statData.CurrentValue)
                : statRef.TrySetCurrentValue(statData.CurrentValue);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetStatValues(
              StatHandle statHandle
            , in TValuePair value
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            return TrySetStatValues(
                  statHandle
                , value.GetBaseValueOrDefault()
                , value.GetCurrentValueOrDefault()
                , ref worldData
            );
        }

        public bool TrySetStatValues(
              StatHandle statHandle
            , in StatVariant baseValue
            , in StatVariant currentValue
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            var owner = statHandle.owner;

            ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  statHandle
                , _store.AsStatLookup()
                , out var statBuffer
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

            var result = statRef.TrySetValues(baseValue, currentValue);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetStatValues(
              StatHandle statHandle
            , in TValuePair value
            , StatBuffer<TStat> statBuffer
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            return TrySetStatValues(
                  statHandle
                , value.GetBaseValueOrDefault()
                , value.GetCurrentValueOrDefault()
                , statBuffer
                , ref worldData
            );
        }

        /// <remarks>
        /// Assumes the "statBuffer" is on the owner of the statHandle
        /// </remarks>
        public bool TrySetStatValues(
              StatHandle statHandle
            , in StatVariant baseValue
            , in StatVariant currentValue
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

            var result = statRef.TrySetValues(baseValue, currentValue);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetStatProduceChangeEvents(StatHandle statHandle, bool value)
        {
            ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  statHandle
                , _store.AsStatLookup()
                , out bool success
                , ref _nullStat
            );

            if (success)
            {
                statRef.ProduceChangeEvents = value;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetStatProduceChangeEvents(StatHandle statHandle, bool value, StatBuffer<TStat> statBuffer)
        {
            ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  statHandle
                , statBuffer
                , out bool success
                , ref _nullStat
            );

            if (success)
            {
                statRef.ProduceChangeEvents = value;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetStatUserData(StatHandle statHandle, uint value)
        {
            ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  statHandle
                , _store.AsStatLookup()
                , out bool success
                , ref _nullStat
            );

            if (success)
            {
                statRef.UserData = value;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetStatUserData(StatHandle statHandle, uint value, StatBuffer<TStat> statBuffer)
        {
            ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  statHandle
                , statBuffer
                , out bool success
                , ref _nullStat
            );

            if (success)
            {
                statRef.UserData = value;
                return true;
            }

            return false;
        }

        /// <remarks>
        /// <paramref name="paramsForStats"/> and <paramref name="results"/> must be of the same length.
        /// </remarks>
        /// <param name="allocator">
        /// Backs the two scratch containers used to group the params by owner. Both are disposed
        /// before returning, so any allocator is safe; see D-02 for why this is not hard-wired to
        /// <see cref="Allocator.Temp"/>.
        /// </param>
        public void TrySetBaseValueToStats(
              ReadOnlySpan<StatValueParams<TValuePair>> paramsForStats
            , Span<bool> results
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
            , Allocator allocator = Allocator.Temp
        )
        {
            var paramsArrLength = paramsForStats.Length;

            if (paramsArrLength != results.Length)
            {
                results.Fill(false);
                return;
            }

            if (paramsArrLength < 1)
            {
                return;
            }

            var ownerParamsIndexMap = new NativeParallelMultiHashMap<StatOwnerHandle, int>(
                  paramsArrLength
                , allocator
            );

            var uniqueOwners = new NativeHashSet<StatOwnerHandle>(paramsArrLength, allocator);

            for (var i = 0; i < paramsArrLength; i++)
            {
                var statParams = paramsForStats[i];

                if (statParams.IsCreated
                    && statParams.Handle.TryGetValue(out var handle)
                    && statParams.StatValues.HasValue
                )
                {
                    ownerParamsIndexMap.Add(handle.owner, i);
                    uniqueOwners.Add(handle.owner);
                }
                else
                {
                    results[i] = false;
                }
            }

            foreach (var owner in uniqueOwners)
            {
                var indexEnumerator = ownerParamsIndexMap.GetValuesForKey(owner);

                if (_store.TryGetBuffers(owner, out var statBuffer, out var modifierBuffer, out var observerBuffer)
                    == false
                )
                {
                    foreach (var arrIndex in indexEnumerator)
                    {
                        results[arrIndex] = false;
                    }

                    continue;
                }

                foreach (var arrIndex in indexEnumerator)
                {
                    var statParams = paramsForStats[arrIndex];

                    results[arrIndex] = TrySetBaseValues(
                          statParams.Handle.GetValueOrThrow()
                        , statParams.StatValues.GetValueOrThrow()
                        , statBuffer
                        , modifierBuffer
                        , observerBuffer
                        , ref worldData
                    );
                }
            }

            uniqueOwners.Dispose();
            ownerParamsIndexMap.Dispose();
        }

        /// <remarks>
        /// <paramref name="paramsForStats"/> and <paramref name="results"/> must be of the same length.
        /// </remarks>
        /// <param name="allocator">
        /// Backs the two scratch containers used to group the params by owner. Both are disposed
        /// before returning, so any allocator is safe; see D-02.
        /// </param>
        public void TrySetCurrentValueToStats(
              ReadOnlySpan<StatValueParams<TValuePair>> paramsForStats
            , Span<bool> results
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
            , Allocator allocator = Allocator.Temp
        )
        {
            var paramsArrLength = paramsForStats.Length;

            if (paramsArrLength != results.Length)
            {
                results.Fill(false);
                return;
            }

            if (paramsArrLength < 1)
            {
                return;
            }

            var ownerParamsIndexMap = new NativeParallelMultiHashMap<StatOwnerHandle, int>(
                  paramsArrLength
                , allocator
            );

            var uniqueOwners = new NativeHashSet<StatOwnerHandle>(paramsArrLength, allocator);

            for (var i = 0; i < paramsArrLength; i++)
            {
                var statParams = paramsForStats[i];

                if (statParams.IsCreated
                    && statParams.Handle.TryGetValue(out var handle)
                    && statParams.StatValues.HasValue
                )
                {
                    ownerParamsIndexMap.Add(handle.owner, i);
                    uniqueOwners.Add(handle.owner);
                }
                else
                {
                    results[i] = false;
                }
            }

            foreach (var owner in uniqueOwners)
            {
                var indexEnumerator = ownerParamsIndexMap.GetValuesForKey(owner);

                if (_store.TryGetBuffers(owner, out var statBuffer, out var modifierBuffer, out var observerBuffer)
                    == false
                )
                {
                    foreach (var arrIndex in indexEnumerator)
                    {
                        results[arrIndex] = false;
                    }

                    continue;
                }

                foreach (var arrIndex in indexEnumerator)
                {
                    var statParams = paramsForStats[arrIndex];

                    results[arrIndex] = TrySetCurrentValues(
                          statParams.Handle.GetValueOrThrow()
                        , statParams.StatValues.GetValueOrThrow()
                        , statBuffer
                        , modifierBuffer
                        , observerBuffer
                        , ref worldData
                    );
                }
            }

            uniqueOwners.Dispose();
            ownerParamsIndexMap.Dispose();
        }

        /// <remarks>
        /// <paramref name="paramsForStats"/> and <paramref name="results"/> must be of the same length.
        /// </remarks>
        /// <param name="allocator">
        /// Backs the two scratch containers used to group the params by owner. Both are disposed
        /// before returning, so any allocator is safe; see D-02.
        /// </param>
        public void TrySetDataToStats(
              ReadOnlySpan<StatValueParams<TValuePair>> paramsForStats
            , Span<bool> results
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
            , Allocator allocator = Allocator.Temp
        )
        {
            var paramsArrLength = paramsForStats.Length;

            if (paramsArrLength != results.Length)
            {
                results.Fill(false);
                return;
            }

            if (paramsArrLength < 1)
            {
                return;
            }

            var ownerParamsIndexMap = new NativeParallelMultiHashMap<StatOwnerHandle, int>(
                  paramsArrLength
                , allocator
            );

            var uniqueOwners = new NativeHashSet<StatOwnerHandle>(paramsArrLength, allocator);

            for (var i = 0; i < paramsArrLength; i++)
            {
                var statParams = paramsForStats[i];

                if (statParams.IsCreated
                    && statParams.Handle.TryGetValue(out var handle)
                    && statParams.StatValues.HasValue
                )
                {
                    ownerParamsIndexMap.Add(handle.owner, i);
                    uniqueOwners.Add(handle.owner);
                }
                else
                {
                    results[i] = false;
                }
            }

            foreach (var owner in uniqueOwners)
            {
                var indexEnumerator = ownerParamsIndexMap.GetValuesForKey(owner);

                if (_store.TryGetBuffers(owner, out var statBuffer, out var modifierBuffer, out var observerBuffer)
                    == false
                )
                {
                    foreach (var arrIndex in indexEnumerator)
                    {
                        results[arrIndex] = false;
                    }

                    continue;
                }

                foreach (var arrIndex in indexEnumerator)
                {
                    var statParams = paramsForStats[arrIndex];

                    results[arrIndex] = TrySetValues(
                          statParams
                        , statBuffer
                        , modifierBuffer
                        , observerBuffer
                        , ref worldData
                    );
                }
            }

            uniqueOwners.Dispose();
            ownerParamsIndexMap.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TrySetBaseValues(
              StatHandle statHandle
            , TValuePair value
            , StatBuffer<TStat> statBuffer
            , StatBuffer<TStatModifier> modifierBuffer
            , StatBuffer<TStatObserver> observerBuffer
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  statHandle
                , statBuffer
                , out bool success
                , ref _nullStat
            );

            if (success && statRef.TrySetBaseValue(value.GetBaseValueOrDefault()))
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
            else
            {
                success = false;
            }

            return success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TrySetCurrentValues(
              StatHandle statHandle
            , TValuePair value
            , StatBuffer<TStat> statBuffer
            , StatBuffer<TStatModifier> modifierBuffer
            , StatBuffer<TStatObserver> observerBuffer
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  statHandle
                , statBuffer
                , out bool success
                , ref _nullStat
            );

            if (success && statRef.TrySetCurrentValue(value.GetCurrentValueOrDefault()))
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
            else
            {
                success = false;
            }

            return success;
        }

        private bool TrySetValues(
              StatValueParams<TValuePair> statParams
            , StatBuffer<TStat> statBuffer
            , StatBuffer<TStatModifier> modifierBuffer
            , StatBuffer<TStatObserver> observerBuffer
            , ref StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver> worldData
        )
        {
            if (statParams.StatValues.TryGetValue(out var statValues) == false)
            {
                return false;
            }

            ref TStat statRef = ref StatAPI.GetStatRefUnsafe<TValuePair, TStat>(
                  statParams.Handle.GetValueOrThrow()
                , statBuffer
                , out bool success
                , ref _nullStat
            );

            if (success == false)
            {
                return false;
            }

            var result = statRef.TrySetValues(
                  statValues.GetBaseValueOrDefault()
                , statValues.GetCurrentValueOrDefault()
            );

            if (result == false)
            {
                return false;
            }

            if (statParams.ProduceChangeEvents.TryGetValue(out var produceChangeEvents))
            {
                statRef.ProduceChangeEvents = produceChangeEvents;
            }

            if (statParams.UserData.TryGetValue(out var userdata))
            {
                statRef.UserData = userdata;
            }

            UpdateStatRef(
                  statParams.Handle.GetValueOrThrow()
                , ref statRef
                , statBuffer
                , modifierBuffer
                , observerBuffer
                , ref worldData
            );

            return result;
        }
    }
}

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
using EncosyTower.Common;
using Unity.Collections;

namespace ApexionGame.Entities.Stats
{
    partial struct StatAccessor<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver, TValuePairComposer>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnly AsReadOnly()
            => new(_store);

        /// <summary>
        /// Read-only view over the same store. Safe to copy into several parallel jobs as long as
        /// nothing writes to the store at the same time.
        /// </summary>
        public struct ReadOnly : IIsCreated
        {
            internal StatStore<TStat, TStatModifier, TStatObserver> _store;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnly(StatStore<TStat, TStatModifier, TStatObserver> store)
            {
                _store = store;
            }

            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _store.IsCreated;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly NativeArray<TStat>.ReadOnly GetStats(StatOwnerHandle owner)
            {
                return StatAPI.GetStats<TValuePair, TStat>(owner, _store.AsStatLookup());
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
                return StatAPI.TryGetStatValue<TValuePair, TStat>(
                      statHandle
                    , _store.AsStatLookup()
                    , out valuePair
                );
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
            /// Useful to store observers before destroying an owner, and then manually update all observers
            /// after destroy. An observers update isn't automatically called when a stats owner is destroyed.
            /// </remarks>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryGetAllObservers(StatOwnerHandle owner, NativeList<TStatObserver> observers)
            {
                return StatAPI.TryGetAllObservers(owner, _store.AsObserverLookup(), observers);
            }

            public readonly bool TryGetStatModifier(StatModifierHandle modifierHandle, out TStatModifier statModifier)
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
        }
    }
}

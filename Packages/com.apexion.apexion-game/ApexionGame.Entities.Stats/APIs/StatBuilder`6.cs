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
    /// <summary>
    /// Fills the stat buffers of a single owner. Replaces the baking-time StatBaker of the ECS version.
    /// </summary>
    /// <remarks>
    /// Every operation is local to one owner, so modifiers added through this type may only observe
    /// stats of that same owner.
    /// </remarks>
    public struct StatBuilder<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver, TValuePairComposer>
        : IIsCreated
        where TValuePair : unmanaged, IStatValuePair, IEquatable<TValuePair>
        where TStat : unmanaged, IStat<TValuePair>
        where TStatModifier : unmanaged, IStatModifier<TValuePair, TStat, TStatModifierStack>
        where TStatModifierStack : unmanaged, IStatModifierStack<TValuePair, TStat>
        where TStatObserver : unmanaged, IStatObserver
        where TValuePairComposer : unmanaged, IStatValuePairComposer<TValuePair>
    {
        internal StatStore<TStat, TStatModifier, TStatObserver> _store;
        internal StatOwnerHandle _owner;
        internal TValuePairComposer _valuePairComposer;

        internal StatBuffer<TStat> _statBuffer;
        internal StatBuffer<TStatModifier> _modifierBuffer;
        internal StatBuffer<TStatObserver> _observerBuffer;

        public readonly StatOwnerHandle Owner
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _owner;
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _statBuffer.IsCreated;
        }

        /// <summary>
        /// Resets the owner back to a freshly created state: empty modifier and observer buffers,
        /// and a stat buffer holding only the <see cref="None"/> stat at index 0.
        /// </summary>
        public void Clear(TValuePairComposer valuePairComposer = default)
        {
            _valuePairComposer = valuePairComposer;
            _statBuffer.Clear();
            _statBuffer.Add(new TStat());
            _modifierBuffer.Clear();
            _observerBuffer.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatHandle<TStatData> CreateStatHandle<TStatData>(
              TStatData statData
            , bool produceChangeEvents
            , uint userData
        )
            where TStatData : unmanaged, IStatData
        {
            return StatAPI.CreateStatHandle<TValuePair, TStat, TStatData, TValuePairComposer>(
                  _owner
                , statData
                , produceChangeEvents
                , userData
                , _statBuffer
                , GetModifierBufferLength()
                , GetObserverBufferLength()
                , _valuePairComposer
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatHandle<TStatData> CreateStatHandle<TStatData>(
              TValuePair valuePair
            , bool produceChangeEvents
            , uint userData
        )
            where TStatData : unmanaged, IStatData
        {
            return StatAPI.CreateStatHandle<TValuePair, TStat, TStatData, TValuePairComposer>(
                  _owner
                , valuePair
                , produceChangeEvents
                , userData
                , _statBuffer
                , GetModifierBufferLength()
                , GetObserverBufferLength()
                , out _
                , _valuePairComposer
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatHandle<TStatData> CreateStatHandle<TStatData>(
              TValuePair valuePair
            , bool produceChangeEvents
            , uint userData
            , out TStatData statData
        )
            where TStatData : unmanaged, IStatData
        {
            return StatAPI.CreateStatHandle<TValuePair, TStat, TStatData, TValuePairComposer>(
                  _owner
                , valuePair
                , produceChangeEvents
                , userData
                , _statBuffer
                , GetModifierBufferLength()
                , GetObserverBufferLength()
                , out statData
                , _valuePairComposer
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatHandle CreateStatHandle(TValuePair valuePair, bool produceChangeEvents, uint userData)
        {
            return StatAPI.CreateStatHandle(
                  _owner
                , valuePair
                , produceChangeEvents
                , userData
                , _statBuffer
                , GetModifierBufferLength()
                , GetObserverBufferLength()
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(StatHandle handle)
        {
            return StatAPI.Contains<TValuePair, TStat>(handle, _statBuffer.AsReadOnlySpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(StatHandle handle, uint userData)
        {
            return StatAPI.Contains<TValuePair, TStat>(handle, userData, _statBuffer.AsReadOnlySpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatHandle<TStatData> SetStatOrCreateHandle<TStatData>(
              StatHandle<TStatData> handle
            , TStatData statData
            , bool produceChangeEvents
            , uint userData
        )
            where TStatData : unmanaged, IStatData
        {
            if (StatAPI.TrySetStatData<TValuePair, TStat, TStatData>(
                  handle
                , statData
                , produceChangeEvents
                , userData
                , _statBuffer
            ))
            {
                return handle;
            }

            return CreateStatHandle(statData, produceChangeEvents, userData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatHandle<TStatData> SetStatOrCreateHandle<TStatData>(
              StatHandle<TStatData> handle
            , TValuePair valuePair
            , bool produceChangeEvents
            , uint userData
            , out TStatData statData
        )
            where TStatData : unmanaged, IStatData
        {
            if (StatAPI.TrySetStatValue<TValuePair, TStat>(
                  handle
                , valuePair
                , produceChangeEvents
                , userData
                , _statBuffer
            ))
            {
                statData = StatAPI.MakeStatData<TValuePair, TStatData>(valuePair);
                return handle;
            }

            return CreateStatHandle<TStatData>(valuePair, produceChangeEvents, userData, out statData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatHandle<TStatData> SetStatOrCreateHandle<TStatData>(
              StatHandle<TStatData> handle
            , TValuePair valuePair
            , bool produceChangeEvents
            , uint userData
        )
            where TStatData : unmanaged, IStatData
        {
            if (StatAPI.TrySetStatValue<TValuePair, TStat>(
                  handle
                , valuePair
                , produceChangeEvents
                , userData
                , _statBuffer
            ))
            {
                return handle;
            }

            return CreateStatHandle<TStatData>(valuePair, produceChangeEvents, userData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatHandle SetStatOrCreateHandle(
              StatHandle handle
            , TValuePair valuePair
            , bool produceChangeEvents
            , uint userData
        )
        {
            if (StatAPI.TrySetStatValue<TValuePair, TStat>(
                  handle
                , valuePair
                , produceChangeEvents
                , userData
                , _statBuffer
            ))
            {
                return handle;
            }

            return CreateStatHandle(valuePair, produceChangeEvents, userData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void SetStat<TStatData>(
              StatHandle<TStatData> handle
            , TStatData statData
            , bool produceChangeEvents
            , uint userData
        )
            where TStatData : unmanaged, IStatData
        {
            StatAPI.SetStatData<TValuePair, TStat, TStatData>(
                  handle
                , statData
                , produceChangeEvents
                , userData
                , _statBuffer
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void SetStat(StatHandle handle, TValuePair valuePair, bool produceChangeEvents, uint userData)
        {
            StatAPI.SetStatValue<TValuePair, TStat>(handle, valuePair, produceChangeEvents, userData, _statBuffer);
        }

        /// <remarks>
        /// Fails when the affected stat, or any stat the modifier observes, does not belong to this owner.
        /// </remarks>
        public bool TryAddStatModifier(
              StatHandle affectedStatHandle
            , TStatModifier modifier
            , out StatModifierHandle statModifierHandle
        )
        {
            var owner = _owner;

            // Cancel if the affected stat is not on this owner
            if (affectedStatHandle.owner != owner)
            {
                statModifierHandle = default;
                return false;
            }

            // Cancel if the modifier involves stats of any other owner
            var tmpObservedStatHandles = new NativeList<StatHandle>(Allocator.Temp);
            modifier.AddObservedStatsToList(tmpObservedStatHandles);

            for (var i = 0; i < tmpObservedStatHandles.Length; ++i)
            {
                if (tmpObservedStatHandles[i].owner != owner)
                {
                    tmpObservedStatHandles.Dispose();
                    statModifierHandle = default;
                    return false;
                }
            }

            tmpObservedStatHandles.Dispose();

            var worldData = new StatWorldData<TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver>(
                  16
                , Allocator.Temp
            );

            var accessor = new StatAccessor<
                TValuePair, TStat, TStatModifier, TStatModifierStack, TStatObserver, TValuePairComposer
            >(_store, _valuePairComposer);

            var success = accessor.TryAddStatModifierSingleOwner(
                  affectedStatHandle
                , modifier
                , _statBuffer
                , _modifierBuffer
                , _observerBuffer
                , out statModifierHandle
                , ref worldData
            );

            worldData.Dispose();

            return success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly int GetModifierBufferLength()
            => _modifierBuffer.IsCreated ? _modifierBuffer.Length : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly int GetObserverBufferLength()
            => _observerBuffer.IsCreated ? _observerBuffer.Length : 0;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;
using EncosyTower.Common;
using EncosyTower.Ids;
using Unity.Jobs;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

using ThrowHelper = EncosyTower.Collections.ThrowHelper;

namespace EncosyTower.StringIds
{
    public struct StringVaultUnsafe : IStringVault, IReadOnlyList<UnmanagedString>
    {
        internal ArrayMapUnsafe<StringHash, StringId> _map;
        internal ArrayMapUnsafe<UnmanagedString, StringId> _collisionMap;
        internal BufferUnsafe<Range> _stringRanges;
        internal int _stringRangesLength;
        internal BufferUnsafe<byte> _stringBuffer;
        internal int _stringBufferLength;
        internal BufferUnsafe<Option<StringHash>> _hashes;
        internal int _hashesLength;
        internal int _count;
        internal ByteBool _allowEmptyString;
        internal AllocatorStrategy _allocator;

        public StringVaultUnsafe(
              int initialCapacity
            , AllocatorStrategy allocator
            , bool allowEmptyString = false
        )
        {
            var capacity = Math.Max(1, initialCapacity);
            _map = new(capacity, allocator);
            _collisionMap = new(capacity, allocator);
            _stringRanges = new(capacity, allocator);
            _stringRangesLength = 0;
            _stringBuffer = new(capacity * 512, allocator);
            _stringBufferLength = 0;
            _hashes = new(capacity, allocator);
            _hashesLength = 0;
            _count = 0;
            _allowEmptyString = allowEmptyString;
            _allocator = allocator;

            Clear();
        }

        public StringVaultUnsafe(in StringVaultUnsafe source, AllocatorStrategy allocator)
        {
            _map = new(source._map, allocator);
            _collisionMap = new(source._collisionMap, allocator);
            _stringRanges = new(source._stringRanges.Capacity, allocator, false);
            source._stringRanges.AsReadOnlySpan().CopyTo(_stringRanges.AsSpan());
            _stringRangesLength = source._stringRangesLength;
            _stringBuffer = new(source._stringBuffer.Capacity, allocator, false);
            source._stringBuffer.AsReadOnlySpan().CopyTo(_stringBuffer.AsSpan());
            _stringBufferLength = source._stringBufferLength;
            _hashes = new(source._hashes.Capacity, allocator, false);
            source._hashes.AsReadOnlySpan().CopyTo(_hashes.AsSpan());
            _hashesLength = source._hashesLength;
            _count = source._count;
            _allowEmptyString = source._allowEmptyString;
            _allocator = allocator;
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _map.IsCreated && _collisionMap.IsCreated
                && _stringRanges.IsCreated && _stringBuffer.IsCreated && _hashes.IsCreated;
        }

        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _hashesLength;
        }

        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }

        public readonly bool AllowEmptyString
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _allowEmptyString;
        }

        public readonly UnmanagedString this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => UnmanagedString.FromBufferAt(
                  _stringRanges.AsReadOnlySpan()[index]
                , StringBufferSpan
            ).GetValueOrThrow();
        }

        /// <safety>The returned vault header is owned by the supplied allocator and must be freed exactly once with Free.</safety>
        public static unsafe StringVaultUnsafe* Alloc(
              int capacity
            , AllocatorStrategy allocator
            , bool allowEmptyString = false
        )
        {
            // SAFETY: The validated allocator returns an owned vault header for the native storage.
            unsafe
            {
                var data = allocator.Allocate<StringVaultUnsafe>();
                *data = new StringVaultUnsafe(capacity, allocator, allowEmptyString);
                return data;
            }
        }

        /// <safety>The returned vault header is owned by the supplied allocator and must be freed exactly once with Free.</safety>
        public static unsafe StringVaultUnsafe* Alloc(
              in StringVaultUnsafe source
            , AllocatorStrategy allocator
        )
        {
            // SAFETY: The validated allocator returns an owned vault header for the deep-copied storage.
            unsafe
            {
                var data = allocator.Allocate<StringVaultUnsafe>();
                *data = new StringVaultUnsafe(source, allocator);
                return data;
            }
        }

        /// <safety>data must be a live header returned by Alloc and must not be used after this call.</safety>
        public static unsafe void Free(StringVaultUnsafe* data)
        {
            // SAFETY: The caller supplies the owned vault header and its allocator is used for the matching release.
            unsafe
            {
                if (data == null)
                {
                    return;
                }

                var allocator = data->_allocator;
                data->Dispose();
                allocator.Free(data);
            }
        }

        public void Dispose()
        {
            if (IsCreated == false)
            {
                return;
            }

            _map.Dispose();
            _collisionMap.Dispose();
            _stringRanges.Dispose();
            _stringBuffer.Dispose();
            _hashes.Dispose();
            this = default;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (IsCreated == false)
            {
                return inputDeps;
            }

            inputDeps = _map.Dispose(inputDeps);
            inputDeps = _collisionMap.Dispose(inputDeps);
            inputDeps = _stringRanges.Dispose(inputDeps);
            inputDeps = _stringBuffer.Dispose(inputDeps);
            inputDeps = _hashes.Dispose(inputDeps);
            this = default;

            return inputDeps;
        }

        public void Clear()
        {
            _map.Clear();
            _collisionMap.Clear();
            _stringRangesLength = 0;
            _stringBufferLength = 0;
            _hashesLength = 0;
            _count = 1;

            StringHash invalidHash = default(UnmanagedString).GetHashCode64();
            _map.Add(invalidHash, default);
            _collisionMap.Add(default, default);
            EnsureCapacity();
            _stringRanges[0] = default;
            _hashes[0] = default;
        }

        public StringId GetOrMakeId(in UnmanagedString str)
        {
            if (AllowEmptyString == false && str.IsEmpty)
            {
                return default;
            }

            StringHash hash = str.GetHashCode64();

            if (_map.TryGetValue(hash, out var id))
            {
                TryGetUnmanagedString(id, out var registeredString);

                if (str == registeredString)
                {
                    return id;
                }

                if (_collisionMap.TryGetValue(str, out var collidedId))
                {
                    return collidedId;
                }

                var index = _count;
                id = new Id(index);
                _count++;
                EnsureCapacity();

                var added = _collisionMap.TryAdd(str, id, out _);
                ThrowIfFailedRegistering(added, str, id);

                if (added)
                {
                    _stringRanges[index] = WriteToBuffer(str);
                    _hashes[index] = Option.Some<StringHash>(hash);
                }

                return id;
            }

            {
                var index = _count;
                id = new Id(index);
                var added = _map.TryAdd(hash, id, out _);
                ThrowIfFailedRegistering(added, str, id);

                if (added)
                {
                    _count++;
                    EnsureCapacity();
                    _stringRanges[index] = WriteToBuffer(str);
                    _hashes[index] = Option.Some<StringHash>(hash);
                }
            }

            return id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Option<StringId> TryGetId(in UnmanagedString str)
            => Option.SomeIf(TryGetId(str, out var result), result);

        public readonly bool TryGetId(in UnmanagedString str, out StringId result)
        {
            if (AllowEmptyString == false && str.IsEmpty)
            {
                result = default;
                return false;
            }

            StringHash hash = str.GetHashCode64();

            if (_map.TryGetValue(hash, out var id))
            {
                TryGetUnmanagedString(id, out var registeredString);

                if (str == registeredString)
                {
                    result = id;
                    return true;
                }

                if (_collisionMap.TryGetValue(str, out var collidedId))
                {
                    result = collidedId;
                    return true;
                }
            }

            result = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Option<UnmanagedString> TryGetUnmanagedString(StringId id)
            => Option.SomeIf(TryGetUnmanagedString(id, out var result), result);

        public readonly bool TryGetUnmanagedString(StringId id, out UnmanagedString result)
        {
            var indexUnsigned = (uint)id.Id;
            var index = (int)indexUnsigned;
            var validIndex = indexUnsigned < (uint)_hashesLength;
            var resultOpt = validIndex
                ? UnmanagedString.FromBufferAt(_stringRanges[index], StringBufferSpan)
                : Option.None;

            result = resultOpt.GetValueOrDefault();
            return resultOpt.HasValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool ContainsId(StringId id)
        {
            var indexUnsigned = (uint)id.Id;
            var index = (int)indexUnsigned;
            return indexUnsigned < (uint)_hashesLength && _hashes[index].HasValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(Span<UnmanagedString> destination)
            => CopyTo(destination, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(Span<UnmanagedString> destination, int length)
            => CopyTo(0, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(int sourceStartIndex, Span<UnmanagedString> destination)
            => CopyTo(sourceStartIndex, destination, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(int sourceStartIndex, Span<UnmanagedString> destination, int length)
            => new UnmanagedStringSpan(
                  _stringRanges.AsReadOnlySpan()[1..Count]
                , StringBufferSpan
            ).Slice(sourceStartIndex, length).CopyTo(destination[..length]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(Span<UnmanagedString> destination)
            => TryCopyTo(destination, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(Span<UnmanagedString> destination, int length)
            => TryCopyTo(0, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(int sourceStartIndex, Span<UnmanagedString> destination)
            => TryCopyTo(sourceStartIndex, destination, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(int sourceStartIndex, Span<UnmanagedString> destination, int length)
            => new UnmanagedStringSpan(
                  _stringRanges.AsReadOnlySpan()[1..Count]
                , StringBufferSpan
            ).Slice(sourceStartIndex, length).TryCopyTo(destination[..length]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncreaseCapacityBy(int amount)
        {
            ThrowIfAmountIsNotValid(amount > 0, amount);
            return IncreaseCapacityTo(Capacity + amount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncreaseCapacityTo(int newCapacity)
        {
            _map.IncreaseCapacityTo(newCapacity);
            return EnsureCapacity();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Enumerator GetEnumerator()
            => new(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly IEnumerator<UnmanagedString> IEnumerable<UnmanagedString>.GetEnumerator()
            => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        private int EnsureCapacity()
        {
            var oldCapacity = Math.Min(_hashes.Capacity, _stringRanges.Capacity);
            var newCapacity = Math.Max(_map.Capacity, _count);

            if (newCapacity > 0 && newCapacity > oldCapacity)
            {
                _hashes.Resize(newCapacity, true);
                _stringRanges.Resize(newCapacity, true);
            }

            _hashesLength = newCapacity;
            _stringRangesLength = newCapacity;

            var newBufferCapacity = newCapacity * 512;

            if (_stringBuffer.Capacity < newBufferCapacity)
            {
                _stringBuffer.Resize(newBufferCapacity, true);
            }

            return _hashes.Capacity;
        }

        private Range WriteToBuffer(in UnmanagedString str)
        {
            var required = _stringBufferLength + str.Length;

            if (required > _stringBuffer.Capacity)
            {
                var doubled = Math.Max(1, _stringBuffer.Capacity * 2);
                _stringBuffer.Resize(Math.Max(required, doubled), true);
            }

            var start = _stringBufferLength;
            str.AsReadOnlySpan().CopyTo(_stringBuffer.AsSpan()[start..required]);
            _stringBufferLength = required;
            return new Range(start, required);
        }

        private readonly ReadOnlySpan<byte> StringBufferSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _stringBuffer.AsReadOnlySpan()[.._stringBufferLength];
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfFailedRegistering(
              [DoesNotReturnIf(false)] bool check
            , in UnmanagedString str
            , StringId id
        )
        {
            if (check == false)
            {
                throw CreateException(str, id);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException(in UnmanagedString str, StringId id)
                => new($"Cannot register a StringId by the same value \"{str}\" with different id \"{id}\".");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfAmountIsNotValid([DoesNotReturnIf(false)] bool isValid, int amount)
        {
            if (isValid == false)
            {
                throw CreateException(amount);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ArgumentOutOfRangeException CreateException(int amount)
                => new(nameof(amount), amount, "amount must be greater than 0");
        }

        public struct Enumerator : IEnumerator<UnmanagedString>
        {
            private readonly StringVaultUnsafe _data;
            private Range _current;
            private int _index;

            internal Enumerator(in StringVaultUnsafe data)
            {
                _data = data;
                _current = default;
                _index = 0;
            }

            public bool MoveNext()
            {
                if ((uint)_index < (uint)_data._count)
                {
                    _current = _data._stringRanges[_index++];
                    return true;
                }

                _index = _data._count + 1;
                _current = default;
                return false;
            }

            public readonly UnmanagedString Current
                => UnmanagedString.FromBufferAt(_current, _data.StringBufferSpan).GetValueOrThrow();

            readonly object IEnumerator.Current
            {
                get
                {
                    ThrowHelper.ThrowIfEnumeratorOperationIsInvalid(_index != 0 && _index != _data._count + 1);

                    return Current;
                }
            }

            public void Reset()
            {
                _index = 0;
                _current = default;
            }

            public readonly void Dispose() { }
        }
    }
}

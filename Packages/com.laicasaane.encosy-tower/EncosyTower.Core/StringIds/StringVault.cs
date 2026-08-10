using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EncosyTower.Collections;
using EncosyTower.Common;
using EncosyTower.Ids;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.StringIds
{
    public sealed partial class StringVault : IStringVault
        , IReadOnlyList<string>
        , ICopyToSpan<string>, ITryCopyToSpan<string>
    {
        /// <summary>
        /// The default instance used by <see cref="StringToId"/> and <see cref="IdToString"/>.
        /// </summary>
        public static StringVault Default => GlobalStringVault.s_vault;

        internal SharedArrayMap<StringHash, StringId> _map;
        internal SharedArrayMap<UnmanagedString, StringId> _collisionMap;
        internal SharedList<Range> _unmanagedStringRanges;
        internal SharedList<byte> _unmanagedStringBuffer;
        internal FasterList<string> _managedStrings;
        internal SharedList<Option<StringHash>> _hashes;
        internal SharedReference<int> _count;
        internal readonly object _lock = new();

        public StringVault(int initialCapacity, bool allowEmptyString = false)
        {
            _map = new(initialCapacity);
            _collisionMap = new(initialCapacity);
            _unmanagedStringRanges = new(initialCapacity);
            _unmanagedStringBuffer = new(initialCapacity * 512);
            _managedStrings = new(initialCapacity);
            _hashes = new(initialCapacity);
            _count = new();

            AllowEmptyString = allowEmptyString;

            Clear();
        }

        public StringVault(ReadOnlySpan<string> managedStrings, bool allowEmptyString = false)
            : this(managedStrings.Length, allowEmptyString)
        {
            foreach (var str in managedStrings)
            {
                GetOrMakeId(str);
            }
        }

        public StringVault(ReadOnlySpan<UnmanagedString> unmanagedStrings, bool allowEmptyString = false)
            : this(unmanagedStrings.Length, allowEmptyString)
        {
            foreach (var str in unmanagedStrings)
            {
                GetOrMakeId(str);
            }
        }

        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _map is not null;
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _hashes.Count;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count.ValueRO;
        }

        public bool AllowEmptyString { get; }

        public string this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _managedStrings[index];
        }

        public void Dispose()
        {
            if (_map == null)
            {
                return;
            }

            _map.Dispose();
            _collisionMap.Dispose();
            _unmanagedStringRanges.Dispose();
            _unmanagedStringBuffer.Dispose();
            _hashes.Dispose();
            _count.Dispose();

            _map = null;
            _collisionMap = null;
            _unmanagedStringRanges = null;
            _unmanagedStringBuffer = null;
            _hashes = null;
            _count = null;
        }

        public void Clear()
        {
            lock (_lock)
            {
                _map.Clear();
                _collisionMap.Clear();
                _unmanagedStringRanges.Clear();
                _unmanagedStringBuffer.Clear();
                _managedStrings.Clear();
                _hashes.Clear();

                // The first item represent an invalid id
                _map.Add(default(UnmanagedString).GetHashCode64(), default);
                _collisionMap.Add(default, default);
                _unmanagedStringRanges.Add(default);
                _managedStrings.Add(string.Empty);
                _hashes.Add(default);

                _count.ValueRW = 1;
            }
        }

        /// <summary>
        /// Creates or retrieves a <see cref="StringId"/> from an <see cref="UnmanagedString"/>.
        /// </summary>
        /// <param name="str">The unmanaged string to create or retrieve the <see cref="StringId"/> for.</param>
        /// <returns></returns>
        public StringId GetOrMakeId(in UnmanagedString str)
        {
            if (AllowEmptyString == false && str.IsEmpty)
            {
                return default;
            }

            lock (_lock)
            {
                var hash = str.GetHashCode64();
                var registered = _map.TryGetValue(hash, out var id);

                if (registered)
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

                    ref var count = ref _count.ValueRW;
                    var index = count;
                    id = new Id(index);

                    count += 1;
                    EnsureCapacity();

                    _collisionMap[str] = id;
                    _unmanagedStringRanges[index] = WriteToBuffer(str);
                    _managedStrings[index] = str.ToString();
                    _hashes[index] = Option.Some<StringHash>(hash);
                }
                else
                {
                    ref var count = ref _count.ValueRW;
                    var index = count;
                    id = new Id(index);

                    if (_map.TryAdd(hash, id))
                    {
                        count += 1;
                        EnsureCapacity();

                        _unmanagedStringRanges[index] = WriteToBuffer(str);
                        _managedStrings[index] = str.ToString();
                        _hashes[index] = Option.Some<StringHash>(hash);
                    }
                    else
                    {
                        ThrowIfFailedRegistering(false, str, id);
                    }
                }

                return id;
            }
        }

        /// <summary>
        /// Creates or retrieves a <see cref="StringId"/> from a managed <see cref="string"/>.
        /// </summary>
        /// <param name="managedString">The managed string to create or retrieve the <see cref="StringId"/> for.</param>
        /// <returns></returns>
        /// <remarks>
        /// The managed string will be capped at the maximum length of 125 UTF8 characters
        /// to fit within the <see cref="UnmanagedString"/> representation.
        /// </remarks>
        public StringId GetOrMakeId([NotNull] string managedString)
        {
            if (AllowEmptyString == false && managedString.IsEmpty())
            {
                return default;
            }

            lock (_lock)
            {
                UnmanagedString str = managedString;
                var hash = str.GetHashCode64();
                var registered = _map.TryGetValue(hash, out var id);

                if (registered)
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

                    ref var count = ref _count.ValueRW;
                    var index = count;
                    id = new Id(index);

                    count += 1;
                    EnsureCapacity();

                    _collisionMap[str] = id;
                    _unmanagedStringRanges[index] = WriteToBuffer(str);
                    _managedStrings[index] = managedString;
                    _hashes[index] = Option.Some<StringHash>(hash);
                }
                else
                {
                    ref var count = ref _count.ValueRW;
                    var index = count;
                    id = new Id(index);

                    if (_map.TryAdd(hash, id))
                    {
                        count += 1;
                        EnsureCapacity();
                        _unmanagedStringRanges[index] = WriteToBuffer(str);
                        _managedStrings[index] = managedString;
                        _hashes[index] = Option.Some<StringHash>(hash);
                    }
                    else
                    {
                        ThrowIfFailedRegistering(false, managedString, id);
                    }
                }

                return id;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<StringId> TryGetId(in UnmanagedString str)
            => Option.SomeIf(TryGetId(str, out var result), result);

        public bool TryGetId(in UnmanagedString str, out StringId result)
        {
            if (AllowEmptyString == false && str.IsEmpty)
            {
                result = default;
                return false;
            }

            var hash = str.GetHashCode64();
            var registered = _map.TryGetValue(hash, out var id);

            if (registered)
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
        public Option<StringId> TryGetId(string managedString)
            => Option.SomeIf(TryGetId(managedString, out var result), result);

        public bool TryGetId(string managedString, out StringId result)
        {
            if (AllowEmptyString == false && managedString.IsEmpty())
            {
                result = default;
                return false;
            }

            UnmanagedString str = managedString;
            var hash = str.GetHashCode64();
            var registered = _map.TryGetValue(hash, out var id);

            if (registered)
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
        public Option<UnmanagedString> TryGetUnmanagedString(StringId id)
            => Option.SomeIf(TryGetUnmanagedString(id, out var result), result);

        public bool TryGetUnmanagedString(StringId id, out UnmanagedString result)
        {
            var indexUnsigned = (uint)id.Id;
            var index = (int)indexUnsigned;
            var validIndex = indexUnsigned < (uint)_hashes.Count;

            var resultOpt = validIndex
                ? UnmanagedString.FromBufferAt(_unmanagedStringRanges.AsReadOnly()[index], _unmanagedStringBuffer.AsReadOnlySpan())
                : Option.None;

            result = resultOpt.GetValueOrDefault();
            return resultOpt.HasValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<string> TryGetManagedString(StringId id)
            => Option.SomeIf(TryGetManagedString(id, out var result), result);

        public bool TryGetManagedString(StringId id, out string result)
        {
            var indexUnsigned = (uint)id.Id;
            var index = (int)indexUnsigned;
            var validIndex = indexUnsigned < (uint)_hashes.Count;

            result = validIndex ? _managedStrings[index] : string.Empty;
            return validIndex && _hashes[index].HasValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsId(StringId id)
        {
            var indexUnsigned = (uint)id.Id;
            var index = (int)indexUnsigned;
            var validIndex = indexUnsigned < (uint)_hashes.Count;
            return validIndex && _hashes[index].HasValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<string> GetManagedStringSpan()
            => _managedStrings.AsReadOnlySpan()[..Count];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Span<string> destination)
            => CopyTo(destination, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Span<string> destination, int length)
            => CopyTo(0, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int sourceStartIndex, Span<string> destination)
            => CopyTo(sourceStartIndex, destination, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int sourceStartIndex, Span<string> destination, int length)
            => GetManagedStringSpan().Slice(sourceStartIndex, length).CopyTo(destination[..length]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(Span<string> destination)
            => TryCopyTo(destination, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(Span<string> destination, int length)
            => TryCopyTo(0, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(int sourceStartIndex, Span<string> destination)
            => TryCopyTo(sourceStartIndex, destination, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(int sourceStartIndex, Span<string> destination, int length)
            => GetManagedStringSpan().Slice(sourceStartIndex, length).TryCopyTo(destination[..length]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Span<UnmanagedString> destination)
            => CopyTo(destination, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Span<UnmanagedString> destination, int length)
            => CopyTo(0, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int sourceStartIndex, Span<UnmanagedString> destination)
            => CopyTo(sourceStartIndex, destination, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int sourceStartIndex, Span<UnmanagedString> destination, int length)
            => new UnmanagedStringSpan(_unmanagedStringRanges.AsReadOnlySpan()[1..Count], _unmanagedStringBuffer.AsReadOnlySpan())
                .CopyTo(sourceStartIndex, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(Span<UnmanagedString> destination)
            => TryCopyTo(destination, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(Span<UnmanagedString> destination, int length)
            => TryCopyTo(0, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(int sourceStartIndex, Span<UnmanagedString> destination)
            => TryCopyTo(sourceStartIndex, destination, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(int sourceStartIndex, Span<UnmanagedString> destination, int length)
            => new UnmanagedStringSpan(_unmanagedStringRanges.AsReadOnlySpan()[1..Count], _unmanagedStringBuffer.AsReadOnlySpan())
                .TryCopyTo(sourceStartIndex, destination, length);

        public int IncreaseCapacityBy(int amount)
        {
            ThrowIfAmountIsNotValid(amount > 0, amount);
            return IncreaseCapacityTo(Capacity + amount);
        }

        public int IncreaseCapacityTo(int newCapacity)
        {
            _map.IncreaseCapacityTo(newCapacity);
            EnsureCapacity();
            return _hashes.Capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FasterListEnumerator<string> GetEnumerator()
            => _managedStrings.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<string> IEnumerable<string>.GetEnumerator()
            => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        private void EnsureCapacity()
        {
            var oldCapacity = Math.Min(_hashes.Capacity, _unmanagedStringRanges.Capacity);
            var newCapacity = Math.Max(_map.Capacity, _count.ValueRO);

            if (newCapacity > 0 && newCapacity > oldCapacity)
            {
                _hashes.IncreaseCapacityTo(newCapacity);
                _unmanagedStringRanges.IncreaseCapacityTo(newCapacity);
                _managedStrings.IncreaseCapacityTo(newCapacity);
            }

            if (_hashes.Count < newCapacity)
            {
                _hashes.AddReplicateNoInit(Math.Max(newCapacity - _hashes.Count, 0));
            }

            if (_unmanagedStringRanges.Count < newCapacity)
            {
                _unmanagedStringRanges.AddReplicateNoInit(Math.Max(newCapacity - _unmanagedStringRanges.Count, 0));
            }

            if (_managedStrings.Count < newCapacity)
            {
                _managedStrings.AddReplicateNoInit(Math.Max(newCapacity - _managedStrings.Count, 0));
            }

            var newBufferCapacity = newCapacity * 512;

            if (_unmanagedStringBuffer.Capacity < newBufferCapacity)
            {
                _unmanagedStringBuffer.IncreaseCapacityTo(newBufferCapacity);
            }
        }

        private Range WriteToBuffer(in UnmanagedString str)
        {
            var buffer = _unmanagedStringBuffer;
            var startIndex = buffer.Count;
            var amount = str.Length;
            var strSpan = buffer.AddReplicateNoInit(amount);
            str.AsReadOnlySpan().CopyTo(strSpan);

            return new(startIndex, startIndex + amount);
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
        private static void ThrowIfFailedRegistering(
              [DoesNotReturnIf(false)] bool check
            , string str
            , StringId id
        )
        {
            if (check == false)
            {
                throw CreateException(str, id);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException(string str, StringId id)
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
    }
}

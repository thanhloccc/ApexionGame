using System;
using System.Collections;
using System.Collections.Generic;
using EncosyTower.Buffers;
using EncosyTower.Common;

namespace EncosyTower.Collections.Unsafe
{
    public partial struct ListUnsafe<T>
        where T : unmanaged
    {
        public readonly struct ReadOnly
            : IEnumerable<T>
            , IEnumerable
            , IReadOnlyList<T>
            , IAsReadOnlySpan<T>
            , IToArray<T>
            , ICopyToSpan<T>
            , ITryCopyToSpan<T>
            , IHasCapacity
            , IHasCount
            , IIsCreated
        {
            internal readonly BufferUnsafe<T>.ReadOnly _buffer;
            internal readonly int _count;
            internal readonly int _version;

            internal ReadOnly(in ListUnsafe<T> source)
            {
                _buffer = source._buffer.AsReadOnly();
                _count = source._count;
                _version = source._version;
            }

            public readonly bool IsCreated
                => _buffer.IsCreated;
            public readonly int Count
                => _count;
            public readonly int Capacity
                => _buffer.Capacity;

            public readonly ref readonly T this[int index]
            {
                get
                {
                    ThrowHelper.ThrowIfIndexIsOutOfRange(
                          (uint)index < (uint)_count
                        , ThrowHelper.CollectionType.ListUnsafeReadOnly
                    );
                    return ref _buffer[index];
                }
            }

            T IReadOnlyList<T>.this[int index]
                => this[index];

            public readonly ReadOnlySpan<T> AsReadOnlySpan()
                => _buffer.AsReadOnlySpan()[.._count];

            public readonly void CopyTo(Span<T> destination)
                => CopyTo(0, destination);

            public readonly void CopyTo(Span<T> destination, int length)
                => CopyTo(0, destination, length);

            public readonly void CopyTo(int sourceStartIndex, Span<T> destination)
                => CopyTo(sourceStartIndex, destination, destination.Length);

            public readonly void CopyTo(int sourceStartIndex, Span<T> destination, int length)
                => new CopyToSpan<T>(AsReadOnlySpan()).CopyTo(sourceStartIndex, destination, length);

            public readonly bool TryCopyTo(Span<T> destination)
                => TryCopyTo(0, destination);

            public readonly bool TryCopyTo(Span<T> destination, int length)
                => TryCopyTo(0, destination, length);

            public readonly bool TryCopyTo(int sourceStartIndex, Span<T> destination)
                => TryCopyTo(sourceStartIndex, destination, destination.Length);

            public readonly bool TryCopyTo(int sourceStartIndex, Span<T> destination, int length)
                => new CopyToSpan<T>(AsReadOnlySpan()).TryCopyTo(
                      sourceStartIndex
                    , destination
                    , length
                );

            public readonly T[] ToArray()
                => AsReadOnlySpan().ToArray();

            public readonly ref readonly T Peek()
            {
                ThrowHelper.ThrowIfEmpty(_count > 0, ThrowHelper.CollectionType.ListUnsafe);
                return ref _buffer[_count - 1];
            }

            public readonly Enumerator GetEnumerator()
                => new(this);

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            public static implicit operator ReadOnly(in ListUnsafe<T> source)
                => new(source);

            public static implicit operator ReadOnlySpan<T>(in ReadOnly source)
                => source.AsReadOnlySpan();
        }

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly ReadOnly _list;
            private readonly int _version;
            private int _index;
            private T _current;

            internal Enumerator(in ReadOnly list)
            {
                _list = list;
                _version = list._version;
                _index = 0;
                _current = default;
            }

            public bool MoveNext()
            {
                // UnsafeList-style enumeration is a snapshot and intentionally does not invalidate on mutation.
                if ((uint)_index < (uint)_list.Count)
                {
                    _current = _list[_index++];
                    return true;
                }
                _index = _list.Count + 1;
                _current = default;
                return false;
            }

            public readonly T Current
                => _current;

            readonly object IEnumerator.Current
                => Current;

            public void Reset()
            {
                _index = 0;
                _current = default;
            }

            public readonly void Dispose()
            {
            }
        }
    }
}

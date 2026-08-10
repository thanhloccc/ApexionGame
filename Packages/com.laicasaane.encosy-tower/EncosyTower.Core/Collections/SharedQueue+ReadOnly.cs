using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EncosyTower.Common;

namespace EncosyTower.Collections
{
    public partial class SharedQueue<T, TNative>
        where T : unmanaged
        where TNative : unmanaged
    {
        public readonly struct ReadOnly
            : IReadOnlyCollection<T>
            , IHasCapacity
            , IHasCount
            , IIsCreated
            , ITryCopyToSpan<T>
        {
            internal readonly SharedQueue<T, TNative> _queue;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReadOnly(SharedQueue<T, TNative> queue)
            {
                _queue = queue;
            }

            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _queue != null && _queue.IsCreated;
            }

            public readonly int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _queue?.Count ?? 0;
            }

            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _queue?.Capacity ?? 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly T Peek()
                => _queue.Peek();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryPeek(out T value)
                => _queue.TryPeek(out value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly T[] ToArray()
                => _queue.ToArray();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyTo(Span<T> destination)
                => _queue.CopyTo(destination);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyTo(Span<T> destination, int length)
                => _queue.CopyTo(destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyTo(int sourceStartIndex, Span<T> destination)
                => _queue.CopyTo(sourceStartIndex, destination);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyTo(int sourceStartIndex, Span<T> destination, int length)
                => _queue.CopyTo(sourceStartIndex, destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryCopyTo(Span<T> destination)
                => _queue.TryCopyTo(destination);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryCopyTo(Span<T> destination, int length)
                => _queue.TryCopyTo(destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryCopyTo(int sourceStartIndex, Span<T> destination)
                => _queue.TryCopyTo(sourceStartIndex, destination);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryCopyTo(int sourceStartIndex, Span<T> destination, int length)
                => _queue.TryCopyTo(sourceStartIndex, destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly Enumerator GetEnumerator()
                => new(this);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => GetEnumerator();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly SharedQueueNative<TNative>.ReadOnly AsNative()
                => _queue.AsNative().AsReadOnly();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ReadOnly(SharedQueue<T, TNative> queue)
                => new(queue);
        }
    }
}

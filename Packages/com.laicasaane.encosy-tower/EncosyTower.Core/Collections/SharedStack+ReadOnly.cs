using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EncosyTower.Common;

namespace EncosyTower.Collections
{
    public partial class SharedStack<T, TNative>
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
            internal readonly SharedStack<T, TNative> _stack;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReadOnly(SharedStack<T, TNative> stack)
            {
                _stack = stack;
            }

            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _stack != null && _stack.IsCreated;
            }

            public readonly int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _stack?.Count ?? 0;
            }

            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _stack?.Capacity ?? 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly T Peek()
                => _stack.Peek();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryPeek(out T value)
                => _stack.TryPeek(out value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly T[] ToArray()
                => _stack.ToArray();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyTo(Span<T> destination)
                => _stack.CopyTo(destination);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyTo(Span<T> destination, int length)
                => _stack.CopyTo(destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyTo(int sourceStartIndex, Span<T> destination)
                => _stack.CopyTo(sourceStartIndex, destination);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyTo(int sourceStartIndex, Span<T> destination, int length)
                => _stack.CopyTo(sourceStartIndex, destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryCopyTo(Span<T> destination)
                => _stack.TryCopyTo(destination);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryCopyTo(Span<T> destination, int length)
                => _stack.TryCopyTo(destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryCopyTo(int sourceStartIndex, Span<T> destination)
                => _stack.TryCopyTo(sourceStartIndex, destination);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryCopyTo(int sourceStartIndex, Span<T> destination, int length)
                => _stack.TryCopyTo(sourceStartIndex, destination, length);

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
            public readonly SharedStackNative<TNative>.ReadOnly AsNative()
                => _stack.AsNative().AsReadOnly();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ReadOnly(SharedStack<T, TNative> stack)
                => new(stack);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EncosyTower.Collections.Unsafe;
using EncosyTower.Common;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections
{
    partial class SharedList<T, TNative>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnly AsReadOnly()
            => new(this);

        public readonly partial struct ReadOnly : IReadOnlyList<T>, IToArray<T>, IReadOnlyIndexer<T>
            , IAsReadOnlySpan<T>, ICopyToSpan<T>, ITryCopyToSpan<T>, IHasCapacity, IHasCount, IIsCreated
        {
            private static readonly SharedList<T, TNative> s_emptyOwner = new();
            private static readonly ReadOnly s_empty = new(s_emptyOwner);

            internal readonly NativeArray<T>.ReadOnly _buffer;
            internal readonly NativeArray<int>.ReadOnly _count;
            internal readonly NativeArray<int>.ReadOnly _version;
            internal readonly unsafe SharedListUnsafe<TNative>* _nativeData;

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            internal readonly AtomicSafetyHandle _nativeSafety;
#endif

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnly(SharedList<T, TNative> list)
            {
                _buffer = list._buffer.AsNativeArray().Reinterpret<T>().AsReadOnly();
                _count = list._count.AsNativeArray().AsReadOnly();
                _version = list._version.AsNativeArray().AsReadOnly();
                // SAFETY: The read-only view borrows the live list header without taking ownership.
                unsafe
                {
                    _nativeData = list._nativeData;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                _nativeSafety = list._buffer.GetSafetyHandle();
#endif
            }

            private unsafe ReadOnly(
                  NativeArray<T>.ReadOnly buffer
                , NativeArray<int>.ReadOnly count
                , NativeArray<int>.ReadOnly version
                , SharedListUnsafe<TNative>* nativeData
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                , AtomicSafetyHandle nativeSafety
#endif
            )
            {
                _buffer = buffer;
                _count = count;
                _version = version;
                // SAFETY: The constructor receives the live header pointer from its owning list view.
                unsafe
                {
                    _nativeData = nativeData;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                _nativeSafety = nativeSafety;
#endif
            }

            public static ReadOnly Empty
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => s_empty;
            }

            public bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _buffer.IsCreated && _count.IsCreated && _version.IsCreated;
            }

            public int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _count[0];
            }

            public int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _buffer.Length;
            }

            public bool IsReadOnly
                => true;

            internal int Version
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _version[0];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator GetEnumerator()
                => new(this);

            public T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    ThrowIfIndexIsOutOfRange((uint)index < (uint)Count);
                    return _buffer.AsReadOnlySpan()[index];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ReadOnly(SharedList<T, TNative> list)
                => list is not null ? list.AsReadOnly() : Empty;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ReadOnlySpan<T>(in ReadOnly list)
                => list.AsReadOnlySpan();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<T> AsReadOnlySpan()
                => _buffer.AsReadOnlySpan()[..Count];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CopyTo(T[] destination, int destinationIndex)
                => CopyTo(destination.AsSpan().Slice(destinationIndex, Count));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CopyTo(Span<T> destination)
                => CopyTo(0, destination);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CopyTo(Span<T> destination, int length)
                => CopyTo(0, destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CopyTo(int sourceStartIndex, Span<T> destination)
                => CopyTo(sourceStartIndex, destination, destination.Length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CopyTo(int sourceStartIndex, Span<T> destination, int length)
                => new CopyToSpan<T>(AsReadOnlySpan()).CopyTo(sourceStartIndex, destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryCopyTo(Span<T> destination)
                => TryCopyTo(0, destination);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryCopyTo(Span<T> destination, int length)
                => TryCopyTo(0, destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryCopyTo(int sourceStartIndex, Span<T> destination)
                => TryCopyTo(sourceStartIndex, destination, destination.Length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryCopyTo(int sourceStartIndex, Span<T> destination, int length)
                => new CopyToSpan<T>(AsReadOnlySpan()).TryCopyTo(sourceStartIndex, destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T[] ToArray()
                => AsReadOnlySpan().ToArray();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public SharedList<U, TNative>.ReadOnly Reinterpret<U>()
                where U : unmanaged
            {
                // SAFETY: The reinterpreted arrays preserve the original allocation and safety-handle lifetime.
                unsafe
                {
                    return new SharedList<U, TNative>.ReadOnly(
                          _buffer.Reinterpret<U>()
                        , _count
                        , _version
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                        , _nativeData
                        , _nativeSafety
#else
                        , _nativeData
#endif
                    );
                }
            }

            [HideInCallstack, StackTraceHidden]
            [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
            [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
            [Conditional(UNITY_COLLECTIONS_CHECKS)]
            private static void ThrowIfIndexIsOutOfRange([DoesNotReturnIf(false)] bool isWithinRange)
            {
                if (isWithinRange == false)
                {
                    throw CreateException();
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                static InvalidOperationException CreateException()
                    => new("index is outside the range of valid indices for the SharedList<T>.ReadOnly");
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();
        }
    }
}

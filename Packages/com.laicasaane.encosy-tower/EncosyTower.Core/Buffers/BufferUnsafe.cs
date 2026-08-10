using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EncosyTower.Collections;
using EncosyTower.Collections.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Buffers
{
    public struct BufferUnsafe<T> : IBuffer<T>, IRefIndexer<T>
#if UNITY_COLLECTIONS
        , INativeDisposable
#endif
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal unsafe void* _buffer;
        internal int _capacity;
        internal AllocatorStrategy _allocator;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferUnsafe(int size, AllocatorStrategy allocatorStrategy, bool clear = true) : this()
        {
            ThrowHelper.ThrowIfInvalidAllocatorStrategy(allocatorStrategy.IsValid);

            Alloc(size, allocatorStrategy, clear);
        }

        /// <safety>buffer must reference capacity writable bytes owned by the caller; this view does not take ownership.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe BufferUnsafe(void* buffer, int capacity, AllocatorStrategy allocator)
        {
            // SAFETY: The caller supplies a valid borrowed buffer and its capacity/allocator contract.
            unsafe
            {
                _buffer = buffer;
            }

            _capacity = capacity;
            _allocator = allocator;
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // SAFETY: Reading the pointer field only observes the allocation state.
                unsafe
                {
                    return _allocator.IsValid && ((_capacity > 0 && _buffer != null) || _capacity == 0);
                }
            }
        }

        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _capacity;
        }

        public readonly ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
                unsafe
                {
                    return ref UnsafeUtility.ArrayElementAsRef<T>(_buffer, index);
                }
            }
        }

        public void Alloc(int newCapacity, AllocatorStrategy allocatorStrategy, bool memClear = true)
        {
            // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
            unsafe
            {
                ThrowHelper.ThrowIfBufferAlreadyAllocated(IsCreated == false);

                _buffer = newCapacity > 0
                    ? allocatorStrategy.Allocate(
                          UnsafeUtility.SizeOf<T>()
                        , UnsafeUtility.AlignOf<T>()
                        , newCapacity
                    )
                    : null;

                _capacity = newCapacity;
                // Keep the caller's strategy as-is: storing the resolved handle would
                // silently turn an Allocator-typed strategy into a handle-typed one.
                _allocator = allocatorStrategy;

                if (memClear && newCapacity > 0)
                {
                    UnsafeUtility.MemClear(_buffer, newCapacity * (long)UnsafeUtility.SizeOf<T>());
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(int newCapacity)
            => Resize(newCapacity, true, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(int newCapacity, bool copyContent)
            => Resize(newCapacity, copyContent, true);

        public void Resize(int newCapacity, bool copyContent, bool memClear)
        {
            // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
            unsafe
            {
                ThrowHelper.ThrowIfResizeUninitializedBuffer(IsCreated);

                if (newCapacity == _capacity)
                {
                    return;
                }

                var newBuffer = newCapacity > 0
                    ? _allocator.Allocate(
                          UnsafeUtility.SizeOf<T>()
                        , UnsafeUtility.AlignOf<T>()
                        , newCapacity
                    )
                    : null;

                var copyCount = Math.Min(_capacity, newCapacity);

                if (copyContent && copyCount > 0)
                {
                    UnsafeUtility.MemCpy(newBuffer, _buffer, copyCount * (long)UnsafeUtility.SizeOf<T>());
                }
                else
                {
                    copyCount = 0;
                }

                if (memClear && newCapacity > copyCount)
                {
                    // Only the tail that did not receive copied content needs clearing.
                    var tail = (byte*)newBuffer + (copyCount * (long)UnsafeUtility.SizeOf<T>());
                    UnsafeUtility.MemClear(tail, (newCapacity - copyCount) * (long)UnsafeUtility.SizeOf<T>());
                }

                if (_buffer != null)
                {
                    _allocator.Free(_buffer);
                }

                _buffer = newBuffer;
                _capacity = newCapacity;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void FastClear() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Clear()
        {
            // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
            unsafe
            {
                if (_buffer != null)
                {
                    UnsafeUtility.MemClear(_buffer, _capacity * (long)UnsafeUtility.SizeOf<T>());
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyFrom(ReadOnlySpan<T> source)
            => CopyFrom(0, source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyFrom(ReadOnlySpan<T> source, int length)
            => CopyFrom(0, source, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyFrom(int destinationStartIndex, ReadOnlySpan<T> source)
            => CopyFrom(destinationStartIndex, source, source.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyFrom(int destinationStartIndex, ReadOnlySpan<T> source, int length)
            => new CopyFromSpan<T>(AsSpan()).CopyFrom(destinationStartIndex, source, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyFrom(ReadOnlySpan<T> source)
            => TryCopyFrom(0, source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyFrom(ReadOnlySpan<T> source, int length)
            => TryCopyFrom(0, source, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyFrom(int destinationStartIndex, ReadOnlySpan<T> source)
            => TryCopyFrom(destinationStartIndex, source, source.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyFrom(int destinationStartIndex, ReadOnlySpan<T> source, int length)
            => new CopyFromSpan<T>(AsSpan()).TryCopyFrom(destinationStartIndex, source, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(Span<T> destination)
            => CopyTo(0, destination);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(Span<T> destination, int length)
            => CopyTo(0, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(int sourceStartIndex, Span<T> destination)
            => CopyTo(sourceStartIndex, destination, destination.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(int sourceStartIndex, Span<T> destination, int length)
            => new CopyToSpan<T>(AsReadOnlySpan()).CopyTo(sourceStartIndex, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(Span<T> destination)
            => TryCopyTo(0, destination);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(Span<T> destination, int length)
            => TryCopyTo(0, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(int sourceStartIndex, Span<T> destination)
            => TryCopyTo(sourceStartIndex, destination, destination.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(int sourceStartIndex, Span<T> destination, int length)
            => new CopyToSpan<T>(AsReadOnlySpan()).TryCopyTo(sourceStartIndex, destination, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>
        /// The returned pointer is valid only while this buffer remains created; callers must uphold its lifetime and bounds.
        /// </safety>
        public readonly unsafe void* GetUnsafePtr()
        {
            // SAFETY: This method intentionally exposes the owned or borrowed buffer without extending its lifetime.
            unsafe
            {
                return _buffer;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan()
        {
            // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
            unsafe
            {
                return new Span<T>(_buffer, _capacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<T> AsReadOnlySpan()
        {
            // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
            unsafe
            {
                return new ReadOnlySpan<T>(_buffer, _capacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnly AsReadOnly()
            => this;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BufferUnsafe<U> Reinterpret<U>()
            where U : unmanaged
        {
            // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
            unsafe
            {
                ThrowHelper.ThrowIfTypesNotEqualSize<T, U>(
                    UnsafeUtility.SizeOf<T>() == UnsafeUtility.SizeOf<U>()
                );

                return new BufferUnsafe<U>(_buffer, _capacity, _allocator);
            }
        }

        public void Dispose()
        {
            if (IsCreated == false)
            {
                return;
            }

            // SAFETY: IsCreated established ownership of the live allocation before freeing it.
            unsafe
            {
                var pointer = _buffer;
                var allocator = _allocator;

                _buffer = null;
                _capacity = 0;
                _allocator = default;

                if (pointer != null)
                {
                    allocator.Free(pointer);
                }
            }
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (IsCreated == false)
            {
                return inputDeps;
            }

            // SAFETY: The scheduled job receives the live buffer and takes ownership of its release.
            unsafe
            {
                var pointer = _buffer;
                var allocator = _allocator;

                _buffer = null;
                _capacity = 0;
                _allocator = default;

                if (pointer == null)
                {
                    return inputDeps;
                }

                return new EncosyMemoryAPI.DisposeJob {
                    ptr = pointer,
                    allocator = allocator,
                }.Schedule(inputDeps);
            }
        }

        public readonly struct ReadOnly : IReadOnlyBuffer<T>, IRefReadOnlyIndexer<T>
        {
            [NativeDisableUnsafePtrRestriction]
            internal readonly unsafe void* _buffer;
            internal readonly int _capacity;
            internal readonly AllocatorStrategy _allocator;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private unsafe ReadOnly(void* buffer, int capacity, AllocatorStrategy allocator)
            {
                // SAFETY: The constructor receives a borrowed pointer from the owning buffer.
                unsafe
                {
                    _buffer = buffer;
                }
                _capacity = capacity;
                _allocator = allocator;
            }

            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    // SAFETY: Reading the pointer field only observes the alias state.
                    unsafe
                    {
                        return _buffer != null && _allocator.IsValid;
                    }
                }
            }

            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _capacity;
            }

            public readonly ref readonly T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
                    unsafe
                    {
                        return ref UnsafeUtility.ArrayElementAsRef<T>(_buffer, index);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyTo(Span<T> destination)
                => CopyTo(0, destination);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyTo(Span<T> destination, int length)
                => CopyTo(0, destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyTo(int sourceStartIndex, Span<T> destination)
                => CopyTo(sourceStartIndex, destination, destination.Length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void CopyTo(int sourceStartIndex, Span<T> destination, int length)
                => new CopyToSpan<T>(AsReadOnlySpan()).CopyTo(sourceStartIndex, destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryCopyTo(Span<T> destination)
                => TryCopyTo(0, destination);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryCopyTo(Span<T> destination, int length)
                => TryCopyTo(0, destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryCopyTo(int sourceStartIndex, Span<T> destination)
                => TryCopyTo(sourceStartIndex, destination, destination.Length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryCopyTo(int sourceStartIndex, Span<T> destination, int length)
                => new CopyToSpan<T>(AsReadOnlySpan()).TryCopyTo(sourceStartIndex, destination, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            /// <safety>
            /// The returned pointer is valid only while the originating buffer remains created and undisposed.
            /// </safety>
            public readonly unsafe void* GetUnsafeReadOnlyPtr()
            {
                // SAFETY: This method intentionally exposes the borrowed buffer without extending its lifetime.
                unsafe
                {
                    return _buffer;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly ReadOnlySpan<T> AsReadOnlySpan()
            {
                // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
                unsafe
                {
                    return new ReadOnlySpan<T>(_buffer, _capacity);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly BufferUnsafe<U>.ReadOnly Reinterpret<U>()
                where U : unmanaged
            {
                // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
                unsafe
                {
                    ThrowHelper.ThrowIfTypesNotEqualSize<T, U>(
                        UnsafeUtility.SizeOf<T>() == UnsafeUtility.SizeOf<U>()
                    );

                    var buffer = new BufferUnsafe<U>(_buffer, _capacity, _allocator);
                    return buffer.AsReadOnly();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ReadOnly(BufferUnsafe<T> buffer)
            {
                // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
                unsafe
                {
                    return new ReadOnly(buffer._buffer, buffer._capacity, buffer._allocator);
                }
            }
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfAlreadyDisposed([DoesNotReturnIf(false)] bool isCreated)
        {
            if (isCreated == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("Cannot dispose an already disposed buffer.");
        }

    }
}

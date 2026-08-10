using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using EncosyTower.Common;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.Collections.Unsafe
{
    [DebuggerDisplay("Value = {Value}")]
    public struct ReferenceUnsafe<T> : IDisposable, IEquatable<ReferenceUnsafe<T>>
        , ICopyToSpan<T>, ITryCopyToSpan<T>
        , ICopyFromSpan<T>, ITryCopyFromSpan<T>
        , IIsCreated, IHasLength, IIndexer<T>
        , IAsSpan<T>, IAsReadOnlySpan<T>
#if UNITY_COLLECTIONS
        , INativeDisposable
#endif
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private unsafe void* _data;

        private AllocatorStrategy _allocator;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReferenceUnsafe(
              AllocatorStrategy allocator
            , NativeArrayOptions options = NativeArrayOptions.ClearMemory
        )
        {
            Allocate(allocator, out this);

            if (options != NativeArrayOptions.ClearMemory)
            {
                return;
            }

            // SAFETY: Allocate produced one writable T-sized allocation owned by this value.
            unsafe
            {
                UnsafeUtility.MemClear(_data, UnsafeUtility.SizeOf<T>());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReferenceUnsafe(T value, AllocatorStrategy allocator)
        {
            Allocate(allocator, out this);
            // SAFETY: Allocate produced one writable T-sized allocation owned by this value.
            unsafe
            {
                *(T*)_data = value;
            }
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // SAFETY: Reading the pointer field only observes the allocation state.
                unsafe
                {
                    return (IntPtr)_data != IntPtr.Zero;
                }
            }
        }

        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsCreated ? 1 : 0;
        }

        /// <safety>
        /// The reference must remain created and undisposed for the duration of the access.
        /// </safety>
        public readonly unsafe T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // SAFETY: Callers must keep this manually allocated reference alive and created.
                unsafe
                {
                    return *(T*)_data;
                }
            }

            [WriteAccessRequired]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                // SAFETY: Callers must keep this manually allocated reference alive and created.
                unsafe
                {
                    *(T*)_data = value;
                }
            }
        }

        /// <safety>
        /// The returned reference is valid only while this allocation remains created and undisposed.
        /// </safety>
        public readonly unsafe ref T ValueAsRef
        {
            [WriteAccessRequired]
            get
            {
                // SAFETY: Callers must keep this manually allocated reference alive and created.
                unsafe
                {
                    return ref UnsafeUtility.AsRef<T>(_data);
                }
            }
        }

        /// <safety>The index must be zero and the reference must remain created for the duration
        /// of the access.</safety>
        public readonly unsafe T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowIfIndexOutOfRange(index);
                // SAFETY: The index is validated and callers must keep this allocation alive.
                unsafe
                {
                    return *(T*)_data;
                }
            }

            [WriteAccessRequired]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                ThrowIfIndexOutOfRange(index);
                // SAFETY: The index is validated and callers must keep this allocation alive.
                unsafe
                {
                    *(T*)_data = value;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ReferenceUnsafe<T> left, ReferenceUnsafe<T> right)
            => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ReferenceUnsafe<T> left, ReferenceUnsafe<T> right)
            => !left.Equals(right);

        [WriteAccessRequired]
        public void Dispose()
        {
            ThrowHelper.ThrowIfUnsafeCollectionIsDisposed(
                IsCreated,
                ThrowHelper.CollectionType.ReferenceUnsafe
            );
            ThrowHelper.ThrowIfUnsafeCollectionAllocatorIsInvalid(
                _allocator.IsValid,
                ThrowHelper.CollectionType.ReferenceUnsafe
            );

            if (ShouldDeallocate(_allocator))
            {
                // SAFETY: The allocation strategy owns this pointer and IsCreated was validated above.
                unsafe
                {
                    _allocator.Free(_data);
                }
            }

            // SAFETY: The owned allocation has been released, so clearing the pointer is a local state update.
            unsafe
            {
                _data = null;
            }
            _allocator = default;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            ThrowHelper.ThrowIfUnsafeCollectionAllocatorIsInvalid(
                _allocator.IsValid,
                ThrowHelper.CollectionType.ReferenceUnsafe
            );
            ThrowHelper.ThrowIfUnsafeCollectionIsDisposed(
                IsCreated,
                ThrowHelper.CollectionType.ReferenceUnsafe
            );

            if (ShouldDeallocate(_allocator))
            {
                // SAFETY: The scheduled job receives the live allocation and takes ownership of its release.
                unsafe
                {
                    var jobHandle = new EncosyMemoryAPI.DisposeJob {
                        ptr = _data,
                        allocator = _allocator,
                    }.Schedule(inputDeps);

                    _data = null;
                    _allocator = default;
                    return jobHandle;
                }
            }

            // SAFETY: No allocation remains to release, so clearing the pointer is a local state update.
            unsafe
            {
                _data = null;
            }
            _allocator = default;
            return inputDeps;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>
        /// The returned pointer is valid only while this reference remains created and undisposed;
        /// callers must uphold its lifetime.
        /// </safety>
        public readonly unsafe void* GetUnsafePtr()
        {
            // SAFETY: This method intentionally exposes the owned allocation without extending its lifetime.
            unsafe
            {
                return _data;
            }
        }

        /// <summary>
        /// Creates a non-owning view over existing memory (e.g. an element of a pinned
        /// managed array). <see cref="Dispose()"/> on a view frees nothing.
        /// </summary>
        internal static unsafe ReferenceUnsafe<T> ConvertExistingData(void* data)
        {
            var reference = default(ReferenceUnsafe<T>);
            // SAFETY: The caller supplies a valid non-owning buffer for the view.
            unsafe
            {
                reference._data = data;
            }
            reference._allocator = new AllocatorStrategy(Allocator.None);
            return reference;
        }

        [WriteAccessRequired]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyFrom(ReferenceUnsafe<T> reference)
            => Copy(this, reference);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(ReferenceUnsafe<T> reference)
            => Copy(reference, this);

        [WriteAccessRequired]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyFrom(ReadOnlySpan<T> source)
            => CopyFrom(0, source);

        [WriteAccessRequired]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyFrom(ReadOnlySpan<T> source, int length)
            => CopyFrom(0, source, length);

        [WriteAccessRequired]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyFrom(int destinationStartIndex, ReadOnlySpan<T> source)
            => CopyFrom(destinationStartIndex, source, source.Length);

        [WriteAccessRequired]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyFrom(int destinationStartIndex, ReadOnlySpan<T> source, int length)
            => new CopyFromSpan<T>(AsSpan()).CopyFrom(destinationStartIndex, source, length);

        [WriteAccessRequired]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyFrom(ReadOnlySpan<T> source)
            => TryCopyFrom(0, source);

        [WriteAccessRequired]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyFrom(ReadOnlySpan<T> source, int length)
            => TryCopyFrom(0, source, length);

        [WriteAccessRequired]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyFrom(int destinationStartIndex, ReadOnlySpan<T> source)
            => TryCopyFrom(destinationStartIndex, source, source.Length);

        [WriteAccessRequired]
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
        public readonly bool Equals(ReferenceUnsafe<T> other)
        {
            // SAFETY: Equality reads both manually allocated values; callers must keep both references created.
            unsafe
            {
                return Value.Equals(other.Value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override bool Equals(object obj)
            => obj != null && obj is ReferenceUnsafe<T> other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override int GetHashCode()
        {
            // SAFETY: Hashing reads this manually allocated value; callers must keep the reference created.
            unsafe
            {
                return Value.GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy(ReferenceUnsafe<T> dst, ReferenceUnsafe<T> src)
        {
            ThrowIfSourceNotCreated(src.IsCreated);
            ThrowIfDestinationNotCreated(dst.IsCreated);

            // SAFETY: Both source and destination creation states are validated before copying exactly one T.
            unsafe
            {
                UnsafeUtility.MemCpy(dst._data, src._data, UnsafeUtility.SizeOf<T>());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan()
        {
            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                return new Span<T>(_data, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<T> AsReadOnlySpan()
        {
            // SAFETY: The established ownership and safety checks keep the native storage live
            // for this pointer dereference.
            unsafe
            {
                return new ReadOnlySpan<T>(_data, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>
        /// The alias does not own storage; it must not outlive the underlying reference allocation.
        /// </safety>
        public readonly unsafe ReadOnly AsReadOnly()
        {
            // SAFETY: The alias borrows this reference's allocation without transferring ownership.
            unsafe
            {
                return new(_data);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnly(ReferenceUnsafe<T> reference)
        {
            // SAFETY: The returned alias borrows reference's allocation and inherits its lifetime contract.
            unsafe
            {
                return reference.AsReadOnly();
            }
        }

        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void CheckAllocateArguments(AllocatorStrategy allocator)
        {
            ThrowHelper.ThrowIfUnsafeCollectionAllocatorIsInvalid(
                allocator.IsValid,
                ThrowHelper.CollectionType.ReferenceUnsafe
            );
        }

        private static void Allocate(AllocatorStrategy allocator, out ReferenceUnsafe<T> reference)
        {
            CheckAllocateArguments(allocator);
            reference = default;
            IsUnmanagedAndThrow();
            reference._allocator = allocator;
            // SAFETY: The validated allocator returns storage sized and aligned for unmanaged T.
            unsafe
            {
                reference._data = allocator.Allocate<T>();
            }
        }

        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
#if UNITY_BURST
        [Unity.Burst.BurstDiscard]
#endif
        private static void IsUnmanagedAndThrow()
            => ThrowHelper.ThrowIfUnsafeCollectionTypeIsManaged<T>(
                UnsafeUtility.IsUnmanaged<T>(),
                ThrowHelper.CollectionType.ReferenceUnsafe
            );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldDeallocate(AllocatorStrategy allocator)
        {
#if UNITY_COLLECTIONS
            if (allocator.TryGetAllocatorHandle(out _))
            {
                return true;
            }
#endif

            return allocator.ToAllocator() > Allocator.None;
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfSourceNotCreated([DoesNotReturnIf(false)] bool isCreated)
        {
            if (isCreated == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("The source UnsafeReference is not created.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfDestinationNotCreated([DoesNotReturnIf(false)] bool isCreated)
        {
            if (isCreated == false)
            {
                throw CreateException();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException()
                => new("The destination UnsafeReference is not created.");
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        private static void ThrowIfIndexOutOfRange(int index)
        {
            if (index != 0)
            {
                throw CreateException(index);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static IndexOutOfRangeException CreateException(int index)
                => new($"Index {index} is out of range of the UnsafeReference which only contains 1 element.");
        }

        /// <summary>
        /// A read-only alias for the value of an UnsafeReference. Does not have its own allocated
        /// storage.
        /// </summary>
        public readonly struct ReadOnly : IIsCreated
        {
            [NativeDisableUnsafePtrRestriction]
            private readonly unsafe void* _data;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal unsafe ReadOnly(void* data)
            {
                // SAFETY: The constructor receives a borrowed pointer from its owning reference.
                unsafe
                {
                    _data = data;
                }
            }

            public bool IsCreated
            {
                get
                {
                    // SAFETY: Reading the pointer field only observes the alias state.
                    unsafe
                    {
                        return (IntPtr)_data != IntPtr.Zero;
                    }
                }
            }

            /// <safety>
            /// The alias must remain backed by a live allocation owned by the originating reference.
            /// </safety>
            public unsafe T Value
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    // SAFETY: The originating reference owns the allocation for the alias lifetime.
                    unsafe
                    {
                        return *(T*)_data;
                    }
                }
            }
        }
    }
}

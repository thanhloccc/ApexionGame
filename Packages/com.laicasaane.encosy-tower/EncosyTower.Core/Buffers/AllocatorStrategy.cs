using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EncosyTower.Common;
using EncosyTower.Collections.Unsafe;
using Unity.Collections;

namespace EncosyTower.Buffers
{
    internal enum AllocatorStrategyType : uint
    {
        None = 0,
        Allocator = 1,
        AllocatorHandle = 2,
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct AllocatorStrategy : IIsValid
    {
        // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
        [FieldOffset(0)] private readonly Allocator _allocator;
        // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
        [FieldOffset(4)] private readonly AllocatorStrategyType _type;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AllocatorStrategy(Allocator allocator) : this()
        {
            _allocator = allocator;
            _type = AllocatorStrategyType.Allocator;
        }

        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _type is AllocatorStrategyType.Allocator or AllocatorStrategyType.AllocatorHandle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator AllocatorStrategy(Allocator allocator)
            => new(allocator);

        public bool TryGetAllocator(out Allocator result)
        {
            if (_type == AllocatorStrategyType.Allocator)
            {
                result = _allocator;
                return true;
            }

            result = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Allocator ToAllocator()
        {
            if (_type == AllocatorStrategyType.Allocator)
            {
                return _allocator;
            }

#if UNITY_COLLECTIONS
            if (_type == AllocatorStrategyType.AllocatorHandle)
            {
                return _handle.ToAllocator;
            }
#endif

            return Allocator.Invalid;
        }

        /// <safety>The returned pointer is owned by this allocator strategy and must be freed exactly once with the same strategy.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void* Allocate(long sizeOf, int alignOf, long count)
        {
            // SAFETY: The allocator strategy validates ownership and the caller receives the allocated buffer.
            unsafe
            {
                return EncosyMemoryAPI.Unmanaged.Allocate(sizeOf, alignOf, count, this);
            }
        }

        /// <safety>ptr must have been allocated by this strategy and must not be used after this call.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Free(void* ptr)
        {
            // SAFETY: ptr was allocated with this strategy and is released exactly once.
            unsafe
            {
                EncosyMemoryAPI.Unmanaged.Free(ptr, this);
            }
        }

        /// <safety>The returned pointer is aligned storage for one unmanaged T and must be freed with this strategy.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe T* Allocate<T>()
            where T : unmanaged
        {
            // SAFETY: The allocator strategy returns storage sized and aligned for unmanaged T.
            unsafe
            {
                return EncosyMemoryAPI.Unmanaged.Allocate<T>(this);
            }
        }

        /// <safety>The returned pointer is aligned storage for count unmanaged T elements and must be freed with this strategy.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe T* AllocateArray<T>(long count)
            where T : unmanaged
        {
            // SAFETY: The allocator strategy returns storage sized and aligned for count unmanaged T elements.
            unsafe
            {
                return EncosyMemoryAPI.Unmanaged.Array.Allocate<T>(count, this);
            }
        }

        /// <safety>ptr must have been allocated by this strategy and must not be used after this call.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Free<T>(T* ptr)
            where T : unmanaged
        {
            // SAFETY: ptr was allocated with this strategy and is released exactly once.
            unsafe
            {
                EncosyMemoryAPI.Unmanaged.Free(ptr, this);
            }
        }

        /// <safety>ptr must have been allocated for count unmanaged T elements by this strategy and must not be used after this call.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void FreeArray<T>(T* ptr, long count)
            where T : unmanaged
        {
            // SAFETY: ptr was allocated with this strategy for count unmanaged T elements.
            unsafe
            {
                EncosyMemoryAPI.Unmanaged.Array.Free(ptr, count, this);
            }
        }

#if UNITY_COLLECTIONS
        // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
        [FieldOffset(0)] private readonly AllocatorManager.AllocatorHandle _handle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AllocatorStrategy(AllocatorManager.AllocatorHandle handle) : this()
        {
            _handle = handle;
            _type = AllocatorStrategyType.AllocatorHandle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator AllocatorStrategy(AllocatorManager.AllocatorHandle handle)
            => new(handle);

        public bool TryGetAllocatorHandle(out AllocatorManager.AllocatorHandle result)
        {
            if (_type == AllocatorStrategyType.AllocatorHandle)
            {
                result = _handle;
                return true;
            }

            result = default;
            return false;
        }

        internal bool TryGetCustomAllocatorHandle(out AllocatorManager.AllocatorHandle result)
        {
            if (_type == AllocatorStrategyType.AllocatorHandle)
            {
                result = _handle;
                return result.IsCustomAllocator;
            }

            var handle = AllocatorManager.ConvertToAllocatorHandle(_allocator);
            var isCustom = _type == AllocatorStrategyType.Allocator && handle.IsCustomAllocator;

            result = isCustom ? handle : default;
            return isCustom;
        }
#endif
    }
}

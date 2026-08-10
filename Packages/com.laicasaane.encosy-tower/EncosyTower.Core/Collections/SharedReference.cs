// https://github.com/stella3d/SharedArray

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EncosyTower.Collections.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections
{
    /// <summary>
    /// An array of length 1, usable as both a NativeArray and managed array
    /// </summary>
    /// <typeparam name="T">The type of the array element</typeparam>
    public sealed class SharedReference<T> : SharedReference<T, T>
        where T : unmanaged
    {
        public SharedReference() : base()
        {
        }

        public SharedReference(T value) : base(value)
        {
        }
    }

    /// <summary>
    /// An array of length 1, usable as both a native and managed array
    /// </summary>
    /// <typeparam name="T">The element type in the managed representation.</typeparam>
    /// <typeparam name="TNative">The element type in the NativeArray representation. Must be
    /// the same size as <typeparamref name="T"/>.</typeparam>
    public class SharedReference<T, TNative> : IDisposable
        , IAsSpan<T>, IAsReadOnlySpan<T>, IAsMemory<T>, IAsReadOnlyMemory<T>
        , IAsNativeArray<TNative>, IAsNativeSlice<TNative>
        where T : unmanaged
        where TNative : unmanaged
    {
#pragma warning disable IDE1006 // Naming Styles
        internal GCHandle _gcHandle;

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
        private AtomicSafetyHandle m_Safety;
#endif

        internal T[] _managed;
        internal NativeArray<TNative> _native;
        internal int _version;
#pragma warning restore IDE1006 // Naming Styles

        protected SharedReference()
        {
            ThrowHelper.ThrowIfNativeAliasTypesHaveDifferentSize<T, TNative>(AreTypesEqualSize());
            Initialize(default);
        }

        public SharedReference(T value)
        {
            ThrowHelper.ThrowIfNativeAliasTypesHaveDifferentSize<T, TNative>(AreTypesEqualSize());
            Initialize(value);
        }

        ~SharedReference()
        {
            Dispose();
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _managed.Length;
        }

        public ref T ValueRW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

                _version++;
                return ref _managed[0];
            }
        }

        public ref readonly T ValueRO
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

                return ref _managed[0];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator T[]([NotNull] SharedReference<T, TNative> self)
        {
            return self.AsManagedArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<T>([NotNull] SharedReference<T, TNative> self)
        {
            return self.AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<T>([NotNull] SharedReference<T, TNative> self)
        {
            return self.AsReadOnlySpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Memory<T>([NotNull] SharedReference<T, TNative> self)
        {
            return self.AsMemory();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlyMemory<T>([NotNull] SharedReference<T, TNative> self)
        {
            return self.AsReadOnlyMemory();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<TNative>([NotNull] SharedReference<T, TNative> self)
        {
            return self.AsSpanNative();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<TNative>([NotNull] SharedReference<T, TNative> self)
        {
            return self.AsReadOnlySpanNative();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeArray<TNative>([NotNull] SharedReference<T, TNative> self)
        {
            return self.AsNativeArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeSlice<TNative>([NotNull] SharedReference<T, TNative> self)
        {
            return self.AsNativeSlice();
        }

        public void Dispose()
        {
            if (_managed == null)
            {
                return;
            }

            _version++;

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
            AtomicSafetyHandle.Release(m_Safety);
#endif

            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }

            _managed = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] AsManagedArray()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            return _managed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            return _managed.AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

            return _managed.AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<T> AsMemory()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            return _managed.AsMemory();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<T> AsReadOnlyMemory()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

            return _managed.AsMemory();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<TNative> AsSpanNative()
        {
            return _native.AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<TNative> AsReadOnlySpanNative()
        {
            return _native.AsReadOnlySpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArray<TNative> AsNativeArray()
        {
            return _native;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<TNative> AsNativeSlice()
        {
            return _native.Slice();
        }

        /// <summary>
        /// Raw pointer into the pinned managed buffer, for the shared native view family.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe TNative* GetUnsafeBufferPointer()
        {
            // SAFETY: The shared reference pins its managed storage for the lifetime of the native alias.
            unsafe
            {
                return (TNative*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(_native);
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
        /// <summary>
        /// The single representative safety handle, reused by the shared native view family.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal AtomicSafetyHandle GetSafetyHandle()
            => m_Safety;
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool AreTypesEqualSize()
        {
            return UnsafeUtility.SizeOf<T>() == UnsafeUtility.SizeOf<TNative>();
        }

        private void Initialize(T value)
        {
            _version++;

            this._managed = new T[] { value };

            // Unity's default garbage collector doesn't move objects around, so pinning the array in memory
            // should not even be necessary. Better to be safe, though
            _gcHandle = GCHandle.Alloc(_managed, GCHandleType.Pinned);
            CreateNativeAlias();

            void CreateNativeAlias()
            {
                // this is the trick to making a NativeArray view of a managed array (or any pointer)
                // SAFETY: _managed is pinned by _gcHandle for the complete duration of this fixed scope.
                unsafe
                {
                    fixed (void* ptr = _managed)
                    {
                        _native = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<TNative>(
                            ptr, _managed.Length, Allocator.None
                        );
                    }
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !DISABLE_SHAREDARRAY_SAFETY
                m_Safety = AtomicSafetyHandle.Create();
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref _native, m_Safety);
#endif
            }
        }
    }
}

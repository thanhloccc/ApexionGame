#if UNITY_COLLECTIONS

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections.Unsafe
{
    public static class EncosyNativeReferenceExtensionsUnsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the native reference alive for the lifetime of the returned reference.</safety>
        public static unsafe ref T ValueAsUnsafeRefRW<T>(this NativeReference<T> reference)
            where T : unmanaged
        {
            // SAFETY: Caller owns the reference lifetime and accepts the writable aliasing contract.
            unsafe
            {
                return ref UnsafeUtility.AsRef<T>(reference.GetUnsafePtr());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the native reference alive for the lifetime of the returned reference.</safety>
        public static unsafe ref readonly T ValueAsUnsafeRefRO<T>(this NativeReference<T> reference)
            where T : unmanaged
        {
            // SAFETY: Caller owns the reference lifetime and accepts the read-only aliasing contract.
            unsafe
            {
                return ref UnsafeUtility.AsRef<T>(reference.GetUnsafePtr());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>The returned span borrows the native reference storage and must not outlive it.</safety>
        public static unsafe Span<T> AsSpan<T>(this NativeReference<T> reference)
            where T : unmanaged
        {
            // SAFETY: The span is one element and borrows the live native reference storage.
            unsafe
            {
                return new Span<T>(reference.GetUnsafePtr(), 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>The returned span borrows the native reference storage and must not outlive it.</safety>
        public static unsafe ReadOnlySpan<T> AsReadOnlySpan<T>(this NativeReference<T> reference)
            where T : unmanaged
        {
            // SAFETY: The span is one element and borrows the live native reference storage.
            unsafe
            {
                return new ReadOnlySpan<T>(reference.GetUnsafeReadOnlyPtr(), 1);
            }
        }
    }
}

#endif

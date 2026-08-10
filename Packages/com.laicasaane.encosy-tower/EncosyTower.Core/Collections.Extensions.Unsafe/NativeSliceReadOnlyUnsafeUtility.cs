using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections.Unsafe
{
    public static class NativeSliceReadOnlyUnsafeUtility
    {
        /// <safety>Caller must keep the slice's backing allocation alive and respect its bounds.</safety>
        public static unsafe void* GetUnsafePtr<T>(this NativeSliceReadOnly<T> nativeSlice)
            where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(nativeSlice.m_Safety);
#endif

            // SAFETY: The write safety handle was checked before exposing the slice backing pointer.
            unsafe
            {
                return nativeSlice._buffer;
            }
        }

        /// <safety>Caller must keep the slice's backing allocation alive and respect its bounds.</safety>
        public static unsafe void* GetUnsafeReadOnlyPtr<T>(this NativeSliceReadOnly<T> nativeSlice)
            where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(nativeSlice.m_Safety);
#endif

            // SAFETY: The read safety handle was checked before exposing the slice backing pointer.
            unsafe
            {
                return nativeSlice._buffer;
            }
        }
    }
}

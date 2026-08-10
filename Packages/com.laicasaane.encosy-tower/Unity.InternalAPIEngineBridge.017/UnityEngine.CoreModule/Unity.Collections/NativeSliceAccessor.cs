using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections.Internals
{
    internal readonly struct NativeSliceAccessor<T> where T : struct
    {
        public readonly NativeSlice<T> Slice;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSliceAccessor(NativeSlice<T> slice)
        {
            Slice = slice;
        }

        /// <safety>The returned pointer borrows Unity's live NativeSlice storage and must not outlive the slice.</safety>
        public unsafe byte* Buffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // SAFETY: Unity owns the NativeSlice buffer and the accessor only exposes its existing pointer.
                unsafe
                {
                    return Slice.m_Buffer;
                }
            }
        }

        public int Stride
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Slice.m_Stride;
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Slice.m_Length;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public int MinIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Slice.m_MinIndex;
        }

        public int MaxIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Slice.m_MaxIndex;
        }

        public AtomicSafetyHandle Safety
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Slice.m_Safety;
        }
#endif
    }
}

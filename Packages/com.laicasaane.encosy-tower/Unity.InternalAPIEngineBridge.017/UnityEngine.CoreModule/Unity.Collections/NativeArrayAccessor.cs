using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections.Internals
{
    internal readonly struct NativeArrayAccessor<T> where T : struct
    {
        public readonly NativeArray<T> Array;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArrayAccessor(NativeArray<T> array)
        {
            Array = array;
        }

        /// <safety>The returned pointer borrows Unity's live NativeArray storage and must not outlive the array.</safety>
        public unsafe void* Buffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // SAFETY: Unity owns the NativeArray buffer and the accessor only exposes its existing pointer.
                unsafe
                {
                    return Array.m_Buffer;
                }
            }
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Array.m_Length;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public int MinIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Array.m_MinIndex;
        }

        public int MaxIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Array.m_MaxIndex;
        }

        public AtomicSafetyHandle Safety
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Array.m_Safety;
        }
#endif
    }
}

#if UNITY_COLLECTIONS

using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections.Unsafe
{
    public static class EncosyNativeListExtensionsUnsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the native list alive and provide an index within its bounds.</safety>
        public static unsafe ref T ElementAsUnsafeRefRW<T>(this NativeList<T> list, int index)
            where T : unmanaged
        {
            // SAFETY: Caller owns the list lifetime and supplies a valid element index.
            unsafe
            {
                return ref UnsafeUtility.ArrayElementAsRef<T>(list.GetUnsafePtr(), index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the native list alive and provide an index within its bounds.</safety>
        public static unsafe ref readonly T ElementAsUnsafeRefRO<T>(this NativeList<T> list, int index)
            where T : unmanaged
        {
            // SAFETY: Caller owns the list lifetime and supplies a valid element index.
            unsafe
            {
                return ref UnsafeUtility.ArrayElementAsRef<T>(list.GetUnsafeReadOnlyPtr(), index);
            }
        }
    }
}

#endif

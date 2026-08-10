using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections.Extensions.Unsafe
{
    public static class ListUnsafeExtensionsUnsafe
    {
        /// <safety>Caller must keep the list alive and must not dispose the returned alias separately.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe BufferUnsafe<T> GetBufferUnsafe<T>(this in ListUnsafe<T> self)
            where T : unmanaged
        {
            // SAFETY: Caller owns the list lifetime.
            unsafe
            {
                return self._buffer;
            }
        }

        /// <safety>Caller must keep the list alive and supply a valid storage index.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ref T GetItemAtUnsafe<T>(this in ListUnsafe<T> self, int index)
            where T : unmanaged
        {
            // SAFETY: The caller owns the list lifetime and supplies the unchecked element index.
            unsafe
            {
                return ref UnsafeUtility.ArrayElementAsRef<T>(self._buffer.GetUnsafePtr(), index);
            }
        }
    }
}
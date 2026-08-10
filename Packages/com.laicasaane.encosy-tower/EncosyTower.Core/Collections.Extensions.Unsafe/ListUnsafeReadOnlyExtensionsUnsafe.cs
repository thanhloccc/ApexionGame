using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;

namespace EncosyTower.Collections.Extensions.Unsafe
{
    public static class ListUnsafeReadOnlyExtensionsUnsafe
    {
        /// <safety>Caller must keep the list alive while using the returned read-only buffer alias.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe BufferUnsafe<T>.ReadOnly GetBufferUnsafe<T>(
            this in ListUnsafe<T>.ReadOnly self
        )
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
        public static unsafe ref readonly T GetItemAtUnsafe<T>(
              this in ListUnsafe<T>.ReadOnly self
            , int index
        )
            where T : unmanaged
        {
            // SAFETY: The caller owns the list lifetime and supplies the unchecked element index.
            unsafe
            {
                return ref self._buffer[index];
            }
        }
    }
}
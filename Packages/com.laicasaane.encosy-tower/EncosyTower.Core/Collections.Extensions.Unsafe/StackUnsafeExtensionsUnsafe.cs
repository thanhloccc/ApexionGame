using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections.Extensions.Unsafe
{
    public static class StackUnsafeExtensionsUnsafe
    {
        /// <safety>Caller must keep the collection alive and must not dispose the returned alias separately.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe BufferUnsafe<T> GetBufferUnsafe<T>(this in StackUnsafe<T> self)
            where T : unmanaged
        {
            // SAFETY: Caller owns the stack lifetime.
            unsafe
            {
                return self._buffer;
            }
        }

        /// <safety>Caller must keep the collection alive and supply a valid storage index.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ref T GetItemAtUnsafe<T>(this in StackUnsafe<T> self, int index)
            where T : unmanaged
        {
            // SAFETY: Caller owns lifetime and supplies unchecked storage index.
            unsafe
            {
                return ref UnsafeUtility.ArrayElementAsRef<T>(self._buffer.GetUnsafePtr(), index);
            }
        }

        /// <safety>Caller must keep the collection alive and must not dispose the returned alias separately.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe BufferUnsafe<T>.ReadOnly GetBufferUnsafe<T>(
            this in StackUnsafe<T>.ReadOnly self
        )
            where T : unmanaged
        {
            // SAFETY: Caller owns the stack lifetime.
            unsafe
            {
                return self._buffer;
            }
        }

        /// <safety>Caller must keep the collection alive and supply a valid storage index.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ref readonly T GetItemAtUnsafe<T>(
              this in StackUnsafe<T>.ReadOnly self
            , int index
        )
            where T : unmanaged
        {
            // SAFETY: Caller owns lifetime and supplies unchecked storage index.
            unsafe
            {
                return ref self._buffer[index];
            }
        }
    }
}
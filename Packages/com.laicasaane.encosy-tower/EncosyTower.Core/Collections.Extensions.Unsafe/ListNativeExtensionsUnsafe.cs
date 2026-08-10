using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections.Extensions.Unsafe
{
    public static class ListNativeExtensionsUnsafe
    {
        /// <safety>Caller must keep the native list alive and must not dispose the returned alias separately.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe BufferUnsafe<T> GetBufferUnsafe<T>(
            this in ListNative<T> self
        )
            where T : unmanaged
        {
            // SAFETY: The caller owns the native list lifetime and the header points at its buffer.
            unsafe
            {
                return self.m_Data->_buffer;
            }
        }

        /// <safety>Caller must keep the native list alive and supply a valid storage index.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ref T GetItemAtUnsafe<T>(
              this in ListNative<T> self
            , int index
        )
            where T : unmanaged
        {
            // SAFETY: The caller owns the list lifetime and supplies the unchecked element index.
            unsafe
            {
                return ref UnsafeUtility.ArrayElementAsRef<T>(
                      self.m_Data->_buffer.GetUnsafePtr()
                    , index
                );
            }
        }
    }
}

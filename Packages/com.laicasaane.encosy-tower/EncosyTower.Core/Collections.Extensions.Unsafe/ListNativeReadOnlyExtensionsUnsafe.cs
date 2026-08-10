using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections.Extensions.Unsafe
{
    public static class ListNativeReadOnlyExtensionsUnsafe
    {
        /// <safety>Caller must keep the native list alive while using the returned read-only alias.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe BufferUnsafe<T>.ReadOnly GetBufferUnsafe<T>(
            this in ListNative<T>.ReadOnly self
        )
            where T : unmanaged
        {
            // SAFETY: The caller owns the native list lifetime and the view borrows its buffer.
            unsafe
            {
                return self.m_Data->_buffer.AsReadOnly();
            }
        }

        /// <safety>Caller must keep the native list alive and supply a valid storage index.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ref readonly T GetItemAtUnsafe<T>(
              this in ListNative<T>.ReadOnly self
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

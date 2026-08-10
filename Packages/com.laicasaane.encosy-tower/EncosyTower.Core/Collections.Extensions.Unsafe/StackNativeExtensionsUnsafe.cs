using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using Unity.Collections.LowLevel.Unsafe;

namespace EncosyTower.Collections.Extensions.Unsafe
{
    public static class StackNativeExtensionsUnsafe
    {
        /// <safety>Caller must keep the collection alive and must not dispose the returned alias separately.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe BufferUnsafe<T> GetBufferUnsafe<T>(
            this in StackNative<T> self
        )
            where T : unmanaged
        {
            // SAFETY: Caller owns the native stack lifetime.
            unsafe
            {
                return self.m_Data->_buffer;
            }
        }

        /// <safety>Caller must keep the collection alive and supply a valid storage index.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ref T GetItemAtUnsafe<T>(
              this in StackNative<T> self
            , int index
        )
            where T : unmanaged
        {
            // SAFETY: Caller owns lifetime and supplies unchecked storage index.
            unsafe
            {
                return ref UnsafeUtility.ArrayElementAsRef<T>(
                      self.m_Data->_buffer.GetUnsafePtr()
                    , index
                );
            }
        }

        /// <safety>Caller must keep the collection alive and must not dispose the returned alias separately.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe BufferUnsafe<T>.ReadOnly GetBufferUnsafe<T>(
            this in StackNative<T>.ReadOnly self
        )
            where T : unmanaged
        {
            // SAFETY: Caller owns the native stack lifetime.
            unsafe
            {
                return self.m_Data->_buffer.AsReadOnly();
            }
        }

        /// <safety>Caller must keep the collection alive and supply a valid storage index.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ref readonly T GetItemAtUnsafe<T>(
              this in StackNative<T>.ReadOnly self
            , int index
        )
            where T : unmanaged
        {
            // SAFETY: Caller owns lifetime and supplies unchecked storage index.
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

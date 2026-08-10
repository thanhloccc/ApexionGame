using System.Runtime.CompilerServices;
using EncosyTower.Buffers;

namespace EncosyTower.Collections.Extensions.Unsafe
{
    public static class QueueNativeExtensionsUnsafe
    {
        /// <safety>Caller must keep the collection alive and must not dispose the returned alias separately.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe BufferUnsafe<T> GetBufferUnsafe<T>(
            this in QueueNative<T> self
        )
            where T : unmanaged
        {
            // SAFETY: Caller owns the native queue lifetime.
            unsafe
            {
                return self.m_Data->_buffer;
            }
        }

        /// <safety>Caller must keep the collection alive; the returned index is a raw ring position.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetHeadUnsafe<T>(
            this in QueueNative<T> self
        )
            where T : unmanaged
        {
            // SAFETY: Caller owns the native queue lifetime.
            unsafe
            {
                return self.m_Data->_head;
            }
        }

        /// <safety>Caller must keep the collection alive; the returned index is a raw ring position.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetTailUnsafe<T>(
            this in QueueNative<T> self
        )
            where T : unmanaged
        {
            // SAFETY: Caller owns the native queue lifetime.
            unsafe
            {
                return self.m_Data->_tail;
            }
        }

        /// <safety>Caller must keep the collection alive and must not dispose the returned alias separately.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe BufferUnsafe<T>.ReadOnly GetBufferUnsafe<T>(
            this in QueueNative<T>.ReadOnly self
        )
            where T : unmanaged
        {
            // SAFETY: Caller owns the native queue lifetime.
            unsafe
            {
                return self.m_Data->_buffer.AsReadOnly();
            }
        }

        /// <safety>Caller must keep the collection alive; the returned index is a raw ring position.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetHeadUnsafe<T>(
            this in QueueNative<T>.ReadOnly self
        )
            where T : unmanaged
        {
            // SAFETY: Caller owns the native queue lifetime.
            unsafe
            {
                return self.m_Data->_head;
            }
        }

        /// <safety>Caller must keep the collection alive; the returned index is a raw ring position.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetTailUnsafe<T>(
            this in QueueNative<T>.ReadOnly self
        )
            where T : unmanaged
        {
            // SAFETY: Caller owns the native queue lifetime.
            unsafe
            {
                return self.m_Data->_tail;
            }
        }
    }
}

using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;

namespace EncosyTower.Collections.Extensions.Unsafe
{
    public static class QueueUnsafeExtensionsUnsafe
    {
        /// <safety>Caller must keep the collection alive and must not dispose the returned alias separately.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe BufferUnsafe<T> GetBufferUnsafe<T>(this in QueueUnsafe<T> self)
            where T : unmanaged
        {
            // SAFETY: Caller owns the queue lifetime.
            unsafe
            {
                return self._buffer;
            }
        }

        /// <safety>Caller must keep the collection alive; the returned index is a raw ring position.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetHeadUnsafe<T>(this in QueueUnsafe<T> self)
            where T : unmanaged
        {
            // SAFETY: Caller owns the queue lifetime.
            unsafe
            {
                return self._head;
            }
        }

        /// <safety>Caller must keep the collection alive; the returned index is a raw ring position.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetTailUnsafe<T>(this in QueueUnsafe<T> self)
            where T : unmanaged
        {
            // SAFETY: Caller owns the queue lifetime.
            unsafe
            {
                return self._tail;
            }
        }

        /// <safety>Caller must keep the collection alive and must not dispose the returned alias separately.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe BufferUnsafe<T>.ReadOnly GetBufferUnsafe<T>(
            this in QueueUnsafe<T>.ReadOnly self
        )
            where T : unmanaged
        {
            // SAFETY: Caller owns the queue lifetime.
            unsafe
            {
                return self._buffer;
            }
        }

        /// <safety>Caller must keep the collection alive; the returned index is a raw ring position.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetHeadUnsafe<T>(this in QueueUnsafe<T>.ReadOnly self)
            where T : unmanaged
        {
            // SAFETY: Caller owns the queue lifetime.
            unsafe
            {
                return self._head;
            }
        }

        /// <safety>Caller must keep the collection alive; the returned index is a raw ring position.</safety>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetTailUnsafe<T>(this in QueueUnsafe<T>.ReadOnly self)
            where T : unmanaged
        {
            // SAFETY: Caller owns the queue lifetime.
            unsafe
            {
                return self._tail;
            }
        }
    }
}
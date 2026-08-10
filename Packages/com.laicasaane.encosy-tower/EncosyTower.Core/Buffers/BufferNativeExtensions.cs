using System.Runtime.CompilerServices;
using EncosyTower.Collections.Unsafe;

using ThrowHelper = EncosyTower.Collections.ThrowHelper;

namespace EncosyTower.Buffers
{
    public static class BufferNativeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ShiftLeft<T>(this BufferNative<T> self, int index, int count)
            where T : unmanaged
        {
            ThrowHelper.ThrowIfBufferShiftIndexIsOutOfRange((uint)index < (uint)self.Capacity);
            ThrowHelper.ThrowIfBufferShiftCountIsOutOfRange((uint)count < (uint)self.Capacity);

            if (count == index)
                return;

            ThrowHelper.ThrowIfBufferShiftCountIsBelowIndex(count > index);

            var array = self.AsNativeArray();
            array.MemoryCopyUnsafe(index + 1, index, count - index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ShiftRight<T>(this BufferNative<T> self, int index, int count)
            where T : unmanaged
        {
            ThrowHelper.ThrowIfBufferShiftIndexIsOutOfRange((uint)index < (uint)self.Capacity);
            ThrowHelper.ThrowIfBufferShiftCountIsOutOfRange((uint)count < (uint)self.Capacity);

            if (count == index)
                return;

            ThrowHelper.ThrowIfBufferShiftCountIsBelowIndex(count > index);

            var array = self.AsNativeArray();
            array.MemoryCopyUnsafe(index, index + 1, count - index);
        }
    }
}

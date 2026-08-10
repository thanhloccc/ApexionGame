using System;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;

namespace EncosyTower.Collections.Extensions.Unsafe
{
    public static class ArraySetNativeExtensionsUnsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the native set created and valid while using the returned buffer view.</safety>
        public static unsafe BufferUnsafe<ArrayMapNode<T>> GetNodesUnsafe<T>(this in ArraySetNative<T> self)
            where T : unmanaged, IEquatable<T>
        {
            // SAFETY: The caller owns the set lifetime and this view borrows its values-info buffer.
            unsafe
            {
                return self.m_Data->_valuesInfo;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the native set created and valid while using the returned buffer view.</safety>
        public static unsafe BufferUnsafe<T> GetItemsUnsafe<T>(this in ArraySetNative<T> self)
            where T : unmanaged, IEquatable<T>
        {
            // SAFETY: The caller owns the set lifetime and this view borrows its values buffer.
            unsafe
            {
                return self.m_Data->_values;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the native set alive for the lifetime of the returned reference.</safety>
        public static unsafe ref T GetItemAtUnsafe<T>(this in ArraySetNative<T> self, int index)
            where T : unmanaged, IEquatable<T>
        {
            // SAFETY: Caller supplies an index within the live values buffer and keeps the set alive.
            unsafe
            {
                return ref self.m_Data->_values[index];
            }
        }
    }
}

using System;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;

namespace EncosyTower.Collections.Extensions.Unsafe
{
    public static class ArraySetNativeReadOnlyExtensionsUnsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the read-only native set created and valid while using the returned view.</safety>
        public static unsafe BufferUnsafe<ArrayMapNode<T>>.ReadOnly GetNodesUnsafe<T>(
            this in ArraySetNative<T>.ReadOnly self
        )
            where T : unmanaged, IEquatable<T>
        {
            // SAFETY: The caller owns the read-only set lifetime and this view borrows its values-info buffer.
            unsafe
            {
                return self.m_Data->_valuesInfo.AsReadOnly();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the read-only native set created and valid while using the returned view.</safety>
        public static unsafe BufferUnsafe<T>.ReadOnly GetItemsUnsafe<T>(this in ArraySetNative<T>.ReadOnly self)
            where T : unmanaged, IEquatable<T>
        {
            // SAFETY: The caller owns the read-only set lifetime and this view borrows its values buffer.
            unsafe
            {
                return self.m_Data->_values.AsReadOnly();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the read-only native set alive for the lifetime of the returned reference.</safety>
        public static unsafe ref readonly T GetItemAtUnsafe<T>(this in ArraySetNative<T>.ReadOnly self, int index)
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

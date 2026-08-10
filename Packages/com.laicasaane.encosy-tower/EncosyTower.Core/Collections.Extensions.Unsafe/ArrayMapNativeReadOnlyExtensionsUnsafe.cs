using System;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;

namespace EncosyTower.Collections.Extensions.Unsafe
{
    public static class ArrayMapNativeReadOnlyExtensionsUnsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the read-only native map created and valid while using the returned view.</safety>
        public static unsafe BufferUnsafe<ArrayMapNode<TKey>>.ReadOnly GetKeysUnsafe<TKey, TValue>(
            this in ArrayMapNative<TKey, TValue>.ReadOnly self
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            // SAFETY: The caller owns the read-only map lifetime and this view borrows its values-info buffer.
            unsafe
            {
                return self.m_Data->_valuesInfo.AsReadOnly();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the read-only native map created and valid while using the returned view.</safety>
        public static unsafe BufferUnsafe<TValue>.ReadOnly GetValuesUnsafe<TKey, TValue>(
            this in ArrayMapNative<TKey, TValue>.ReadOnly self
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            // SAFETY: The caller owns the read-only map lifetime and this view borrows its values buffer.
            unsafe
            {
                return self.m_Data->_values.AsReadOnly();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the read-only native map alive for the lifetime of the returned reference.</safety>
        public static unsafe ref readonly TValue GetValueAtUnsafe<TKey, TValue>(
              this in ArrayMapNative<TKey, TValue>.ReadOnly self
            , int index
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            // SAFETY: Caller supplies an index within the live values buffer and keeps the map alive.
            unsafe
            {
                return ref self.m_Data->_values[index];
            }
        }
    }
}

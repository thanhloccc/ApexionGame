using System;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;

namespace EncosyTower.Collections.Extensions.Unsafe
{
    public static class ArrayMapNativeExtensionsUnsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the native map created and valid while using the returned buffer view.</safety>
        public static unsafe BufferUnsafe<ArrayMapNode<TKey>> GetKeysUnsafe<TKey, TValue>(
            this in ArrayMapNative<TKey, TValue> self
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            // SAFETY: The caller owns the map lifetime and this view borrows its values-info buffer.
            unsafe
            {
                return self.m_Data->_valuesInfo;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the native map created and valid while using the returned buffer view.</safety>
        public static unsafe BufferUnsafe<TValue> GetValuesUnsafe<TKey, TValue>(
            this in ArrayMapNative<TKey, TValue> self
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            // SAFETY: The caller owns the map lifetime and this view borrows its values buffer.
            unsafe
            {
                return self.m_Data->_values;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <safety>Caller must keep the native map created and valid for the lifetime of the
        /// returned reference.</safety>
        public static unsafe ref TValue GetValueAtUnsafe<TKey, TValue>(
              this in ArrayMapNative<TKey, TValue> self
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

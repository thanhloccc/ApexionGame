using System;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;

namespace EncosyTower.Collections.Extensions.Unsafe
{
    public static class ArrayMapUnsafeExtensionsUnsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferUnsafe<ArrayMapNode<TKey>> GetKeysUnsafe<TKey, TValue>(
            this in ArrayMapUnsafe<TKey, TValue> self
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
                => self._valuesInfo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferUnsafe<TValue> GetValuesUnsafe<TKey, TValue>(this in ArrayMapUnsafe<TKey, TValue> self)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
                => self._values;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetValueAtUnsafe<TKey, TValue>(this in ArrayMapUnsafe<TKey, TValue> self, int index)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
                => ref self._values[index];
    }
}

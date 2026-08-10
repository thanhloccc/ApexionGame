using System;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;

namespace EncosyTower.Collections.Extensions.Unsafe
{
    public static class ArraySetUnsafeExtensionsUnsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferUnsafe<ArrayMapNode<T>> GetNodesUnsafe<T>(this in ArraySetUnsafe<T> self)
            where T : unmanaged, IEquatable<T>
                => self._valuesInfo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferUnsafe<T> GetItemsUnsafe<T>(this in ArraySetUnsafe<T> self)
            where T : unmanaged, IEquatable<T>
                => self._values;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetItemAtUnsafe<T>(this in ArraySetUnsafe<T> self, int index)
            where T : unmanaged, IEquatable<T>
                => ref self._values[index];
    }
}

using System;
using System.Runtime.CompilerServices;
using EncosyTower.Buffers;
using EncosyTower.Collections.Unsafe;

namespace EncosyTower.Collections.Extensions.Unsafe
{
    public static class ArraySetUnsafeReadOnlyExtensionsUnsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferUnsafe<ArrayMapNode<T>>.ReadOnly GetNodesUnsafe<T>(this in ArraySetUnsafe<T>.ReadOnly self)
            where T : unmanaged, IEquatable<T>
                => self._valuesInfo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferUnsafe<T>.ReadOnly GetItemsUnsafe<T>(this in ArraySetUnsafe<T>.ReadOnly self)
            where T : unmanaged, IEquatable<T>
                => self._values;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T GetItemAtUnsafe<T>(this in ArraySetUnsafe<T>.ReadOnly self, int index)
            where T : unmanaged, IEquatable<T>
                => ref self._values[index];
    }
}

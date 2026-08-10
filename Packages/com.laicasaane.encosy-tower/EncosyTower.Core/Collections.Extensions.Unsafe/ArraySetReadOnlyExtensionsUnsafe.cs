using System.Runtime.CompilerServices;
using EncosyTower.Buffers;

namespace EncosyTower.Collections.Unsafe
{
    public static class ArraySetReadOnlyExtensionsUnsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferManaged<ArrayMapNode<T>>.ReadOnly GetNodesUnsafe<T>(this in ArraySet<T>.ReadOnly self)
            => self._set._valuesInfo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferManaged<T>.ReadOnly GetItemsUnsafe<T>(this in ArraySet<T>.ReadOnly self)
            => self._set._values;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T GetItemAtUnsafe<T>(this in ArraySet<T>.ReadOnly self, int index)
            => ref self._set._values[index];
    }
}

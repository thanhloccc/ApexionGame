using System.Runtime.CompilerServices;
using EncosyTower.Buffers;

namespace EncosyTower.Collections.Unsafe
{
    public static class ArraySetExtensionsUnsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferManaged<ArrayMapNode<T>> GetNodesUnsafe<T>(this ArraySet<T> self)
            => self._valuesInfo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferManaged<T> GetItemsUnsafe<T>(this ArraySet<T> self)
            => self._values;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetItemAtUnsafe<T>(this ArraySet<T> self, int index)
            => ref self._values[index];
    }
}

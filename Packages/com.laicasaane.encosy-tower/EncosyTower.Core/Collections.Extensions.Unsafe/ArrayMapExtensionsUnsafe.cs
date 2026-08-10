using System.Runtime.CompilerServices;
using EncosyTower.Buffers;

namespace EncosyTower.Collections.Unsafe
{
    public static class ArrayMapExtensionsUnsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferManaged<ArrayMapNode<TKey>> GetKeysUnsafe<TKey, TValue>(this ArrayMap<TKey, TValue> self)
            => self._valuesInfo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferManaged<TValue> GetValuesUnsafe<TKey, TValue>(this ArrayMap<TKey, TValue> self)
            => self._values;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetValueAtUnsafe<TKey, TValue>(this ArrayMap<TKey, TValue> self, int index)
            => ref self._values[index];
    }
}

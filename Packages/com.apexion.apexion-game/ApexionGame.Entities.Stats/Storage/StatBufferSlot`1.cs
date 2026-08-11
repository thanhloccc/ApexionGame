using Unity.Collections.LowLevel.Unsafe;

namespace ApexionGame.Entities.Stats
{
    internal unsafe struct StatBufferSlot<T>
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        public UnsafeList<T>* list;
    }
}

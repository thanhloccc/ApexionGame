using System.Runtime.CompilerServices;

namespace ApexionGame.Entities.Stats
{
    internal struct StatOwnerSlot
    {
        public int version;
        public uint modifierIdCounter;

        public readonly bool IsAlive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => version > 0;
        }
    }
}

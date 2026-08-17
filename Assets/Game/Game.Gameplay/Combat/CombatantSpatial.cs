using Unity.Mathematics;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// Everything a hit query needs about one combatant, and nothing else.
    /// </summary>
    /// <remarks>
    /// Kept separate from <see cref="Combatant"/> so the one O(N) loop in combat walks a
    /// contiguous array of blittable structs instead of dereferencing a managed object per
    /// candidate. <c>Id</c> is carried inline so a query can report hits without a reverse lookup.
    /// </remarks>
    public struct CombatantSpatial
    {
        public CombatantId Id;
        public float3 Position;
        public float3 Forward;
        public float Radius;
        public byte Team;
        public bool IsAlive;
    }
}

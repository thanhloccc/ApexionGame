using Unity.Mathematics;

namespace ApexionGame.Entities.Stats.Samples.Rts
{
    /// <summary>Distances on the battlefield, in one place so movement and combat cannot disagree.</summary>
    public static class RtsGeometry
    {
        /// <summary>Surface-to-surface distance between two units. Negative means overlapping.</summary>
        public static float Gap(RtsUnit a, RtsUnit b)
            => math.distance(a.Position, b.Position) - a.Archetype.Radius - b.Archetype.Radius;

        public static float Gap(float2 point, RtsUnit unit)
            => math.distance(point, unit.Position) - unit.Archetype.Radius;

        public static bool Within(float2 point, RtsUnit unit, float radius)
            => math.distance(point, unit.Position) <= radius + unit.Archetype.Radius;
    }
}

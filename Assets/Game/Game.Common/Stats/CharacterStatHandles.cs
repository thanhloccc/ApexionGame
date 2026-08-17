using System.Runtime.CompilerServices;
using ApexionGame.Entities.Stats;
using EncosyTower.Common;

namespace Game.Common
{
    public readonly record struct CharacterStatHandles(
          StatHandle MaxHealth
        , StatHandle Armor
        , StatHandle MoveSpeed
        , StatHandle CarryCapacity
    )
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<StatHandle> Resolve(CharacterStatKind kind)
            => kind switch {
                CharacterStatKind.MaxHealth => MaxHealth,
                CharacterStatKind.Armor => Armor,
                CharacterStatKind.MoveSpeed => MoveSpeed,
                CharacterStatKind.CarryCapacity => CarryCapacity,
                _ => Option.None,
            };
    }
}

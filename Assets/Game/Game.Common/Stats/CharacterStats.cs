using ApexionGame.Entities.Stats;

namespace Game.Common
{
    [StatCollection(typeof(GameStatSystem), 1000)]
    public partial struct CharacterStats
    {
        [StatData(StatVariantType.Float)] public partial struct MaxHealth { }

        [StatData(StatVariantType.Float)] public partial struct Armor { }

        [StatData(StatVariantType.Float)] public partial struct MoveSpeed { }

        [StatData(StatVariantType.Float)] public partial struct CarryCapacity { }
    }
}

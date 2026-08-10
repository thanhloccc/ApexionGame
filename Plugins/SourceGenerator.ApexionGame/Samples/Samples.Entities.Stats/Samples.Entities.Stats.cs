using ApexionGame.Entities.Stats;

namespace Samples.Entities.Stats
{
    [StatSystem(StatDataSize.Size8)]
    public static partial class StatSystem { }

    public enum DirectionType : byte
    {
        Forward,
        Backward,
        Up,
        Down,
        Left,
    }

    [StatCollection(typeof(StatSystem), 1000)]
    public partial struct Stats
    {
        [StatData(StatVariantType.Float)] public partial struct Hp { }

        [StatData(StatVariantType.Float)] public partial struct MoveSpeed { }

        [StatData(typeof(DirectionType))] public partial struct Direction { }

        [StatData(StatVariantType.Half2)] public partial struct DirectionVector { }
    }
}

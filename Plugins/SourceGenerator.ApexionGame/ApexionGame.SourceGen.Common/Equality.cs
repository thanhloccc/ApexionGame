// Vendored from EncosyTower.SourceGen (com.laicasaane.encosy-tower).
// Snapshot copy — intentionally NOT kept in sync; see 07-DECISIONS.md D-107.
// Namespace changed from EncosyTower.SourceGen to ApexionGame.SourceGen.
namespace ApexionGame.SourceGen
{
    public enum EqualityStrategy
    {
        Default = 0,
        Equals,
        Operator,
    }

    public readonly record struct Equality(EqualityStrategy Strategy, bool IsStatic, bool IsNullable);
}


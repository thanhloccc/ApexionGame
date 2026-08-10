// Vendored from EncosyTower.SourceGen (com.laicasaane.encosy-tower).
// Snapshot copy — intentionally NOT kept in sync; see 07-DECISIONS.md D-107.
// Namespace changed from EncosyTower.SourceGen to ApexionGame.SourceGen.
namespace ApexionGame.SourceGen
{
    public readonly record struct MemberExistence(bool DoesExist, bool IsStatic, bool IsNullable, int ParamCount)
    {
        public MemberExistence DefaultIfNullableIs(bool value)
            => IsNullable == value ? default : this;
    }
}


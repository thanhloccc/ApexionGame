// Vendored from EncosyTower.SourceGen (com.laicasaane.encosy-tower).
// Snapshot copy — intentionally NOT kept in sync; see 07-DECISIONS.md D-107.
// Namespace changed from EncosyTower.SourceGen to ApexionGame.SourceGen.
using Microsoft.CodeAnalysis;

namespace ApexionGame.SourceGen
{
    public static class AccessibilityExtensions
    {
        public static string ToKeyword(this Accessibility self)
        {
            return self switch {
                  Accessibility.Internal => "internal"
                , Accessibility.Private => "private"
                , Accessibility.Protected => "protected"
                , Accessibility.ProtectedAndInternal => "private protected"
                , Accessibility.ProtectedOrInternal => "protected internal"
                , Accessibility.Public => "public"
                , _ => string.Empty
            };
        }
    }
}


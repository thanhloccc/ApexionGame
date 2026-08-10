#if UNITY_EDITOR

using EncosyTower.Editor.ProjectSetup;

namespace EncosyTower.Editor.Entities
{
    [Feature("9. EncosyTower: Entities Stats")]
    [RequiresPackage(PackageRegistry.Unity, "com.unity.entities", "1.4.7")]
    [RequiresPackage(PackageRegistry.OpenUpm, "com.latios.latiosframework", "0.15.9", isOptional: true)]
    internal readonly struct FeatureEntitiesStats { }
}

#endif

---
name: encosy-sourcegen-requires-partial
description: Every type carrying an EncosyTower attribute must be partial, and generated members follow a Get_X/Set_X body convention
metadata:
  type: reference
---

EncosyTower ships Roslyn generators as DLLs in
`Packages/com.laicasaane.encosy-tower/EncosyTower.Core/SourceGenerators/`. Two mechanics trip up
almost every first attempt:

1. **`partial` is mandatory** on any type annotated with `[ObservableObject]`, `[ObservableProperty]`,
   `[RelayCommand]`, `[Data]`, `[DataTableAsset]`, `[Database]`, `[Persistence]`, `[Persist]`,
   `[WrapType]`, `[WrapRecord]`, `[UnionId]`, `[EnumTemplate]`, `[PolyEnumStruct]`, `[Variant]`,
   `[MonoBinder]`, `[EnumExtensionsFor]`. Missing `partial` is the top cause of "the generated
   method doesn't exist".
2. **Members are declared as forwarding bodies**, and the generator supplies the implementation plus
   the backing field:
   - `[ObservableProperty] public int Gold { get => Get_Gold(); set => Set_Gold(value); }` → `_gold`
   - `[DataProperty] public readonly float Hp => Get_Hp();` → `_hp`
   - `[Table] public readonly HeroTableAsset Heroes => Get_Heroes();`

**Why:** these are not optional conventions — the generator matches on them, and getting either
wrong produces compile errors that look like missing APIs rather than missing `partial`.

**How to apply:** write the forwarding body exactly as above, mark the type `partial`, and let
Unity compile before trusting IDE errors — generated code only materialises after a compile.
`.gen.cs` files are checked in and regenerated behind `*_GENERATOR` defines; never hand-edit them.
See [[encosy-tower-is-standard-library]].

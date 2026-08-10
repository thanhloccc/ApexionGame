# Contributing

*[Tiếng Việt](CONTRIBUTING.vi.md)*

Issues and pull requests are welcome, in English or Vietnamese.

## Before you change the runtime

Read [DEC-005](Documentation~/07-DECISIONS.md#dec-005) first. The propagation loop deliberately has
**no visited-set**, and the reason is counter-intuitive: re-visiting a stat is not wasted work, it is
the mechanism that makes the graph converge. Re-adding a visited-set is the single easiest way to
break this library while every obvious test still passes.

The [decision log](Documentation~/07-DECISIONS.md) records every divergence from upstream with its
reasoning. If your change adds another one, add an entry.

## Layout

| Path | What lives there |
|---|---|
| `ApexionGame.Entities.Stats/` | runtime — hand-written generic layer |
| `ApexionGame.Entities.Stats/Generators/` | editor codegen for the value-type tables (`*.gen.cs`) |
| `ApexionGame.Entities.Stats/SourceGenerators/` | prebuilt Roslyn DLLs, deployed by MSBuild |
| `ApexionGame.Entities.Stats.Authoring/` | `ScriptableObject` authoring |
| `ApexionGame.Entities.Stats.Editor/` | Stat Debugger window |
| `ApexionGame.Entities.Stats.Samples/` | runnable sample + playground window |
| `ApexionGame.Entities.Stats.Tests/` | 95 tests, Editor only |
| `Plugins/SourceGenerator.ApexionGame/` | Roslyn solution — generators, analyzers, refactorings, tests |

More detail in [05-ASSEMBLY-LAYOUT.md](Documentation~/05-ASSEMBLY-LAYOUT.md).

## Building the generators

The Roslyn projects are a separate solution and do not build with Unity. An MSBuild target copies
the output into `ApexionGame.Entities.Stats/SourceGenerators/` with the asset labels Unity needs, so
a plain build is all that is required:

```powershell
dotnet build Plugins/SourceGenerator.ApexionGame/ApexionGame.SourceGen.slnx -c Release
```

Unity picks the new DLLs up on the next domain reload. See
[04-CODEGEN.md](Documentation~/04-CODEGEN.md) for debugging a generator against a real compilation.

## Tests

Three suites, and a change to the runtime or the generators should keep all three green:

```powershell
# 1. Runtime — Unity, no Editor window required
unity test . --mode EditMode --filter "ApexionGame.Entities.Stats.Tests"

# 2. Generators and analyzers — outside Unity
dotnet test Plugins/SourceGenerator.ApexionGame/ApexionGame.SourceGen.Tests

# 3. The sample, compiled through the real generators outside Unity.
#    This is the regression gate for the usage shown in the docs.
dotnet build Plugins/SourceGenerator.ApexionGame/Samples/Samples.RpgStats/Samples.RpgStats.csproj
```

Two things to know about suite 1 and 2:

- Unity's test runner **excludes `[Explicit]` tests even when you name them in `--filter`**. That is
  why the benchmarks carry no `[Explicit]` attribute — the whole class runs in under 2 seconds.
- `dotnet test` prints `Passed!` even when tests come back `Inconclusive`. Check the skip count, not
  the banner.

Set `UNITY_OS_INSTALL_ROOT` to your Unity install directory once and the `unity test` command above
works from any shell.

## Code style

`.editorconfig` at the repository root is authoritative, and
[CODING-CONVENTIONS.md](../../../CODING-CONVENTIONS.md) explains the conventions that a formatter
cannot enforce. The ones that matter most here:

- Everything in the core is `unmanaged`. No `class`, no delegates, no managed fields on the hot path.
- Public APIs fail through `bool TryXxx(...)`; validation lives in `ThrowHelper` methods marked
  `[Conditional]` so they vanish in release builds.
- New generated code follows the existing `Spec` + `WriteCode` split, so a diff against the
  EncosyTower generators stays readable.

## Commit and PR notes

- One logical change per commit.
- If you change generated output, say what the generated diff looks like — reviewers cannot see it.
- If you fix something that is also broken upstream, note it, so the fix can be offered back.

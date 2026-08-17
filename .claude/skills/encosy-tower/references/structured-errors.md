# Structured `Result` errors — PolyEnumStructs, never flat enums

Read this before creating, changing, or reviewing **any** type used as `TError` in
`Result<T, TError>`.

## The non-negotiable rule

> A gameplay/domain failure returned through `Result<T, TError>` is modelled as an EncosyTower
> **PolyEnumStruct union**, never as a flat `enum`.

A flat `enum FooError { None, Invalid, NotFound }` throws away every piece of context the caller
needs: *which* id was not found, *what* the state actually was, *how much* was missing. The
PolyEnum shape keeps that payload, stays unmanaged and Burst-friendly, and produces a real message.

Plain enums stay correct for **states, modes, flags, categories** — `PlayerActionState`,
`WeaponFireMode`, `EquipmentSlotMask`. The rule is about error contracts only.

### Ground truth in this repo

Verified present on 2026-08-15.

| File | Status |
|---|---|
| `Packages/com.apexion.apexion-game/ApexionGame.Core/HFSM/MachineError.cs` | ✅ **the reference implementation — read this first.** Every case is produced by `MachineBuilder\`2+Validate.cs`, so the call sites are there too |
| `Packages/com.apexion.apexion-game/ApexionGame.Tests.EditorMode/ApexionGame.Core/HFSM/MachineErrorTests.cs` | ✅ the expected test shape |
| `Packages/com.laicasaane.encosy-tower/Samples~/EncosyTower.Samples.Persistence/Persistences/PlayerDataError.cs` | ✅ package sample |

`MachineError` adds one member the skeleton below omits, and it is worth copying:

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public FixedString512Bytes ToFixedString()
    => _error.ToMessage(_prefix);
```

`ToString()` allocates; `ToFixedString()` lets a Burst-side or logging caller stay allocation-free.
Add it to every new error wrapper.

> **Stale pointers, removed.** Earlier revisions cited `Assets/Game/Game.Gameplay/…/PlayerError.cs`,
> `WeaponError.cs`, `EquipmentError.cs`, `PlayerPersistenceError.cs` and `PlayerErrorTests.cs`.
> **None of those files exist** — `Assets/Game/Game.*` are empty folders with no `.asmdef`, and the
> files are absent from git history too. There is no known legacy flat-enum error left in the tree;
> if you find one, it is new information, not the documented exception.

Anything in `EncosyTower.Core` named `*Error` (`AddressableKeyError`, `ResourceKeyError`,
`NativeRentingError`, `AtlasedSpriteKeyError`) belongs to the package, predates this rule, and is
**not** a precedent for project code.

## Required shape

Outer wrapper carries the prefix and delegates `ToString()`; the nested `Error` union carries the
cases. `[MethodImpl(AggressiveInlining)]` on the wrapper members, per house style.

```csharp
using System.Runtime.CompilerServices;
using EncosyTower.PolyEnumStructs;
using Game.Common.Ids;
using Unity.Collections;

namespace Game.Gameplay.Feature
{
    [PolyEnumFactoryFor(typeof(Error))]
    public readonly partial struct FeatureError
    {
        private readonly FixedString64Bytes _prefix;
        private readonly Error _error;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FeatureError(in Error error) : this(error, default)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FeatureError(in Error error, in FixedString64Bytes prefix)
        {
            _prefix = prefix;
            _error = error;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FeatureError Prefix(in FixedString64Bytes prefix)
            => new(_error, prefix);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
            => _error.ToMessage(_prefix).ToString();

        [PolyEnumStruct]
        readonly partial struct Error
        {
            private static FixedString512Bytes InitMessage(in FixedString64Bytes prefix)
            {
                FixedString512Bytes message = default;

                if (prefix.IsEmpty == false)
                {
                    message.Append('[');
                    message.Append(prefix);
                    message.Append(']');
                    message.Append(' ');
                }

                return message;
            }

            private static FixedString512Bytes Message(
                  in FixedString64Bytes prefix
                , in FixedString128Bytes text
            )
            {
                var message = InitMessage(prefix);
                message.Append(text);
                return message;
            }

            partial interface IEnumCase
            {
                FixedString512Bytes ToMessage(in FixedString64Bytes prefix);
            }

            // Payload-free case — no context would make it more actionable.
            public readonly partial struct Undefined
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                    => Message(prefix, "An unknown feature error has occurred.");
            }

            // Contextual case — record struct, typed id payload.
            public readonly partial record struct UnknownDefinition(PlayerSkillId SkillId)
            {
                public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                {
                    var message = InitMessage(prefix);
                    message.Append("The feature definition id '");
                    message.Append(SkillId.Value);
                    message.Append("' is unknown.");
                    return message;
                }
            }
        }
    }
}
```

## Case and payload rules

- **Always include `Undefined`.** `default(TError).ToString()` must be safe and meaningful — a
  `Result` error field can be default-initialized.
- Payload-free case → `readonly partial struct`. Contextual case → `readonly partial record struct`.
- Payloads: typed ids (`PlayerSkillId`, `WeaponId`), state values, tokens, amounts, deadlines.
  **Never** strings where a typed id exists.
- Payloads stay **immutable, small, unmanaged**. Never retain mutable runtime objects, exceptions,
  `UnityEngine.Object`s, collections, or service references — that defeats the Burst/`FixedString`
  design and leaks lifetimes into the error.
- Messages are built with `FixedString512Bytes`. Managed formatting (`.ToString()`) is acceptable
  only on a cold path where `FixedString` has no direct `Append` overload — `double` is one such
  case. Never on a path that a job or a per-frame log can reach.
- Call sites use the **generated factories with parentheses**: `MachineError.EmptyMachine()`,
  `MachineError.UnknownState(ordinal)`,
  `MachineError.TriggerQueueFull(trigger, capacity)`. Enum-style member access
  (`MachineError.EmptyMachine` without parentheses) does not exist, and seeing it is the tell that
  someone reintroduced a flat enum.
- **Keep validation in one place.** `MachineError`'s thirteen cases are all produced by a single
  `MachineBuilder`2+Validate.cs`, which makes the error surface auditable in one read. Do that for
  new error types too rather than scattering factories across the module.
- Add `.Prefix(nameof(Operation))` at boundaries where the failing operation is otherwise ambiguous.

## Validation before handoff

Cover with tests:

1. one payload case and its **exact** message string;
2. `Prefix(...)` formatting;
3. `default(TError)` / `Undefined` behaviour;
4. each changed call site returning the intended generated case;
5. any transaction/rollback behaviour tied to the error.

**Compile through Unity** — PolyEnum factories are source-generated, so IDE analysis proves nothing.
Use `unity-cli-workflow` for that compile and for running the tests:

```powershell
unity test . --mode EditMode --non-interactive --filter "<YourErrorTests>" `
    --output "<scratchpad>/editmode-results.xml"
```

Audit the touched scope before reporting done. Scope the search to the assemblies that actually hold
first-party code — `Packages/com.apexion.apexion-game` today, plus `Assets/Game` once it has any:

```powershell
# 1. a flat enum whose name ends in Error — the violation itself
rg -n "enum\s+\w*Error\b" Packages/com.apexion.apexion-game Assets/Game -g '*.cs'

# 2. every Result<,> that carries an error type — check each TError is a PolyEnum wrapper
rg -n "Result<[^>]*\w+Error" Packages/com.apexion.apexion-game Assets/Game -g '*.cs'

# 3. enum-style member access on an error type — the tell that a flat enum came back.
#    Expect noise from field access (`_error.ToMessage`); read the hits, do not just count them.
rg -n "\b[A-Z]\w*Error\.[A-Za-z_]\w*(?!\()" Packages/com.apexion.apexion-game Assets/Game -g '*.cs' --pcre2
```

Review every hit inside what you touched. **An existing unrelated legacy violation never authorizes
a new flat enum.** Do not report success while a touched `Result` error is still a flat enum.

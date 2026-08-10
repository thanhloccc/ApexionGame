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

| File | Status |
|---|---|
| `Assets/Game/Game.Gameplay/Player/Common/PlayerError.cs` | ✅ the reference implementation — read this first |
| `Assets/Game/Game.Gameplay/Weapons/Common/WeaponError.cs` | ✅ conforming |
| `Assets/Game/Game.Gameplay/Equipment/Common/EquipmentError.cs` | ✅ migrated from a flat enum |
| `Assets/Game/Game.Data/Persistence/Player/PlayerPersistenceError.cs` | ❌ **legacy flat enum** — do not copy; migrate when that surface is next touched |
| `Packages/…/Samples~/EncosyTower.Samples.Persistence/Persistences/PlayerDataError.cs` | ✅ package sample |

`Assets/Game/Game.Gameplay.Tests/Player/PlayerErrorTests.cs` shows the expected test shape.

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
  case, see `PlayerError.InvalidTime`.
- Call sites use the **generated factories with parentheses**: `PlayerError.Dead()`,
  `PlayerError.UnknownDefinition(skillId)`. Enum-style member access (`PlayerError.Dead`) does not
  exist and is a sign someone reintroduced a flat enum.
- Add `.Prefix(nameof(Operation))` at boundaries where the failing operation is otherwise ambiguous.

## Validation before handoff

Cover with tests:

1. one payload case and its **exact** message string;
2. `Prefix(...)` formatting;
3. `default(TError)` / `Undefined` behaviour;
4. each changed call site returning the intended generated case;
5. any transaction/rollback behaviour tied to the error.

**Compile through Unity** — PolyEnum factories are source-generated, so IDE analysis proves nothing.
Use `unity-cli-workflow` for that compile and for running the tests.

Audit the touched scope before reporting done:

```powershell
rg -n "enum\s+\w*Error\b" Assets/Game -g '*.cs'
rg -n "Result<.*\w+Error" Assets/Game -g '*.cs'
rg -n "Error\.[A-Za-z0-9_]+(?!\()" Assets/Game -g '*.cs' --pcre2
```

Review every hit inside what you touched. **An existing unrelated legacy violation never authorizes
a new flat enum.** Do not report success while a touched `Result` error is still a flat enum.

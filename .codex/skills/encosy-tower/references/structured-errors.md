# Structured Result errors

Use this reference whenever game code creates, changes, or reviews a type used as
`TError` in `Result<T, TError>`.

## Non-negotiable decision

- Represent expected gameplay/domain failures with EncosyTower PolyEnumStructs.
- Never introduce or preserve a touched flat `enum ...Error` as a Result error.
- Use a plain enum only for states, modes, flags, or categories that are not
  returned as `TError`.
- Treat the current `Game.Gameplay.Equipment.EquipmentError` enum as legacy debt,
  not as a reference implementation.

Signature-level references:

- `Packages/com.laicasaane.encosy-tower/Samples~/EncosyTower.Samples.Persistence/Persistences/PlayerDataError.cs`
- `Assets/Game/Game.Gameplay/Weapons/Common/WeaponError.cs`
- `Assets/Game/Game.Gameplay/Player/Common/PlayerError.cs`

## Required shape

```csharp
[PolyEnumFactoryFor(typeof(Error))]
public readonly partial struct FeatureError
{
    private readonly FixedString64Bytes _prefix;
    private readonly Error _error;

    private FeatureError(in Error error) : this(error, default)
    {
    }

    private FeatureError(in Error error, in FixedString64Bytes prefix)
    {
        _prefix = prefix;
        _error = error;
    }

    public FeatureError Prefix(in FixedString64Bytes prefix)
        => new(_error, prefix);

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

        public readonly partial struct Undefined
        {
            public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
                => Message(prefix, "An unknown feature error has occurred.");
        }

        public readonly partial record struct UnknownDefinition(FeatureId Id)
        {
            public FixedString512Bytes ToMessage(in FixedString64Bytes prefix)
            {
                var message = InitMessage(prefix);
                message.Append("The feature definition id '");
                message.Append(Id.Value);
                message.Append("' is unknown.");
                return message;
            }
        }
    }
}
```

Apply the repository's inlining and formatting conventions to the final
implementation.

## Case and payload rules

- Include `Undefined`; `default(TError).ToString()` must be safe and meaningful.
- Use a payload-free `readonly partial struct` only when no context improves the
  error.
- Use a `readonly partial record struct` for contextual cases.
- Prefer typed IDs, tokens, state values, amounts, and deadlines over strings.
- Keep payloads immutable and small; do not retain mutable runtime objects,
  exceptions, Unity objects, collections, or service references.
- Build messages with `FixedString512Bytes`. Managed formatting is acceptable only
  on a cold error path when `FixedString` has no direct append overload.
- Return generated factories with parentheses, for example
  `FeatureError.UnknownDefinition(id)`, never enum-style members.
- Add `.Prefix(nameof(Operation))` at boundaries where the operation is otherwise
  ambiguous.

## Required validation

Add or update tests that cover:

1. one payload case and its exact message;
2. `Prefix(...)` formatting;
3. `default(TError)`/`Undefined` behavior;
4. each changed call site returning the intended generated case;
5. any transaction rollback behavior associated with the error.

Compile through Unity so PolyEnum source generation is exercised. IDE analysis is
not sufficient.

Audit before handoff:

```powershell
rg -n "enum\s+\w*Error\b" Assets/Game -g '*.cs'
rg -n "Result<.*\w+Error" Assets/Game -g '*.cs'
rg -n "Error\.[A-Za-z0-9_]+(?!\()" Assets/Game -g '*.cs' --pcre2
```

Review every match in the touched scope. Existing unrelated legacy violations do
not authorize another flat enum.

# Type and API design

`CODING-CONVENTIONS.md` §8.

## 1. Choosing a type shape

| Shape | For |
|---|---|
| `struct` | value-like models, small data carriers |
| `readonly struct`, `readonly record struct` | immutable value types |
| `[WrapRecord] readonly partial record struct` | wrapper / id types — the generator fills it in |
| `internal readonly struct` | marker types with no members |
| `class` | identity, lifetime, polymorphism |

```csharp
[WrapRecord]
public readonly partial record struct StringId(Id Id) : IIsValid
{
    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Id > 0;
    }
}
```

**Implement `IEquatable<T>` on every struct** used in equality comparisons or as a cache key —
otherwise every comparison boxes. The full pattern is in `members-and-attributes.md` §4.

Nested types are fine for closely related helpers, and a nested static class can group API helpers:

```csharp
public static partial class MyVault
{
    public static class API { ... }
}
```

## 2. Reuse the capability interfaces

Before inventing a member, check whether a shared capability interface already names it:
`IIsCreated`, `IIsValid`, `IHasValue`, `IHasCount`, `IHasCapacity`, `IClearable`, `IToArray<T>`,
`IAsSpan<T>`, and the rest in the library's contracts folders.

Implementing them is not decoration — the library's generic extension methods are constrained on
them, so a type that implements `IAsSpan<T>` gets the whole span extension set for free. See
`encosy-tower/references/collections-and-math.md`.

## 3. Failure signatures — the rule with no exceptions

> A method that can fail returns `Option<T>` or follows `bool TryXxx(...)`.
> **Never return `null`. Never throw for an expected failure.**

```csharp
public Option<T> Find([NotNull] Predicate<T> match) { ... }
public bool TryAdd<T>(T instance) { ... }
```

Exceptions are for **programmer error**, raised through the project's throw helpers — not for a
lookup that found nothing, a parse that failed, or a file that was absent.

The prefix says the shape, so a caller knows the contract before reading the body:

| Prefix / suffix | Returns |
|---|---|
| `Try*` | `bool` + `out`, or `Option<T>` |
| `*OrError` | `Result<T, TError>` |
| `*OrThrow` | the value, throws on failure |
| `*OrDefault` | the value, falls back to a supplied default |

Modelling the `TError` itself is `encosy-tower/references/structured-errors.md`, and it is
**never a flat enum**.

## 4. Sentinels and defaults

Sentinel and default instances are `static readonly` fields, not properties or methods:

```csharp
public readonly static Option<T> None = default;
public static readonly DevLogger Default = new();
```

## 5. Attributes and serialization

- Custom attributes always declare `[AttributeUsage(...)]` and **validate their constructor
  arguments**.
- Unity-serialized structs use `[Serializable]` with `[field: SerializeField]` on **auto-properties,
  not public fields**:

```csharp
[Serializable]
public struct BindingProperty
{
    [field: SerializeField]
    public string TargetPropertyName { get; set; }
}
```

## 6. Access modifiers

- **Always write the accessibility** on non-interface members.
- Modifier order: `public`, `private`, `protected`, `internal`, `static`, `extern`, `new`, `virtual`,
  `abstract`, `sealed`, `override`, `readonly`, `unsafe`, `volatile`, `async`.
- Interface members with a default implementation include `public`.

> **Never change an existing public API for style alone** — type names, namespaces, signatures,
> receivers, `in`/`ref`/`readonly`, or field shapes stay as they are.

That last one is a hard stop. A style-only public API change breaks consumers for zero benefit, and
"it would read better" is not a reason. If the API is genuinely wrong, that is a design change with
a design change's justification — see `refactoring`.

## 7. Inheritance and constraint wrapping

Base list stays on one line while the declaration fits in 100 characters:

```csharp
public struct LocationInfo : IEquatable<LocationInfo>
```

Past 100, wrap with leading commas, one entry per line, indented. Between 100 and 120 wrapping is
recommended; past 120 it is required. **Interfaces gated by defines come last:**

```csharp
public partial struct ListNative<T> : IDisposable, IReadOnlyList<T>, IIndexer<T>
    , IAsSpan<T>, IAsReadOnlySpan<T>, IToArray<T>
    , IIncreaseCapacity, IClearable
#if UNITY_COLLECTIONS
    , INativeDisposable
#endif
    where T : unmanaged
```

`where` clauses always go on the next line, one per line, indented one level.

## 8. Before shipping a public API

- [ ] Can it fail? Then `Option<T>` or `TryXxx` — not `null`, not an exception
- [ ] Struct used in comparisons or as a key? Then `IEquatable<T>`, fully implemented
- [ ] Does a capability interface already name this member? Use it
- [ ] Is every accessibility written explicitly?
- [ ] Does the name's prefix match its actual return shape?
- [ ] Is this changing an existing public API? If for style alone — **stop**
- [ ] Does the type carry a source-generator attribute? Then it must be `partial`, and the members
      only exist after a Unity compile (`unity-cli-workflow`)

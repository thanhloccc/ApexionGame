# Members, attributes and performance annotations

`CODING-CONVENTIONS.md` §9 and §11.

## 1. Attribute placement

- Related attributes may share a line: `[SerializeField, HideInInspector]`.
- Long or documentation-relevant attributes get their own line.
- `[MethodImpl]` may share a line on a small method, or sit alone above the signature.
- `[field: SerializeField]` serializes an auto-property's backing field.

**On properties, `[MethodImpl]` goes on the accessor, not the property:**

```csharp
public readonly int Count
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _count;
}
```

Multi-line accessors keep the same shape — the attribute repeats on each accessor:

```csharp
public T this[int index]
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
        ThrowIfIndexOutOfRange((uint)index < (uint)_count);
        return _buffer[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    set
    {
        ThrowIfIndexOutOfRange((uint)index < (uint)_count);
        _version++;
        _buffer[index] = value;
    }
}
```

## 2. `in`, `readonly`, and overload pairs

- **Value APIs come in pairs** — one overload takes `T item`, one takes `in T item`.
- **Every struct instance method that does not mutate state is marked `readonly`.** This is not
  decoration: without it the compiler makes a defensive copy on every call through an `in`
  parameter, which is exactly the copy `in` was there to avoid.
- **`in` for readonly struct parameters on hot paths.** Never `in` for classes or `ref struct`.
- Extension methods mark the receiver `[NotNull]` and take structs by `in`:

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static bool Contains<T>([NotNull] this in SharedListNative<T> self, T item)
    where T : unmanaged, IEquatable<T>
```

## 3. Native container fields

Fields of native containers use Unity's `m_PascalCase`, inside an `IDE1006` pragma pair — an
exception to the project's `_camelCase` rule, because Unity's own tooling expects it.

## 4. The `IEquatable<T>` pattern

Implement it whole. A partial implementation is worse than none, because the missing piece silently
falls back to the boxing default.

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly bool Equals(Foo other)
    => string.Equals(fieldA, other.fieldA, StringComparison.Ordinal)
    && fieldB == other.fieldB
    ;

[MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly override bool Equals(object obj)
    => obj is Foo other && Equals(other);

[MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly override int GetHashCode()
    => HashCode.Combine(fieldA, fieldB);

[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static bool operator ==(Foo left, Foo right)
    => left.Equals(right);

[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static bool operator !=(Foo left, Foo right)
    => left.Equals(right) == false;
```

- **Every string comparison uses `string.Equals(..., StringComparison.Ordinal)`.** Culture-sensitive
  comparison is both slower and wrong for identifiers.
- **Combine hashes with `HashCode.Combine`.** Never sum or XOR raw field hashes by hand.
- Multi-line boolean chains end with a trailing `;` on its own line.

## 5. `[MethodImpl(AggressiveInlining)]` — when it actually pays

The JIT already inlines tiny methods on its own — bodies within roughly 32 IL bytes, about five
simple statements. So the attribute is **not** a general-purpose speed marker.

**It pays on bodies of 5 to 9 statements**, in these shapes:

- property accessors with real logic
- operator overloads and conversion operators
- wrappers that forward to a single call
- `return new(...)` factory methods

Annotating a 1–4 statement body is redundant, though acceptable on hot-path structs for
explicitness.

**Do not apply when:**

- the body has more than 9 statements
- it contains loops, deep branching, `async`/`await`, or `try`/`catch`/`finally`
- the method is on a cold path

Applying it everywhere is a real cost, not a neutral one: it bloats call sites and pollutes the
instruction cache, which is the opposite of the intent.

## 6. `[MethodImpl(NoInlining)]` — cold paths must opt out

Cold code left un-annotated may be inlined into hot callers, bloating them for no benefit.

**Always annotate:**

- unconditional throw helpers — methods that always throw
- `CreateException` local functions inside conditional guards
- logging methods — string formatting and I/O are never hot
- error and fallback paths — anything that only runs when things go wrong

`[Conditional("DEBUG")]` methods compile away entirely in release, so they need no annotation.

```csharp
[MethodImpl(MethodImplOptions.NoInlining)]
[HideInCallstack, StackTraceHidden, DoesNotReturn]
private static void ThrowArgumentNullException(string paramName)
    => throw new ArgumentNullException(paramName);

[MethodImpl(MethodImplOptions.NoInlining)]
private static void LogWarningInvalidUserId(ILogger logger)
{
    logger.LogWarning("User id is invalid.");
}

[Conditional("DEBUG")]
private static void AssertIndexInRange(int index, int length)
{
    Debug.Assert((uint)index < (uint)length);
}
```

## 7. These are conventions, not an optimisation strategy

Everything in §5 and §6 is **house style applied while writing** — where the annotation goes and
which shape gets which attribute.

Whether a given path is worth optimising at all, what the frame budget is, and what evidence proves
a change helped — that is `midcore-perf-budget`, and it requires a measurement. Adding
`AggressiveInlining` to a method is not a performance improvement until something measured says so.

Choosing the data structure or math type in the first place is `encosy-tower` →
`references/collections-and-math.md`.

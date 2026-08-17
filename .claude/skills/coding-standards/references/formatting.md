# Formatting and everyday style

`CODING-CONVENTIONS.md` §2 and §4. This file is the working form; the document is authoritative if
the two ever disagree.

## 1. Basics

- **4-space indent**, no tabs. **LF** endings. Every file ends with a newline.
- One statement per line. Never squeeze a method body onto one line.
- **≤100 characters, 120 hard limit** — comments included. Break anything longer.

## 2. Braces

Opening brace on its own line for types, methods, properties, accessors, and control blocks.

**Every control block gets braces** — `if`, `else`, `for`, `foreach`, `while`, `do`, `switch` cases.
No exceptions, even for a single `return` or `count++`.

```csharp
// ❌
if (count == 0) return;
if (index < 0)
    return false;

// ✅
if (count == 0)
{
    return;
}
```

Short inline forms are fine when they fit on one line: `get => _count;`, short lambdas, switch arms,
`new Foo { Value = 42 }`.

## 3. Blank lines

Blank line **above and below any statement that opens a `{ }` scope** — `if`, `for`, `foreach`,
`while`, `switch`, `try`, `using`, `lock`.

Skip it when two blocks touch by design (`if`/`else`, `try`/`catch`) or when the block is the first
or last statement in its parent scope.

```csharp
var count = list.Count;

for (var i = 0; i < count; i++)
{
    var item = list[i];
    DoWork(item);
}

return result;
```

## 4. Fields

No blank lines *within* a group. One blank line *between* groups, in this order:
`const` → `static readonly` → `static` → instance. One blank line between the last field and the
next member.

```csharp
private const int MAX_SIZE = 100;
private const string PREFIX = "item_";

private static readonly string[] s_keywords = { "if", "else" };

private static int s_counter;

private string _name;
private int _value;

public string Name => _name;
```

**No column alignment.** One space before `=`. Only `switch` expression arms may align their `=>`.

## 5. Spaces and wrapping

Space after every comma, around operators, and in generic constraints. When a long line wraps, **the
operator starts the new line**.

```csharp
// ✅
public static int IndexOf<T, TComparer>(this in ListNative<T> self, T item, TComparer comparer)
    where T : unmanaged
    where TComparer : unmanaged, IEqualityComparer<T>
```

Each `where` on its own line, indented one level.

### Expression bodies

`=>` goes on a **new indented line** for methods and properties. Accessors, lambdas and switch arms
may keep it inline.

```csharp
// ❌
public void AddRange(ReadOnlySpan<T> items) => AddRange(items, items.Length);

// ✅
public void AddRange(ReadOnlySpan<T> items)
    => AddRange(items, items.Length);
```

### Leading-comma wrapping

A parameter list stays on one line while it fits in 100 characters. Past 120 it **must** wrap. When
wrapping: **leading commas, one parameter per line, closing `)` on its own line.**

```csharp
public static TypeModel Extract(
      INamedTypeSymbol symbol
    , CancellationToken token
    , ModelOptions options
)
```

Call arguments and `new` expressions use the same style:

```csharp
return new FieldModel(
      name: field.Name
    , typeName: field.Type.Name
    , typeFullName: field.Type.FullName
    , accessibility: field.Accessibility
);
```

## 6. Object initializers

Opening brace on the **same line** as `new`. Trailing comma after the last entry.

```csharp
var settings = new AesManaged {
    Key = rfc2898.GetBytes(16),
    IV = rfc2898.GetBytes(16),
};
```

A short initializer may stay on one line: `new Foo { Value = 42 }`.

## 7. Everyday style (§4)

- **`var`** when the type is obvious from the right-hand side; explicit type otherwise.
- **Target-typed `new(...)`** when the type is clear from context.
- **Pattern matching**: `is not`, `is { }`, `is not null`, `switch` expressions.
- **`== false` instead of `!`** in conditions: `if (string.IsNullOrEmpty(x) == false)`. Plain `!expr`
  is fine inside expression-bodied operators where it reads naturally.
- **No empty control blocks.** Invert the condition instead of `if (cond) { }`. Remove empty
  `else { }`. Merge `if (a) { } else { work; }` into `if (a == false) { work; }`.
- **Flatten nested ifs**: `if (a && b)` over `if (a) { if (b) … }`. `else if`, not `else { if … }`.

### Switch expressions

Opening brace on the same line, one arm per line, trailing comma, `_` arm last:

```csharp
public override bool Equals(object obj)
    => obj switch {
        Option<T> other => DefaultEquals(this, other),
        Bool<T> other => Equals(other),
        _ => false,
    };
```

### Loops

Prefer `for` when the collection has an indexer and a `Count`/`Length`. **Cache the count first.**

```csharp
var count = list.Count;

for (var i = 0; i < count; i++)
{
    var item = list[i];
    // ...
}
```

Use `foreach` only for types that expose nothing but `GetEnumerator()`. **Never call LINQ `.Count()`
on something that already has `.Count` or `.Length`.**

### Lambdas and LINQ

Mark lambdas **`static`** whenever they capture nothing: `.Where(static t => t.IsValid)`.
Multi-statement lambdas go on separate lines, opening brace on the same line as `=>`. Short LINQ
chains stay inline; long chains wrap one method per line.

### `goto` early exit

Parsing and validation methods use a labeled `goto` for early exit instead of nested `if` chains.
Labels are `ALL_UPPER`.

```csharp
public bool TryParse(ReadOnlySpan<char> str, out MyType result)
{
    if (str.IsEmpty)
    {
        goto FAILED;
    }

    if (int.TryParse(str, out var value))
    {
        result = new(value);
        return true;
    }

FAILED:
    result = default;
    return false;
}
```

This is deliberate house style, not a smell. It keeps the success path flat and puts every failure
exit in one place.

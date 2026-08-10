# EncosyTower C# Coding Conventions

## How to use this document

- These rules apply to all C# code in this repository.
- Follow them when writing new code or reformatting old code.
- Sections are ordered by value for effort: the earlier the section, the more often it applies and the cheaper it is to follow.
- Good example files: `Collections/FasterList.cs`, `Collections/SharedListNative.cs`, `Collections/ArrayMapNative.cs`, `Collections.Extensions/SharedListNativeExtensions.cs`, `Common/Option.cs`, `Vaults/SingletonVault.cs`, `PubSub/Publishers/CachedPublisher.cs`, `Logging/DevLogger.cs`.

## 1. Naming

| What | Style | Example |
|---|---|---|
| Classes, structs, enums, delegates | PascalCase | `TypeModel`, `FieldSymbol` |
| Interfaces | `I` + PascalCase | `IPrintable`, `IEquatable` |
| Methods, properties | PascalCase | `Extract()`, `IsValid`, `HasAttribute` |
| Public/protected fields | camelCase | `enumName`, `hasFlags` |
| Public/protected readonly fields | PascalCase | `Name`, `FullName`, `Attributes` |
| Public/protected static fields | PascalCase | `Default`, `Empty`, `None` |
| Private/internal fields | `_` + camelCase | `_builder`, `_symbol`, `_count` |
| Private/internal static fields | `s_` + camelCase | `s_pool`, `s_safetyId` |
| Constants (`const`) | ALL_UPPER | `PRIME`, `NEWLINE`, `MAX_SIZE` |
| Locals and parameters | camelCase | `hintName`, `builder`, `result` |
| `goto` labels | ALL_UPPER | `FAILED`, `DONE`, `RETRY` |

- Fields in native container structs follow Unity's `m_PascalCase` style. Wrap them in an IDE1006 pragma pair:

  ```csharp
  #pragma warning disable IDE1006 // Naming Styles
      [NativeDisableUnsafePtrRestriction]
      internal unsafe ListUnsafe<T>* m_Data;

  #if ENABLE_UNITY_COLLECTIONS_CHECKS
      internal AtomicSafetyHandle m_Safety;
  #endif
  #pragma warning restore IDE1006 // Naming Styles
  ```

- Extension classes for types this project does not own (BCL, Unity) are named `Encosy<Type>Extensions`, for example `EncosyStringExtensions`. Extension classes for project types are named `<Type>Extensions`.
- Async methods end with `Async`.
- Custom attribute class names end with `Attribute`.

## 2. Formatting

### Basics

- 4-space indentation. No tabs.
- LF line endings.
- Every file ends with a newline.
- One statement per line. Never squeeze a whole method body onto one line.
- Line length: try to stay under 100 characters. 120 is the hard limit — break any line longer than that, comments included.

### Braces

- Opening brace goes on its own line for types, methods, properties, accessors, and control blocks.

  ```csharp
  public void Add(T item)
  {
      CheckResizeWrite();
      ...
  }
  ```

- Every control block body gets braces — `if`, `else`, `for`, `foreach`, `while`, `do`, `switch` cases. No exceptions, even for a single `return` or `count++`.

  ```csharp
  // ❌ SHOULDN'T
  if (count == 0) return;
  if (index < 0)
      return false;

  // ✅ SHOULD
  if (count == 0)
  {
      return;
  }
  ```

- Short inline forms are fine when they fit on one line: `get => _count;`, short lambdas, switch arms, and `new Foo { Value = 42 }`.

### Blank lines

- Put a blank line above and below any statement that opens a `{ }` scope — `if`, `for`, `foreach`, `while`, `switch`, `try`, `using`, `lock`, and so on.
- Skip the blank line when two blocks touch by design (`if`/`else`, `try`/`catch`) or when the block is the first or last statement in its parent scope.

  ```csharp
  var count = list.Count;

  for (var i = 0; i < count; i++)
  {
      var item = list[i];
      DoWork(item);
  }

  return result;
  ```

### Fields

- No blank lines between fields of the same group.
- Groups in this order, separated by one blank line: `const`, then `static readonly`, then `static`, then instance fields.
- One blank line between the last field and the next member.

  ```csharp
  private const int MAX_SIZE = 100;
  private const string PREFIX = "item_";

  private static readonly string[] s_keywords = { "if", "else" };

  private static int s_counter;

  private string _name;
  private int _value;

  public string Name => _name;
  ```

- No column alignment. One space before `=`. Only `switch` expression arms may align their `=>`.

  ```csharp
  // ❌ SHOULDN'T — column-aligned
  var monoBinderBaseSymbol      = compilation.GetTypeByMetadataName(...);
  var isOuterClassSealed        = userClassSymbol.IsSealed;

  // ✅ SHOULD
  var monoBinderBaseSymbol = compilation.GetTypeByMetadataName(...);
  var isOuterClassSealed = userClassSymbol.IsSealed;
  ```

### Spaces

- Space after every comma. Spaces around operators. Spaces in generic constraints.
- When a long line wraps, the operator starts the new line.

  ```csharp
  // ❌ SHOULDN'T
  public static int IndexOf<T,TComparer>(this in ListNative<T> self,T item,TComparer comparer)
      where T:unmanaged where TComparer:unmanaged,IEqualityComparer<T>

  // ✅ SHOULD
  public static int IndexOf<T, TComparer>(this in ListNative<T> self, T item, TComparer comparer)
      where T : unmanaged
      where TComparer : unmanaged, IEqualityComparer<T>
  ```

### Wrapping methods and calls

- Expression-bodied methods and properties put `=>` on a new line, indented one level. Accessors, lambdas, and switch arms may keep `=>` inline.

  ```csharp
  // ❌ SHOULDN'T
  public void AddRange(ReadOnlySpan<T> items) => AddRange(items, items.Length);

  // ✅ SHOULD
  public void AddRange(ReadOnlySpan<T> items)
      => AddRange(items, items.Length);
  ```

- A parameter list stays on one line while the whole line fits in 100 characters. Over 120, it must wrap. When wrapping: leading commas, one parameter per line, closing `)` on its own line.

  ```csharp
  public static TypeModel Extract(
        INamedTypeSymbol symbol
      , CancellationToken token
      , ModelOptions options
  )
  ```

- Multi-line call arguments and `new` expressions use the same leading-comma style:

  ```csharp
  context.OutputSource(
        outputSourceGenFiles
      , candidate.openingSource
      , declaration.WriteCode()
  );

  return new FieldModel(
        name: field.Name
      , typeName: field.Type.Name
      , typeFullName: field.Type.FullName
      , accessibility: field.Accessibility
  );
  ```

### Object initializers

- Opening brace on the same line as `new`. Trailing comma after the last entry.

  ```csharp
  var settings = new AesManaged {
      Key = rfc2898.GetBytes(16),
      IV = rfc2898.GetBytes(16),
  };
  ```

- A short initializer may stay on one line: `new Foo { Value = 42 }`.

## 3. Usings & Namespaces

- `using` directives go outside the namespace block in production code.
- Sort alphabetically in three groups, no blank line between them: `System*` first, then `EncosyTower*`, then `Unity*`.

  ```csharp
  using System;
  using System.Collections.Generic;
  using System.Runtime.CompilerServices;
  using EncosyTower.Buffers;
  using EncosyTower.Common;
  using EncosyTower.Debugging;
  using Unity.Collections;
  using Unity.Collections.LowLevel.Unsafe;
  ```

- `using static` is fine for importing constant containers.
- Production code uses block-scoped namespaces (`namespace X { ... }`). File-scoped namespaces (`namespace X;`) only in tests and samples.
- One file may hold several namespace blocks when platform-specific partials of the same type must live together under `#if`.
- Conditional type aliases (`using T = ...` under `#if`) go inside the namespace block:

  ```csharp
  namespace EncosyTower.PubSub
  {
  #if UNITASK
      using UnityTask = Cysharp.Threading.Tasks.UniTask;
  #else
      using UnityTask = UnityEngine.Awaitable;
  #endif
  ```

## 4. Everyday Code Style

- `var` when the type is obvious from the right-hand side. Explicit type otherwise.
- Target-typed `new(...)` when the type is clear from context.
- Prefer pattern matching: `is not`, `is { }`, `is not null`, `switch` expressions.
- Write `== false` instead of `!` in conditions: `if (string.IsNullOrEmpty(x) == false)`. Plain `!expr` is fine inside expression-bodied operators where it reads naturally.
- No empty control blocks. Invert the condition instead of `if (cond) { }`. Remove empty `else { }`. Merge `if (a) { } else { work; }` into `if (a == false) { work; }`.
- Flatten nested ifs: `if (a && b)` instead of `if (a) { if (b) ... }`. Use `else if` instead of `else { if ... }`.
- `switch` expressions for type dispatch — opening brace on the same line, one arm per line, trailing comma, `_` arm last:

  ```csharp
  public override bool Equals(object obj)
      => obj switch {
          Option<T> other => DefaultEquals(this, other),
          Bool<T> other => Equals(other),
          _ => false,
      };
  ```

### Loops

- Prefer `for` when the collection has an indexer and a `Count`/`Length`. Cache the count first:

  ```csharp
  var count = list.Count;

  for (var i = 0; i < count; i++)
  {
      var item = list[i];
      // ...
  }
  ```

- Use `foreach` only for types that expose nothing but `GetEnumerator()`.
- Never call LINQ `.Count()` on something that already has `.Count` or `.Length`.

### Lambdas & LINQ

- Mark lambdas `static` whenever they capture nothing: `.Where(static t => t.IsValid)`.
- Multi-statement lambdas go on separate lines, opening brace on the same line as `=>`.
- Short LINQ chains stay inline. Long chains wrap one method per line.

### Early exit with `goto`

- Parsing and validation methods use labeled `goto` for early exit instead of nested `if` chains. Labels are ALL_UPPER:

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

## 5. Comments

- **Do not write comments or XML docs unless the Project Owner asks for them.** This covers `//`, `/* */`, section dividers, and every XML doc tag.
- **Do not touch existing comments** in files you edit. Leave them exactly as they are.
- Always allowed:
  - `// SAFETY:` comments above `unsafe` blocks — these are **required** (see Section 13)
  - License headers and attribution comments on vendored or ported files
  - Auto-generated comments from build tools (`<auto-generated>`)
  - `#region` / `#endregion`
- When XML docs are requested, keep them short: `<summary>` says what the type does, `<remarks>` covers thread safety and constraints. Never repeat what the code already says.

  ```csharp
  /// <summary>
  /// A dictionary that stores its values in a contiguous array, so the values
  /// can be iterated directly as an array, without an enumerator.
  /// </summary>
  /// <remarks>
  /// Not thread-safe.
  /// </remarks>
  public partial struct ArrayMapNative<TKey, TValue> : ...
  ```

## 6. Files & Layout

### File naming

| File content | Convention | Example |
|---|---|---|
| Single primary type | Type name matches file name | `ArrayMap.cs` |
| Partial split of a type | `TypeName+Aspect.cs` | `ArrayMap+ReadOnly.cs` |
| Nested type split | `TypeName+NestedTypeName.cs` | `SharedList+Enumerator.cs` |
| Generic type | `` TypeName`N.cs `` | `` Id`1.cs ``, `` StringEnum`2.cs `` |
| Async extension file | `TypeName_Async.cs` | `` ProcessHub`1_Async.cs `` |
| Backend-specific partial | `TypeName_Backend.cs` | `UnityTasks_Awaitable.cs`, `UnityTasks_UniTask.cs` |
| Generated file | `*.gen.cs` — never hand-edit | `ByteBools.gen.cs` |

- One primary type per file. Small helper types may stay next to the type they serve.
- One extension class per file:

  ```text
  ListProxyExtensions.cs
  ListProxyReadOnlyExtensions.cs   // separate file, not appended to the file above
  ```

- Mark a type `partial` when it is split across files.
- A module folder keeps its own `AssemblyInfo.cs` when it needs assembly-level attributes.
- Files copied or ported from other projects keep the original license header at the very top, plus a comment linking to the source URL (see `FasterList.cs`).

### Define blocks

- Do not add a file-local define block merely because a file calls or declares validation guards.
  Conditional guards reference the constants in `EncosyTower.Debugging.ValidationDefines` directly.
- A file that contains inline validation branches keeps a file-local block. The condition must match
  the applicable `ValidationDefines` set; common validation uses:

  ```csharp
  #if !(UNITY_EDITOR || DEBUG || ENCOSY_RUNTIME_CHECKS) || DISABLE_ENCOSY_CHECKS
  #define __ENCOSY_NO_VALIDATION__
  #else
  #define __ENCOSY_VALIDATION__
  #endif
  ```

- Areas with their own symbol append it to the condition. Collections and Buffers append both
  `ENCOSY_COLLECTIONS_RUNTIME_CHECKS` and `ENABLE_UNITY_COLLECTIONS_CHECKS`, so either symbol
  enables Encosy Collections validation. A feature wrapper remains outermost, with any kept
  validation block inside it:

  ```csharp
  #if !(UNITY_EDITOR || DEBUG || ENCOSY_RUNTIME_CHECKS || ENCOSY_COLLECTIONS_RUNTIME_CHECKS || ENABLE_UNITY_COLLECTIONS_CHECKS) || DISABLE_ENCOSY_CHECKS
  #define __ENCOSY_NO_VALIDATION__
  #else
  #define __ENCOSY_VALIDATION__
  #endif
  ```

  ```csharp
  #if UNITASK || UNITY_6000_0_OR_NEWER
  ...whole file...
  #endif
  ```

| Symbol | Scope |
|---|---|
| `ENCOSY_RUNTIME_CHECKS` | Enables every Encosy validation guard in release builds |
| `ENCOSY_COLLECTIONS_RUNTIME_CHECKS` | Collections, Buffers, collection extensions, and StringIds enumerators |
| `ENABLE_UNITY_COLLECTIONS_CHECKS` | Enables Unity native-container safety and Encosy Collections validation |
| `ENCOSY_PUBSUB_RUNTIME_CHECKS` | PubSub inline validation |
| `ENCOSY_PROCESSING_RUNTIME_CHECKS` | Processing guards and inline validation |
| `ENCOSY_STATS_RUNTIME_CHECKS` | Entities.Stats guards and generated stats code |
| `DISABLE_ENCOSY_CHECKS` | Strips all conditional Encosy guards and disables all kept inline branches |

`ENCOSY_COLLECTIONS_RUNTIME_CHECKS` is the lighter Encosy-only option. Do not replace direct
`#if ENABLE_UNITY_COLLECTIONS_CHECKS` safety blocks with Encosy symbols. `DISABLE_ENCOSY_CHECKS`
strips Encosy validation but does not disable Unity's own native-container safety implementation.

- Editor-only files: wrapped whole in `#if UNITY_EDITOR`, live in an `Editor*` folder, use an `EncosyTower.Editor.*` namespace, and mark public APIs with `[ApiForEditor]`.

  ```csharp
  #if UNITY_EDITOR

  namespace EncosyTower.Editor.Common
  {
      [ApiForEditor]
      public static class SerializableGuidEditorExtensions
      {
          ...
      }
  }

  #endif
  ```

## 7. Member Ordering

Members appear in this order of member kinds:

1. Fields
2. Constructors (static constructor first)
3. Indexers
4. Properties
5. Operators
6. Methods
7. Nested types (interfaces, then static classes, then other types)

Within each kind, sort twice:

- First by modifier: `const` → `static readonly` → `static` → instance.
- Then by visibility: `public` → `protected` → `private`.

Separate each group with one blank line. Inside a group, no blank lines (see field rules in Section 2).

Example skeleton:

```csharp
public const int MAX = 10;

private const int MIN = 0;

public static readonly Foo Default = new();

private static int s_counter;

private int _value;

public Foo(int value) { ... }

public int Value => _value;

public static Foo operator +(Foo a, Foo b) { ... }

public static Foo Create() { ... }

public void DoWork() { ... }

private void DoWorkCore() { ... }
```

### Unity types (`MonoBehaviour`, `ScriptableObject`, ...)

Same rules, with one change: serialized fields come first, because Unity serializes `public` fields by default.

1. `public` fields (serialized by default)
2. `[SerializeField] protected` fields
3. `[SerializeField] private` fields
4. Everything else, in the plain-C# order above.

## 8. Type & API Design

### Choosing a type shape

- `struct` for value-like models and small data carriers.
- `readonly struct` and `readonly record struct` for immutable value types.
- Wrapper/id types are `[WrapRecord] readonly partial record struct`, filled in by source generators:

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

- Implement `IEquatable<T>` on every struct used in equality comparisons or as a cache key.
- Marker types with no members are `internal readonly struct`.
- Nested types are fine for closely related helpers. A nested static class can group related API helpers:

  ```csharp
  public static partial class MyVault
  {
      public static class API { ... }
  }
  ```

### API rules

- Reuse the shared capability interfaces instead of inventing new members: `IIsCreated`, `IIsValid`, `IHasValue`, `IHasCount`, `IHasCapacity`, `IClearable`, `IToArray<T>`, `IAsSpan<T>`, ...
- Sentinel and default instances are `static readonly` fields:

  ```csharp
  public readonly static Option<T> None = default;
  public static readonly DevLogger Default = new();
  ```

- A method that can fail returns `Option<T>` or follows the `bool TryXxx(...)` pattern. Never return `null`, never throw for an expected failure.

  ```csharp
  public Option<T> Find([NotNull] Predicate<T> match) { ... }
  public bool TryAdd<T>(T instance) { ... }
  ```

- Custom attributes always declare `[AttributeUsage(...)]` and validate their constructor arguments.
- Unity-serialized structs use `[Serializable]` with `[field: SerializeField]` on auto-properties, not public fields:

  ```csharp
  [Serializable]
  public struct BindingProperty
  {
      [field: SerializeField]
      public string TargetPropertyName { get; set; }
  }
  ```

### Access modifiers

- Always write the accessibility on non-interface members.
- Modifier order: `public`, `private`, `protected`, `internal`, `static`, `extern`, `new`, `virtual`, `abstract`, `sealed`, `override`, `readonly`, `unsafe`, `volatile`, `async`.
- Never change an existing public API for style alone — type names, namespaces, signatures, receivers, `in`/`ref`/`readonly`, or field shapes stay as they are.
- Interface members with a default implementation include `public`:

  ```csharp
  public interface IObservableObject
  {
      public bool TryGetMember(Queue<string> names, out IObservableObject result)
      {
          result = default;
          return false;
      }
  }
  ```

### Inheritance & constraint wrapping

- The base list stays on the same line while the whole declaration fits in 100 characters:

  ```csharp
  public struct LocationInfo : IEquatable<LocationInfo>
  ```

- Past 100 characters, wrap with leading commas, one entry per line, indented one level. Between 100 and 120 wrapping is recommended; past 120 it is required. Interfaces gated by defines come last:

  ```csharp
  internal struct PropertyDefinition
      : IEquatable<PropertyDefinition>
      , ICloneWithDim<PropertyDefinition>
      , ICast<PropertySignature>

  public partial struct ListNative<T> : IDisposable, IReadOnlyList<T>, IIndexer<T>
      , IAsSpan<T>, IAsReadOnlySpan<T>, IToArray<T>
      , IIncreaseCapacity, IClearable
  #if UNITY_COLLECTIONS
      , INativeDisposable
  #endif
      where T : unmanaged
  ```

- `where` clauses always go on the next line, one per line, indented one level:

  ```csharp
  public sealed class ObjectPool<T>
      where T : class

  public static string PrintToString<T>(T printable)
      where T : struct, IPrintable
  ```

## 9. Members & Attributes

- Related attributes may share a line: `[SerializeField, HideInInspector]`.
- Long or documentation-relevant attributes get their own lines:

  ```csharp
  [SerializeField]
  [HideInInspector]
  internal string _subtitle;
  ```

- `[MethodImpl]` can share a line with other attributes on a small method, or sit alone above the signature.
- Use `[field: SerializeField]` to serialize an auto-property's backing field:

  ```csharp
  [field: SerializeField]
  public string Name { get; set; }
  ```

- On properties, `[MethodImpl]` goes on the accessor, not the property:

  ```csharp
  public readonly int Count
  {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => _count;
  }
  ```

- Multi-line accessors keep the same shape:

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

- Value APIs come in pairs — one overload takes `T item`, one takes `in T item`:

  ```csharp
  public void Add(T item) { ... }
  public void Add(in T item) { ... }
  ```

- Extension methods mark the receiver `[NotNull]` and take structs by `in`:

  ```csharp
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool Contains<T>([NotNull] this in SharedListNative<T> self, T item)
      where T : unmanaged, IEquatable<T>
  ```

- Every struct instance method that does not mutate state is marked `readonly`:

  ```csharp
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public readonly override int GetHashCode()
      => _value.GetHashCode();
  ```

- Use `in` for readonly struct parameters on hot paths to avoid copies. Never use `in` for classes or `ref struct` types.

  ```csharp
  public static bool Equals(in Option<T> a, in Option<T> b)
      where T : IEquatable<T>
  ```

### The `IEquatable<T>` pattern

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

- Every string comparison in `Equals` uses `string.Equals(..., StringComparison.Ordinal)`.
- Combine hashes with `HashCode.Combine` (or a project equivalent). Never sum raw field hashes by hand.
- Multi-line boolean chains end with a trailing `;` on its own line.

## 10. Errors & Validation

The goal: keep the happy path fast and clean, keep throwing and logging out of it.

### Ground rules

- Never throw a raw exception at the call site. Use a dedicated helper.
- One helper checks one rule. Name it after what it checks.
- A rule used by one type stays a `private static` helper in that type. Shared rules belong to the
  area's ThrowHelper. `EncosyTower.Collections.ThrowHelper` serves Collections and Buffers;
  `EncosyTower.Debugging.ThrowHelper` contains only generic, non-conditional exception factories.
- Never call generic `ThrowHelper.ThrowIfFalse(...)` from collection call sites.
- Never write a catch-all `Validate(...)` that mixes unrelated checks.
- Every throw helper carries `[HideInCallstack, StackTraceHidden]` so it stays out of Unity console stack traces.
- Collection-specific shared messages take `EncosyTower.Collections.ThrowHelper.CollectionType`,
  not a type-name string. Resolve `CollectionType` only in the cold exception path. Unknown values
  fall back to `"collection"`.

### Conditional guards (`ThrowIfXxx`)

- The guard takes the already-evaluated condition as a `bool`, marked `[DoesNotReturnIf(false)]` (or `[DoesNotReturnIf(true)]` when true means throw).
- Import `EncosyTower.Debugging.ValidationDefines` statically and add the applicable `Conditional`
  set when the check should disappear in release builds. Common guards use `UNITY_EDITOR`, `DEBUG`,
  and `RUNTIME_CHECKS`; an area with its own opt-in symbol adds its area constant. Collections guards
  additionally use `UNITY_COLLECTIONS_CHECKS`, which maps to `ENABLE_UNITY_COLLECTIONS_CHECKS`.
- The guard itself must **not** be `NoInlining` — the fast path stays inlineable. Exception construction goes into a local static function named `CreateException`, marked `[MethodImpl(NoInlining)]`.

  The two snippets below are unrelated checks; they only show the wrong and right shape of the pattern.

  ```csharp
  using static EncosyTower.Debugging.ValidationDefines;

  // ❌ SHOULDN'T
  ThrowHelper.ThrowIfFalse((uint)startIndex < (uint)_count, "out of bound start index");
  ThrowHelper.ThrowIfFalse((uint)length <= (uint)(_count - startIndex), "out of bound length");

  // ✅ SHOULD
  [HideInCallstack, StackTraceHidden]
  [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
  [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
  [Conditional(UNITY_COLLECTIONS_CHECKS)]
  private static void ThrowIfInvalidAllocatorStrategy([DoesNotReturnIf(false)] bool validStrategy)
  {
      if (validStrategy == false)
      {
          throw CreateException();
      }

      [MethodImpl(MethodImplOptions.NoInlining)]
      static InvalidOperationException CreateException()
          => new(
              "Allocator strategy must be either Unity.Collections.Allocator " +
              "or Unity.Collections.AllocatorManager.AllocatorHandle"
          );
  }
  ```

- The `[DoesNotReturnIf(true)]` variant, for checks where true means failure:

  ```csharp
  [HideInCallstack, StackTraceHidden]
  private static void ThrowIfSameType([DoesNotReturnIf(true)] bool check)
  {
      if (check)
      {
          throw CreateException();
      }

      [MethodImpl(MethodImplOptions.NoInlining)]
      static InvalidOperationException CreateException()
          => new("Value type and error type must be different.");
  }
  ```

- Complex conditions (`sizeof`, type comparisons, ...) get their own `[MethodImpl(AggressiveInlining)]` helper, evaluated at the call site:

  ```csharp
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static bool IsValidSize()
      => UnsafeUtility.SizeOf<TBuffer>() >= UnsafeUtility.SizeOf<T>();

  static MyType()
  {
      ThrowIfInvalidSize(IsValidSize());
  }

  [HideInCallstack, StackTraceHidden]
  private static void ThrowIfInvalidSize([DoesNotReturnIf(false)] bool check)
  {
      if (check == false)
      {
          throw CreateException();
      }

      [MethodImpl(MethodImplOptions.NoInlining)]
      static InvalidOperationException CreateException()
          => new($"sizeof({typeof(TBuffer)}) must be >= sizeof({typeof(T)})");
  }
  ```

### Unconditional throw helpers (`ThrowXxx`)

- A method that always throws combines `[MethodImpl(NoInlining)]`, `[HideInCallstack, StackTraceHidden]`, and `[DoesNotReturn]`:

  ```csharp
  [MethodImpl(MethodImplOptions.NoInlining)]
  [HideInCallstack, StackTraceHidden, DoesNotReturn]
  private static void ThrowArgumentNullException(string paramName)
      => throw new ArgumentNullException(paramName);
  ```

- Add the applicable `ValidationDefines` attribute set when the throw is dev-only (see
  `Vaults/SingletonVault.cs`).

### Recoverable failures

- `Try*` methods log and return `false` instead of throwing. The log call goes in a named `LogError_Xxx` helper:

  ```csharp
  if (_singletons.ContainsKey(Type<T>.Hash))
  {
      LogError_InstanceAlreadyExists<T>();
      return false;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private static void LogError_InstanceAlreadyExists<T>()
      => StaticDevLogger.LogError($"An instance of {typeof(T)} has already been existing");
  ```

- A method that throws in dev builds but must keep working in release builds uses both paths:

  ```csharp
  #if !(UNITY_EDITOR || DEBUG || ENCOSY_RUNTIME_CHECKS) || DISABLE_ENCOSY_CHECKS
  #define __ENCOSY_NO_VALIDATION__
  #else
  #define __ENCOSY_VALIDATION__
  #endif

  if (instance == null)
  {
  #if __ENCOSY_VALIDATION__
      throw CreateArgumentNullException_Instance();
  #else
      return false;
  #endif
  }
  ```

### Messages & checks

- Error messages are plain sentences. Say what is required or what went wrong.
- Long messages split across concatenated interpolated strings, one per line:

  ```csharp
  throw new InvalidCastException(
      $"Cannot cast an instance of type {obj.GetType()} to {typeof(T)} " +
      $"even though it is registered for {typeof(T)}"
  );
  ```

- Range checks use the unsigned-cast trick: `(uint)index < (uint)_count`. One cast catches both negative and too-large values.
- Prefer string interpolation over concatenation. Raw string literals (`"""..."""`) in tests and generated code.

## 11. Performance Annotations

### `[MethodImpl(AggressiveInlining)]` — hot paths

The JIT already inlines tiny methods on its own (bodies within ~32 IL bytes, roughly up to 5 simple statements). The attribute pays off on bodies of **5 to 9 statements** in these shapes:

- Property accessors with real logic
- Operator overloads and conversion operators
- Wrappers that forward to a single call
- `return new(...)` factory methods

Annotating a 1–4 statement body is redundant, but acceptable on hot-path structs for explicitness.

Do **not** apply when:

- the body has more than 9 statements
- the method contains loops, deep branching, `async`/`await`, or `try`/`catch`/`finally`
- the method is on a cold path

### `[MethodImpl(NoInlining)]` — cold paths

Cold code must opt out of inlining. Otherwise the JIT may inline it into hot callers, bloating the call site and polluting the instruction cache.

Always annotate:

- **Unconditional throw helpers** — methods that always throw
- **`CreateException` local functions** — inside conditional guards
- **Logging methods** — string formatting and I/O are never hot
- **Error and fallback paths** — anything that only runs when things go wrong

`[Conditional("DEBUG")]` methods compile away entirely in release — they need no annotation.

```csharp
// Unconditional throw helper — always throws, annotate the method directly
[MethodImpl(MethodImplOptions.NoInlining)]
[HideInCallstack, StackTraceHidden, DoesNotReturn]
private static void ThrowArgumentNullException(string paramName)
    => throw new ArgumentNullException(paramName);

// Logging — cold path, never inline
[MethodImpl(MethodImplOptions.NoInlining)]
private static void LogWarningInvalidUserId(ILogger logger)
{
    logger.LogWarning("User id is invalid.");
}

// Debug-only — [Conditional] compiles it away; no annotation needed
[Conditional("DEBUG")]
private static void AssertIndexInRange(int index, int length)
{
    Debug.Assert((uint)index < (uint)length);
}
```

## 12. Async, Logging & Conditional Compilation

### Async

- Async method names end with `Async`.
- `CancellationToken token = default` is always the last parameter. Pass the token to every async call.
- Check the token at the start of significant work: `if (token.IsCancellationRequested) { return ...; }`.
- Always re-throw `OperationCanceledException`. Never swallow it.
- Async code supports both UniTask and Unity `Awaitable` through the `UNITASK` define and the `UnityTask` alias (see Section 3).

  ```csharp
  public static async Awaitable WaitUntilAsync(
        this MonoBehaviour target
      , Func<bool> predicate
      , CancellationToken token = default
  )
  ```

- `IDisposable` classes use the `Dispose(bool disposing)` + `GC.SuppressFinalize(this)` pattern.

### Logging

- Log through `EncosyTower.Logging` (`StaticDevLogger`, `DevLogger`, `Logger`, ...). Never call `UnityEngine.Debug.Log*` directly.
- Log wrappers carry `[HideInCallstack, StackTraceHidden]` to stay out of user stack traces:

  ```csharp
  [HideInCallstack, StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void LogInfo(object message)
  {
      StaticDevLogger.LogInfo(message);
  }
  ```

- Dev-only logging goes through the `Dev` loggers — they compile away in release builds.
- Standalone logging methods are cold paths — annotate with `[MethodImpl(NoInlining)]` (see Section 11).

### Conditional compilation

- Guard async or platform-specific code at the file level (`#if UNITASK || UNITY_6000_0_OR_NEWER`).
- Define internal compile symbols at the top of the files that use them (see Section 6).
- Prefer `[Conditional("SYMBOL")]` on methods over wrapping every call site in `#if`.
- Platform-specific type aliases go at namespace scope.

## 13. Unsafe Code & Native Containers

- Every `unsafe` block gets a `// SAFETY:` comment directly above it. The comment explains why this specific operation is sound — not a generic sentence pasted everywhere.

  ```csharp
  // ❌ SHOULDN'T — inline block, trailing comment, generic copy-pasted text
  public void Add(T item) { CheckResizeWrite(); // SAFETY: The checked owner, allocator, or caller contract...
      unsafe { m_Data->Add(item); } }

  // ✅ SHOULD
  public void Add(T item)
  {
      CheckResizeWrite();

      // SAFETY: CheckResizeWrite validates the live header before the write.
      unsafe
      {
          m_Data->Add(item);
      }
  }
  ```

- `unsafe` blocks are always multi-line, brace on its own line, indented to match the surrounding code. Never one-liners.
- Method body order: safety check first, then the SAFETY comment, then the `unsafe` block.

  ```csharp
  public readonly int Count
  {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get
      {
  #if ENABLE_UNITY_COLLECTIONS_CHECKS
          AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
  #endif
          // SAFETY: The read check above validates the live header before reading its count.
          unsafe
          {
              return m_Data->Count;
          }
      }
  }
  ```

- Native wrapper structs carry these attributes and fields:

  ```csharp
  [StructLayout(LayoutKind.Sequential)]
  [NativeContainer]
  public partial struct ListNative<T> : ...
      where T : unmanaged
  {
  #pragma warning disable IDE1006 // Naming Styles
      [NativeDisableUnsafePtrRestriction]
      internal unsafe ListUnsafe<T>* m_Data;

  #if ENABLE_UNITY_COLLECTIONS_CHECKS
      internal AtomicSafetyHandle m_Safety;

  #if UNITY_BURST
      private static readonly Unity.Burst.SharedStatic<int> s_SafetyId
          = Unity.Burst.SharedStatic<int>.GetOrCreate<ListNative<T>>();
  #else
      private static int s_SafetyId;
  #endif
  #endif
  #pragma warning restore IDE1006 // Naming Styles
  ```

## 14. Quick Checklist

Formatting & style:

- [ ] 4-space indent, LF endings, final newline, one statement per line.
- [ ] Braces on their own line; braces on ALL control blocks, no exceptions.
- [ ] Blank line around every block-scoped statement.
- [ ] Line length: aim for 100, never exceed 120 (comments included).
- [ ] No column alignment; one space before `=`.
- [ ] Spaces after commas, around operators, in `where T : unmanaged`; each `where` on its own line.
- [ ] Method/property `=>` on a new indented line; inline accessor/lambda/switch-arm arrows are fine.
- [ ] Long parameter and argument lists wrap with leading commas, `)` on its own line.
- [ ] Field groups ordered `const` → `static readonly` → `static` → instance, no blank lines inside a group.

Naming & files:

- [ ] Naming table respected; consts and `goto` labels ALL_UPPER; native fields `m_PascalCase` with IDE1006 pragmas.
- [ ] One primary type per file; partials in `TypeName+Part.cs`; generics in `` TypeName`N.cs ``; one extension class per file.
- [ ] `Encosy<Type>Extensions` for extensions on BCL/Unity types.
- [ ] Validation define block at the top of files that use checks; feature defines above it, wrapping the whole file.
- [ ] Editor-only code in `Editor*` folders, `EncosyTower.Editor.*` namespace, `[ApiForEditor]`.
- [ ] Usings outside namespace, grouped `System*` → `EncosyTower*` → `Unity*`; block-scoped namespaces.

Code style:

- [ ] `== false` instead of `!` in conditions.
- [ ] No empty control blocks; flatten nested ifs; `goto FAILED` early-exit in parse/validate methods.
- [ ] `for` with cached count over `foreach`/LINQ where an indexer exists; `static` lambdas.
- [ ] Member ordering per Section 7 (Unity types: serialized fields first).
- [ ] No new comments or XML docs unless the Project Owner asks (SAFETY, license headers, `#region` exempt); never touch existing comments.

Types & APIs:

- [ ] `readonly struct` for small values; `[WrapRecord] readonly partial record struct` for ids/handles; capability interfaces over ad-hoc members.
- [ ] `IEquatable<T>` pattern: Ordinal string compares, `HashCode.Combine`, operators delegate to `Equals`.
- [ ] `T item` / `in T item` overload pairs; `readonly` on non-mutating struct methods; `in` for readonly struct params on hot paths.
- [ ] No public API changes for style alone.
- [ ] Failures return `Option<T>` or `bool TryXxx(...)`, never `null`, never throw for expected failures.

Errors, performance & the rest:

- [ ] Validation via dedicated `ThrowIfXxx` / `ThrowXxx` helpers with `[HideInCallstack, StackTraceHidden]`, `[DoesNotReturnIf]`/`[DoesNotReturn]`, the applicable `ValidationDefines` `Conditional` set for dev-only, and a `NoInlining` local `CreateException`. No inline `ThrowHelper.ThrowIfFalse`, no generic `Validate` helpers.
- [ ] `Try*` methods log via `LogError_Xxx` helpers and return `false`.
- [ ] `[MethodImpl(AggressiveInlining)]` per Section 11; cold paths (`ThrowXxx`, `CreateException`, logging) get `NoInlining`.
- [ ] Async methods end with `Async`; `CancellationToken token = default` last; never swallow `OperationCanceledException`.
- [ ] Logging via `EncosyTower.Logging`, never `UnityEngine.Debug` directly; wrappers get `[HideInCallstack, StackTraceHidden]`.
- [ ] `// SAFETY:` comment above every `unsafe` block; block multi-line; comment specific; check → SAFETY → `unsafe` order.

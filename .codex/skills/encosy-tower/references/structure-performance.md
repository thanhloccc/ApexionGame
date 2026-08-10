# Structure, naming, collections, and math

## Structure

- Make assembly name, `rootNamespace`, and folder name match.
- Put runtime, editor, authoring, and test surfaces in sibling dotted assemblies such as `X`, `X.Editor`, `X.Authoring`, and `X.Tests`.
- Use one top-level module folder per concept. Keep small modules flat.
- Prefer established folder names: `Contracts`, `Annotations`, `Internals`, `SourceGen`, `Extensions`, `APIs`, `Common`, `Converters`, `Generators`, `Components`, `Storage`, `Debugging`, `Views`, and `StyleSheets`.
- Do not introduce `Interfaces`, `Utils`, `Misc`, or `Scripts` as generic buckets.
- Folders organize code; they do not automatically create namespace segments. Keep a module's public surface in one flat namespace. Reserve segments for opt-in surfaces such as `Internals`, `Unsafe`, `Extensions`, `SourceGen`, `Editor`, `Caches`, `Generators`, and `Debugging`.
- Use `Type.cs`, `Type+Aspect.cs`, ``Type`N.cs``, `Type_Async.cs`, and `*.gen.cs` consistently. Keep one primary type and one extension class per file.

Follow `CODING-CONVENTIONS.md`, including block-scoped production namespaces, using ordering, private `_camelCase`, private static `s_camelCase`, `ALL_UPPER` constants, braces on control blocks, and prescribed member ordering.

## Collections

Choose by usage rather than habit:

- Prefer `FasterList<T>`, `ArrayMap<K,V>`, and `ArraySet<T>` for hot managed paths.
- Prefer struct read-only views over `IReadOnly*` when boxing or copies matter.
- Rent short-lived hot-path collections from the matching EncosyTower pool.
- Use `Shared*` when managed-built data crosses to jobs without copying.
- Use Unity native collections when a consumer requires them; use EncosyTower `*Native` for its layout or allocator APIs.
- Use unsafe containers only for a measured, documented reason. Dispose every native owner correctly.
- Keep plain BCL collections for serialization, startup/UI work, tiny fixed sets, and honest public contracts.

## Math

Use `Unity.Mathematics` for per-frame, per-entity, Burst, and job work. Keep UnityEngine math types at Transform, physics, UI, animation, and serialization boundaries. Prefer squared-distance comparisons and safe normalization where appropriate; optimize branches or reciprocal square roots only when the hot path benefits.

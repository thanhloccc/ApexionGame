---
name: prefer-high-performance-collections-and-math
description: Default to EncosyTower.Collections / Unity.Collections and Unity.Mathematics — BCL collections and Mathf are the deliberate fallback, not the starting point
metadata:
  type: feedback
---

On 2026-08-07 the user added a standing requirement: **prioritise the high-performance collections
from `EncosyTower.Collections` and `Unity.Collections`, and use `Unity.Mathematics` for math.**

Concretely:
- `FasterList<T>` over `List<T>`; `ListFast<T>` when a foreign API demands a real `List<T>`.
- `ArrayMap<K,V>` / `ArraySet<T>` over `Dictionary<K,V>` / `HashSet<T>` — contiguous values, so
  they iterate as an array with no enumerator.
- `<Type>.ReadOnly` / `DictionaryReadOnly` / `HashSetReadOnly` over `IReadOnly*` interface params.
- `FasterListPool<T>` / `ArrayMapPool` / `QueuePool` / `StackPool` / `StringBuilderPool` for
  short-lived collections in hot paths.
- The `Shared*` family (`SharedArray<T>`, `SharedList<T>`, `SharedArrayMap<K,V>`, …) when data
  built in managed code is consumed by a job — one allocation, managed and native views, no copy.
- `Unity.Collections` types when the consumer dictates them; EncosyTower's `ListNative<T>`,
  `ArrayMapNative<K,V>`, `QueueNative<T>`, `StackNative<T>`, `ReferenceNative<T>` when the
  contiguous layout or `AllocatorStrategy` is wanted. `Collections.Unsafe` only with a measured,
  commented reason.
- `Unity.Mathematics` (`float2/3/4`, `int2/3/4`, `quaternion`, `half`, `math.*`) for anything
  per-frame, per-entity, in a job, or `[BurstCompile]`d — `Mathf`/`Vector3`/`Quaternion` only at the
  Unity API boundary. `math.lengthsq`/`distancesq` when comparing, `math.saturate` for `Clamp01`,
  `math.rsqrt` for `1/sqrt`, `math.select` instead of a branch in a Burst inner loop.

**Why:** this is a real production game project and the package was adopted for its
allocation-conscious data structures; reaching for BCL types by habit throws that away.

**How to apply:** this is a default for every file, not a later optimisation pass — pick the fast
type when first declaring the collection. Plain `List<T>` / `Dictionary<K,V>` / `Mathf` stay correct
for startup code, small fixed sets, serialized fields, and public contracts where `IReadOnlyList<T>`
is the honest type; use them there without wrapping. Selection tables and safety rules for the
native/shared views: `.claude/skills/encosy-tower/references/collections-and-math.md`. Related:
[[review-encosy-before-implementing]], [[encosy-tower-is-standard-library]].

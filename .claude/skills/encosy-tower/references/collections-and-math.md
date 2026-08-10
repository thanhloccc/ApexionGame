# Picking collections and math

Default to the high-performance types. BCL `List<T>` / `Dictionary<K,V>` / `HashSet<T>` and
`Mathf` / `Vector3` are the fallback, not the starting point.

---

## 1. Managed collections — `EncosyTower.Collections`

| Instead of | Use | Why |
|---|---|---|
| `List<T>` | **`FasterList<T>`** | class; exposes the backing array via `AsSpan()` / `AsReadOnlySpan()` / `ToArray()`; validation is `[Conditional]`-stripped in release; has a `.ReadOnly` view struct |
| a `List<T>` you must keep (foreign API) | **`ListFast<T>`** | `readonly struct` wrapping an existing `List<T>` through `ListExposed<T>` — span access and no enumerator allocation without giving up the `List<T>` the other API needs |
| `Dictionary<K,V>` | **`ArrayMap<K,V>`** | values stored contiguously, so you can iterate values as an array with no enumerator; other ops on par with `Dictionary`. Growing is slower (two arrays resize). Not thread-safe, `IDisposable` |
| `HashSet<T>` | **`ArraySet<T>`** | same layout as `ArrayMap` |
| `IReadOnlyDictionary` / `IReadOnlyCollection` params | **`DictionaryReadOnly<K,V>`, `HashSetReadOnly<T>`, `<Type>.ReadOnly`** | struct views — no interface boxing, no defensive copy |
| a list whose storage lives elsewhere | **`ListProxy<TProvider,TBuffer,T>`** | list API over any `IBufferProvider<TBuffer,T>` — buffer, count and version stay owned by the provider |
| `Stack<T>` / `Queue<T>` | `SharedStack<T>` / `SharedQueue<T>`, or the `*Native` forms | see the dual-view and native tables below |

Every one of these has matching extension sets in `Collections.Extensions` and
`Collections.Extensions.Unsafe`, and most implement the `Collections.Contracts` interfaces
(`IClearable`, `IAsSpan`, `ITryGetValue`, `IResizable`, `IHasCapacity`, …) — implement those on your
own containers and the generic extensions apply for free.

**Allocation discipline:** for short-lived collections in a hot path, rent instead of allocating —
`FasterListPool<T>`, `ArrayMapPool<K,V>`, `QueuePool<T>`, `StackPool<T>`, `StringBuilderPool`
(`EncosyTower.Pooling`).

---

## 2. Managed ↔ native without a copy — the `Shared*` family

This is the reason to prefer EncosyTower over raw `Unity.Collections` when data crosses the
managed/job boundary. One allocation, two views.

| Type | Managed view | Native view |
|---|---|---|
| `SharedArray<T>` / `SharedArray<T,TNative>` | `T[]` | `NativeArray<TNative>` |
| `SharedList<T>` / `SharedList<T,TNative>` | `IList<T>` | `.AsNative()` → `SharedListNative<TNative>` |
| `SharedArrayMap<K,V>` | `ArrayMap`-style | `SharedArrayMapNative<K,V>` |
| `SharedStack<T>` / `SharedQueue<T>` | managed | `SharedStackNative<T>` / `SharedQueueNative<T>` |
| `SharedReference<T>` | single value | native reference |

Use when you build/mutate data in managed code and then schedule a job over it — no
`CopyTo(NativeArray)` round trip.

**Safety:** a native view borrows the shared allocation and **must not outlive the owner**; the
native view's capacity is immutable. Dispose the owner, not the view.

---

## 3. Native collections — Unity's vs EncosyTower's

Reach for **`Unity.Collections`** when the consumer dictates the type:

`NativeArray<T>`, `NativeList<T>`, `NativeSlice<T>`, `NativeHashMap<K,V>`,
`NativeParallelHashMap<K,V>` / `NativeParallelMultiHashMap<K,V>`, `NativeQueue<T>`,
`NativeReference<T>`, `NativeBitArray`, `FixedString32Bytes`…`FixedString4096Bytes`, `NativeText`,
`TransformAccessArray`, `UnsafeList<T>`, `Allocator.Temp/TempJob/Persistent`.

Reach for **`EncosyTower.Collections`** natives when you want the contiguous-values layout, the
shared-allocation trick, or an API the Unity type lacks:

`ListNative<T>`, `QueueNative<T>`, `StackNative<T>`, `ArrayMapNative<K,V>`, `ArraySetNative<T>`,
`ReferenceNative<T>`, `SharedListNative<T>`, `SharedArrayMapNative<K,V>`, `NativeSliceReadOnly<T>`.
These are safety-checked wrappers (one `AtomicSafetyHandle` check, then forward) over the unsafe
tier and take an `AllocatorStrategy` rather than a raw `Allocator`.

Unsafe tier for the innermost loops, when the safety handle itself is the cost —
`EncosyTower.Collections.Unsafe`: `ListUnsafe<T>`, `ArrayUnsafe<T>`, `ArrayMapUnsafe<K,V>`,
`ArraySetUnsafe<T>`, `QueueUnsafe<T>`, `StackUnsafe<T>`, `ReferenceUnsafe<T>`,
`SharedListUnsafe`, `SharedArrayMapUnsafe`, `SharedStackUnsafe`, `SharedQueueUnsafe`, plus
`EncosyMemoryAPI` / `EncosyCollectionSafetyAPI`. Only step down a tier with a measured reason, and
say so in a comment.

Every native/unsafe container must be disposed. `ArrayMapNative` also offers
`Dispose(JobHandle) → JobHandle` for deferred disposal.

Common bulk operations already exist as jobs in `EncosyTower.Jobs` — `ListJobs`, `HashMapJobs`,
`HashSetJobs`, `QueueJobs`, `BitArrayJobs`, `ArrayMapNativeJobs`,
`EncosyIJobParallelForTransformExtensions`. Check there before writing a one-off `IJob`.

---

## 4. Math — `Unity.Mathematics`

Use `float2/3/4`, `int2/3/4`, `uint2/3/4`, `bool2/3/4`, `quaternion`, `float3x3`, `float4x4`,
`half` / `half2/3/4`, `Random`, and the `math.*` free functions.

`Mathf.*`, `Vector2/3/4`, `Quaternion`, `Matrix4x4` stay at the **Unity API boundary only** —
`Transform`, physics, UI, animation, serialized inspector fields. Everything that runs per-frame
per-entity, inside a job, or under `[BurstCompile]` uses `Unity.Mathematics`.

| Instead of | Use |
|---|---|
| `Mathf.Min/Max/Clamp/Abs/Sign/Lerp/Pow/Sqrt` | `math.min/max/clamp/abs/sign/lerp/pow/sqrt` |
| `Mathf.Clamp01(x)` | `math.saturate(x)` |
| `Vector3.Dot/Cross/Normalize` | `math.dot/cross/normalize` (or `normalizesafe`) |
| `v.magnitude` / `Vector3.Distance` | `math.length(v)` / `math.distance(a,b)` |
| comparing magnitudes | `math.lengthsq` / `math.distancesq` — skips the `sqrt` |
| `1f / Mathf.Sqrt(x)` | `math.rsqrt(x)` |
| `if (cond) a else b` in a Burst inner loop | `math.select(a, b, cond)` — branchless |
| `Quaternion` in job/component data | `quaternion` (+ `math.mul`, `math.slerp`, `quaternion.LookRotationSafe`) |
| `float` fields in a large array where precision allows | `half` / `half2/3/4` — halves memory and cache pressure |

The package itself models this: `math.max`, `math.min`, `math.clamp`, `math.select` and
`math.half*` throughout `Pooling.Native`, `SceneObjectPoolBehaviour`, `ByteBools.gen.cs` and the
stats system.

Pair with `[BurstCompile]` and `Unity.Burst.CompilerServices` hints
(`EncosyTower.Core/Unity.Burst.CompilerServices`, plus `Checks.BurstAssumePositive` in
`EncosyTower.Debugging`) when a job is measurably hot.

---

## 5. When the plain BCL type is the right answer

Not everything needs this. Keep `List<T>` / `Dictionary<K,V>` / `Mathf` / `Vector3` when:

- the code runs once at startup or on a UI click, and the collection holds a handful of items;
- a Unity or third-party API demands the exact type and `ListFast<T>` cannot bridge it;
- the type crosses a public API boundary where `IReadOnlyList<T>` is the honest contract;
- serialization requires it (`[SerializeField] List<T>`, Newtonsoft round-trips).

In those cases just use the BCL type — do not wrap it for appearances. The rule is
"performance-critical paths use the fast types", not "never write `List<T>`".

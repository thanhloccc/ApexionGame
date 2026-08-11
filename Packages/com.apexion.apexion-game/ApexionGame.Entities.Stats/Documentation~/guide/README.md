# Guide

User documentation for `ApexionGame.Entities.Stats`.

*[Tiếng Việt](vi/README.md) · [Package README](../../README.md) · [Design docs](../README.md)*

## Reading order

| # | Page | Answers |
|---|---|---|
| 1 | [Getting started](01-GETTING-STARTED.md) | What do I install, and what is the smallest thing that works? |
| 2 | [Core concepts](02-CONCEPTS.md) | What is a stat here, and how does a write reach the stats that depend on it? |
| 3 | [Declaring stats](03-DECLARING-STATS.md) | What do the three attributes generate, and which hooks must I write myself? |
| 4 | [Using stats](04-USING-STATS.md) | Create owners, read, write, add and remove modifiers, consume events, destroy owners. |
| 5 | [Persistence](05-PERSISTENCE.md) | Save, load, and the remap pass that keeps cross-owner links alive. |
| 6 | [Jobs & performance](06-JOBS-AND-PERFORMANCE.md) | What is thread-safe, how to schedule, what it costs. |
| 7 | [Tooling](07-TOOLING.md) | Stat Debugger, Quick Actions, authoring assets, regenerating the type tables. |
| 8 | [API reference](08-API-REFERENCE.md) | Type-by-type surface, hand-written and generated, plus diagnostics. |
| 9 | [Pitfalls & FAQ](09-PITFALLS.md) | The mistakes that fail silently. |

Short on time: read [Core concepts](02-CONCEPTS.md), then run
[the sample](../../../ApexionGame.Entities.Stats.Samples/README.md) and click through its ten cases.
Between them they cover most of what the other pages spell out.

## If you are coming from upstream

The API mirrors [`EncosyTower.Entities.Stats`][encosy] and
[Trove Stats][trove] closely enough that most code ports by changing type names. What is genuinely
different:

| Change | Where it bites |
|---|---|
| `Entity` → `StatOwnerHandle`, `DynamicBuffer<T>` → `StatBuffer<T>` | signatures, everywhere |
| `StatBaker` → `StatBuilder`; no baking pipeline | owner creation |
| `IStatModifier` gained `RemapObservedStats` | modifiers written for upstream will not compile until you add the hook |
| Propagation has no visited-set | nothing to change, but see [DEC-005](../07-DECISIONS.md#dec-005) |
| Batch APIs take an `Allocator` (default `Temp`) | optional argument at the end |
| `Accessor.ReadOnly` dropped `ComponentLookup<StatOwner>` | it only existed to register an ECS read dependency |

The full list — 23 divergences, each with its reasoning — is in the
[decision log](../07-DECISIONS.md).

[trove]: https://github.com/PhilSA/Trove
[encosy]: https://github.com/laicasaane/EncosyTower

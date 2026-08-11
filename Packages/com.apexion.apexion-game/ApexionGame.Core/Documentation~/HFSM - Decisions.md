# HFSM — Decisions

*[Tiếng Việt](HFSM%20-%20Decisions.vi.md) · [Index](README.md)*

One row per choice: what was picked, what was rejected, and why. `chốt` = settled, `open` = still
worth arguing about during review.

| id | Topic | Status |
|---|---|---|
| [DEC-001](#dec-001) | Namespace is `ApexionGame.HFSM`, not `ApexionGame.Core.HFSM` | chốt (user) |
| [DEC-002](#dec-002) | Class states + fluent builder, not a source generator | chốt (user) |
| [DEC-003](#dec-003) | `TContext : class` | chốt |
| [DEC-004](#dec-004) | Two guard delegate shapes; `GuardInfo` passed by value | chốt |
| [DEC-005](#dec-005) | Explicit `EndComposite()` rather than a nested lambda | chốt |
| [DEC-006](#dec-006) | `TriggerId` = `TypeId` + ordinal | chốt |
| [DEC-007](#dec-007) | Unmatched triggers are dropped, not retained | chốt |
| [DEC-008](#dec-008) | State data in a `byte[]` blob via `UnsafeUtility.As`, not `NativeArray` | chốt |
| [DEC-009](#dec-009) | `StateInfo` carries `NodeIndex`, not `TState` | chốt |
| [DEC-010](#dec-010) | `CurrentState` returns region 0's leaf | chốt |
| [DEC-011](#dec-011) | `ApexionGame.Core` does not reference `EncosyTower.Core.Extended` | chốt |
| [DEC-012](#dec-012) | One transition per tick per region | chốt |
| [DEC-013](#dec-013) | Authoring guards resolve through a catalog, not serialized `ScriptableObject` guards | **open** |
| [DEC-014](#dec-014) | Dense `int[]` for active/history, not `ArrayMap` | chốt |
| [DEC-015](#dec-015) | `[CallerArgumentExpression]` for guard text, accepting the release-build string cost | chốt |
| [DEC-016](#dec-016) | Debug registration is automatic, unlike Stats' voluntary registration | chốt |
| [DEC-017](#dec-017) | Guard value display is a build-time heuristic, not expression-tree parsing | chốt |
| [DEC-018](#dec-018) | No `PubSub` broadcast of state changes by default | chốt |
| [DEC-019](#dec-019) | Type names are unabbreviated; `HierarchicalStateMachine` + `Machine*` + bare | chốt (user) |

---

## DEC-001

**Namespace is `ApexionGame.HFSM`, not `ApexionGame.Core.HFSM`.** — *chốt, confirmed by the user*

EncosyTower's assembly is `EncosyTower.Core`, but its modules are `EncosyTower.PubSub`,
`EncosyTower.Collections`, `EncosyTower.Pooling` — the `.Core` segment names an assembly, never a
namespace prefix. Mirroring that gives consumers `using ApexionGame.HFSM;`.

Rejected: `ApexionGame.Core.HFSM`, which is more literally predictable from the folder path but
breaks the convention the rest of the codebase follows, and makes every consumer `using` longer for
no benefit.

Rejected: `ApexionGame.StateMachines` with the folder renamed to match, which would have removed the
acronym everywhere. The user kept `HFSM/` — the acronym stays where it reads as an acronym, and
disappears from identifiers ([DEC-019](#dec-019)).

## DEC-002

**States are classes wired by a fluent builder; no source generator.** — *chốt, chosen by the user*

Rejected: `[StateMachine]` partial structs with generated switch dispatch. It would be faster and
Burst-capable, but it drags in a Roslyn generator project of the same order as the Stats one
(~5–10k LOC) and makes stepping through a transition in a debugger much harder. The stated workload
(a few hundred agents) does not need what that buys.

Rejected: delegate-only configuration with no subclassing. Least code to write, but the logic of
fifteen states ends up inside one enormous method, breakpoints land in lambdas, and captured
closures are an easy source of accidental shared state.

## DEC-003

**`TContext` is constrained to `class`.** — *chốt*

A blackboard is written to from inside `OnUpdate`. With a `class`, `OnUpdate(TContext ctx, …)` lets
a behaviour mutate fields naturally. With an unconstrained `TContext` the signature would have to be
`ref TContext`, which for a reference type means a behaviour could reassign the machine's context —
a footgun for no gain.

Cost: a struct context is impossible, so a context is always one allocation per agent. At a few
hundred agents that is a few hundred long-lived objects, which is not a problem the design needs to
solve.

## DEC-004

**Two guard delegate shapes, and `GuardInfo` is passed by value.** — *chốt*

`.When(static c => c.Health < 20f)` is the common case and must stay one short line.
`.When(static (c, i) => i.TimeInState > 5f)` covers the rest. Two overloads, discriminated by a flag
on the transition, so the one-argument form pays nothing extra at call time.

`GuardInfo` is a 24-byte `readonly struct` passed **by value**, not `in`. With `in`, a lambda
would need explicit parameter types — `static (EnemyContext c, in GuardInfo i) => …` — and the
terseness that makes the builder readable would be gone. 24 bytes copied per guard evaluation is
cheaper than the readability loss.

## DEC-005

**Scopes are closed with an explicit `EndComposite()` / `EndParallel()`.** — *chốt*

Rejected: a nested-builder lambda, `.Composite(Combat, c => c.Child<…>()…)`, which makes imbalance
impossible at write time. It reads well but nests the declaration two levels deep for every
composite, and a deeply hierarchical machine becomes a pyramid.

The imbalance risk is handled by validation instead: `MachineError.UnbalancedScope` names the node
and the depth, and there is a test for it. A build-time error message is an acceptable substitute for a
compile-time impossibility here, because `Build()` runs at startup, not in the field.

## DEC-006

**`TriggerId` is `TypeId<TTrigger>` plus the ordinal.** — *chốt*

`.On(EnemyTrigger.AttackFinished)` converts the enum member to an int. If the id were just that int,
`EnemyTrigger.Staggered` and `BossTrigger.Staggered` — both ordinal 0 — would be the same trigger,
and a boss's stun would fire an enemy's transition. Pairing with `TypeId` from `EncosyTower.Types`
makes them distinct for 8 bytes.

Rejected: a third generic parameter `HierarchicalStateMachine<TContext, TState, TTrigger>`. It
fixes the collision at compile time but forces every signature, every field and every user declaration to carry a third
type argument, and prevents one shared trigger enum across machines.

## DEC-007

**A queued trigger that matches nothing in the current configuration is dropped.** — *chốt*

Rejected: keeping it queued until something handles it. That means a `Hit` fired while the machine
was in a state that ignores hits resurfaces minutes later, the moment the machine reaches a state
that handles it. That class of bug is very hard to trace back to its cause.

The drop is recorded in the transition log flagged `unmatched` and logged at Warning level under the
debug define, so it is visible rather than silent — which is the part that actually matters.

## DEC-008

**Per-state data lives in a managed `byte[]` blob, accessed with `UnsafeUtility.As`.** — *chốt*

`UnsafeUtility.As<byte, TData>(ref blob[offset])` returns a **GC-tracked managed reference**. The
array is never pinned, the collector may still move it, and there is no `GCHandle` and no `unsafe`
block on the tick path.

Rejected: `NativeArray<byte>` per instance. It would give a stable pointer this design never needs,
in exchange for a `Dispose` obligation on every agent and one native allocation per spawn.

Rejected: `byte[]` pinned with a `GCHandle`. 500 pinned arrays fragment the managed heap for no
benefit over the `As` approach.

Constraint this creates: offsets must be aligned to `UnsafeUtility.AlignOf<TData>()` at build time.
An unaligned `ref` to a `double` is undefined behaviour on some platforms, so alignment is baked in
`Build()` and covered by `MachineStateDataTests`.

## DEC-009

**`StateInfo` carries `NodeIndex`, not `TState`.** — *chốt*

`StateBehaviour<TContext>` is generic over the context only, so one behaviour class can be reused
across machines with different state enums. If `StateInfo` carried `TState`, the behaviour base
would need a third type parameter and that reuse would disappear.

Cost: reading a state name from inside a behaviour needs `machine.StateOf(info.Node)` rather than a
direct field. Behaviours rarely need it — they know which state they are.

## DEC-010

**`CurrentState` returns the deepest leaf of region 0.** — *chốt*

With parallel regions there is no single current state, and a property that must return one has to
pick. Region 0 is the first declared region, which is the one people think of as "the main one"
(movement, in the usual layout).

Rejected: throwing when the machine has parallel regions. `CurrentState` is used constantly in
non-parallel machines and in logging; making it conditional on machine shape would be hostile.

`GetActiveStates(Span<TState>)` is the honest API for the parallel case, and the doc comment on
`CurrentState` points at it.

## DEC-011

**`ApexionGame.Core` never references `EncosyTower.Core.Extended`.** — *chốt*

The in-game console integration (`IVisualCommand` to toggle the overlay) is genuinely useful, but it
lives in `EncosyTower.Core.Extended`. Referencing that from the runtime assembly would make every
future consumer of `ApexionGame.Core` — including ones that want nothing but the state machine —
depend on it.

The command therefore lives in `ApexionGame.Core.Samples`, where it also serves as the worked
example of how to wire it up in a real project.

## DEC-012

**At most one transition per tick, per region.** — *chốt*

Rejected: looping until no transition is eligible. That makes a single frame unbounded — a
misconfigured pair of guards can ping-pong forever — and turns the transition log into a wall of
entries within one frame.

The cost is that a chain `A → B → C` takes three ticks. Where a genuine immediate chain is needed,
`.Internal()` with a `.Do(action)` does the work without a state change, or the intermediate state
is not actually a state and should be folded into the action.

## DEC-013

**Asset-authored guards resolve through a typed catalog, not serialized `ScriptableObject` guards.** — *open*

A `GuardAsset : ScriptableObject` with `abstract bool Evaluate(object ctx, …)` would be the
obvious Unity-flavoured design, but it boxes the context on every evaluation and puts a virtual call
plus a cast on the tick path — precisely what the rest of the design avoids.

The catalog — `GuardCatalog<TContext>` mapping `StringId → Guard<TContext>`, populated in
code — keeps guards typed and allocation-free while still letting the asset reference them by name.
The tradeoff is that a designer cannot add a *new* guard without a programmer, only rewire existing
ones.

Flagged **open** because it depends on how much authoring autonomy designers are actually meant to
have, which phase 9 is far enough away that it need not be settled now.

## DEC-014

**`_activeChild` and `_historyChild` are dense `int[]`, not `ArrayMap<int,int>`.** — *chốt*

Only composites need an entry, so a sparse map looks like the right shape. At 20–40 nodes it is not:
`ArrayMap`'s node array plus bucket array is larger than a flat `int[]` of the same node count, and
it puts a hash and a probe on the transition path.

The rule this follows is the real one from the performance defaults — `ArrayMap` over `Dictionary`
when a map is what you need — not "always reach for the fanciest container".

## DEC-015

**Guard source text comes from `[CallerArgumentExpression]`, and the string literals stay in release builds.** — *chốt*

`[CallerArgumentExpression(nameof(guard))]` makes the compiler pass the literal source text of the
lambda. The guard pane then shows `c.Health < 20f` with the user never naming anything — the single
highest-value debug feature for the least user effort.

The cost is that those string literals are baked into the call site and remain in a release build
even when `APEXION_HFSM_DEBUG` is off. A parameter's presence cannot be `#if`'d away without changing
the signature. For a project with a few dozen machines this is a few hundred bytes; the alternative
— requiring `.When(guard, "SeesPlayer")` everywhere — trades a real ergonomic cost for a negligible
size win.

The *storage* is still gated: `_guardMeta` is only allocated under the define, so nothing is retained
at runtime.

## DEC-016

**Machines register with the debug registry automatically; Stats' stores register voluntarily.** — *chốt*

The Stats module made registration opt-in for a good reason: a `StatStore` is native memory, an
adapter object has to be constructed to erase its type parameters, and a registration outliving the
store is a dangling pointer.

None of that holds here. `HierarchicalStateMachine<TContext, TState>` implements `IMachineDebug`
itself, so there is no adapter; it is a managed object, so a stale registration is a leak rather than a crash; and the
registration call is `[Conditional]`, so a release build does not make it at all.

Automatic registration is what makes the debugger work with zero user code, which was the point.

## DEC-017

**The guard pane's value display is a build-time pattern match, not expression-tree analysis.** — *chốt*

Showing `Health = 74.0` next to `c.Health < 20f` requires knowing which context field the guard
reads. Doing that properly means `Expression<Func<…>>` instead of a plain delegate — which allocates,
compiles slowly, and cannot be used in IL2CPP without care.

Instead, the source text captured in DEC-015 is matched once at build time against
`c.<Field> <op> <literal>`. When it matches, the field is resolved by reflection **once** and cached
as a getter delegate. When it does not, the row shows the expression and the boolean result.

A heuristic that covers the common case and degrades to something still useful beats a complete
solution that costs the tick path.

## DEC-018

**State changes are not broadcast through PubSub by default.** — *chốt*

`HierarchicalStateMachine.StateChanged` is a plain `Action<TState, TState>`. Publishing every
transition through `GlobalMessenger` would mean 500 agents × N transitions crossing a global dispatch every second,
for information almost nobody is listening to.

Where a decoupled listener genuinely is wanted — an achievement system reacting to "boss entered
phase 2" — the owner subscribes to `StateChanged` and publishes its own domain message. That is one
line at the one place it matters, rather than a cost paid by every machine.

## DEC-019

**Type names carry no abbreviation. Three tiers: the machine, `Machine*`, and bare concept names.** — *chốt, chosen by the user*

The first draft prefixed everything with `Hfsm` — `HfsmBuilder`, `HfsmNode`, `HfsmError`. Compact,
but an abbreviation the reader has to expand mentally at every single identifier.

The scheme now:

| Tier | Rule | Examples |
|---|---|---|
| The machine | Spelled out | `HierarchicalStateMachine<TContext, TState>` |
| Belongs to a *machine* | `Machine*` | `MachineBuilder`, `MachineDefinition`, `MachineRunner`, `MachineError`, `MachineOptions`, `MachinePhase`, `IMachineControl`, `IMachineTickable`, `IMachineDebug` |
| Everything else | Bare | `StateNode`, `Transition`, `StateInfo`, `GuardInfo`, `NodeIndex`, `TriggerId`, `Guard<TContext>`, `TransitionLog`, `HistoryMode`, `TickMode`, `AsyncPolicy`, `TransitionCause` |

"Hierarchical" appears exactly once, because it describes the machine — not a builder, not a node.

Rejected: expanding the prefix mechanically everywhere, which produces
`HierarchicalStateMachineTransitionBuilder<TContext, TState>` and
`HierarchicalStateMachineTransitionLogEntry`. Consistent, but a field declaration wraps before it
says anything.

Rejected: `StateMachine*` for the middle tier (`StateMachineBuilder`, `StateMachineError`). It reads
well but repeats "State" against `StateNode`, `StateInfo`, `StateBehaviour`, where "state" means the
node rather than the machine — two meanings of one word inside one namespace.

**The cost, accepted knowingly:** bare names collide more easily. `Transition` collides with
`UnityEngine.UIElements.Transition`, which bites in `ApexionGame.Core.Editor` where both `using`s are
needed. The fix is a one-line alias in the affected files, documented in
[Layout §1.2](HFSM%20-%20Layout.md#12-the-collision-this-creates-and-the-fix). `NodeIndex`,
`StateInfo`, `GuardInfo` and `TriggerId` were checked against the referenced Unity and EncosyTower
assemblies and are clear.

**A knock-on fix.** The sample NPC in [API Surface §9](HFSM%20-%20API%20Surface.md#9-full-worked-example)
was called `Guard` (`GuardState`, `GuardContext`, `GuardBrain`). With the guard *delegate* now named
`Guard<TContext>`, that reads as `Guard<GuardContext>` — legal C#, unreadable prose. The sample NPC
is renamed `Sentry`.

**A real bug this rename exposed.** The first draft declared both
`public static class Hfsm<TContext, TState>` (holding `Define`) and
`public sealed class Hfsm<TContext, TState>` (the machine) — the same name and arity twice in one
namespace, which does not compile. `Define` is now a **static member of the machine class**, so
`HierarchicalStateMachine<EnemyContext, EnemyState>.Define("EnemyBrain")` is the entry point and
there is no separate factory type.

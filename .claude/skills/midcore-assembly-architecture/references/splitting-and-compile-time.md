# Splitting, merging, and what an assembly costs

## 1. The cost model

An assembly is not free. Each one adds:

- **a compilation unit** — fixed overhead per assembly, paid on every rebuild that touches it;
- **a node in the rebuild graph** — everything above it recompiles when it changes;
- **a reference list to maintain** — every consumer must be told about it;
- **a boundary that resists change** — moving a type across it is a bigger edit than moving it
  across a folder.

The last one is the point. A boundary is *supposed* to resist change; that is what makes it useful.
It is also why creating one speculatively is a bad trade — you pay the resistance immediately and
collect the benefit only if the guess was right.

## 2. Rebuild shape, not assembly count

The naive optimisation is "fewer assemblies = faster builds". Wrong at both extremes:

- **One giant assembly** — every change rebuilds everything. Worst possible iteration time.
- **Hundreds of tiny ones** — per-assembly overhead dominates, and the dependency graph is so deep
  that a change near the bottom still cascades everywhere.

What actually matters is **how much rebuilds when you touch the thing you touch most often**. The
files you edit hourly should sit high in the graph, with little above them. A gameplay tweak that
rebuilds the whole game means gameplay is sitting too low.

Diagnostic question: *"what do I edit twenty times a day, and how much recompiles when I do?"*
That answer, not the assembly count, tells you whether the split is right.

## 3. When to split

Split when **at least one** is true:

| Trigger | Why it is real |
|---|---|
| A change to one concept forces a rebuild of an unrelated one | The reason the split model exists |
| A gate or optional package applies to part but not all | Cannot express two gates on one assembly |
| Editor / test / authoring code must stay out of the player build | Correctness, not preference |
| One part is genuinely reused across titles | Extraction candidate — see `dependency-rules.md` §6 |
| Two parts have genuinely different rates of change | The stable part stops being rebuilt by the volatile one |
| A boundary you already respect informally keeps getting violated by accident | Make the compiler enforce what review keeps catching |

## 4. When not to split

- **It holds one type.** A file is not an architecture.
- **"For organisation."** That is what folders are for, and folders are free.
- **Speculative reuse.** "We might need this elsewhere" has a poor track record and a real cost now.
- **To shorten a namespace.** Namespaces are a naming concern, decided separately from assemblies in
  most conventions — check the `structure_naming` authority.
- **Because a file got long.** Split the file.
- **To break a cycle.** See `dependency-rules.md` §5 — extraction may be the fix, but "add an
  assembly" is not the same as "extract the right concept".

## 5. Carrying out a split

Order matters; done in the wrong order you spend the middle of the operation unable to compile.

1. **Name the concept you are extracting first.** If it has no name, stop — you are moving files, not
   splitting a module, and you will produce a junk drawer.
2. **Check the direction.** The new assembly must sit at or below the layer of everything that will
   reference it. If it needs something from a consumer, the split is wrong or a cycle is hiding.
3. **Create the assembly with its full reference/gate set before moving any file.** Half-configured
   assemblies produce error storms that hide the real problem. Check the profile's `first_file_rule`
   for anything that must be copied in — omitting it makes guarded code disappear silently rather
   than loudly.
4. **Move files with their metadata.** In engines that track assets by side-car metadata files, a
   move that loses the side-car breaks every reference to that asset. This is not recoverable by
   re-importing.
5. **Move in dependency order** — leaves first, so the tree compiles at each step.
6. **Compile after each group**, not once at the end. The first error in a big move is informative;
   the hundredth is noise.
7. **Re-run the graph audit** (`dependency-rules.md` §4) and confirm no new upward edge appeared.
8. **Run the tests that cover the moved code.** A split that compiles is not a split that works —
   gates and conditional compilation can silently drop code that no longer sees its define.

## 6. Merging two assemblies

Rarer than splitting, and legitimate in exactly two cases:

- **They are one concept and always were.** Nothing ever uses one without the other, and the
  boundary has produced only ceremony.
- **The split was speculative and the reuse never came.**

Merging is *not* the fix for a cycle (`dependency-rules.md` §5) and *not* the fix for slow builds —
it usually makes rebuild shape worse, because the merged unit is now rebuilt by changes to either
half.

Before merging, check: does anything reference one but not the other? If yes, the boundary is load
bearing and merging forces those consumers to take on the whole thing.

## 7. Scale expectations at midcore

Rough calibration, not a rule:

| Assemblies | Typical situation |
|---|---|
| 1–5 | Prototype, or a project that has not hit the problem yet |
| 10–40 | Midcore, healthy — layers are separated, features have boundaries, editor/test code is isolated |
| 100+ | Either a very large team with strong tooling, or a split reflex that has replaced thought |

The number matters far less than whether the **layer invariant holds** and whether the **rebuild
shape** matches editing habits. A 12-assembly project with an upward reference is in worse shape than
a 40-assembly one without.

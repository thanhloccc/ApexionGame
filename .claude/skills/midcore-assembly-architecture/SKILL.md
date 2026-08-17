---
name: midcore-assembly-architecture
description: REQUIRED before creating, splitting, merging or renaming an assembly in a game project with more than a handful of modules. Owns the dependency graph — which layer an assembly belongs to, which direction references may point, how to break a cycle, what an extra assembly costs in compile time, and how to gate a module behind a feature flag. Load it before the first file of a new assembly, whenever a reference is added between two existing assemblies, and when compile times start creeping. This is the most expensive decision to reverse in a codebase, so it is made deliberately and up front rather than discovered later. Triggers on "new assembly", "asmdef", "assembly definition", "circular dependency", "cyclic reference", "compile time", "module", "layer", "boundary", "compile-time gate", "define symbol", "defineConstraints", "refactor into", "split", "extract package", and Vietnamese phrasing "tạo assembly", "tách module", "tách package", "phụ thuộc vòng", "compile chậm", "chia tầng", "để code này ở đâu".
---

# Assembly architecture at scale

Read `.claude/project-profile.md` in the current repo before proposing anything. It pins the
technology, conventions and authority skills this file deliberately does not name.
If the profile is missing, say so and ask — do not guess, and do not assume a specific engine
package or standard library.

An assembly graph is the one structural decision that gets **more** expensive to change over time,
not less. Files move cheaply. Namespaces rename cheaply. An assembly that half the codebase depends
on, pointing the wrong way, is a months-long problem.

## The one rule

> **References point down the layer stack. Never up. Never sideways between peers.**

Everything else in this skill is a consequence of it or a tool for enforcing it.

## The layer model

Five layers. A project may collapse some, but it may not reorder them.

| # | Layer | Holds | May reference |
|---|---|---|---|
| 1 | **Common** | ids, value types, enums, shared contracts, pure helpers | nothing in-project |
| 2 | **Data** | content tables, save schemas, definitions loaded from disk | Common |
| 3 | **Systems** | gameplay logic, simulation, rules — the part worth testing | Common, Data |
| 4 | **Presentation** | UI, view, audio, VFX, input binding | Common, Data, Systems |
| 5 | **Composition** | boot, scene wiring, dependency assembly, the app itself | everything |

Only **Composition** may reference everything. It is the only layer allowed to know the whole graph,
and it should be small — wiring, not logic.

**The load-bearing property:** Systems must be testable without Presentation. If a rules change
cannot be tested without instantiating a view, the layering has already failed.

### Sibling assemblies are not layers

Editor, authoring, and test surfaces are **siblings** of the assembly they serve, not layers of
their own — `X`, `X.Editor`, `X.Authoring`, `X.Tests`. They may reference their runtime assembly;
nothing may reference them. Check the profile: some projects centralise tests into one assembly
rather than one per module, and that is a convention to match, not to fix.

## Before creating an assembly

Answer these in order. Stop at the first "no".

1. **Does an existing assembly already own this concept?** Add the file there. Most new code does not
   need a new assembly, and a needless one is a permanent tax.
2. **Which layer is it?** If the answer is "it depends" or "both", the concept is two concepts —
   split it before creating anything.
3. **Does anything it needs live above it?** Then either the layering is wrong or you have found a
   real cycle. Go to *Breaking a cycle*.
4. **Does it earn its own assembly?** One of: independent lifetime, a different compilation gate, a
   different audience (editor/tests/samples), or it is genuinely optional. "It feels cleaner" and
   "we might reuse it" are not reasons.
5. **Game-specific or reusable?** The profile's `assembly_map.split_rule` decides whether it belongs
   in the game or in a shared package. Getting this wrong is cheap to fix now, expensive after it
   ships.

Then create it with the project's naming and reference conventions — owned by the
`structure_naming` authority skill, not by this one. Check the profile for a `first_file_rule`; many
projects require reference or define blocks that, if omitted, cause guarded code to silently vanish.

## Detecting a violation

Assembly definitions are text. The graph is checkable without opening the editor.

```bash
# every assembly and what it references — read the graph in one pass
for f in $(find . -name "*.asmdef" -not -path "*/Library/*"); do
  echo "── $f"; grep -A20 '"references"' "$f"
done
```

What you are looking for:

| Symptom | Means |
|---|---|
| A Common-layer assembly referencing anything in-project | The layer is not actually Common; something leaked down |
| Two peers referencing each other | A cycle the build will reject, or one that was "solved" by merging them badly |
| Systems referencing Presentation | Logic is reaching into view code; rules are now untestable |
| A `.Tests` or `.Editor` assembly referenced by a runtime assembly | Editor-only code is about to ship, or fail the player build |
| An assembly referenced by almost everything | Either a legitimate Common, or a bag that should be split |

The engine will reject a hard cycle at compile time. It will **not** reject an upward reference that
happens to be acyclic — that one is yours to catch, and it is the common failure.

## Breaking a cycle

A wants B, B wants A. In order of preference:

1. **The dependency is one-directional and you have it backwards.** Most cycles are this. Look at
   which one is more general; that one is lower and must not know the other.
2. **Extract the shared piece downward.** The ids, contracts or value types both need move to a
   lower layer. This is the right fix far more often than the two below.
3. **Invert with a contract.** The lower assembly declares an interface; the higher one implements
   it; Composition wires them. Costs an indirection — spend it deliberately.
4. **Decouple with messages/events.** The lower assembly publishes; the higher subscribes. Genuinely
   removes the reference, but makes control flow harder to follow — reserve for where the
   decoupling is wanted for its own sake, not as a cycle workaround.

**Never** fix a cycle by merging the two assemblies. It makes the cycle invisible instead of absent,
and the merged assembly now has two reasons to change.

## What an assembly costs

Each one adds a compilation unit, a link step, and a node in the dependency graph that must
recompile when anything below it changes. A few dozen is normal at midcore scope; a few hundred
small ones is a self-inflicted wound.

**Split when:** a change to one concept forces a rebuild of an unrelated one · a gate or optional
package applies to part but not all · editor/test code must be kept out of the player build · one
part is genuinely reusable across titles.

**Do not split when:** it holds one type · "for organisation" — that is what folders are for ·
speculative reuse · to make a namespace shorter.

The signal that a split is **overdue**: touching a leaf concept triggers a long rebuild of things
that have nothing to do with it.

## Feature gating

Compiling a module out — for an optional package, a platform, or a feature toggle — is an assembly
concern because the gate lives on the assembly definition.

The failure that eats hours: **a guard silently evaluating false because the define was never
propagated to the new assembly.** The code compiles, does nothing, and reports no error.

- After adding any gated assembly, prove the symbol resolves inside it — a deliberate compile error
  behind the guard is the fastest check.
- Constrain the whole assembly rather than wrapping every file when the module is entirely optional.
- Check the profile's `first_file_rule`: several ecosystems require copying a version-defines block
  into every new assembly, and omitting it is exactly this failure.

## This skill does not own

| Question | Owner |
|---|---|
| Folder layout, namespaces, file naming, member order | `structure_naming` authority (see profile) |
| Which library/module to use for a given feature | `structure_naming` authority |
| How to compile, test, or read the build log | `unity_operations` authority |
| How content tables are organised | `midcore-data-pipeline` |

This skill stops at the assembly boundary and hands off.

## References

- `references/dependency-rules.md` — the layer model in depth, worked placement examples, the full
  cycle-breaking decision tree, and how to audit an existing graph.
- `references/splitting-and-compile-time.md` — what an assembly actually costs, split/merge
  triggers, and how to carry out a split without breaking references.
- `references/feature-flags.md` — gates, define propagation, optional packages, and the silent-guard
  failure and its check.

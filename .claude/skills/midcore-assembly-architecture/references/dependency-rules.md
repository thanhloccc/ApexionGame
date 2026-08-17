# Dependency rules — layers, placement, cycles

## 1. Why layers and not "modules"

A flat set of modules that reference each other freely is a graph with no invariant. You cannot
answer "may A reference B?" without knowing the whole graph, and nobody knows the whole graph after
month six.

Layers give a **local** answer: compare two numbers. That is the entire value — not tidiness, but
that a correct decision can be made without global knowledge.

## 2. The layers, in detail

### 1 — Common

Ids, value types, enums, unit-like wrappers, shared contracts, pure functions. **References nothing
in-project.**

The discipline: Common is where things go because *many layers need them*, not because *nobody knew
where else to put them*. The moment it accumulates "misc helpers", it becomes the assembly everything
depends on and nobody can change.

Test: could this type be understood by someone who knows nothing about the game's rules? If no, it
is probably Systems.

### 2 — Data

Content table definitions, save schemas, anything whose shape is dictated by data on disk.
References Common.

Data holds **shape and access**, not **rules**. "A weapon row has a damage field and a required ammo
id" is Data. "A weapon may only fire if the equipped ammo matches" is Systems. Putting the second in
Data is the most common leak, and it is what makes rules untestable without loading content.

### 3 — Systems

Gameplay logic, simulation, progression, the state machines. References Common and Data.

**This is the layer worth testing**, so it is the layer that must not touch views. If a rule needs to
tell the player something, it produces a result or raises an event — it does not call a UI method.

### 4 — Presentation

UI, views, audio, VFX, camera, input binding. References Common, Data, Systems.

Presentation reads from Systems and sends intent back. It never owns rules. The test: delete
Presentation entirely — Systems should still compile and its tests should still pass.

### 5 — Composition

Boot, scene wiring, service registration, the concrete app. References everything.

Keep it thin. It is the only place allowed to know every assembly, so every line of logic here is
logic that cannot be tested or reused. Wiring only.

### Collapsing layers

A smaller project may merge Data into Common, or Presentation into Composition. That is fine. What
is not fine is **reordering** — Data must never reference Systems, however small the project is,
because that is the edge that later becomes impossible to remove.

## 3. Placement — worked examples

| The thing | Layer | Why |
|---|---|---|
| `WeaponId`, `QuestId` | Common | Every layer names them; they encode no rules |
| "damage type" enum | Common | A vocabulary, not a rule |
| The weapon table's row shape | Data | Shape dictated by content on disk |
| "which ammo fits which weapon" as a **column** | Data | It is authored content |
| "which ammo fits which weapon" as a **check** | Systems | It is a rule that consumes the column |
| Save schema for player progress | Data | A serialized shape |
| "level 7 requires 12000 xp" as a curve **table** | Data | Authored |
| "the player levelled up" decision | Systems | A rule |
| Damage number popup | Presentation | View |
| The quest state machine | Systems | Rules and transitions |
| Quest log screen | Presentation | View over Systems state |
| "on boot, load the database then start the sim" | Composition | Wiring |

The recurring pattern: **the authored value is Data, the decision made from it is Systems.** When
someone argues a thing is "obviously Data" because it comes from a table, ask whether the code
*decides* anything. If it does, it is Systems reading Data.

## 4. Auditing an existing graph

```bash
# 1. list every assembly definition
find . -name "*.asmdef" -not -path "*/Library/*" | sort

# 2. dump each one's references
for f in $(find . -name "*.asmdef" -not -path "*/Library/*"); do
  echo "── $f"
  python -c "import json,sys;d=json.load(open(sys.argv[1]));print(d.get('name'));[print('   ->',r) for r in d.get('references',[])]" "$f" 2>/dev/null \
    || grep -A20 '"references"' "$f"
done

# 3. anything referencing a test or editor assembly from runtime code
grep -rl '"references"' --include=*.asmdef . | xargs grep -l 'Tests\|Editor' 
```

Read the dump with the layer table in hand and mark each assembly's layer. Then look for:

- an assembly whose layer number is **lower** than something it references → upward reference
- two assemblies at the same number referencing each other → peer cycle
- a runtime assembly referencing a `.Tests` / `.Editor` sibling → will break the player build
- an assembly referenced by nearly everything → either a legitimate Common or a bag to split

**Record the layer assignment somewhere durable** once you have made it. An audit that lives only in
one conversation gets redone from scratch every time, with different answers.

## 5. Breaking a cycle — the decision tree

```
A and B want each other.
│
├─ Is one of them clearly more general than the other?
│    └─ YES → the general one is LOWER. Its reference to the specific one is the bug.
│             Remove it; the specific one keeps its reference. Done.
│
├─ Do they both need the same third thing (ids, contracts, value types)?
│    └─ YES → extract that third thing DOWNWARD into a lower assembly.
│             Both reference it; neither references the other. This is the best fix
│             and the most commonly missed one.
│
├─ Does the lower one need to *call into* behaviour the higher one implements?
│    └─ YES → invert: lower declares an interface, higher implements it,
│             Composition wires the two. Costs one indirection.
│
└─ Is the coupling genuinely a notification, not a call?
     └─ YES → the lower publishes an event/message; the higher subscribes.
              Removes the edge, but control flow becomes non-obvious.
              Choose this because you want the decoupling, not to escape a cycle.
```

**Anti-fix: merging A and B.** The cycle is now internal and invisible. The merged assembly has two
reasons to change, and the next person cannot see that the boundary was ever there. If you genuinely
believe A and B are one concept, that is a different argument — make it explicitly, do not arrive at
it by way of a cycle.

**Anti-fix: a `Shared` assembly that both reference, holding whatever ended the cycle.** This is
extraction done without thought. Extract a *named concept* downward — ids, contracts, a value type.
If you cannot name what you extracted, you have created tomorrow's junk drawer.

## 6. Package extraction

Moving an assembly out of the game into a shared package is a layer decision plus a lifetime
decision. Check the profile's `assembly_map.split_rule`.

It qualifies when **all** hold:

- it references nothing above it and nothing title-specific;
- its concepts would mean the same thing in a different game;
- it has its own tests, or could;
- someone would be annoyed to rewrite it next project.

It does not qualify because it is well written, or because it is finished. Those are properties of
good game code too.

Extract **after** the boundary has held still for a while. Extracting early freezes an interface you
have not finished learning, and un-freezing it across two consumers costs more than the extraction
saved.

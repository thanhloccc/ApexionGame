# RTS Battle — the sample

*[Tiếng Việt](README.vi.md) · [Library](../ApexionGame.Entities.Stats/README.md) ·
[Design doc](Documentation~/RTS-BATTLE-DESIGN.vi.md)*

A small RTS you can actually play, built to answer one question in the first thirty seconds:

> why would I want a dependency-graph stat system instead of a `Dictionary<string, float>`?

Spawn units, buy research, cast buffs and debuffs, field a hero, break the enemy stronghold. Every
mechanic in it exists because it puts one of the library's features on screen.

Open **`rts-battle.unity`** and press Play.

## Controls

| | |
|---|---|
| `Q` `W` `E` `R` | spawn Footman / Archer / Knight / Warchief |
| `1` … `6` | pick a spell, then **left-click the field** to cast it there |
| left-click a unit | inspect it — this is the panel that matters |
| right-drag, arrow keys | pan · mouse wheel: zoom |
| `Space` | pause · `F5`: restart · `Esc`: cancel a spell |

Team B is played by an AI. The **auto-play** button in the top bar hands your side to one too, so the
sample can run unattended.

## What to look at

**1 — The inspector.** Click a footman. `Attack` reads `14 → 29`, and underneath it says
`▸ + Team A/node.AttackBonus`. That number is not stored on the unit; it is computed from a stat that
lives on a *different owner*, and it was already correct before anything asked for it.

**2 — Research.** Buy *Weapons*. One `TrySetStatBaseValue` on the team node, and the log tells you how
many stats changed. There is no loop over the army anywhere in this project — including units that spawn
minutes later.

**3 — The hero.** Field the Warchief and watch the whole army's Attack rise. Let it kill three units and
it levels: one write to `AuraPower`, and the log counts the stats that followed. Then let it die — the
army loses the aura in that same propagation.

**4 — Buffs are modifiers, durations are yours.** Cast Bloodlust and the inspector grows two rows on the
target. When it expires the log says `Bloodlust expired on 14 unit(s): 11 of 14 modifier(s) given back` —
the missing three died with their units. The runtime has no concept of duration;
`RtsEffectSystem` is what supplies it.

**5 — The leak you cannot see.** The **STAT GRAPH** panel counts observers on each team's node. Press
`cull 5 of mine` and it drops. Press `sloppy`, then `cull 5 of mine` again, and it does not — because a
destroyed owner does not remove the observer entries its modifiers left on someone else's node. That is
the most expensive mistake in this library, and here it is a number on screen.

Also worth pressing: `cycle` (asks the runtime for an aura that would close a loop, and gets refused),
`recalc` (runs everything through the generated Burst job), `prune` (removes modifiers that reported a
missing source), and `node` (inspects the shared bonus node itself — base values are research, modifiers
are hero auras).

## The three lessons

**A unit costs five modifiers, always.** Three links to its team's node, one `Hp ≤ MaxHp` cap, one
`AttackInterval` floor. Not five per hero, not five per research level — five. That is what the shared
node buys: point every unit at N heroes directly instead and every spawn, death and level-up scales with
N. There is a test that pins it.

**A death removes its modifiers *before* destroying its owner.** Anything a dying unit put on *another*
owner has to come off by hand — its links (which observe the node) and, for a hero, its aura terms (which
live on the node). Anything sitting on the dying unit's own stats needs nothing: `DestroyOwner` clears
that owner's buffers. The rule is not "remove everything", it is "remove anything that touches an owner
other than the one being destroyed".

**`AuraPower` exists to break a cycle.** The obvious hero aura is "the team's Attack bonus is 25% of the
hero's Attack" — but the hero is a unit, so its Attack already reads that bonus. `TryAddStatModifier`
refuses the loop at insert time and returns `false` without throwing, which is the good outcome and also
the easy one to ignore. `AuraPower` is a stat that is not downstream of the aura, so the edge is legal.

## How it is built

**Two assemblies**, so the layering is a compiler rule rather than a habit: `Rts.Game` references
`Rts.Core`, and `Rts.Core` has never heard of it. Nothing in the simulation can reach for a `GameObject`,
and a test proves it.

```
Rts.Core/                 ← the game. No MonoBehaviour, no scene, no UI.
├── Stats/                [StatSystem], the six modifier kinds, the two collections
├── Content/              every number in the game, immutable, in RtsContent.cs
├── Model/                RtsUnit · RtsTeam · RtsEffect · RtsBattleFeed · RtsGeometry
├── World/RtsStatWorld    the only type that touches the store, the accessor or the world data
├── Systems/              one file per mechanic:
│   ├── RtsSpawnSystem        the five modifiers every unit is born with
│   ├── RtsMovementSystem     targeting, walking, separation
│   ├── RtsCombatSystem       swings, damage, healing — writes only Hp
│   ├── RtsEffectSystem       spells, and the duration the runtime does not provide
│   ├── RtsEconomySystem      supply, and the one write research makes
│   └── RtsDeathSystem        removing modifiers before destroying an owner
├── Diagnostics/          RtsGraphProbe (measure) · RtsGraphExperiments (poke) · RtsStatInspector (explain)
├── Journal/              what happened, as data — no sentences in the simulation
├── Ai/RtsAiDirector      plays a side, without randomness
└── Match/RtsMatch        owns who exists and the order things happen in. Nothing else.

Rts.Game/                 ← the presentation. Knows about Core; Core does not know about it.
├── RtsGame.cs            the only MonoBehaviour: wiring, and one Update
├── RtsMatchLoop.cs       frame time in, whole ticks out, interpolation factor back
├── RtsPlayerController   every verb the keyboard and the HUD share
├── Input/ · World/       Input System sampling · primitives, unit views, camera rig
└── Hud/                  a theme, a widget kit, and one class per panel
                          (top bar · command · graph · spell bar · journal · inspector)

Rts.Tests/                ← 20 EditMode tests: graph invariants, effects, match flow, layering
```

`RtsMatch` is ~250 lines and holds two things: who exists, and the order things happen in. Every mechanic
is a system you can read on its own — which is the difference between this and the 2,000-line partial
class it replaced.

The scene holds a camera, one GameObject and a `UIDocument`. The field, the units and the entire interface
are built at runtime.

**Collections.** `FasterList<T>`, `ArrayMap<K,V>` and `ArraySetNative<T>` from EncosyTower throughout, so
the per-tick work — draining change events, de-duplicating touched stats, tracking dangling modifiers —
allocates nothing.

**No art assets.** Bodies are Unity primitives with runtime `Universal Render Pipeline/Unlit` materials;
health bars are quads. Copy the folder into another project and it runs. (Primitives rather than sprites
because this project is configured with the 3D Universal Renderer, where the sprite path is a
pink-material trap that has nothing to do with what the sample teaches.)

## Tests

```powershell
unity test . --mode EditMode --filter "ApexionGame.Entities.Stats.Samples.Rts.Tests"
```

Twenty tests in four files, each pinning a mistake that is invisible when made.

| | |
|---|---|
| `RtsGraphInvariantTests` | the five-modifier invariant; research and auras reaching units that spawn later; clean deaths leaving no observers behind — and the counter-test that sloppy deaths *do* leak, so the warning above is a verified claim rather than folklore |
| `RtsEffectTests` | expiry giving modifiers back; a recast refreshing instead of stacking; over-healing stopping at `MaxHp` through nothing but the `ClampMax` modifier; a refused cast costing neither supply nor cooldown |
| `RtsMatchFlowTests` | determinism under a fixed seed; a full match running to a winner with no dangling modifiers left; commands refused once it is decided |
| `RtsLayeringTests` | no scene objects in the simulation, no reference to the presentation layer, no mutable public fields on content |

The tests drive whole matches with no scene to load, and they use the same API the game does — there are no
test-only hooks in the simulation. `cull 5 of mine` is a button in the HUD first and a test tool second.

## Settings worth turning

On the `rts-battle` GameObject:

| | |
|---|---|
| `Tick Seconds` | 0.05 — the fixed step; the view interpolates between two of them |
| `Supply Per Second` | the whole economy in one number |
| `Max Units Per Team` | 60 by default; the separation pass is O(n²) per team |
| `Sloppy Deaths` | the leak, also toggleable live from the HUD |
| `Random Seed` | fixed, so a replay of the same inputs is a replay |
| `Auto Play Player Team` | hand your side to the AI on start |

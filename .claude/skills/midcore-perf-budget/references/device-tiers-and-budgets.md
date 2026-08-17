# Device tiers and budgets

## 1. Why a number is required first

Without a budget, "optimize this" has no completion condition. Every change is arguable, every
system's cost is "a bit high", and effort goes wherever the last complaint came from.

With a budget, the question becomes checkable: **is this system inside its share?** If yes, leave it
alone regardless of how it looks. If no, the gap is the size of the problem.

## 2. Naming tiers

Three is usually right. Each tier is a **named reference device you can actually hold**, not a
category.

| Tier | Means | Reference |
|---|---|---|
| **Min** | the weakest device you commit to supporting | a specific model |
| **Mid** | where most of the player base sits | a specific model |
| **High** | where you spend the headroom on quality | a specific model |

"Low-end Android" is not a tier — it spans an order of magnitude of performance. A named model gives
a device to test on and a number that means something.

**Min-spec is a business decision, not an engineering one.** It determines how much of the market you
can sell to. Surface it; do not pick it yourself.

## 3. The budget table

Fill this in per tier. The numbers below are **placeholders showing the shape** — do not copy them
as targets. If the profile says `device_tiers: unknown — ask`, the correct action is to ask, not to
adopt these.

```
TIER: <name>          Reference device: <model>          Target: <n> fps → <x> ms/frame

  Frame budget                     ms        %
  ├─ Simulation / gameplay        ___       ___
  ├─ Rendering                    ___       ___
  ├─ UI                           ___       ___
  ├─ Audio + misc                 ___       ___
  └─ Headroom                     ___       ___     ← 20–30%; do not plan to use it
                                  ────
                          TOTAL   ___       100%

  Memory ceiling                   MB
  ├─ Content / assets             ___
  ├─ Textures                     ___
  ├─ Audio                        ___
  ├─ Managed heap                 ___
  └─ Engine + runtime             ___
                                  ────
                          TOTAL   ___

  Load times                       s
  ├─ Cold start → main menu       ___
  └─ Menu → playable              ___
```

**Headroom is not spare budget.** It absorbs the worst case — the busiest combat, the fullest
inventory, a background app stealing CPU, a device that is thermally throttled. A build that hits
its target only in the quiet moments does not hit its target.

## 4. Dividing the budget across systems

Once the per-category budget exists, divide the simulation share among the systems that consume it.
This is what makes a specific system's cost judgeable.

Two rules:

- **Allocate to systems that exist**, not to a hypothetical breakdown. Measure the current split
  first, then decide which shares are wrong.
- **Sum to less than the category budget**, leaving slack. Systems that exactly fill their budget
  leave nothing for the one that grows.

A system over its share is a task. A system under it is finished, regardless of its code quality.

## 5. Thermal and sustained performance

The trap specific to mobile and handhelds: **a device is fastest in the first minute.**

Benchmarks taken in the first thirty seconds of a cold device are the best case, and the player does
not live there. As the device heats, clocks drop — a frame time that is comfortable at second 10 can
be over budget at minute 10, with no code change.

So:

- measure after a **sustained** run, not immediately after launch;
- state the elapsed time in any reported measurement;
- treat "fine for a minute, drops after five" as a **budget failure**, not a device quirk.

## 6. Load and startup

Frame time gets attention; startup time gets ignored and is measured directly by store analytics and
player patience.

Budget both:

- **cold start → interactive** — the number a player experiences on first launch;
- **transition times** — menu to playable, scene to scene.

Measure on a **cold** device with a **fresh install**, not with a warm cache after twenty launches.
The warm number is the one you will accidentally optimise for, and no player ever sees it.

## 7. When there is no budget yet

If the profile says `unknown — ask`, ask for exactly these:

1. **Minimum-spec device** — the weakest model you commit to supporting.
2. **Mid-tier reference** — where the player base is expected to sit.
3. **Target frame rate** — 30 or 60, per tier; they can differ.
4. **Memory ceiling** — or the target platform's limit, if that is the real constraint.

Those four unblock the whole table. Until they exist:

- do not quote a frame budget;
- do not call anything "fast enough" or "too slow";
- **do** still measure, and report absolute numbers with the device stated — measurements are
  useful even before a target exists, and they inform what the target should be.

Record the answers in the profile, not in a conversation. That is the entire point of the profile:
the number is decided once, by a person, and stays decided.

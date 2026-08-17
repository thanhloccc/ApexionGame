# Events and seasons

## 1. Server time is the only clock

Anything time-bounded uses server time. Never the device clock.

Device clocks are wrong in both directions — timezone confusion, drift, a phone reset — and some
players change them deliberately to skip timers or re-trigger daily rewards.

**The mechanism:** fetch server time, store the **offset** from device time, and compute all
game-relevant times as device time plus offset. Store the offset, not the absolute time, so it
survives while the device clock keeps ticking normally.

**Rules**

- Refresh the offset on reconnect and periodically, not only at boot.
- **Time never moves backwards** from the game's perspective. If a correction would move it back,
  hold the previous value until real time catches up — otherwise a player can be granted the same
  reward twice.
- **Detect large device-clock jumps** and treat them as suspicious rather than authoritative.
- Never grant progress based on device time alone.

## 2. Offline

A player will be offline while an event runs. Decide, per mechanic:

| Question | Must have an answer |
|---|---|
| Do timers advance offline? | Yes/no — and it must be consistent across every timer |
| What happens on reconnect? | Reconcile forward; never grant twice |
| Can they participate offline? | Usually yes, with results reconciled later |
| What if the event ended while offline? | They must see what happened, not a blank |

**Never let offline play accumulate rewards that reconciliation cannot verify.** If offline progress
is credited, it must be reconstructable from something the server can check — otherwise the device
clock has become the source of truth by the back door.

## 3. Lifecycle edge cases

These are where event bugs live. Each needs a decided behaviour, before launch:

| Case | Decide |
|---|---|
| Event ends **mid-session** | Grace to finish the current run? Or stop immediately? |
| Player enters **after it closed** | What they see — a summary, or nothing? |
| Player restores an **old save** from during the event | Does old progress still count? |
| Player is **mid-purchase or mid-reward** when it ends | The transaction must complete or roll back cleanly |
| Event is **cancelled early** via kill switch | What happens to accumulated progress? |
| Two events **overlap** | Deliberate, or a scheduling mistake? Can both be entered? |
| Event **rewards a retired content id** | Fails at grant time — validate at authoring |

The most common shipped bug: **the event ends while the player is inside it**, and the code assumes
it cannot. Test that path explicitly.

## 4. Season and progress state

Season progress is persisted player state, so `midcore-save-migration` applies in full:

- season progress lives in the **save**, and its shape is versioned;
- a **new season is not a schema change** if the shape was designed for repetition — design it that
  way from season one;
- **archiving a finished season** is a migration, and the decision about what players keep is a
  design decision;
- **never delete a past season's progress** without deciding what the player retains.

The design that avoids most of this: one season-progress shape holding a season identifier, rather
than a new field per season. Otherwise every season is a schema change, forever, at a fixed cadence.

## 5. Ship content dark, then flip

Content delivery and activation are separate mechanisms with different latencies.

```
1. Author the event content; it passes normal validation and the release gates
2. Ship it in the build (or push it to the content delivery system)
3. CONFIRM distribution — enough of the population actually has it
4. Flip the remote switch
```

Skipping step 3 produces a broken state for everyone who has not fetched yet: the switch says the
event is live, the content is not there.

Consequences:

- **event content goes through the same validation as all content** — it is not exempt for being
  temporary (`midcore-data-pipeline`);
- **the client handles "switch on, content missing" gracefully** — it will happen to someone;
- **the dark content must be inert** until switched on. Not visible, not referenced, not loaded.

## 6. Scheduling

- **Never schedule an event to start unattended when nobody is available**, especially the first
  time. If it breaks at 03:00 on a Saturday, it is broken until someone wakes up.
- **Stagger start times** for large populations if the backend has a load ceiling.
- **Define the end explicitly.** An event with no end is a permanent feature that nobody
  code-reviewed as one.
- **Rehearse on a test schedule** with compressed durations, so the lifecycle edge cases in §3 are
  observed rather than reasoned about.
- **Have a kill switch before it starts** (`kill-switches.md`). Not after.

## 7. Testing time-bounded content

Time is the hardest thing to test, so make it injectable:

- **Game logic never reads the clock directly** — time is supplied, exactly as in
  `midcore-testing`, `determinism-and-golden.md`.
- Test the boundaries: **just before start, just after start, just before end, just after end.**
- Test **crossing a boundary mid-session** — the case most likely to be broken.
- Test **large clock jumps**, forwards and backwards.
- Test **a save written during the event, loaded after it ended.**

Every one of these is a real player scenario, and none of them can be observed by waiting for the
real event.

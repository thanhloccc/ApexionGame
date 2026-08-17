# Telemetry

## 1. Event names are permanent

An analytics event name and its properties are as permanent as a save schema version, for the same
reason: **historical data has already been recorded against them.**

Rename `level_complete` to `stage_complete` and every comparison across the rename breaks. The old
data does not get renamed; it sits under the old name forever, and any query spanning the change must
know about it.

So:

- **decide the taxonomy early** and deliberately;
- **change by adding, never by renaming**;
- **deprecate rather than delete** — stop sending an event, keep the name reserved;
- **never reuse a name** with different semantics. Same failure as reusing a save field: no error
  fires, the numbers are simply wrong.

## 2. Naming

One convention, applied everywhere:

```
<subject>_<verb_past>          level_completed, item_purchased, quest_abandoned
```

- **Past tense** — an event records something that happened.
- **Subject first**, so related events sort together.
- **No versions in names.** `tutorial_completed_v2` has nowhere to go next.
- **No spaces, no punctuation, one casing.** Query languages care.
- **Properties, not name variants.** One `item_purchased` with an `item_id` property, not one event
  name per item — otherwise the name space grows with content and no query can generalise.

## 3. What to instrument for a progression game

Four groups. This is a starting set, not a maximum.

### Funnel — where players stop

- app opened, first session started
- tutorial: started, each step completed, finished, **abandoned**
- first meaningful milestone reached
- each early-session milestone for the first several sessions

The funnel answers the only question that matters early: **where do they leave?** Instrument
abandonment explicitly; an absent event is ambiguous between "did not do it" and "not instrumented".

### Progression — how far and how fast

- level or chapter started / completed / failed, with elapsed time and attempt count
- power or progression milestones reached
- content unlocked
- **failure with cause** — this is where difficulty tuning comes from

Attempt count and failure cause are what turn "this level is hard" into an actionable number.

### Economy — sources and sinks

- currency **earned**, with source
- currency **spent**, with sink
- item acquired / consumed, with reason
- balance snapshot at session end

**Every currency movement carries a source or sink label.** Without it, inflation is visible but its
cause is not, and economy problems compound silently over months.

### Session shape

- session start and end, with duration
- what the player did in the session, at a summary level
- return interval

## 4. Properties worth attaching to everything

- player identifier (stable, and **respecting the project's privacy commitments**)
- app version and build number — so a regression can be attributed to a release
  (`midcore-release-pipeline`)
- server timestamp, not device time (`events-and-seasons.md`)
- session identifier
- device tier, if it is known — performance and behaviour correlate with it
- active experiment assignments, if any (`remote-config.md` §7)

The version stamp is the one most often missing and most often needed: without it, "did this get
worse" cannot be answered.

## 5. The volume trap

More events is not more insight. Past a point it is less:

- costs scale with volume, on ingestion and on storage;
- high-frequency events (anything per-frame or per-input) will swamp everything else;
- an event nobody has ever queried is pure cost;
- large payloads on mobile consume the player's data allowance, which is a real harm.

**Instrument decisions and outcomes, not mechanics.** "Player opened the shop" is a decision.
"Player scrolled the shop list" is a mechanic, and it will outnumber everything real by orders of
magnitude.

Review the event list periodically and remove what nothing queries. An unqueried event is a cost with
no benefit, and it makes the useful events harder to find.

## 6. Getting it wrong is expensive

Three failures that cannot be fixed retroactively:

| Failure | Consequence |
|---|---|
| Event not instrumented | The data does not exist. Adding it now starts from zero — no history |
| Missing property | Cannot segment past data; the breakdown you need was never recorded |
| Renamed event | Comparison across the rename is broken, permanently |

The asymmetry: **adding an event later gives you data from later.** When a question arises about
launch behaviour six months in, the answer exists only if it was instrumented before launch. That is
why the taxonomy is designed before shipping and not iterated into existence.

## 7. Privacy and data handling

Not optional, and not solely a legal formality:

- **Never log personally identifying information** in gameplay telemetry.
- **Never log save contents**, credentials, or anything from a text field a player typed.
- Use a **stable pseudonymous identifier**, not a device or account identifier, where the platform
  requires it.
- Honour the platform's consent and opt-out mechanisms — an opt-out that still sends events is a
  compliance failure.
- Know the retention policy and that deletion requests can actually be satisfied.

If the project has no stated position on any of this, that is a gap to surface rather than a decision
to make in passing.

## 8. Verify before shipping

Telemetry fails silently — nothing in the game breaks when events stop arriving.

- Confirm events **arrive** and are well-formed, in a real build, before release.
- Confirm properties are populated and not defaulting to empty.
- **Add a completeness check to the release gate** if the project depends on telemetry.
- Watch volume after release: a sudden drop usually means an event stopped firing, not that players
  stopped playing.

# Kill switches and rollout

## 1. Every remote feature has an off switch

> The off position returns to **shipped behaviour** — the state the build was tested in.

Not "a degraded mode", not "an empty state nobody designed". The build shipped working without the
feature; the switch returns to that.

**Design the switch with the feature, not after.** A kill switch added later is untested, and it is
first exercised during the incident it exists for — which is the worst possible moment to discover
that turning the feature off leaves the game in a state nobody has seen.

## 2. Properties a kill switch must have

| Property | Why |
|---|---|
| **Takes effect fast** | An incident is measured in minutes. Next-launch is usually acceptable; next-week is not |
| **Cannot itself fail open** | If config cannot be fetched, the switch's default must be safe |
| **Independent of the feature** | A switch inside the failing system cannot turn it off |
| **Tested before launch** | Flip it in a real build and confirm shipped behaviour returns |
| **Reversible** | Turn it back on once fixed, without a client update |
| **Single-purpose** | One switch, one feature. Bundled switches turn off more than intended |

The third row is the subtle one: a kill switch evaluated by the subsystem it kills does not work when
that subsystem is what is broken. Evaluate it above the feature.

## 3. Testing the switch

Before the feature ships, in a real build:

1. Feature on → works as intended.
2. Feature off → **shipped behaviour**, no errors, no empty screens, no dangling references.
3. Flip **on → off mid-session** → the state it lands in is coherent.
4. Flip **off → on mid-session** → same.
5. Config fetch fails entirely → the safe default applies.

Step 3 is the one that finds real bugs. A player will be inside the feature when it is turned off,
and the code that assumes it cannot disappear mid-use is exactly the code being turned off.

## 4. Staged rollout

Turning something on for everyone at once means finding out about a problem from everyone at once.

```
internal → small percentage → larger percentage → full
```

At each stage:

- **decide the duration in advance** — long enough for the metrics to move;
- **watch the specific metrics** (§5);
- **do not advance because it has been quiet** — advance because the metrics say it is fine;
- **roll back on the trigger** (§6), without a discussion about whether it is bad enough.

Assignment must be **stable**: a player who has the feature keeps it as the percentage grows.
Reassigning at each stage means players lose access to something they were using, which reads as a
bug and generates support load.

## 5. What to watch during a rollout

| Signal | Watch for |
|---|---|
| Crash rate | Any increase in the exposed group versus the control |
| Session length / count | A drop can indicate a break the crash rate does not show |
| Funnel completion | Players getting stuck at a new point |
| The feature's own metrics | Is anyone using it, and completing it? |
| Economy flows | Unexpected currency movement — see `telemetry.md` §3 |
| Support volume | Often the fastest signal, and the least instrumented |
| Error and clamp rates | A config clamp firing means someone made a mistake |

**Compare against a control group**, not against yesterday. Day-of-week effects and other releases
will otherwise be attributed to your feature.

## 6. Define the rollback trigger in advance

Write it down **before** the rollout starts:

```
Roll back immediately if:
  - crash rate in the exposed group exceeds <control + X%>
  - <key funnel step> completion drops by more than <Y%>
  - any data-integrity error appears in the logs, at any rate
  - support reports <Z> or more instances of <specific symptom>

Otherwise hold at the current stage for <duration> and re-evaluate.
```

Why in advance: during an incident, every threshold looks arguable, and the person who built the
feature is the least able to judge it. A pre-agreed number removes the argument at the moment
arguing is most expensive.

**Roll back first, diagnose after.** The switch exists so that stopping the harm does not require
understanding it.

## 7. After a rollback

1. **Confirm the rollback worked** — the metric returned to baseline. A flipped switch is not a fixed
   problem until the data agrees.
2. **Preserve the evidence** — logs, telemetry, affected player identifiers. It will be needed and it
   ages out.
3. **Identify who was affected** and whether anything needs repair. Progress or currency issues may
   need correction, and that is a decision for someone accountable, not an implementation detail.
4. **Fix, then re-roll from the start** — internal, small percentage, and up. Not straight back to
   where it was when it broke.
5. **Write down what the signal was**, so the next rollout watches for it.

## 8. Toggle hygiene

Runtime toggles accumulate, and a codebase where every feature is behind a toggle nobody remembers
the state of has stopped being reproducible from its source.

- **Every toggle has an owner and an expiry**, written in the declaration.
- **Promote or delete** once a feature is fully rolled out: the shipped default becomes the new
  behaviour, and both the toggle and the loser branch are removed.
- **Never nest toggles.** Two are four states; three are eight, and nobody tests eight.
- **Audit periodically.** A toggle that has been at 100% for six months is not a toggle, it is dead
  code plus a risk that someone flips it.

Permanent kill switches are legitimate for genuinely risky subsystems — a backend integration, a
payment path. Those are documented as permanent, with the shipped-behaviour fallback maintained and
tested, rather than left as forgotten rollout debris.

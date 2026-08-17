---
name: midcore-live-ops
description: Use when game behaviour or content must change without shipping a client update — remote configuration, time-bounded events and seasons, A/B tests, runtime feature toggles and kill switches, and the telemetry needed to tell whether any of it worked. Covers what is safe to drive remotely versus what must ship in the build, offline-first defaults, server time and clock skew, event taxonomy, and staged rollout with a defined rollback trigger. Load it BEFORE making anything remotely configurable, because the shipped default and the kill switch are part of the design rather than additions to it. Backend-agnostic — the project's backend, if any, is pinned in the project profile. Triggers on "remote config", "live ops", "liveops", "event", "season", "battle pass", "A/B test", "experiment", "feature flag", "kill switch", "rollout", "analytics", "telemetry", "funnel", "retention", "player metrics", and Vietnamese phrasing "cấu hình từ xa", "sự kiện trong game", "mùa giải", "thống kê người chơi", "tắt tính năng từ xa", "chỉnh số từ server".
---

# Live-ops

Read `.claude/project-profile.md` in the current repo before proposing anything. It pins the
technology, conventions and authority skills this file deliberately does not name.
If the profile is missing, say so and ask — do not guess, and do not assume a specific engine
package or standard library.

**Check `live_ops.backend` before anything else.** If it says `none installed`, then no backend has
been chosen, and choosing one is a package and vendor decision the user has not made. Say so and ask.
Design against the rules below — they hold regardless of vendor — but do not assume a service exists,
and do not name one.

## The one rule

> **Values can be driven remotely. Shapes cannot.**

A number, a toggle, a date, an availability flag — safe. Anything the client's code structure depends
on — a new field, a new content shape, a new kind of thing — is a client update wearing a config
file's clothing.

The test: *could a client from three versions ago receive this value and behave sensibly?* If not, it
is not remote configuration.

## Offline-first, always

Every remotely-driven value has a **shipped default that is safe on its own**. The fetch will fail:
no network, a slow network, an expired certificate, a backend outage, a malformed response, a player
on an airplane.

Four failure cases, each needing a defined behaviour:

| Failure | Behaviour |
|---|---|
| Fetch fails | Use the shipped default. Game fully playable |
| Fetch is slow | Do not block boot on it. Never gate the first screen on a network call |
| Returns malformed data | Validate; reject the whole payload; keep the defaults |
| Returns a valid but absurd value | Clamp to a sane range, shipped in the build |

That last one is the one that gets skipped, and it is how a typo in a config console — an extra zero
— reaches every player at once.

## Before making anything remotely configurable

1. **What is the shipped default?** If there is no safe default, it is not a candidate.
2. **What happens when the fetch fails?** Answer concretely, not "it will work".
3. **What is the valid range?** Clamp in the client, using bounds that ship in the build.
4. **What is the kill switch?** Every remote feature has an off position that returns to shipped
   behaviour.
5. **How will you know it worked?** If no telemetry answers that, the control is unmeasurable.
6. **Could this be content instead?** Content that ships and is switched on is often simpler and
   safer than a value fetched at runtime — see `midcore-data-pipeline`.

## Server time is the only clock

Anything time-bounded — events, seasons, daily resets, timers — uses **server time**. Never the
device clock.

Device clocks are wrong, in both directions, and some players set them deliberately to skip timers.

Handle explicitly:

- **clock skew** — track the offset between server and device, apply it consistently;
- **offline play** — what happens to a timer while offline, and how it reconciles on reconnect;
- **an event ending mid-session** — the player is inside it when it closes;
- **an event that already closed** — a player enters late, or restores an old save;
- **a device clock moved backwards** — never let it grant progress.

## Content must arrive before the switch flips

Content delivery and activation are separate mechanisms with different latencies. Content that a
remote switch enables must **already be in the build or already downloaded** before the switch
flips — otherwise the switch produces a broken state for anyone who has not fetched it yet.

The sequence that works: **ship the content dark → confirm distribution → flip the switch.** Never
the reverse. See `midcore-data-pipeline/references/keys-and-groups.md`.

## Telemetry names are permanent

An analytics event name and its properties are as permanent as a save schema version, for exactly the
same reason: **historical data has already been recorded against them.** Renaming one breaks every
comparison across the rename.

So the taxonomy is decided early, deliberately, and changed by adding rather than renaming.

## This skill does not own

| Question | Owner |
|---|---|
| Which backend, and how to integrate it | Not decided — see the profile; surface the choice |
| Content tables and delivery grouping | `midcore-data-pipeline` |
| Compile-time feature gating | `midcore-assembly-architecture` |
| Persisted player state and its migration | `midcore-save-migration` |
| Build versioning and release gates | `midcore-release-pipeline` |
| Monetisation, IAP, receipt validation, store policy | **out of scope** — a genuinely separate discipline. Say so rather than half-covering it |

## References

- `references/remote-config.md` — the value/shape line, shipped defaults, validation and clamping,
  and what belongs in content instead.
- `references/events-and-seasons.md` — server time, clock skew, offline reconciliation, event
  lifecycle edge cases, and shipping content dark.
- `references/telemetry.md` — event taxonomy, what to instrument for a progression game, permanence,
  and the volume trap.
- `references/kill-switches.md` — off switches that revert to shipped behaviour, staged rollout,
  what to watch, and defining the rollback trigger in advance.

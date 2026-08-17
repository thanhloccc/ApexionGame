# Remote configuration

## 1. The value/shape line

> **Values can be driven remotely. Shapes cannot.**

| Safe remotely | Not safe remotely |
|---|---|
| A number — drop rate, cost, cooldown, multiplier | A new field the client has never seen |
| A boolean — feature on/off | A new kind of thing — a new item category |
| A date — event start and end | A change to how existing data is interpreted |
| An availability flag on shipped content | Content that is not in the build or downloaded |
| A choice among options the client already implements | A new option the client does not implement |

The test: **could a client from three versions ago receive this value and behave sensibly?** Old
clients keep running for a long time — players do not update promptly — so every remote value is
received by versions you have stopped thinking about.

If the answer is no, the change is a client update, and calling it configuration only moves the
breakage to a place with no gates in front of it.

## 2. Shipped defaults

Every remotely-driven value has a default **compiled into the build** that is safe on its own.

- The game must be fully playable on defaults alone. Not degraded-but-launchable — playable.
- The default is the value you would ship if the backend never existed.
- **Never default to a sentinel** — zero, empty, or null meaning "waiting for config". That value
  will be used, because the fetch will sometimes not complete.

Consequence worth stating: **a first-run player with no network gets a complete game.** If that is
not true, the dependency is not configuration, it is a requirement.

## 3. Validate every payload

Remote config is untrusted input. It arrives from a console where someone typed it, and typos there
reach every player at once with no build gate in between.

| Check | On failure |
|---|---|
| Well-formed and parses | Reject the whole payload; keep current values |
| Every key recognised | Ignore unknown keys — a newer backend must not break an older client |
| Every value the right type | Reject that value; keep the default |
| Every value within a **shipped** range | **Clamp**, and record that it was clamped |
| Internally consistent — start before end | Reject the related group |

**Reject the whole payload on a structural failure, per-value on a value failure.** Half-applied
config is a state nobody designed and nobody tested.

The clamp is the important one. Bounds ship in the build, so a wrong value in the console is
survivable rather than global. Record clamping as a telemetry event — a clamp firing means someone
made a mistake and does not know it yet.

## 4. When it takes effect

Decide, per value, and write it down:

| Timing | Suits |
|---|---|
| Next launch | Anything affecting balance mid-session — safest by a wide margin |
| Next session boundary | Values that would be unfair to change mid-run |
| Immediately | Kill switches only |

**Default to next launch.** Values changing mid-session produce bugs nobody can reproduce, because
the state that caused them no longer exists — and players correctly perceive mid-run rule changes as
unfair.

Kill switches are the exception, because their whole purpose is stopping something now
(`kill-switches.md`).

## 5. Caching

- **Cache the last good payload** and use it when a fetch fails. Better than defaults, because it is
  the intended current configuration.
- **Never cache a rejected payload.**
- **Bound the cache age.** A cached config from months ago may reference an event long over; past
  some age, prefer shipped defaults.
- **The cache is not a save.** Losing it is harmless; it must never hold player state.

## 6. Config versus content

A frequent design question with a clear answer most of the time.

| Use content | Use remote config |
|---|---|
| The thing has structure — rows, fields, references | The thing is a single value |
| It needs validation against other data | It stands alone |
| Designers author it in bulk | An operator flips it |
| It must be testable before shipping | It must change without a build |

**Prefer content, switched on by config.** Content goes through import validation, review, and the
release gates. A config value goes through none of those.

The pattern: **ship the content dark, flip one remote boolean to reveal it.** Structure is validated
at build time; timing is decided at run time. See `events-and-seasons.md` §5.

## 7. A/B tests

An A/B test is remote config with an assignment, and the extra rules matter:

- **Assignment is stable per player.** A player switching variants mid-test invalidates both the data
  and their experience.
- **Both variants ship in the build.** If one requires a client update, it is a staged rollout, not
  an experiment.
- **Define the metric and the duration before starting.** Deciding what counts as a win after seeing
  the data is not a test.
- **Have an exit.** Every experiment ends — with the winner promoted to the shipped default and the
  loser's code deleted. Long-running experiments become permanently forked behaviour nobody
  understands.
- **Never experiment on anything that touches player money or persisted progress** without an
  explicit decision from someone accountable. Losing a variant must not mean losing progress.

## 8. Never remotely configure

- Anything that changes the **shape** of persisted data — that is a save migration
  (`midcore-save-migration`);
- anything that must be true for the game to boot;
- anything with no safe default;
- anything whose wrong value cannot be clamped;
- security or entitlement decisions that the client alone enforces.

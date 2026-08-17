# Reproduce first

Everything else depends on this. Without a reliable reproduction you cannot narrow, cannot confirm a
fix, and cannot write a regression test. **Getting to a reliable repro is usually most of the work,
and it is work — not a preliminary.**

## 1. What a good reproduction is

| Property | Why |
|---|---|
| **Reliable** | Happens every time, or at a known rate. Otherwise "fixed" means "did not happen once" |
| **Minimal** | Fewest steps, smallest data, fewest systems involved |
| **Deterministic** | Same input, same result — no seed, timing or ordering luck |
| **Fast** | You will run it dozens of times |
| **Written down** | Someone else must be able to run it, including you tomorrow |

## 2. Getting there

1. **Start from the report**, exactly as given. Do not clean it up yet — the "irrelevant" step is
   sometimes the cause.
2. **Confirm it happens for you.** If not, that difference is the first clue (§5).
3. **Remove one thing at a time**, re-testing after each. Systems, steps, data, options.
4. **Stop when removing anything makes it disappear.** That is the minimal repro, and its boundary is
   already telling you where the cause is.
5. **Write it down** — steps, data, environment, and what "wrong" looks like versus expected.

**Do not skip step 3 because you think you know the cause.** A minimal repro that contradicts your
theory is the cheapest possible correction.

## 3. Making it deterministic

Sources of luck, in the order they cause trouble:

| Source | Fix |
|---|---|
| Unseeded randomness | Fix the seed; record it in the repro |
| Variable timestep | Fixed step; drive frames explicitly |
| Wall-clock time | Inject time |
| Unstable iteration order | Sort before iterating, or use an ordered collection |
| Uninitialised or leftover state | Start from a known state every run |
| Concurrency / async ordering | Force the ordering, or the bug is *about* the ordering |

Same rules as `midcore-testing/references/determinism-and-golden.md` §1. Making the repro
deterministic often **is** the fix — if determinism removes the bug, non-determinism was the cause.

## 4. Intermittent bugs

"Sometimes" is data. Treat the rate as a measurement:

1. **Quantify it.** Run 50 times, count. 2/50 and 45/50 are different problems with different
   likely causes.
2. **Look for what varies.** Timing, ordering, memory pressure, machine load, which run is first.
3. **Try to make it worse.** Deliberately amplify the suspected factor — more entities, slower
   frames, artificial delay. **A bug you can make happen every time is a solved reproduction.**
4. **Never "fix" an intermittent bug by testing once.** With a 1-in-20 bug, one clean run is 95%
   likely regardless of whether you changed anything. Re-run at least as many times as it took to
   see it originally.

Point 4 is the most common false victory in debugging.

## 5. Bugs that only happen elsewhere

Another machine, another device, a build but not the editor, one player but not others.

**List the differences before theorising.** Actually list them — hardware, OS, build configuration,
data, settings, locale, permissions, timing, install history, how long the process has run.

Then bisect **the environment** rather than the code (`narrowing.md` §2). The cause is in that list,
and the list is finite.

If it only reproduces in a build, check the routing table in `SKILL.md` first — stripping,
conditional compilation, and packaging account for most build-only failures and are already
documented elsewhere.

## 6. When it genuinely cannot be reproduced

It happens: a one-off crash from a real player, with a log and nothing else.

Then be explicit about what you are doing:

- **Read the evidence you have** thoroughly (`evidence.md`) — often it is more than it seems.
- **Form a mechanism from the evidence**, and say plainly that it is unconfirmed.
- **Add observability** — logging, a counter, an assertion — so the *next* occurrence is diagnosable.
  Shipping better evidence is a legitimate and often correct outcome.
- **If you change code, say the change is speculative** and state what would confirm it.

What not to do: apply a plausible fix and call the bug closed. It will reappear, the earlier fix will
be assumed to have worked, and it will now be much harder to investigate.

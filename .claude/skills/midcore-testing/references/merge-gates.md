# What must pass, and reporting honestly

## 1. Gates by change type

Not every change needs every tier. Match the gate to what could break.

| Change | Required | Also worth running |
|---|---|---|
| Content rows only | Content validation | — |
| Content **schema** | Content validation + tests reading that table | Logic tests for systems using it |
| **Persisted type** | **Full migration matrix** — every shipped version | Content validation |
| Game rules / simulation | Logic tests; golden run if one exists | Content validation |
| Hot path | Benchmark before/after; logic tests unchanged | — |
| Assembly graph / references | Full compile, then the whole suite | Graph audit — `midcore-assembly-architecture` |
| Wiring / composition | Smoke boot | Targeted play-session test |
| UI / view | Run it and look | — |
| Build configuration | Full gate list — `midcore-release-pipeline` | — |

The **persisted type** row has no discretion. There is no version of "the migration tests were slow
so I skipped them" that ends well.

## 2. The zero-tests-executed trap

> A green run with zero tests executed is a failed run, not a pass.

Ways a suite reports success while testing nothing:

- a filter matching no test names — a typo in a namespace is enough;
- a test assembly that failed to compile and was skipped;
- a suite excluded by configuration;
- a category or platform filter excluding everything;
- tests marked ignored and silently counted as not-failing.

**Always read the executed count.** If it is zero, or lower than last time, that is the finding —
report it as a failure, not as a pass with a note.

## 3. Reporting honestly

When reporting that something is verified, state:

1. **What ran** — which suites, which filter.
2. **The counts** — executed, passed, failed, skipped. Numbers, not "all green".
3. **What did not run**, and why. Explicitly, by name.
4. **What was verified by other means** — "checked by running the app" is a legitimate verification
   for view code, as long as it is stated as that rather than implied to be a test.

Examples:

```
GOOD  EditMode suite, filter `Content` and `Migration`: 84 executed, 84 passed.
      Play-mode suite not run — the project has none.
      UI change verified by running the app; no automated coverage for view code.

BAD   Tests pass.

BAD   Everything's green.        ← green what? how many? which suite?
```

The instruction that matters: **if a step was skipped, say so.** A verification report that omits
what was not done is not a report, it is a claim.

## 4. When a test fails

In order:

1. **Read the failure.** Not the summary — the actual assertion, the actual values.
2. **Decide: real regression, or intended behaviour change?** This is the fork that determines
   everything after it.
3. **Real regression** → fix the code. Never the test.
4. **Intended change** → update the test *deliberately*, and say in the change description that a
   test's expectations were updated and why.
5. **Cannot tell** → that is information. Investigate rather than picking whichever resolution is
   faster.

The failure to avoid: adjusting an assertion until it passes. It is always available, always fast,
and it silently converts a test into a record of current behaviour rather than intended behaviour.

## 5. Adding a test after a bug

Every bug that reached a play session or a player is a missing test with a known location.

1. Write the test **first**, and watch it fail on the unfixed code. A test that has never failed for
   the right reason is not known to detect anything.
2. Fix the bug; confirm the test now passes.
3. Put it at the **lowest tier that catches it**. Most bugs that feel like integration failures are
   content-integrity or logic bugs one tier down, where the test is faster and more stable.

For a content bug, this means a new invariant in the content suite
(`data-and-balance-tests.md` §2) — which is how that suite grows into something specific to the
project rather than generic.

## 6. Keeping the suite fast enough to be run

A suite people avoid running protects nothing.

- Content validation and migration tests must stay in **seconds**. They run on every change.
- Keep the play-session tier in single digits.
- Fix or delete flaky tests immediately (`determinism-and-golden.md` §3).
- If the suite is too slow, **split by tier** — always run the fast tiers, run the slow ones on the
  release gate. Never respond to slowness by deleting coverage or by making a tier optional.

The goal: the tiers that catch player-visible failures are cheap enough that skipping them is never
tempting.

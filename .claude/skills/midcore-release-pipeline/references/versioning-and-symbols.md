# Versioning and symbols

## 1. Two numbers, two jobs

| | Display version | Build number |
|---|---|---|
| Audience | players, store listing | engineering, crash reports |
| Shape | human-meaningful, follows the release cadence | monotonic integer |
| Reused? | never | **never**, under any circumstance |
| Job | "which release is this" | "which exact build is this" |

The build number is the one that matters for diagnosis. Two builds sharing a number are
indistinguishable in every crash report from either — the information is simply gone.

**Increment on every build that leaves the machine**, including internal and QA builds. A build
number reserved only for store releases cannot identify the QA build that crashed.

## 2. Stamp the commit into the build

The single highest-value line in the whole pipeline: record the commit hash **inside the build**,
readable at runtime and attached to crash reports.

Without it, connecting a crash to source means correlating timestamps against pipeline history — a
process that is unreliable and stops working when the pipeline history is pruned.

Also worth stamping: build number, build configuration, and the content version if it can vary
independently. Surface them somewhere reachable in-game — a debug screen, a settings footer — so a
tester can read them off a device without any tooling.

## 3. Hotfix versioning

Decide the scheme **before the first hotfix**, because during one nobody has time to design it.

Requirements:

- a hotfix is visibly a hotfix of a specific release, not a new release;
- it can be produced from the released commit plus a minimal change — which requires that released
  commit to be tagged and findable;
- the build number still increases monotonically;
- the store accepts it as an update to what is live.

The practical prerequisite: **tag every release commit**. A hotfix that cannot start from the exact
released source is a new release, with all the risk that implies, at the worst possible moment.

## 4. Symbols

> An unsymbolicated crash from a shipped build is unactionable.

Native and ahead-of-time-compiled builds produce stack traces as addresses. Turning those into
functions and lines requires the symbol files generated **by that specific build**. A rebuild from
the same commit produces different addresses — symbols are not reproducible.

Rules:

1. **Archive symbols for every build that leaves the machine**, keyed by build number.
2. **Keep them longer than any player is on that build.** Longer than feels necessary.
3. **Verify symbolication end-to-end before shipping** — take a real crash from a real build and
   confirm it resolves. Discovering the archive is broken during an incident is the standard way this
   fails.
4. **Losing symbols is release-blocking.** Not a warning.
5. **Upload to the crash reporting service as part of the pipeline**, not manually. A manual step is
   skipped exactly once, and it is always the build that crashes.

## 5. Stripping

Aggressive code stripping removes code nothing statically references — which is fine until something
references it **dynamically**: reflection, serialization, dependency injection, or type lookup by
name.

The result is a failure that appears **only in release builds**, often only on one platform, and
often not at startup but at the moment the stripped path is first taken.

- **Test the release configuration**, not just development. A development build proves nothing about
  a stripped one.
- **Preserve what must survive**, explicitly, using whatever mechanism the toolchain provides. Keep
  the preserve list under review — it grows, and stale entries hide real problems.
- **When a release-only failure appears, suspect stripping first.** It is the most common cause of
  "works in the editor, fails in the build".
- **Changing the stripping level is a re-test, not a setting change.** It alters what code exists.

## 6. Recording what shipped

Per release, keep a durable record:

| Field | Why |
|---|---|
| Display version + build number | Identity |
| Commit hash | The link to source |
| Content version | Content can move independently |
| **Save schema versions** | Feeds the migration matrix — `midcore-save-migration` |
| Platforms and stores | What went where |
| Gate results | Evidence the release was legitimate |
| Known issues | So the next incident is not re-diagnosed |

The save schema row is the one that quietly matters most. `save.shipped_versions` in the profile must
be updated at release time — it is the input to every future migration's test matrix, and if it is
wrong, migrations are tested against a shorter history than actually exists in the wild.

## 7. Release checklist

- [ ] Build number incremented, never reused
- [ ] Commit hash stamped into the build and readable at runtime
- [ ] Release commit tagged
- [ ] All blocking gates passed, with artifacts — `ci-gates.md`
- [ ] Symbols archived and uploaded, symbolication verified against a real crash
- [ ] Release configuration tested on a real device, not just development
- [ ] `save.shipped_versions` updated in the profile
- [ ] Release record written
- [ ] Any skipped gate named explicitly in the release report

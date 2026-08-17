---
name: debugging
description: REQUIRED when investigating why something is broken — a crash, a wrong value, a hang, an intermittent failure, or behaviour that does not match expectation. Covers reproducing reliably before changing anything, narrowing by bisection across code, data, time and configuration, reading evidence instead of guessing, and the debugging anti-patterns that produce changes nobody can explain. The rule underneath everything is never fix a bug you cannot explain — a symptom that disappears without a known mechanism is a coincidence that has been committed, not a fix. Load it when investigating a failure and before changing any code in response to one. Routes engine- and build-specific symptoms to the skills that already document them. Triggers on "bug", "crash", "broken", "why does", "not working", "unexpected", "wrong value", "hang", "freeze", "deadlock", "intermittent", "flaky", "reproduce", "repro", "investigate", "root cause", "stack trace", "exception", and Vietnamese phrasing "sao nó lỗi", "bị crash", "không chạy", "sai kết quả", "treo", "lỗi lúc có lúc không", "tìm nguyên nhân", "debug giúp".
---

# Debugging

Read `.claude/project-profile.md` in the current repo before proposing anything. It pins the
technology, conventions and authority skills this file deliberately does not name.
If the profile is missing, say so and ask — do not guess, and do not assume a specific engine
package or standard library.

## The one rule

> **Never fix a bug you cannot explain.**

A change that makes a symptom disappear without a known mechanism is a coincidence that has been
committed. The bug is still there, it is now harder to find, and it will return somewhere else —
usually in a form that no longer resembles the original report.

Two direct consequences:

- **Reproduce before changing anything.** A bug you cannot reproduce reliably cannot be confirmed
  fixed — you can only confirm it did not happen this time.
- **State the mechanism before the fix.** One sentence: *"X happens because Y, when Z."* If you
  cannot write it, you are not ready to change code.

## The order

```
1. REPRODUCE     reliably, minimally, deterministically
2. NARROW        bisect — code, data, time, configuration
3. EXPLAIN       state the mechanism in one sentence
4. FIX           the mechanism, not the symptom
5. PROVE         the repro now passes, and a test locks it in
```

Steps get skipped under pressure, and it is always 1 and 3. Skipping 1 means you never know it is
fixed; skipping 3 means you never know what you did.

## Before touching the code

| Question | If the answer is unclear |
|---|---|
| What exactly is the wrong behaviour, versus expected? | Get a concrete observation, not "it's broken" |
| Can I reproduce it? | `references/reproduce.md` — this is the whole job until it is yes |
| When did it last work? | A working version bounds the search — `references/narrowing.md` |
| What changed? | Code, data, configuration, environment — all four are suspects |
| Is it one bug or several? | Two symptoms with one cause is common; so is the reverse |

## Symptoms that belong to another skill

Do not re-derive these — they are documented, and a second explanation will drift from the first:

| Symptom | Go to |
|---|---|
| A generated member does not exist; the attribute "did nothing" | `unity_operations` authority — source generators only run in a real compile |
| A guard is silently false; gated code vanished with no error | `midcore-assembly-architecture/references/feature-flags.md` §2 |
| Works in the editor, fails in a build; stripping; unreadable crash | `midcore-release-pipeline/references/versioning-and-symbols.md` |
| A test passes sometimes | `midcore-testing/references/determinism-and-golden.md` §3 |
| Slow, not wrong | `midcore-perf-budget` — a different discipline |
| Save will not load, or loaded wrong | `midcore-save-migration/references/safety-and-recovery.md` |
| Content reference missing, dangling id | `midcore-data-pipeline/references/validation.md` |

Checking this table early is often the whole investigation. These are the highest-frequency failures
in this kind of project, and each has a known mechanism already written down.

## After the fix

1. **The original reproduction now passes** — the same one, not a variant.
2. **Write the mechanism down** where the next person will find it — the commit message at minimum.
3. **Add a test at the lowest tier that catches it** (`midcore-testing`). Watch it fail on the
   unfixed code first; a test never observed failing is not known to detect anything.
4. **Ask whether the same mechanism exists elsewhere.** Most bugs have siblings.

## This skill does not own

| Question | Owner |
|---|---|
| Finding bugs that have **not** shown up yet, reviewing a diff | `code-review` (built-in) |
| Changing structure once the cause is known | `refactoring` |
| Designing the replacement when the cause is a design flaw | `system-design` |
| Running the editor, tests, or reading a build log | the `unity_operations` authority |
| Which tests to add and what counts as verified | `midcore-testing` |

## References

- `references/reproduce.md` — making a bug reliable, minimal and deterministic; intermittent bugs;
  bugs that only happen somewhere else.
- `references/narrowing.md` — bisection on four axes, the hypothesis loop, and why a test that
  eliminates beats a test that confirms.
- `references/evidence.md` — reading stacks and logs correctly, what the tools misreport, and
  instrumenting on purpose instead of scattering logs.
- `references/anti-patterns.md` — shotgun debugging, symptom fixes, "it works now", blaming the
  tools, and how to notice you are doing one.

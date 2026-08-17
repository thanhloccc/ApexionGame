# Anti-patterns

Ordered by how much they cost. Each has a **tell** — the thought you notice yourself having.

---

## 1. Shotgun debugging

Changing several things at once and re-testing.

**Tell:** *"Let me try a few things."*

**Why it costs:** if it works, you do not know which change did it — so you keep all of them,
including the ones that did nothing and the one that broke something else. If it does not work, you
have learned nothing and the code has drifted.

**Instead:** one change, one test, revert if it did not help. Slower per step, far faster to the
answer.

**The scattered-logging variant:** twenty log lines at once produces a wall of output nobody reads.
Place one at a boundary and bisect (`narrowing.md` §4).

---

## 2. Fixing the symptom

Suppressing the visible failure without addressing the mechanism.

**Tell:** *"Just add a null check here."*

**Why it costs:** the null is a **fact about your program** — something upstream produced it, and
that something is still doing so. The guard converts a loud failure into a silent wrong behaviour,
which is strictly worse. Now the bug manifests further away, with the original signal removed.

**Instead:** ask why it is null. If a null is legitimate, then handling it is the fix, and you can
say so. If it is not, the bug is upstream.

Same shape: clamping a value that should never be out of range; catching an exception to make it go
away; retrying until it succeeds.

---

## 3. "It works now"

The symptom stopped and nobody knows why.

**Tell:** *"Huh — it's fine now."*

**Why it costs:** nothing was fixed. Either it was intermittent and you got a lucky run
(`reproduce.md` §4), or something incidental masked it. It will return, and the earlier
investigation will be assumed to have concluded.

**Instead:** if you cannot explain the change in behaviour, **you have not finished**. Re-run enough
times to beat the original failure rate. If it genuinely cannot be explained, say so explicitly
rather than closing it.

This is the one rule of the skill, restated: **never fix a bug you cannot explain.**

---

## 4. Blaming the tools

Concluding the compiler, engine, library or hardware is at fault.

**Tell:** *"This must be a bug in X."*

**Why it costs:** it ends the investigation. It is occasionally true, and it is usually the last
hypothesis for a reason — your code is newer, less used, and less tested than the platform.

**Instead:** eliminate your own code first. If you still believe it, produce a **minimal reproduction
against the tool alone** — which is exactly what you would need to report it anyway, and which
usually reveals the real cause partway through.

**The exception worth respecting:** known platform behaviours that are documented and expected —
generated code needing a real compile, defines not propagating, stripping removing reflected code.
Those are in `SKILL.md`'s routing table. Checking that table is not blaming the tools; it is
recognising a known mechanism.

---

## 5. Debugging by guessing

Forming a theory from intuition and changing code to test it, without observing anything.

**Tell:** *"It's probably the cache."*

**Why it costs:** a guess costs a code change plus a test cycle. An observation usually costs one log
line. And guesses are anchored on what you last worked on rather than on evidence.

**Instead:** turn the guess into a prediction and observe it (`narrowing.md` §1). Guessing is a fine
way to *generate* hypotheses; it is a bad way to *test* them.

---

## 6. Reading instead of observing

Staring at code, re-reading, expecting the bug to become visible.

**Tell:** twenty minutes on the same function.

**Why it costs:** you read what you meant, not what you wrote — and the more familiar the code, the
stronger the effect. This is why explaining it to someone else works so reliably.

**Instead:** after two passes with nothing found, stop and add an observation. The code says what it
should do; only an observation says what it does.

---

## 7. Fixing it in the wrong place

Locating the cause correctly, then patching where it is convenient rather than where it belongs.

**Tell:** *"Easier to handle it in the caller."*

**Why it costs:** every other caller still has the bug, and now there is a special case that looks
arbitrary to the next reader.

**Instead:** fix it at the level where the invariant belongs. If that is expensive, say so and record
the compromise — a comment with the reason, so the next person is not left guessing
(`refactoring/references/when-not-to.md`).

---

## 8. Treating a negative grep as proof of absence

Searching, finding nothing, and concluding the thing does not exist.

**Tell:** *"I grepped for it and it's not there, so it doesn't exist."*

**Why it costs:** a search proves only that **your pattern** did not match **the paths you searched**.
It says nothing about generated code, compiled assemblies, package internals, files with a different
name, or a folder you did not include. Acting on it produces confident, wrong statements — and the
correction usually arrives from a compiler or a colleague, expensively.

Three real shapes of this, all from one session:

| Concluded | Reality | Why the grep lied |
|---|---|---|
| "the tooling package is not installed" | It was | The package is not listed in the manifest file that was searched |
| "the generator does not emit `ToFixedString`" | It does | Generated code is not on disk; the folder searched held only attributes |
| "this attribute adds a write API" | It adds nothing | Never verified at all — assumed from the attribute's name |

**Instead:** ask the thing that knows.

- Does a type/member exist? **Ask the compiler or the runtime** — a deliberate reference, a reflection
  dump, a one-line evaluation against the live domain.
- Is a package/tool present? **Ask the tool**, not a file that is supposed to list it.
- Does an attribute do X? **Compile something that depends on X**, or introspect the result.

State the evidence with the claim: *"reflection over the built type shows no setters"* is checkable.
*"I didn't find it"* is not.

---

## Self-check

If any of these is true, stop:

- [ ] I have changed more than one thing since the last test
- [ ] I cannot state the mechanism in one sentence
- [ ] I have not reproduced it since I started changing code
- [ ] I am about to add a guard without knowing why the condition occurs
- [ ] The symptom went away and I do not know why
- [ ] I have been reading the same code for twenty minutes
- [ ] My current theory is "the platform is broken" and I have not tried to reproduce it in isolation
- [ ] I am about to say something does not exist because a search did not find it

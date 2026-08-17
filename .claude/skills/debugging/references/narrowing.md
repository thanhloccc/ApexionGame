# Narrowing

Once the bug is reproducible, the job is halving the search space until one thing is left.

## 1. The hypothesis loop

```
1. STATE     what might be wrong — specific enough to be false
2. PREDICT   what you would observe if it were true, AND if it were not
3. TEST      make the observation
4. KILL      eliminate the hypothesis, or narrow it
```

**Step 2 is what makes it work.** A prediction that only says what you would see if you are *right*
cannot eliminate anything — everything looks like confirmation. A hypothesis that cannot be wrong is
not a hypothesis.

> **One test that eliminates half the space beats ten that confirm what you already believe.**

Write hypotheses down when there is more than one. Under pressure, people re-test the same idea in
slightly different ways and mistake repetition for progress.

## 2. The four axes

Bisect on whichever axis has a known-good and known-bad end.

### Code — history

Known-good version, known-bad version, halve. If the history is clean this is nearly mechanical and
mercilessly effective.

Requirements: a reliable repro (`reproduce.md`), and a check that is fast enough to run at every
step. If the check is slow, invest in making it fast first — you will run it a dozen times.

The trap: a commit range where the code did not build or the bug was masked by another. Note those
and step over them explicitly.

### Data — content and input

Halve the data. Half the rows, half the entities, half the file.

This is the fastest axis when the bug is data-dependent, and it is under-used because people think
of bugs as living in code. A crash that survives with one row of content has a one-row repro.

### Time — when in the run

Boot, first frame, after N minutes, on a transition, after a save/load cycle. Narrow the *moment*,
not the code.

Especially effective for state corruption: find the last moment the state is right and the first
moment it is wrong, and the cause is between them.

### Configuration — build and environment

Build configuration, defines, platform, settings, locale, permissions. `reproduce.md` §5 produces the
list; bisect it the same way.

## 3. Narrowing inside the code

Once the region is known:

1. **Verify the input.** More bugs than expected are correct code receiving wrong data. Check the
   inputs before reading the logic.
2. **Verify the output at the boundary.** Wrong here, right there → the cause is between.
3. **Check the state the code depends on**, not just its parameters — statics, singletons, whatever
   was set up earlier.
4. **Check counts and ordering**, not only values. "Called twice" and "called in the wrong order"
   produce correct-looking values at every individual step.
5. **Read the code as written, not as intended.** The most expensive minutes in debugging are spent
   reading what you meant. If it is not yielding, read it aloud, or explain it to someone.

## 4. Binary search inside a function

For a long function or a long frame, the crude method works and is often fastest:

- assert or log at the midpoint;
- state right at the midpoint → the cause is after it;
- state wrong → before.

Three or four iterations locate almost anything. This beats reasoning about the whole function, and
it beats scattering logs everywhere (`anti-patterns.md` §1).

## 5. Two bugs, or one

Watch for the signs:

- a fix improves things but does not fix them → probably two;
- the symptom changes shape rather than disappearing → probably two;
- two reports that seem unrelated share a repro step → probably one.

**When you suspect two, separate them before continuing.** Debugging two interacting bugs at once is
where hypotheses stop being falsifiable — every test result can be explained by "the other one".

## 6. Knowing when to stop narrowing

Stop when you can write the mechanism in one sentence: *"X happens because Y, when Z."*

Not before — a located line is not a mechanism, and fixing a located line without the mechanism is
how symptom fixes get made (`anti-patterns.md` §2).

Not much after, either. Once the mechanism is known, further investigation is curiosity; note what is
still unexplained and move to the fix.

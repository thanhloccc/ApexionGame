# Verification

## 1. The check that catches most mistakes

> **If the tests had to change, it was not a refactor.**

A structural change is behaviour-preserving by definition, so the observable behaviour the tests
assert on is identical. A test that needs editing means one of three things:

| The test changed because… | Then |
|---|---|
| Behaviour actually changed | This was a behavioural change wearing a refactor's name. Split it |
| The test asserted on internal structure | The test was coupled to implementation — a real finding. Fix the test **first**, separately, then refactor |
| The test's setup had to change | Acceptable *if* the assertions did not. Setup follows structure; assertions follow behaviour |

The third row is the legitimate case, and it is narrow: constructing the object differently is fine;
expecting a different result is not.

## 2. When there is no safety net

Most code worth refactoring has no tests — that is often *why* it is hard to change.

**Write a characterisation test first.** Not a test of what the code *should* do — a test of what it
*does*, today, bugs included.

1. Call the code with representative inputs.
2. Record what comes out. If it is wrong, **record the wrong value anyway**.
3. Assert on exactly that.
4. Refactor. The test must still pass, **unchanged**.
5. Fix the bug afterwards, as a separate behavioural change, updating the test then.

The point is not correctness. It is a **tripwire**: it fails the moment the refactor changes
something, which is the only thing you need to know while restructuring.

**Do not fix bugs you find while writing characterisation tests.** Record them, keep going
(`scope-control.md` §2).

## 3. What "behaviour preserved" actually covers

Behaviour is more than return values. A refactor can preserve outputs and still break something:

| Also behaviour | How it breaks |
|---|---|
| **Side effects** | The extracted method is now called twice, or in a different order |
| **Call counts** | An inlined method with a side effect now runs per loop iteration |
| **Ordering** | Two operations that used to be sequential now are not, or vice versa |
| **Exceptions** | A different exception type, or thrown at a different point |
| **Timing / frame boundaries** | Work moved across a frame or an await |
| **Lifetime** | Something now lives longer or shorter — created later, disposed earlier |
| **Allocation** | Usually fine, but not if a hot path is involved — `midcore-perf-budget` |

Return-value tests catch none of the first six. When a refactor touches any of them, the safety net
needs to assert on them specifically — a recorded sequence of calls, a lifecycle log, a golden run.

## 4. Reviewing a structural diff

A structural diff should be **boring**. Read it looking for the lines that are not:

1. **Scan for non-mechanical lines.** In a rename touching 200 sites, 199 are identical in shape. The
   one that is not is either a bug or an undeclared behavioural change.
2. **Check every default value** introduced by a forwarding overload — `safe-sequences.md` → change
   a signature.
3. **Check the direction of every new reference.** Moves are how upward dependencies get created.
4. **Check nothing was deleted quietly.** A member that vanished with its consumers is fine; one that
   vanished because it "looked unused" is a bet on the compiler seeing every use — and reflection,
   serialization and generated code all defeat that.
5. **Check the tests are unchanged.** Back to §1.

## 5. When behaviour cannot be pinned down

Sometimes current behaviour is genuinely unobservable — it depends on timing, on a real device, on
state you cannot reconstruct.

Then say so, and pick one:

- **Narrow the refactor** to a part whose behaviour *is* observable.
- **Build the observability first** — logging, a recorded trace — as its own change, then refactor.
- **Do not refactor.** Restructuring code whose behaviour you cannot verify is rewriting, and it
  should be called that so it gets the scrutiny a rewrite deserves.

Naming the third option matters. The failure mode is doing a rewrite while calling it a refactor,
which borrows the safety implication of the word without the safety.

## 6. Before calling a refactor done

- [ ] The tree compiled after every step, not only at the end
- [ ] The safety net passed after every step
- [ ] **The tests were not edited** — or the edits were setup-only, and that is stated
- [ ] Side effects, call counts and ordering were checked, not assumed
- [ ] No new reference points the wrong way
- [ ] Nothing was deleted on the assumption it was unused
- [ ] The scope written down at the start is what actually happened
- [ ] Findings noticed along the way were recorded, not fixed

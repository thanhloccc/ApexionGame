# Scope control

A refactor that grows past what can be reviewed gets abandoned, reverted, or merged unreviewed. All
three are worse than not starting.

## 1. Fix the scope before the first edit

Write these down. Three lines is enough, and it is the difference between a refactor and a
wandering.

```
GOAL      what becomes cheaper / impossible / stops being violated
IN        the files and members this touches
OUT       things I will notice and not fix
DONE      the observable condition that ends this refactor
```

**`DONE` is the one people skip**, and it is why refactors do not end. "The code is cleaner" has no
completion condition. "`WeaponSystem` no longer reads from `SaveStore`" does.

## 2. The "while I'm here" trap

Refactoring reveals other problems. That is not a coincidence — you are reading code carefully,
probably for the first time in a while.

**Every one of those is a separate change.** The reasoning that feels compelling in the moment —
*"it's two lines, and I'm already in this file"* — is exactly how a 40-line diff becomes 400 and
stops being reviewable.

Instead:

1. **Write it down** — a note, a list at the bottom of the working notes, an issue.
2. **Keep going** with the original scope.
3. **Decide afterwards**, when the original refactor is merged and the finding can be judged on its
   own merit rather than on proximity.

Most of those notes turn out not to be worth doing. That information is only available once the
urgency of being in the file has passed.

### The exception

If the thing you found **blocks** the refactor — the extraction cannot be done without fixing it —
then it is in scope. Say so explicitly, update the written scope, and if it doubles the size, stop
and reconsider whether the whole thing should be two changes.

## 3. Signals to stop and reassess

| Signal | Means |
|---|---|
| The diff no longer fits in one review sitting | Split it, or revert and re-plan |
| You cannot state what is left to do | Scope was never fixed, or it drifted |
| The tree has not compiled for a while | A sequence step was skipped — `safe-sequences.md` |
| Tests started needing edits | **Not a refactor any more** — `verification.md` |
| You are renaming things to make the diff look coherent | The structure is fighting back |
| A second refactor started inside the first | Finish or revert one |
| It crossed an assembly boundary unplanned | Graph question first — `midcore-assembly-architecture` |

**Reverting is cheap and available.** A refactor abandoned at step 1 of three costs almost nothing.
The same refactor abandoned at 80% costs everything already spent plus the merge conflicts it
accumulated while it sat.

## 4. Size, honestly

| Size | Reviewable? | Do |
|---|---|---|
| One operation, one type | yes | just do it |
| One operation, many call sites | yes if mechanical | do it, commit the mechanical part separately |
| Several operations, one area | borderline | split by operation, one commit each |
| Several operations, several areas | no | do not start; pick one |

**Mechanical and non-mechanical changes go in separate commits.** A rename touching 200 call sites is
reviewable *because* it is uniform. Mix in three hand-written changes and the reviewer must now read
all 200 lines carefully to find them — so they will not.

## 5. Refactoring in the middle of a feature

Common and legitimate: you are building something, and the surrounding code makes it hard.

The order that works:

1. **Stop** the feature work.
2. **Commit or stash** it, so the two are not entangled.
3. Do the refactor **alone**, with its own safety net, and merge it.
4. Return to the feature on top of the refactored code.

The order that does not work is doing both at once, which produces a diff where nobody — including
you, next week — can tell which line was the feature and which was the cleanup. This is the same
rule as the skill's core rule, applied at the change level: **structure or behaviour, not both.**

If stopping is genuinely not possible, then the refactor waits. Note it, finish the feature, do it
after. A refactor deferred by a day is fine; a refactor entangled with a feature is not.

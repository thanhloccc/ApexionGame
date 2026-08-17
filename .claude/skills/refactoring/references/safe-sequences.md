# Safe sequences

One sequence per operation. Each ends with **the trap** — the way that operation loses work silently.

The shape underneath all of them: **add → move consumers → remove.** Three steps, the tree compiling
after each. Doing it in one step is where refactors break.

---

## Rename

1. Add the new name as the real declaration; keep the old one forwarding to it.
2. Move call sites across, compiling as you go.
3. Remove the old name.

For a purely local symbol, the tool's rename is atomic and safe — use it. The three-step form is for
anything crossing a file, assembly, or published surface.

**The trap:** a rename that is really a **meaning change**. If `Update` becomes `Tick` and the timing
also changes, that is two changes. Renaming first makes the behavioural half invisible in the diff,
because the reviewer is busy reading the rename.

---

## Extract method

1. Copy the block into a new method; leave the original in place.
2. Call the new method from the original site; delete the copied block.
3. Compile and run the safety net **before** touching anything else.

**The trap:** captured state. A block that read three locals and mutated one has a real signature —
three parameters and a return, or a `ref`. Guessing it produces a method that compiles and quietly
operates on a copy. Enumerate what the block reads and what it writes before extracting.

---

## Extract type

1. Create the new type with the fields and methods it needs, **copied not moved**.
2. Have the original delegate to it.
3. Move consumers to the new type one at a time.
4. Remove the now-dead members from the original.

**The trap:** extracting a type that shares mutable state with its origin. Now two objects own one
piece of truth, and they will disagree. If the state cannot travel with the type, the seam is in the
wrong place — see `system-design/references/state-ownership.md`.

---

## Inline

1. Confirm every call site can take the body — no name collisions, no different visibility.
2. Inline one call site at a time, compiling after each.
3. Remove the declaration when the last one is gone.

**The trap:** inlining something with a side effect that the call count changes. A method called
twice, inlined into a loop, may now run per iteration. Check what it *does*, not just what it
returns.

---

## Move a member between types

1. Add it to the destination; keep the original forwarding.
2. Move callers.
3. Remove the original.

**The trap:** the move creates a dependency pointing the wrong way. A member that moves "closer to
its data" can force the destination to reference something it should not. Check the direction
*before* step 1 — `midcore-assembly-architecture/references/dependency-rules.md`.

---

## Split one type into two

1. **Name both halves.** If one has no name, stop — the split is not understood yet.
2. Assign **every** field and method to exactly one half, on paper, before editing.
3. Create the second type; copy its members; original delegates.
4. Move consumers, one at a time.
5. Delete the moved members from the original.

**The trap:** a field both halves need. It belongs to one of them — decide which, and let the other
ask. Duplicating it creates two sources of truth; leaving it in a shared "base" usually just renames
the problem.

---

## Merge two types into one

1. Verify nothing uses one without the other. If something does, the boundary is load-bearing —
   merging forces that consumer to take the whole thing.
2. Move members across, keeping both compiling.
3. Redirect consumers.
4. Remove the emptied type.

**The trap:** merging to escape a circular dependency. It hides the cycle instead of removing it,
and the merged type now has two reasons to change —
`midcore-assembly-architecture/references/dependency-rules.md` §5.

---

## Change a signature

1. Add the new overload; the old one forwards to it with the old defaults.
2. Move callers, one at a time — **this is where the real work is**, because each caller has to
   supply the new argument correctly.
3. Remove the old overload.

**The trap:** the forwarding overload supplies a default that is *safe* but not *correct* for every
existing caller. Every migrated call site must be looked at individually. If there are too many to
look at individually, that is a signal the signature change is bigger than it looked.

---

## Replace an implementation behind an interface

1. Write the new implementation alongside the old.
2. Put both behind the interface if they are not already.
3. Switch consumers over, ideally one at a time or behind a toggle.
4. Run the safety net against **both** implementations — the same tests must pass for each.
5. Remove the old one.

**The trap:** the interface was shaped around the old implementation, so the new one contorts to fit.
If the new implementation needs a different interface, that is a design change first
(`system-design`), and the replacement comes after.

---

## Rules that apply to every sequence

- **Compile after each step.** The first error in a big move is informative; the hundredth is noise.
- **Run the safety net after each step**, not only at the end.
- **Never leave the tree uncompilable across a break.** Interruptions happen; the way back must be
  short.
- **Commit at step boundaries.** A three-step refactor is three commits, each reviewable, each
  bisectable.
- **Move files with their metadata.** In engines that track assets by side-car files, a move that
  loses the side-car breaks every reference and is not recoverable by re-import.
- **If the tests had to change, stop** — this was not a refactor (`verification.md`).

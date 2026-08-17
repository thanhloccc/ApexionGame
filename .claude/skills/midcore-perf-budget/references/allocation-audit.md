# Allocation audit

## 1. When allocation pressure actually matters

Managed allocation costs nothing directly. The cost arrives later, as collection, and it arrives as a
**hitch** — which is why it matters disproportionately in games.

| Situation | Matters? |
|---|---|
| Per-frame, sustained, during gameplay | **Yes** — this is the whole problem |
| Per-entity per-frame with many entities | **Yes** — multiplies fast |
| On a rare event — level load, menu open | Rarely |
| At startup | Almost never |
| In editor-only or debug code | No |

So the target is narrow: **allocations in code that runs every frame while the player is playing.**
Everything else is usually a poor use of time, and chasing startup allocations is a classic way to
spend a week for no measurable gain.

## 2. Finding the real ones

1. **Measure allocation per frame** in a steady-state gameplay scenario. If it is already zero or
   near-zero in the hot loop, stop — there is nothing here.
2. **Identify the largest contributors**, not all of them. The distribution is usually very
   uneven: a few sites dominate.
3. **Confirm the call site is actually per-frame.** A large allocation on a path that runs twice per
   level is not a per-frame allocation, however large.
4. **Fix the top contributor. Measure again.** Do not fix ten at once — you will not know which
   mattered, and some "fixes" add cost.

## 3. Usual sources

Ordered by how often they turn out to be the real cause:

| Source | Why it allocates |
|---|---|
| **Closures capturing state** | Each capture is an object; inside a per-frame loop this is per-frame garbage |
| **Boxing** | A value type crossing an object-typed boundary — an interface, a params array, a format call |
| **String building** | Concatenation, interpolation, number formatting. Debug and UI text are the usual offenders |
| **Query/sequence chains** | Allocate enumerators and intermediates per call |
| **Temporary collections** | A new list per frame to hold a few items |
| **Enumerating some collection types by interface** | Boxes the enumerator, silently |
| **Delegates and event subscriptions in a loop** | Re-created rather than cached |
| **Logging that formats even when disabled** | The format cost is paid before the check |

That last one is worth checking early — it is common, invisible in code review, and free to fix.

## 4. Confirming a fix is real

An allocation "removed" by reasoning is not removed. After each change:

- **Re-measure allocation per frame** in the same scenario. The number moved, or it did not.
- **Check the frame time too.** Removing an allocation can add work — a pooled path with bookkeeping
  may cost more than the allocation did.
- **Watch for cost moving rather than disappearing.** A rented buffer that is never returned is a
  leak; a cache that grows unboundedly is worse than the allocation it replaced.

If the allocation count did not move, the change did not do what was believed. Revert it — an
optimisation that does not optimise is complexity with no payment.

## 5. Which types to prefer

**Not this skill's decision.** The project's default collection, buffer and math types are a standing
convention owned by the `structure_naming` authority — check the profile.

This skill covers **whether an allocation is real, and whether removing it changed anything.** Do not
duplicate type guidance here; a second, drifting copy of it is worse than none.

## 6. Pooling — when it earns its cost

Pooling is the usual answer to per-frame allocation, and it is not free: it adds lifetime management,
and lifetime bugs are worse than garbage.

Pool when **all** hold:

- the allocation is per-frame and measured;
- objects are genuinely short-lived and interchangeable;
- the return point is unambiguous — you can say exactly where each object goes back.

Do not pool when:

- the lifetime is unclear or conditional — that is a leak or a use-after-return waiting to happen;
- the object is expensive to reset correctly (stale state is a subtle, hard-to-trace bug class);
- the allocation was not measured to matter.

**A pooled object that is never returned is a leak that looks like an optimisation**, and it is
harder to find than the allocation was.

## 7. Startup and load allocation

Different problem, usually not worth solving:

- Startup allocation affects **load time**, not frame hitches. If load time is inside budget, ignore
  it entirely.
- **Peak memory during load** can matter on a min-spec device — that is a memory problem, not an
  allocation-rate problem, and the fix is different (load less at once, not allocate less).
- Optimising startup allocation for its own sake is one of the most common time sinks in
  performance work. Check it against the load budget before starting.

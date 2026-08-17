# Evidence rules

> **No performance claim without a before-and-after measurement, on a stated device tier, under
> stated conditions.**

## 1. What is not evidence

| Claim | Why it fails |
|---|---|
| "This should be faster" | A hypothesis. A meaningful share of them are wrong |
| "This avoids an allocation" | Maybe. Did allocation-per-frame move? |
| "This is O(n) instead of O(n²)" | Asymptotics say nothing at the actual n. At n=8, the O(n²) often wins |
| "It feels smoother" | Possibly real, but unmeasured and unreproducible |
| "The editor profiler shows it is faster" | Only comparable to another editor measurement — never a device claim |
| "It is a best practice" | Not a measurement of this code in this project |
| "Benchmarks say this pattern is faster" | Someone else's code, hardware and workload |

None of these are dishonest. They are all **unverified**, and the point of the rule is that the
verification is cheap compared to shipping a change that did nothing.

## 2. What is evidence

A pair of measurements, differing in exactly one change, with conditions stated:

```
Device      mid-tier reference, <model>, release build
Scenario    combat scene, 50 agents, 60s sustained, measured from t=120s
Metric      frame time p50 / p99, allocation per frame

Before      12.4 ms / 31.2 ms,  4.1 KB/frame
After        9.8 ms / 11.6 ms,  0 KB/frame
Change      pooled the per-frame target list in the targeting system
```

Note the p99 improving far more than the p50 — that is the allocation hitch disappearing, and it is
invisible in an average.

## 3. Reporting rules

1. **State the device and configuration.** An editor number is an editor number; label it.
2. **State the scenario** in enough detail that someone else could reproduce it.
3. **Give both numbers.** "40% faster" without absolutes hides whether it went 10 ms → 6 ms (real) or
   0.05 ms → 0.03 ms (noise).
4. **Give a distribution, not just a mean.** p99 is closer to what players feel.
5. **Say how many runs.** One sample from a phone is noise.
6. **Name what you did not measure.** Frame time improved — did memory? did load time?

## 4. Legitimate outcomes that are not wins

Three results are as valuable as an improvement, and all three must be reported rather than buried:

**No measurable difference.** The change did not help. **Revert it.** An optimisation that does not
optimise is complexity with no payment — and reporting the null result saves the next person from
repeating the experiment.

**A deliberate regression.** A change that costs performance but buys correctness, maintainability or
a feature. Legitimate — *record it with what it bought*, in a comment at the site, so the next person
does not "fix" it back and reintroduce the bug it solved.

**A moved cost.** Frame time improved because work moved to load time. That is a real trade, and it
is only a win if the load budget can absorb it. Check both.

## 5. Refusing a claim

When asked to accept or make a performance claim without evidence, say so plainly and give the
measurement that would settle it. This applies to your own changes with the same force.

```
The change looks like it should reduce per-frame allocation, but I have not measured it.
To confirm: allocation-per-frame in the combat scenario, before and after, on the mid-tier
device. Without that I cannot say whether it helped — the path may not be hot, or the
compiler may already handle it.
```

This is not pedantry. Unverified optimisations accumulate into a codebase that is harder to read,
harder to change, and no faster — every one of them defended by a plausible reason nobody ever
checked.

## 6. Regression protection

Once something is measured and fixed, protect it:

- **Add a benchmark** for genuinely hot paths — `midcore-testing` tier 4. Assert on **relative**
  budgets or allocation counts, not absolute milliseconds, or it fails on every other machine.
- **Record the measurement** where the next person will find it — a comment at the site, or the
  feature's documentation. A number that lives only in a conversation is lost.
- **Re-measure periodically**, not only when someone complains. A slow drift across many changes is
  invisible per change and obvious over a release.

## 7. Before reporting any performance work

- [ ] Measured before, on a stated device and configuration
- [ ] Changed exactly one thing
- [ ] Measured after, identical conditions, multiple runs
- [ ] Both absolute numbers reported, with a distribution
- [ ] Compared against the budget from the profile — or noted that no budget exists
- [ ] Null and negative results reported, not buried
- [ ] Deliberate regressions recorded with what they bought
- [ ] Regression protection added for anything worth keeping

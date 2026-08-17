# Profiling workflow

## 1. Measure before you look at code

The strongest instinct in optimisation is to read code, spot something inefficient, and fix it. It is
usually wasted work: the inefficient thing is often not hot, and the hot thing is often not where it
looks.

**Profile first. Every time.** Including when you are certain.

## 2. What the editor misreports

Profiling inside a development editor is convenient and systematically misleading:

| The editor adds | Effect |
|---|---|
| Editor-only allocations and checks | Costs that do not exist in a build |
| Domain reload, asset import, inspector refresh | Appears as gameplay cost |
| Deep-profiling instrumentation | Changes the shape of what you measure |
| Development-build safety checks | Inflates hot paths, unevenly |
| Editor and tooling memory | Memory figures that mean nothing for a device |
| Desktop-class CPU and memory bandwidth | Absolute timings unrelated to a phone |

**What the editor is good for:** finding *which system* is expensive, and comparing before against
after **under identical editor conditions**.

**What it is not:** evidence of shipped performance. Any number reported as real-device performance
must come from a real device, in a release-configuration build.

## 3. Measuring on a target device

1. **Release configuration**, not a development build, unless the profiler requires one — and if it
   does, say so when reporting, because the numbers are inflated.
2. **A real reference device** from the profile's tier list.
3. **A repeatable scenario** — same scene, same content, same actions, same duration. Write it down;
   you will need to reproduce it exactly for the "after".
4. **Sustained**, not the first thirty seconds. See `device-tiers-and-budgets.md` §5.
5. **Nothing else running.** Background apps make results irreproducible.
6. **Same device for before and after.** Two devices of the same model are not the same device once
   thermal state and battery level differ.

## 4. Reading a frame

Distinguish two shapes, because they have different causes and different fixes:

| Shape | Looks like | Usually caused by |
|---|---|---|
| **Sustained cost** | every frame is too slow | too much work per frame; an algorithm; too many entities |
| **Spike / hitch** | most frames fine, occasional stall | GC, asset load, instantiation burst, shader compile, a once-per-N-frames path |

**Average frame time hides spikes**, and spikes are what players notice — a steady 33 ms reads as
smooth, while a 16 ms average with a 200 ms stall every few seconds reads as broken.

Report **worst-case and percentiles**, not just the mean. The 99th percentile frame is closer to the
felt experience than the average.

## 5. Isolating a cause

1. **Start at the top.** Which category — simulation, rendering, UI — is over its share? Do not begin
   in a leaf function.
2. **Descend one level at a time**, following the largest cost. The bottleneck is rarely where
   intuition points.
3. **Correlate spikes with events.** A hitch every N seconds is a periodic task; a hitch on entering
   an area is a load.
4. **Check counts before speeds.** "This function is slow" is often "this function is called 40,000
   times". The fix for the second is not to make it faster.
5. **Confirm by removal.** Temporarily disable the suspected system and measure. If the frame time
   does not move, it was not the cause — regardless of how bad the code looked.

Step 5 is the cheapest way to avoid optimising the wrong thing, and it is the one most often skipped.

## 6. Capturing a comparable before/after

The measurement is worthless if conditions differ. Fix and record:

- device, OS version, build configuration;
- scene, content state, entity counts;
- actions performed and their duration;
- elapsed time from launch (thermal state);
- profiler settings — deep profiling on or off, identically.

Change **one thing** between the two. Two changes measured together cannot be attributed, and if the
net is neutral you cannot tell whether both did nothing or one helped and one hurt.

Run each side more than once. A single sample from a phone is noise; three consistent runs are a
measurement.

## 7. Memory

Different discipline from frame time:

- **Peak matters more than average.** The peak is what triggers a termination by the OS.
- **Measure at the worst moment** — the heaviest scene, the fullest inventory, after a long session.
- **Watch for growth over time.** Memory that rises steadily across a session is a leak, and it will
  eventually terminate the app on the min-spec device. A single snapshot cannot show this; sample
  repeatedly across a long run.
- **Separate managed heap from native and asset memory.** They have different causes and different
  fixes, and a total figure hides which is growing.

## 8. When the profiler shows nothing

Sometimes the profile is flat and the game still feels bad. Check:

- **Frame pacing** — inconsistent frame delivery feels worse than a slower steady rate;
- **Input latency** — a separate axis from frame time entirely;
- **Load and transition stalls** — outside the frame loop, so invisible in a frame profile;
- **Thermal throttling** — measured at the wrong moment;
- **A different device tier** — the problem may not exist on the device you are holding.

"Feels bad" is a real report. It just needs to be turned into a measurement of the right thing before
anything gets changed.

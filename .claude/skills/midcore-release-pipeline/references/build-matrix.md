# The build matrix and reproducibility

## 1. What actually varies

List the axes explicitly. Most projects have fewer than they think, and the ones they have are often
undocumented.

| Axis | Typical values | Notes |
|---|---|---|
| Platform | the target platforms from the profile | The main axis |
| Configuration | development / release | Different stripping, different logging, different symbols |
| Content variant | full / demo / regional | Only if the project genuinely has them |
| Store / channel | per storefront | Often differs only in signing and identifiers |
| Architecture | per platform | Usually dictated by the platform, not chosen |

**Keep the matrix small on purpose.** Every combination is a build to run, a set of artifacts to
store, and a configuration that can drift. A matrix that grows past what anyone actually tests is a
set of untested builds with a reassuring pipeline.

The question for each axis: *does anyone install and run this combination?* If no, it is not a build
target — it is a build that exists so a box can be ticked.

## 2. What must be identical across the matrix

| Identical | May differ |
|---|---|
| The commit | Signing identity |
| Content and its version | Platform-specific settings |
| Display version and build number | Architecture, stripping level |
| Game logic and balance | Logging verbosity by configuration |

The rule underneath: **a difference between builds must come from the configuration, never from the
build machine or the order in which builds ran.** If two platform builds from one commit disagree
about balance or content, something is reading state that is not in the repo.

## 3. Reproducibility

> A build is a function of a commit plus a configuration.

Things that break this, in rough order of frequency:

| Break | Symptom |
|---|---|
| **Uncommitted or generated content** | Build differs by machine; content missing on CI |
| **Local paths in configuration** | Works for one person only |
| **Editor state affecting output** | Which scene was open changes the build |
| **A dependency resolved to "latest"** | Two builds from one commit differ |
| **A cache treated as an input** | First clean build fails or differs |
| **Manual pre-build steps** | Forgotten exactly once, at the worst time |

Tests for whether reproducibility actually holds:

1. **Clean checkout in a fresh location builds successfully** with no manual steps.
2. **Two builds from the same commit produce functionally identical artifacts.** Byte-identical is
   unrealistic — timestamps and signing differ — but content, version and behaviour must match.
3. **A different machine produces the same result.**

If any fails, find the input that is not in the repo. That input is why a future crash will be
un-investigable.

## 4. Generated files are not sources

Project and solution files, caches, intermediate libraries, temporary directories, and logs are
outputs. Never edited, never trusted as truth, never relied on by the build.

Check the profile's `release.generated` for the specific set in this project. A pipeline that depends
on a generated file being present is a pipeline that fails on a clean checkout — the exact case CI is
supposed to represent.

## 5. Pre-build steps

Anything that must happen before a build — content import, code generation, asset processing — is
part of the build, not a thing a person remembers.

- **Automate it inside the pipeline.** A documented manual step is an undocumented failure waiting
  for the release where someone is in a hurry.
- **Make it idempotent.** Running it twice must be safe; pipelines retry.
- **Fail loudly.** A pre-build step that silently does nothing produces a build missing content, and
  that is usually discovered by a tester days later.
- **Order them explicitly.** Content import before validation before build. Order that works by
  accident stops working when a step gets faster.

## 6. Artifacts to keep

Per build that leaves the machine, keyed by build number:

| Artifact | Why | Keep for |
|---|---|---|
| The build itself | Reproducing a report on the exact binary | Until the version is retired |
| **Symbol files** | Crashes are unactionable without them, and unreproducible | **Longer than any player is on it** |
| Build log | Diagnosing what the build did | Months |
| Test and validation reports | Evidence the gates passed | Months |
| The commit hash | The link between crash and source | Forever — it is one line |
| Size report | Tracking growth over releases | Forever — it is small |

Symbols are the one where loss is unrecoverable. Everything else can be regenerated from the commit;
symbols cannot, because a rebuild produces different addresses.

## 7. Local builds versus pipeline builds

Developers will build locally, and that is fine. What matters:

- **A local build is never published.** Publication comes from the pipeline, which is the only thing
  that runs the gates and archives the artifacts.
- **A local build is clearly identifiable** — a marker in the build number or version — so a bug
  report from one is not mistaken for a pipeline build.
- **The local and pipeline paths use the same configuration**, so "works locally, fails in CI" is a
  real signal about the environment rather than routine noise.

The failure to avoid: a release cut from someone's machine because the pipeline was broken. It skips
every gate, has no archived symbols, and cannot be reproduced.

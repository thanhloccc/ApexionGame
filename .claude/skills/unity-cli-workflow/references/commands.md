# Unity CLI — the verified command surface

Probed on this machine on 2026-08-15. **Re-probe with `unity --help` if anything below fails** —
the CLI is pre-1.0 and its surface moves between versions.

| Fact | Value |
|---|---|
| `unity` binary | `C:\Users\ADMIN\AppData\Local\Unity\bin\unity.exe` (on `PATH`) |
| CLI version | `1.0.0-beta.3` |
| Project editor version | `6000.3.20f1` (`ProjectSettings/ProjectVersion.txt`) |
| Installed editors | `2022.3.62f3`, `6000.0.75f1`, `6000.3.17f1`, `6000.3.20f1` — the match is installed |
| Test framework | `com.unity.test-framework` `1.6.0` in `Packages/manifest.json` — tests can run |
| Unity Pipeline package | **INSTALLED** — absent from `manifest.json`, so **never judge it from there**; ask the Editor (corrected 2026-08-15) |

Shell is PowerShell: `;` + `if ($?)` instead of `&&`, `2>$null` instead of `2>/dev/null`.

---

## 1. Prerequisites and state

```powershell
unity --version                 # CLI version
unity doctor                    # environment diagnostic — run this first when something is off
unity editors                   # installed editors (-i for interactive)
unity license                   # active licenses on this machine
unity status                    # live state of every connected Editor: port, project, version, PID
unity pipeline list             # Editor instances + their Pipeline package status
```

`unity status` is the correct way to answer *"is the project already open?"* — do not guess, and do
not launch a competing batch process when it reports a live instance on this project.

## 2. Global options worth knowing

| Option | Use |
|---|---|
| `--json` / `--format json` | machine-readable output — prefer when you are going to parse it |
| `--non-interactive` | never prompt; use this whenever you invoke the CLI unattended |
| `--quiet` | suppress informational chatter |
| `--verbose` | full error detail + stack traces when a command fails opaquely |
| `--no-banner` | drop the startup banner |

Default to `--non-interactive` here — the tool has no TTY and a prompt will hang until timeout.

## 3. Running tests — `unity test`

**There is a first-class test command. Do not reach for batch mode + `-executeMethod` for tests.**

```powershell
# targeted EditMode run, results written where you can read them
unity test . --mode EditMode --non-interactive `
    --filter "ApexionGame.HFSM" `
    --output "<scratchpad>/editmode-results.xml"

# PlayMode
unity test . --mode PlayMode --non-interactive --output "<scratchpad>/playmode-results.xml"

# pass raw editor args after --
unity test . --mode EditMode -- -nographics
```

| Option | Meaning |
|---|---|
| `--mode <EditMode\|PlayMode>` | omit and the editor's default platform runs — always pass it explicitly |
| `--filter <pattern>` | matches test names; this is how you keep a run targeted |
| `--output <path>` | NUnit XML report (default `test-results.xml` in the cwd — point it at the scratchpad instead of dirtying the repo) |
| `--timeout <seconds>` | kill the process; **disabled by default**, so a hung PlayMode run blocks forever without it |
| `--editor-version` / `-e` | override the editor; only when the project version is deliberately not being used |
| `--allow-install` | installs the missing editor — do not pass unless the user authorized an install |

**The NUnit XML is the evidence.** Read it, quote the pass/fail counts, and name any suite you did
not run. A zero exit code with zero tests executed is a failed run, not a pass.

Test assemblies in this repo: `ApexionGame.Tests.EditorMode` (`Packages/com.apexion.apexion-game/`),
plus the package's own `EncosyTower.Tests.EditorMode` / `EncosyTower.Tests.PlayMode`.

## 4. Checking compilation

There is no `unity compile`. In order of preference:

1. **`unity test . --mode EditMode --filter "<a namespace that exists>"`** — the run cannot start
   until the project compiles, so a compile error surfaces as a test-run failure with the compiler
   output. This is the cheapest honest compile check.
2. **`unity command recompile` + `recompile_status`** against an already-open Editor — **the fastest
   check in this project**, and it does not fight the Editor for the project lock.
3. **Batch mode** with an explicit log path, then read the log (§7).

Source generators (`[PolyEnumStruct]`, `[Database]`, `[ObservableProperty]`, …) only run inside a
Unity compile. **IDE/Roslyn analysis proves nothing about generated members** — never report a
source-gen feature done without a Unity compile behind it.

## 5. Driving an open Editor — `unity command`

**Pipeline is installed here**, so this is the preferred route whenever an Editor is already open —
it avoids spawning a competing process that would fight for the project lock.

The commands that matter most for a code change:

```powershell
unity command clear_console      --project-path=$p --non-interactive
unity command recompile          --project-path=$p --non-interactive     # works while unfocused
unity command recompile_status   --project-path=$p --non-interactive     # poll until completed
unity command get_console_logs   --project-path=$p --non-interactive --severity Error
unity command run_tests          --project-path=$p --non-interactive --mode EditMode --filter <F>
unity command test_status        --project-path=$p --non-interactive
```

`recompile_status` returns `{"status":"completed","failed":<bool>,"errors":[...]}` — the errors array
is the compiler output, and it is the fastest honest compile check available in this project.

### `eval` — check a generated member without writing a test

`unity command eval --code "<C#>"` runs against the live domain, so it is the cheapest way to prove a
**source-generated** member exists and behaves — a factory, a `ToString()`, a generated constructor.

Two contract details, both learned the hard way:

- **The code must be a full statement**: `return <expr>;`. A bare expression fails with
  `; expected`.
- **Avoid string literals and `|`** in the code when invoking through PowerShell — quotes get eaten
  and `|` truncates the argument. Use `nameof(...)`, enum members and numbers instead.

```powershell
unity command eval --project-path=$p --non-interactive `
  --code 'return default(Game.Gameplay.Items.ItemError).ToString();'
```

This is a **verification** tool, not a substitute for tests: it proves a member exists and works
once, in the editor domain. What gates a change is still the test suite.

**With two Editors open**, `unity list` fails with "Multiple Unity Editor instances"; always pass
`--project-path`.

```powershell
unity command --project-path="<absolute-project-path>"            # no command => lists what is available
unity command <name> --project-path="<abs>" --timeout 120 <args>
unity list                                                        # tools registered by the Pipeline package
```

Read the discovered list, then that command's help, **before** invoking it — Pipeline commands are
extensible and differ by installed package version. One logically scoped mutation at a time; capture
the output; verify the resulting files.

## 6. Opening, running, building

```powershell
unity open .                                          # interactive Editor at the right version
unity run . -- <args>                                 # batch mode, args forwarded to the editor
unity build . --non-interactive                       # spawns the editor in batch mode; see --help
```

Run a build only when the task needs one. Do not implicitly install build-support modules
(`unity install-modules` / `unity modules`) — that is an install, and installs need authorization.

## 7. Batch-mode fallback

Only when nothing above fits. Resolve the matching editor binary from `unity editors`, prefer a
committed or task-local Editor script with a static entry point:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe" `
    -batchmode -quit `
    -projectPath "c:\Users\ADMIN\Documents\ApexionGames\ApexionGame" `
    -executeMethod Namespace.Type.Method `
    -logFile "<scratchpad>\unity-batch.log"
```

- **Always** pass an explicit `-logFile` and read the **complete** log, not the tail — the first
  compiler error is at the top and the interesting one is rarely last.
- Add `-accept-apiupdate` only when API migration is intentionally allowed.
- Add `-nographics` for anything that does not need a display.
- Keep the automation script when it is reusable; delete it when it was a one-off.
- The Editor holds a project lock. If `unity status` shows the project open elsewhere, **stop and
  report the conflict** rather than launching a competing process.

Default Editor log (when `-logFile` was not passed):
`C:\Users\ADMIN\AppData\Local\Unity\Editor\Editor.log`, and the project's own `Logs/` folder.

## 8. What counts as evidence

| Claim | Acceptable evidence |
|---|---|
| "it compiles" | a Unity compile with no errors in the log, or a test run that started |
| "tests pass" | the NUnit XML report, with counts |
| "the build works" | the build report / exit code |
| "the asset imported" | the asset re-read after import, or the Editor log |
| "the Editor opened" | **not evidence of anything** |

Always summarize: the command you ran, its result, and explicitly the validation you did **not** run.

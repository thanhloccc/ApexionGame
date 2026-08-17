# Feature flags, gates, and the silent-guard failure

Compile-time gating is an assembly concern because the gate is declared on the assembly definition,
and because a mis-propagated define fails **silently**.

## 1. The three kinds of gate

| Kind | Granularity | Use for |
|---|---|---|
| **Assembly constraint** | whole assembly compiles or does not | a module that is entirely optional — an integration, a platform backend, a sample set |
| **Conditional compilation guard** | a file or a block | a few members that differ per platform or per optional dependency |
| **Runtime flag** | a branch at run time | anything that must be switchable after the build ships — see `midcore-live-ops` |

Choose the coarsest one that works. An entirely optional module behind an assembly constraint is one
declaration; the same module wrapped file-by-file is dozens of places to get wrong, and the compiler
will not tell you when you miss one.

**Compile-time gates cannot be changed after the build.** If the answer might need to change in
production, it is a runtime flag, not a define.

## 2. The silent-guard failure

The single most expensive mistake in this area, and it produces no error:

> A new assembly does not receive a define symbol. Every guard on that symbol evaluates false. The
> guarded code is compiled out. The project builds clean. The feature simply does nothing.

Nothing in the toolchain reports this. There is no missing reference, no unresolved symbol — the code
is *absent*, which is exactly what a guard is supposed to be able to do.

It is common because define propagation is per-assembly in most ecosystems: symbols derived from
installed packages are declared **on each assembly definition that wants to see them**, and a new
assembly starts with none of them.

### The check

After creating or modifying any gated assembly, prove the symbol resolves **inside that assembly**:

```csharp
#if !MY_EXPECTED_SYMBOL
#error MY_EXPECTED_SYMBOL is not visible in this assembly — the define was not propagated.
#endif
```

Compile once, confirm the error does *not* fire, then delete the block. Ten seconds, and it converts
a silent absence into a loud failure.

Check the profile's `assembly_map.planned.first_file_rule` — several ecosystems require copying a
block of package-derived defines into every new assembly, and omitting it is precisely this failure.

## 3. Propagation rules

1. **Symbols derived from optional packages are per-assembly.** Copy the declaration block from a
   reference assembly that already works. Do not hand-write it from memory; the list grows.
2. **Global/player-level symbols apply everywhere but are easy to lose per platform.** A symbol set
   for one build target and not another produces "works on my machine" in its purest form. When
   reading a symbol list, note which target it belongs to.
3. **Stale symbols outlive their packages.** A symbol left in the player settings after its package
   was removed is worse than useless — it reads as evidence the feature is on. Treat the *manifest*
   as the source of truth for what is installed, never the symbol list.
4. **Constraints and guards must agree.** An assembly constrained to a symbol whose files also guard
   on it is redundant but harmless. The reverse — files guarding on a symbol the assembly is not
   constrained to — is where partial compilation bugs live.

## 4. Gating an optional integration

The shape that works, in order:

1. The integration lives in **its own assembly**, at the layer its dependencies allow.
2. That assembly is **constrained** to the symbol its package defines. Package absent → assembly not
   compiled → zero cost, zero errors.
3. Consumers do **not** reference it directly. They talk to a contract in a lower assembly.
4. **Composition** decides at boot whether an implementation exists. Absent → a null/no-op
   implementation, chosen in one place.

This keeps `#if` out of gameplay code entirely. Guards concentrate at the assembly boundary and in
one wiring site, instead of spreading through call sites where each one is a chance to forget.

## 5. Feature toggles for unfinished work

Gating work-in-progress is legitimate; leaving it gated forever is not.

- **Give every toggle an expiry** — a version or date by which it is removed or promoted. Write it in
  the declaration, not in a tracker nobody reads.
- **Default off in shipping configurations, on in development**, so the code stays compiled and
  cannot rot behind a guard nobody exercises.
- **Never nest toggles.** Two toggles are four states; three are eight, and nobody tests eight. If
  two features interact, that is one toggle.
- **Delete on completion.** The toggle and both branches — the loser too. A dead branch behind a
  permanently-true guard is code that will be read, trusted, and eventually re-enabled by accident.

The failure mode to name explicitly: a codebase where every module is behind a toggle nobody
remembers the state of. At that point the build is not reproducible from the source, because the
source no longer determines what is in it.

## 6. Auditing gates

```bash
# every assembly-level constraint in the project
grep -rl "defineConstraints" --include=*.asmdef . | while read f; do
  echo "── $f"; grep -A5 "defineConstraints" "$f"
done

# every guard symbol used in source, ranked by frequency
grep -rhoE "#if[[:space:]]+!?[A-Z_][A-Z0-9_]*" --include=*.cs . \
  | awk '{print $NF}' | tr -d '!' | sort | uniq -c | sort -rn | head -30
```

Read the second list against the project's actual installed packages. Three things to look for:

- a symbol guarded in source that **no assembly declares** — that code is dead, silently;
- a symbol declared but **never guarded on** — leftover, delete it;
- a symbol whose package is **no longer in the manifest** — the guard is now permanently false, and
  anyone reading it will assume otherwise.

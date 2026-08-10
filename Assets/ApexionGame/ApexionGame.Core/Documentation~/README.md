# ApexionGame.Core — Documentation

*[Tiếng Việt](README.vi.md)*

Reusable, game-agnostic runtime for ApexionGames projects. Everything here is built on
**EncosyTower** (`Packages/com.laicasaane.encosy-tower`) and is meant to survive past the current
title.

## Modules

| Module | Namespace | Status | Docs |
|---|---|---|---|
| `HFSM/` — hierarchical finite state machine | `ApexionGame.HFSM` | **Awaiting review** | [HFSM - Overview](HFSM%20-%20Overview.md) |

## HFSM — reading order

| # | File | Read it when |
|---|---|---|
| 1 | [HFSM - Overview](HFSM%20-%20Overview.md) | always first — the request, the expected output, the step table |
| 2 | [HFSM - API Surface](HFSM%20-%20API%20Surface.md) | you want to see what the user writes |
| 3 | [HFSM - Flows](HFSM%20-%20Flows.md) | you need the transition algorithm, ordering rules, parallel/history/async semantics |
| 4 | [HFSM - Data Model](HFSM%20-%20Data%20Model.md) | you need the types, the collection chosen for each, and the memory budget |
| 5 | [HFSM - Layout](HFSM%20-%20Layout.md) | you are creating files or asmdefs |
| 6 | [HFSM - Debugging](HFSM%20-%20Debugging.md) | you are building the four debug surfaces |
| 7 | [HFSM - Decisions](HFSM%20-%20Decisions.md) | you disagree with something and want to know why it is that way |
| 8 | [HFSM - Roadmap](HFSM%20-%20Roadmap.md) | you are planning or executing a phase |

Every file has a `.vi.md` mirror kept section-for-section in sync.

## Conventions

- Style: `CODING-CONVENTIONS.md` at the repo root.
- Architecture and module selection: `.claude/skills/encosy-tower/`.
- Unity operations (compile, tests, builds): `.claude/skills/unity-cli-workflow/`.
- The sibling `ApexionGame.Entities.Stats/Documentation~/` set is the reference for this project's
  doc culture — same level of detail is expected here.

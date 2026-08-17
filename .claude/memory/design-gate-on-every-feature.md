---
name: design-gate-on-every-feature
description: Every feature plan must answer who owns each piece of state, how pieces communicate, and what was rejected — the system-design gate
metadata:
  type: feedback
---

The user's instruction, 2026-08-15: *"mọi tính năng tôi yêu cầu làm cho game đều phải có architecture
thật tốt chuẩn chỉ như 1 Senior Engineer thực thụ"*, and *"game midcore cũng cần phải có 1
architecture thật tốt ngay từ giai đoạn start dự án"*.

So `system-design` is a **standing gate on every feature**, not a reference consulted when asked. A
plan is not ready for review until it states: **decomposition · state ownership table · communication
mechanisms with reasons · at least one rejected alternative** — plus, when the feature runs per frame
or scales with entity count, **the performance tier and why not higher and not lower**.

**Why:** `Assets/Game` was empty when this was decided, so the first features set the pattern for
everything after. Ownership in particular is the one decision that cannot be refactored later — it
spreads into every call site and into the save format.

**How to apply:** load `system-design` alongside `encosy-tower` on every "làm feature / thêm hệ
thống / implement" request — the trigger overlap between them is deliberate. The user also asked for
high performance **without over-engineering**: start at the lowest tier that plausibly works, and
state both why not higher *and* why not lower. Push back with a concrete cost when a request leads to
a bad design, propose an alternative, then **do it anyway if the decision stands** — the third beat
is what keeps the first two welcome. Related: [[write-feature-doc-before-code]],
[[midcore-skills-read-project-profile]], [[planning-advice-from-encosy-author]].

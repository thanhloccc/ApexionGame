# Communication between pieces

## 1. The rule

> **Use the most direct mechanism that does not create a dependency pointing the wrong way.**

Directness is a feature: a direct call can be read, stepped through, and traced. Every step away from
it buys decoupling and pays in traceability.

The corollary that matters most: **events are not the default.** They are the tool for removing a
dependency you are **not allowed** to have. Using one where a direct call was legitimate makes the
code harder to follow and buys nothing.

## 2. The mechanisms

| Mechanism | Use when | Costs |
|---|---|---|
| **Direct call** | The caller needs a result and is allowed to know the callee | Hard dependency — but readable and debuggable |
| **Query / request** | You need an answer but should not know who answers | One indirection; requires exactly one responder |
| **Event / message** | No result needed; 0..N listeners; caller must not know them | **Control flow becomes hard to follow**; ordering not guaranteed |
| **Shared state + poll** | Many readers, read every frame anyway | Nobody knows when it changed; fragile without a clear owner |

### Direct call

Default. If A is allowed to depend on B and needs an answer, call it. A stack trace tells the whole
story.

### Query / request

One registered responder, returns a value. Use when the caller must not know the implementation but
does need the answer — a rules system asking for the current gold amount.

Costs: exactly one responder must be registered, which is a runtime failure rather than a compile
error. Worth it at a real boundary; not worth it inside one concept.

### Event / message

Fire and forget, 0..N listeners.

**Legitimate reasons:**

- the dependency would point the **wrong way** (`midcore-assembly-architecture`);
- there are **genuinely many** listeners, unknown to the publisher;
- the listener set changes at runtime.

**Not legitimate:**

- "to decouple" when the dependency was fine;
- to avoid passing a reference;
- to escape a cycle that should be fixed by extraction instead.

**The real cost:** given a symptom, you cannot answer "what runs when this happens?" by reading. You
must find every subscriber, and order between them is not guaranteed. That cost is worth paying when
the alternative is an illegal dependency, and it is pure loss otherwise.

### Shared state + poll

Readers look at owned state each frame. Legitimate when readers run every frame anyway and want the
current value rather than a change notification — rendering a health bar.

Requires a **clear single owner** (`state-ownership.md`). Without one this is not a communication
mechanism, it is a global variable.

## 3. Choosing

```
Does the caller need a result?
├─ YES → Is the caller allowed to depend on the callee?
│        ├─ YES → direct call
│        └─ NO  → query/request  (and check: should it be allowed? often the real fix)
│
└─ NO  → Does anyone need to know it happened?
         ├─ NO  → nothing. Do not announce for the sake of announcing
         └─ YES → Is the listener set known and small, with a legal dependency?
                  ├─ YES → direct call
                  └─ NO  → event
```

The parenthetical in the second branch is the most useful line here. A query used *only* to dodge a
dependency that would have been perfectly legal is indirection with no benefit — check the layering
before adding the mechanism.

## 4. Ownership decides the mechanism

Communication follows from `state-ownership.md`, not the reverse:

| Need | Mechanism |
|---|---|
| Read a value you do not own | query the owner, or read a read-only view |
| Know when something you do not own changed | owner publishes; you subscribe |
| Change something you do not own | **ask the owner** — never write it |

If a design wants to write state it does not own, that is not a communication problem. The ownership
is wrong, or the piece is in the wrong place.

## 5. Ordering

The failure mode of event-heavy designs.

- **Do not depend on subscriber order.** If two listeners must run in a fixed order, they are not
  independent listeners — sequence them explicitly in one place.
- **Do not publish mid-mutation.** A listener that reads state while the publisher is halfway through
  changing it sees a torn value. Finish, then publish.
- **Beware re-entrancy.** A listener that causes the same event to fire again is a stack overflow or
  a corrupted state, and it is usually found in production.
- **Decide when it is delivered** — immediately, end of frame, next frame — and write it down. This
  is a design decision that gets made accidentally.

## 6. Recording it

For the feature gate, one table:

| From | To | Mechanism | Why this one |
|---|---|---|---|
| input translation | combat sim | direct call | sim owns the decision; caller may depend on it |
| combat sim | feedback/VFX | event | sim must not know presentation exists |
| HUD | health state | read-only view, polled | HUD renders every frame anyway |
| inventory | combat sim | query | sim needs the equipped weapon; must not own inventory |

The **"why this one"** column is the point. Without it, the table records what happened rather than
what was decided, and the next person cannot tell which choices are load-bearing.

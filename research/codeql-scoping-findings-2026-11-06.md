# CodeQL Scoping Findings

Repo: `Bunnings-Technology-Delivery/dotnet.common` @ `badbca94c`
Alerts pulled: 267 open (170 note / 97 warning), 25 rule categories.

---

## #1738 — Virtual call in constructor

### Enumerated sites (7)

| # | Alert | File | Class | Member | Line | Recommended fix | Public signature impact |
|---|-------|------|-------|--------|------|-----------------|------------------------|
| 1 | 134 | `src/dotnet/Components/PubSub/Emp/Bunnings.Common.PubSub.Emp/Models/Event.cs` | `Event<T>` (abstract record) | content ctor → `LazyContent = new Lazy<T>(content)` | 175 | Make `LazyContent` non-virtual (see pattern below) | None — `protected` only |
| 2 | 135 | same | `Event<T>` | content ctor → `LazyStream = new Lazy<Stream>(...)` | 177 | Make `LazyStream` non-virtual | None — `protected` only |
| 3 | 136 | same | `Event<T>` | content ctor → `LazyRawContent = new Lazy<byte[]>(...)` | 186 | Make `LazyRawContent` non-virtual; remove `BinaryEvent` override | None — `protected` only |
| 4 | 137 | same | `Event<T>` | stream ctor → `LazyStream = new Lazy<Stream>(contentStream)` | 216 | Make `LazyStream` non-virtual | None — `protected` only |
| 5 | 138 | same | `Event<T>` | stream ctor → `LazyRawContent = new Lazy<byte[]>(...)` | 218 | Make `LazyRawContent` non-virtual | None — `protected` only |
| 6 | 139 | same | `Event<T>` | stream ctor → `LazyContent = new Lazy<T>(() => ReadContentFromStream(LazyStream.Value))` | 231 | Non-virtual `LazyContent` resolves the virtual *access*; the abstract call is inside a deferred lambda (safe at runtime, but still flagged by the rule) | None — `protected` only |
| 7 | 140 | `src/dotnet/Components/PubSub/Subscriber/LargeEntities/Bunnings.Common.PubSub.Subscriber.LargeEntities/LargeEntitySubscriptionPipelineRegistration.cs` | `LargeEntitySubscriptionPipelineRegistration<TState>` | ctor → `SplitOptions = SplitOptions.Parallel` | 38 | Make `SplitOptions` non-virtual (`protected SplitOptions SplitOptions { get; }`) | None — `protected`, no overrides exist |

### Fix pattern decision

**Recommended: drop `virtual` from the three lazy properties + `SplitOptions`.**

Rationale:

1. **Sites 1–6 (Event.cs):** The base ctor assigns to `protected virtual` auto-properties (`LazyContent` L142, `LazyStream` L147, `LazyRawContent` L152). In C# a property assignment in a ctor dispatches virtually, so any derived override runs before the derived ctor body — the classic vcc hazard. Only **one** production override exists: `BinaryEvent.LazyRawContent => LazyContent` (BinaryEvent.cs:100). Its purpose is to make `ToBytes()` return the already-materialised `LazyContent` instead of re-reading the stream. If `LazyRawContent` becomes non-virtual, `BinaryEvent` must drop the override; its `ToBytes()` (BinaryEvent.cs:103) currently returns `LazyRawContent.Value`, which would then resolve to the base's own lazy — for `BinaryEvent` that lazy is built from the raw bytes anyway (`ReadContentFromStream` = `stream.ToBytes()`), so behaviour is preserved. The test-only override `BadEvent.LazyContent => throw` (EventTests.BadEvent.cs:9) also needs updating.
   - Alternative considered: keep virtual and move lazy construction into derived ctors. Rejected — every concrete subclass is a `sealed` record with its own factory methods; duplicating the three lazy initialisations across 7+ derived ctors is more code and more drift risk than removing one keyword.
   - Site 6 specifically: even after de-virtualising, the lambda calls `ReadContentFromStream` (abstract). CodeQL flags the virtual *reference* in ctor context; making the accessed property non-virtual removes the dispatch concern. The call itself is deferred to first access, which is safe. If the rule still fires on the abstract method reference, the cleanest follow-up is to have the stream ctor accept the deserialiser as a parameter or move the lazy into the derived class — but this is a second, smaller change.

2. **Site 7 (LargeEntitySubscriptionPipelineRegistration.cs):** Trivial. `SplitOptions` (L28) is `protected virtual` with zero overrides (verified by grep; only subclass is the sealed internal `InlineLargeEntitiesPipelineRegistration`). Drop `virtual`. No behaviour change.

**Backwards-compat note:** All four members are `protected`; no public API surface changes. Per repo convention (backwards compatibility for public APIs) this is safe. `BinaryEvent`'s public `ToBytes()` keeps its signature and observable behaviour.

**Test impact:** `BadEvent` test double (EventTests.BadEvent.cs:9) overrides `LazyContent` — must be updated to match the non-virtual declaration (or the test reworked to not rely on the override).

---

## #1739 — Low-severity findings

### Actual alert inventory (vs issue body)

The issue body says "~140 findings across 21 rule categories" and lists 21 rules. Actual open count is **267 alerts across 25 categories**. Four categories are missing from the issue table:

| Rule | Count | Note |
|------|-------|------|
| `cs/local-not-disposed` | 41 | Missing from issue body |
| `cs/dereferenced-value-may-be-null` | 14 | Missing from issue body |
| `actions/unpinned-tag` | 13 | GitHub Actions workflow pinning — different domain |
| `cs/dispose-not-called-on-throw` | 2 | Missing from issue body |

Also: the 7 `cs/virtual-call-in-constructor` alerts are tracked separately under #1738 and should be excluded from #1739's count (leaving 260).

### Category ranking (pass 1 / pass 2 / defer)

**Pass 1 — mechanical, low-risk, high signal (≈58 findings, 9 rules):**

| Rule | Count | Why pass 1 |
|------|-------|-----------|
| `cs/linq/missed-where` | 19 | Pure refactor (`.Where` → LINQ); no behaviour change; sample: ProductSettingsBuilderExtensions.cs:146, ProblemContextDictionary.cs:319, TuiForm.cs:531 |
| `cs/useless-upcast` | 7 | Delete redundant cast; zero risk. Sample: WebApiModuleFilter.cs:189, ApiVersionOperationFilter.cs:31 |
| `cs/missed-using-statement` | 7 | Add `using` disposal; small, localised. Sample: SourcesDialog.cs:34, CosmosLiveSubscription.cs:84 |
| `cs/constant-condition` | 6 | Dead branches / always-true checks; quick wins. Sample: ReliableRedisConnection.ConnectionContainer.cs:68, OtelSource.cs:23 |
| `cs/path-combine` | 7 | Replace `Path.Combine` with string interpolation where args can't be absolute; correctness-adjacent. Sample: PlaintextFileSecretStore.cs:23, DpapiSecretStore.cs:43/97/142 |
| `cs/missed-ternary-operator` | 6 | Style; trivial. |
| `cs/inefficient-containskey` | 3 | `.ContainsKey(k) && d[k]` → `TryGetValue`; perf micro-fix. |
| `cs/useless-tostring-call` | 1 | Trivial. |
| `cs/useless-type-test` | 1 | Trivial. |

**Pass 2 — requires judgement, moderate effort (≈90 findings):**

| Rule | Count | Why pass 2 |
|------|-------|-----------|
| `cs/local-not-disposed` | 41 | Each site needs a real dispose decision (is the object IDisposable? is it disposed elsewhere?). Not mechanical. |
| `cs/dereferenced-value-may-be-null` | 14 | Null-guard additions; need to verify the null path is actually reachable. |
| `cs/static-field-written-by-instance` | 7 | Potential concurrency bugs (EmpxSeedManager.cs:49, BaseExecutor.cs:60/74); needs design review per site. |
| `cs/call-to-unmanaged-code` + `cs/unmanaged-code` | 8 | P/Invoke review; likely legitimate interop that should be suppressed with justification rather than changed. |
| `cs/call-to-obsolete-method` | 4 | Replace with current API; check for behaviour deltas. |
| `cs/class-name-matches-base-class` | 2 | Rename or suppress; naming judgement. |
| `cs/missed-readonly-modifier` | 2 | Trivial but touches visibility semantics. |
| `cs/dispose-not-called-on-throw` | 2 | Exception-path dispose; small but correctness-sensitive. |
| `cs/complex-condition` | 2 | Refactor boolean expressions; readability. |
| `cs/cast-from-abstract-to-concrete-collection` | 2 | Verify runtime type; may need `as` + guard. |
| `cs/expose-implementation` | 1 | Return interface instead of concrete type; small API-surface change. |
| `cs/simplifiable-boolean-expression` | 1 | Trivial. |
| `actions/unpinned-tag` | 13 | Pin action tags in workflows; mechanical but separate domain (CI config, not C#). Could be its own slice. |

**Defer — policy decision needed (98 findings):**

| Rule | Count | Why defer |
|------|-------|-----------|
| `cs/catch-of-all-exceptions` | 98 | See treatment below. This is a house-style question, not a bug hunt. |

### catch-of-all-exceptions treatment

98 alerts across **78 files** (top: TuiForm.cs ×4, SourcesDialog.cs ×3, ConfigCommand.cs ×3, ReliableRedisConnection.ConnectionContainer.cs ×3). The repo has ~190 total `catch (Exception` occurrences, so roughly half are flagged (the rest are in contexts the rule doesn't fire on, e.g. top-level handlers or `when:` filters).

**Recommendation: do NOT include in pass 1.** Options, in order of preference:

1. **Suppress with a documented policy** (preferred): add a CodeQL suppression / `// codeql[cs/catch-of-all-exceptions]` comment policy in the query config, with a written rationale (e.g. "we catch `Exception` at pipeline/tool boundaries and log with context; specific-catch is enforced by review for library code"). This removes 98 alerts without touching 78 files.
2. **Scope down the rule**: if the org wants specific catches in library components but not in CLI tools, split the rule application by path (exclude `src/dotnet/Tools/**`).
3. **Full remediation**: last resort — 78 files, each needing a decision on which exceptions are genuinely recoverable. High effort, low safety yield for a `note`-severity rule.

### Target threshold recommendation

Current: 267 open (260 excluding #1738's 7).

- After #1738 fix: −7 → 260
- After pass 1 (mechanical slice): −58 → ~202
- After catch-of-all-exceptions suppression: −98 → ~104
- After pass 2: −90 → ~14

**Suggested gate target: ≤ 100 open alerts after pass 1 + suppression**, with pass 2 tracked as a follow-up epic. A hard gate at 0 is unrealistic given `actions/unpinned-tag` (13) and the pass-2 judgement items; a gate at ≤100 keeps the signal-to-noise ratio healthy and makes new-alert detection meaningful.

### Suggested slice for pass 1 (specific)

**Scope:** 9 rules, ~58 findings, all mechanical:

1. `cs/linq/missed-where` (19)
2. `cs/useless-upcast` (7)
3. `cs/missed-using-statement` (7)
4. `cs/path-combine` (7)
5. `cs/constant-condition` (6)
6. `cs/missed-ternary-operator` (6)
7. `cs/inefficient-containskey` (3)
8. `cs/useless-tostring-call` (1)
9. `cs/useless-type-test` (1)

Plus the #1738 fix (7 sites) as a companion slice since it's small and well-understood.

**Total pass-1 blast radius: ~65 findings across ~45 files.** Each change is a localised refactor with no public API impact. Suitable for a single PR or a small stack of PRs, one per rule category for easy review.

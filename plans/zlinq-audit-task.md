GOAL: Audit Bunnings.Common production code for hand-rolled loops and allocation-avoidance patterns that ZLinq could replace with plain LINQ. Research only — produce a report, change nothing.

CONTEXT: ZLinq (Cysharp/ZLinq v1.6.1, MIT) = zero-allocation LINQ: `ReadOnlySpan<T>` overloads of standard operators, SIMD-accelerated Where/Select/Aggregate, LINQ-to-Tree (JSON, filesystem), source-gen for arbitrary value types. SYNC in-memory only — it does NOT cover async-enumerable, IO, or DB streaming.

SCOPE:
- Repo: /home/bmazzarol/work/dotnet.common — all `src/dotnet/Components/*/` production code.
- EXCLUDE: Tests, obj/, bin/, Demo(s), Usecases, Obsolete, Elm.

FIND these patterns:
1. Hand-rolled `for`/`while` loops over collections, strings, or byte/char buffers where a LINQ pipeline would express the same thing
2. `[SuppressMessage]` or comments justified by allocation or performance ("avoid allocating an enumerator", "Performance", "no alloc", etc.)
3. Manual buffer management (rented buffers, manual upper-case conversion loops, manual JSON/string scanning)
4. Anything in Bunnings.Common.Json that ZLinq's JSON LINQ-to-Tree could simplify

CLASSIFY every finding:
- (A) ZLinq-able: sync, in-memory, span-friendly (collections, strings, bytes, JSON tree)
- (B) Out of scope: async-enumerable, IO-bound, DB streaming, stateful mutation, or where the loop does work LINQ can't express cleanly

OUTPUT — one report at /home/bmazzarol/work/bmazzarol-bunnings/research/zlinq-audit.md:
- Summary counts: (A) vs (B)
- Findings table: file:line · pattern · (A)/(B) classification · one-line rationale
- For each (A): a 2-5 line sketch of the ZLinq LINQ version and the expected win (allocations eliminated / code lines reduced / readability)
- A ranked top-5 shortlist for benchmarking (biggest expected perf + simplicity win, most representative)
- Honest overall assessment: is the win concentrated in a few files or spread across Common? Is ZLinq integration into Common actually earned?

HARD CONSTRAINTS:
- Read-only in dotnet.common. No edits, no commits, no PRs there.
- Only write file: the report above.
- Do not install packages or run the ZLinq benchmark suite — this is a code audit, not a perf run.

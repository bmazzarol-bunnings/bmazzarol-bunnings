# ZLinq Audit — Bunnings.Common

**Scope:** all `src/dotnet/Components/*/` production code (Tests, obj/bin, Demos, Usecases, Obsolete, Elm excluded).
**Method:** read-only audit. Grep for index-based loops (`for (var i = 0; i <`), `while` loops, allocation-justified `SuppressMessage`/comments, rented-buffer code, JSON/string scanning; read each hit's surrounding code; verify ZLinq v1.6.1 API surface against the source (`AsValueEnumerable` over `Span`/`ReadOnlySpan`/array/`List`, `Select`/`SelectAt`, `Where`/`WhereAt`, `Aggregate`, `JoinToString`, `GroupBy`, `First`, `Any`/`All`, `Contains`, SIMD `AsVectorizable`, and `ZLinq.Json` axes over `JsonNode`).

ZLinq API facts used (verified in Cysharp/ZLinq @ tag `1.6.1`):
- `AsValueEnumerable()` works on `T[]`, `List<T>`, `ArraySegment<T>`, `Span<T>`, `ReadOnlySpan<T>`, `IEnumerable<T>`.
- `Select(source, (x, i) => ...)` and `Where(source, (x, i) => ...)` carry an index overload.
- `JoinToString` = zero-alloc `string.Join` equivalent with stackalloc buffer.
- `ZLinq.Json` LINQ-to-Tree is **axes over `JsonNode`** (`Descendants()`, `Children()`, `Ancestors()`, …) — not a `Utf8JsonReader`/`Utf8JsonWriter` replacement.

---

## Summary counts

| Class | Count | Meaning |
|---|---|---|
| **(A)** ZLinq-able | **24 sites / 19 files** | sync, in-memory, span-friendly (projection, fold, contains/first-match, join, char/byte mapping) |
| **(B)** Out of scope | **~45 sites / 40+ files** | async-enumerable, IO-bound, DB streaming, stateful mutation, side-effect writes, in-place buffer mutation, custom tokenizers/parsers |

Key structural finding: the codebase is **already heavily allocation-conscious** (ArrayPool, StringBuilderInstancePool, stackalloc, `SuppressMessage(... "avoid allocating an enumerator")`). Most index loops are either (a) tiny one-liners where LINQ is a readability wash, or (b) deliberately hand-rolled because they mutate state, do IO, or do per-element side-effect writes that LINQ cannot express.

---

## Findings table

All paths relative to `src/dotnet/Components/`.

### (A) ZLinq-able

| # | file:line | pattern | rationale |
|---|---|---|---|
| A1 | `Validation/Bunnings.Common.Validation/Validations.CheckEach.cs:83,130,177,224` | 4× fold: `newResult = newResult.PropertyCheckReducer(KeyValuePair.Create(i, list[i]), ...)` | pure stateless fold over `IReadOnlyList<T>` — textbook `Aggregate` |
| A2 | `Validation/Bunnings.Common.Validation/BaseValidationService.cs:53` | fold: `result += results[i]` | pure merge-reduce over array |
| A3 | `Validation/Bunnings.Common.Validation/Exceptions/ValidationFailureException.cs:58` | fold: `exception = exception.Merge(failures[i].Exception)` (filtered) | filter + reduce |
| A4 | `Validation/Bunnings.Common.Validation/Validations.CheckEachAsync.cs:52` | fold: `newResult += taskResults[i]` | pure merge-reduce (sync section over completed tasks) |
| A5 | `AspNetCore/Validation/Bunnings.Common.AspNetCore.Validation/ValidationErrorsProblemDetails.cs:53` | group-into-dictionary loop | `GroupBy(...).ToDictionary(...)` |
| A6 | `AspNetCore/Validation/Bunnings.Common.AspNetCore.Validation/ObjectValidationService.cs:93` | group-into-dictionary loop | same as A5 |
| A7 | `PubSub/Emp/Bunnings.Common.PubSub.Emp/Models/Topic.cs:539` (`BuildTopicString`) | join-with-separator + per-char `c == Space ? NotApplicable : c` mapping | `Select` over char span + `JoinToString`; hot per-event path |
| A8 | `PubSub/Emp/Bunnings.Common.PubSub.Emp/Models/Topic.cs:495` (`Sanitize`) | same char mapping per part, separator-joined | `Select` + `JoinToString` |
| A9 | `Foundation/String/Bunnings.Common.String/Extensions/StringSimilarityExtensions.cs:53,122` | in-place upper-case fill: `rightUpper[i] = char.ToUpperInvariant(right[i])` (×2) | expressible as `Select(c => char.ToUpperInvariant(c))`; wins code lines, not allocs (buffers already pooled/stackalloc'd) |
| A10 | `Foundation/Stream/Bunnings.Common.Stream/MultipartStream.cs:54` (`GetStreams`) | projection loop `list[i] = _streams[i].Stream` | `Select` into rented/pre-sized array |
| A11 | `Foundation/Stream/Bunnings.Common.Stream/MultipartStream.cs:342` (`Length`) | sum loop | `Aggregate` |
| A12 | `Telemetry/Bunnings.Common.Telemetry.Abstractions/Extensions/DiagnosticsExtensions.cs:22` | copy loop with `// Avoid Enumerator allocation` comment | `AddRange`-equivalent; ZLinq removes the loop entirely |
| A13 | `Telemetry/Bunnings.Common.Telemetry.Instrumentation.MediatR/Behavior/TelemetryBehaviour.cs:199,208,237,299` | 4× `tagList.Add(tags[i])` copy loops | trivial copy; `AddRange` or ZLinq |
| A14 | `Telemetry/Bunnings.Common.Telemetry/Logging/OpenTelemetryProcessors/LogRecordWriteableAttributesExtension.cs:32` | `buffer.Add(logRecordAttributes[i])` copy loop | trivial copy |
| A15 | `Telemetry/Bunnings.Common.Telemetry/Logging/OpenTelemetryProcessors/CategoryDynamicFilterProcessor.cs:60` | linear contains loop `filter.EventsIds[i] == data.EventId` | `Contains` |
| A16 | `Telemetry/Bunnings.Common.Telemetry/Logging/OpenTelemetryProcessors/LogComputedEventIdProcessor.cs:32` | first-match key lookup over `stateValues` | `FirstOrDefault` |
| A17 | `Telemetry/Bunnings.Common.Telemetry/Logging/Exporters/Model/ProcessedLogRecord.cs:30` | first-match key lookup over attributes | `FirstOrDefault` |
| A18 | `AspNetCore/Telemetry/Bunnings.Common.AspNetCore.Telemetry/WebTelemetryExtensions.cs:81` | any-match loop over sampled paths | `Any` |
| A19 | `HttpClient/Authorization/Bunnings.Common.HttpClient.Authorization/Services/AuthTokenServiceSettings.cs:68` | first-match loop over clients by prefix | `FirstOrDefault` |
| A20 | `Redis/Bunnings.Common.Redis/Implementation/ReliableRedisConnection.ConnectionContainer.cs:139` | copy loop into `_state.Profilers` | trivial copy |
| A21 | `PubSub/Emp/Bunnings.Common.PubSub.Emp/Models/AcknowledgementScope.cs:64,76` | filter-and-add loop; plain add loop | `Where` + copy / trivial copy |
| A22 | `Pipelines/Bunnings.Common.Pipelines/Extensions/ComponentExtensions.cs:24` | join-with-separator over generic type names | `JoinToString` |
| A23 | `Telemetry/Bunnings.Common.Telemetry.Instrumentation.MediatR/LogMessages.cs:206` | join-with-separator over observations | `JoinToString` (multi-part per element, so partial) |
| A24 | `PubSub/Emp/Solace/Bunnings.Common.PubSub.Emp.Solace/Impl/SolaceSmfSendHandler.cs:95` | fill loop `messages[i] = sessionWrapper.CreateMessage(handle)` | `Select` into rented array |

### (B) Out of scope

| # | file:line | pattern | rationale |
|---|---|---|---|
| B1 | `Foundation/Stream/Bunnings.Common.Stream/MultipartStream.cs:107,125,136,153,196,245` + `WriteInternal` | ctor cumulative-offset build; Flush/Dispose/read-position/write-chunk loops | stateful cursor arithmetic, Stream IO; LINQ can't express running offsets or cross-stream writes |
| B2 | `Foundation/String/.../StringSimilarityExtensions.cs:58,64-78,113-142` | Levenshtein DP + LCS DP inner loops | cross-cell dependency (`previous[r-1]`), row-buffer swap — not a projection/fold |
| B3 | `Telemetry/.../JsonSerialization/AttributeWriterHelper.cs:81,89,97,105` | 4× `writer.WriteNumberValue(arr[i])` JSON write loops | per-element side-effect on `Utf8JsonWriter`; LINQ can only hide it in `Aggregate`, worse readability |
| B4 | `OData/Bunnings.Common.OData/Selecting/QuerySelector.cs:715` | in-place lowercasing of `nameCharacters` | in-place mutation of a shared char buffer |
| B5 | `OData/Bunnings.Common.OData.Queryable/BaseODataExpressionWalker.cs` (many) | expression-tree walking, `while (expression is Convert ...)` unrolling | tree recursion, not sequence processing |
| B6 | `Cli/SecretStore/.../KeychainSecretStore.cs:331` | P/Invoke `Marshal.StringToCoTaskMemUTF8` loop | native interop side effects |
| B7 | `Persistence/Database/Oracle/ActionRouter/.../BaseRouterAction.cs:195,229` | log-emit loops over logs/errors | per-element `Logger.*` side effects |
| B8 | `PubSub/Emp/Solace/.../SolaceSmfSendHandler.cs:144,252,283`, `BaseSolaceSmfReceiveHandler(.ReceivedBatch).cs` | batch windowing, message build/ack bookkeeping | stateful counters, async SDK calls |
| B9 | `PubSub/Emp/Bunnings.Common.PubSub.Emp.InMemory/InMemoryEmpClient.ReceiveHandler.cs` | message pump | queue/async pump |
| B10 | `Authentication/Bunnings.Common.Authentication.DeviceFlow/DeviceFlowService.cs` | async polling loops | async |
| B11 | `AspNetCore/Hateoas/.../LinkProcessingService.cs:43` | async processor chain `links = await filter.Process(...)` | async stateful chain |
| B12 | `AspNetCore/Versioning/.../VersionAvailabilityMatcherPolicy.cs:52` | candidate scan with `continue`/match logic | stateful matching |
| B13 | `AspNetCore/Validation/.../ValidationFilter.cs:29`, `EndpointRouteBuilderExtensions.cs:49` | per-parameter validation side effects | mutation of context, control flow |
| B14 | `Pipelines/Bunnings.Common.Pipelines/Resources/ResourcesImpl.cs` (whole file) | async release loops, pooled `ArrayPool<KeyValuePair>` staging | async, pooling, removal-while-iterating |
| B15 | `Pipelines/Bunnings.Common.Pipelines/Builder/Impl/PipelineBuilder.PipelineImpl.cs` | `goto` in async state machine, `[SuppressMessage(... Justification = "Performance")]` | async state machine; comment is about async-local scoping, not LINQ |
| B16 | `Pipelines/.../Executors/BaseExecutor.cs`, `BaseSplitMerge*Executor.cs`, `Operations/Window|Split|Flatten|FlatMap` | windowed producer/consumer machinery | stateful pipeline windows |
| B17 | `Pipelines/.../Consumers/Standard/FanOutConsumer.cs:78`, `Builder/Combinators/PipelineBuilder.CombinedConsumers.cs:107` | fold over concurrent results with state switch | fold is expressible but async/stateful context; low win |
| B18 | `Pipelines/.../Models/RunHandle.cs:78,107,163` | nested-handle rental/reuse loops | stateful rental bookkeeping |
| B19 | `Pipelines/.../Producers/Enumerator/ISyncEnumeratorProducer.cs:40`, `IEnumeratorProducer.cs:42` | `MoveNext` retry loops | enumerator state machine |
| B20 | `Worker/ScalingWorker/.../ScalingBackgroundWorkerCore.cs:506,548,596` | slot/task management | stateful task management |
| B21 | `Foundation/Stream/.../LineEndingNormalizingStream.cs` | stream transform with rented buffers | IO stream |
| B22 | `Foundation/Xml/.../ChildWrappingXmlWriter.cs:92` | per-child-stream byte/char copying via `ArrayPool` | IO buffer management |
| B23 | `AI/Bunnings.Common.AI/Embedding/LLamaEmbedder.cs:61,94,120,148,192` | tokenization chunking, native batch fill, embedding extraction | native interop + stateful cursor |
| B24 | `RoslynIndexer/.../Storage/DuckDbRoslynIndexStore.cs:1740` | batched parameter binding | DB streaming |
| B25 | `RoslynIndexer/.../Roslyn/RoslynProjectAnalyzer.cs:57,125` | per-document indexing with `GetCompilationAsync` | async IO |
| B26 | `PubSub/Publisher/Cosmos/.../CosmosChangeFeedProducer.cs:298,530` | change-feed loops | async DB feed |
| B27 | `Persistence/Cosmos/.../Extensions/AsyncEnumerableExtensions.cs:23,38` | `while (iterator.HasMoreResults)` | async DB |
| B28 | `Telemetry/.../Logging/Instrumentation/HttpClient/OuterLoggingHandler.cs:98` | query-param redaction (`queryParams.Set`) | mutation of shared state |
| B29 | `Telemetry/.../JsonSerialization/AttributeAggregator.cs:53,214` | capped-attribute aggregation, cache clears | stateful caches with size caps |
| B30 | `Telemetry/.../Metrics/Instrumentations/EventCounters/PrometheusDataModel.cs:75` | per-char filter+**expand** (invalid char → separator) with index-dependent first-char rule | expansion is not 1:1 mapping; `WhereAt`+`Select` can't emit 1→2 chars |
| B31 | `Foundation/String/.../StringCaseExtensions.cs` (`ToSnakeCase`) | per-char with look-back (`bc = text[i-1]`) | depends on previous char — not a map |
| B32 | `OData/.../Filtering/Configuration/FilterPatternHelper.cs:41` | emits both upper and lower variants per char | expansion (1→2) |
| B33 | `Gateway/Apigee/.../ApigeeRouteTemplate.cs:19,52,122,153` | route-template tokenizer (`while (i < route.Length)`) | stateful parser |
| B34 | `OData/.../ODataQuery.cs:201`, `MemberExpressionBuilder.cs:28`, `ODataFilterService.cs:289,348` | query-string/predicate parsers | stateful parsers |
| B35 | `Cli/SecretStore/.../SecretStoreOptions.cs:101`, `SecretKeys.cs:57` | option/key string parsing | stateful parsers |
| B36 | `RoslynIndexer/.../Models/DeterministicId.cs` | length-prefix buffer packing, base62 encoding | stateful buffer writes |
| B37 | `Foundation/Json/Bunnings.Common.Json/ProblemContextDictionary.Create.cs` | `foreach` over pairs into `Utf8JsonWriter` | side-effect writes |
| B38 | `PubSub/Emp/.../TopicSubscriptionValidator.cs:32` | positional topic-part matching with `>` wildcard | control-flow matching |
| B39 | `Foundation/Cryptography/Bunnings.Common.Cryptography.OData/BaseSensitiveStringExpressionProvider.cs:33` | per-cipher-id array building | control-flow branching per index |
| B40 | pooling infrastructure: `EventHeadersBuilderPool.cs`, `RequestContextMiddleware.cs:33`, `LogThreadEnricherProcessor.cs`, `HealthCheckFilterProcessor.cs`, `RunHandlePoolingContract.cs`, `StringBuilderInstancePool` usage | object/string-builder pooling | pooling design, not sequence processing |

---

## (A) sketches

**A1 — Validations.CheckEach (4 sites)**
```csharp
// now: for (var i = 0; i < length; i++) newResult = newResult.PropertyCheckReducer(KeyValuePair.Create(i, list[i]), ...)
// ZLinq:
return readOnlyList.AsValueEnumerable()
    .SelectAt((x, i) => (i, x))
    .Aggregate(newResult, (r, kv) => r.PropertyCheckReducer(kv, predicate, onFailure, propertyName, code));
```
Win: 4 loop bodies → 3-line expressions; no per-iteration `KeyValuePair` boxing/closure in the hot path; zero allocs (struct enumerator). Readability: fold intent explicit.

**A2/A4 — BaseValidationService / CheckEachAsync merge**
```csharp
return results.AsValueEnumerable().Aggregate(result, (r, x) => r + x);
```
Win: loop → one line; zero allocs.

**A3 — ValidationFailureException.TryGetException**
```csharp
return failures.AsValueEnumerable()
    .Where(f => f.Exception is not null)
    .Aggregate((Exception?)null, (acc, f) => acc!.Merge(f.Exception));
```
Win: filter+reduce in one expression; no allocs.

**A5/A6 — group-into-dictionary (ValidationErrorsProblemDetails, ObjectValidationService)**
```csharp
return failures.AsValueEnumerable()
    .GroupBy(f => f.PropertyName ?? string.Empty)
    .ToDictionary(g => g.Key, g => g.Select(f => new ValidationError(f)).ToList(), StringComparer.OrdinalIgnoreCase);
```
Win: TryGetValue/Add/Add boilerplate (~12 lines) → 3 lines; same alloc profile (one list per group).

**A7/A8 — Topic.BuildTopicString / Sanitize (hot EMP deserialization path)**
```csharp
static ReadOnlySpan<char> ReplaceSpace(ReadOnlySpan<char> part)
    => ZLinq.Select(part, c => c == Space ? NotApplicable : c).ToArray(); // one small alloc; or Select into a pre-sized span via ToArrayPool
// BuildTopicString becomes:
return string.Join(TopicSeparator, topicParts.Select(ReplaceSpace)) + (part is not [Terminator] ? TopicSeparator + Terminator : "");
```
Win: eliminates per-part StringBuilder churn on a per-received-event path; code lines cut ~in half. (Exact win needs benchmark — string allocation semantics of `Select` output are the open question.)

**A9 — StringSimilarityExtensions upper-case fill**
```csharp
// now: for (var i = 0; i < rightLength; i++) rightUpper[i] = char.ToUpperInvariant(right[i]);
// ZLinq: rightUpper[..rightLength] = right.AsSpan().Select(c => char.ToUpperInvariant(c)).ToArrayPool();
```
Win: mostly code lines (2 → 1); allocs unchanged if using `ToArrayPool`; DP loops stay hand-rolled (B2).

**A10/A11 — MultipartStream.GetStreams / Length**
```csharp
// GetStreams:
_streams.AsValueEnumerable().Select(s => s.Stream).CopyTo(list); // or ToArray()
// Length:
return _streams.AsValueEnumerable().Aggregate(0L, (len, s) => len + s.Stream.Length);
```
Win: loop → expression; zero allocs; the `[SuppressMessage("ForCanBeConvertedToForeach", "avoid allocating an enumerator")]` suppression can be dropped.

**A12–A24 — trivial copies/lookups**
```csharp
// A12: tagList.AddRange(additional);           // (ZLinq not even needed — AddRange does it)
// A13/A14/A20/A21: tags.AsValueEnumerable().CopyTo(tagList);  // or AddRange
// A15: filter.EventsIds.Contains(data.EventId)
// A16/A17/A19: source.AsValueEnumerable().FirstOrDefault(kvp => kvp.Key == "{OriginalFormat}")
// A18: furtherSampledPaths.AsValueEnumerable().Any(p => ...)
// A22/A23: parts.AsValueEnumerable().JoinToString(", ")
```
Win: readability only; alloc impact ~zero either way. Several of these are *better served by plain `AddRange`/`Contains`* than by ZLinq — noted honestly below.

---

## Ranked top-5 shortlist for benchmarking

| Rank | Target | Why |
|---|---|---|
| 1 | **A7/A8 — `Topic.BuildTopicString`/`Sanitize`** (`Bunnings.Common.PubSub.Emp`) | Runs on every EMP event received (wire deserialization path). String span mapping + join is exactly ZLinq's home turf. Biggest expected perf win (StringBuilder churn per part) and most representative of "string processing" category. |
| 2 | **A1 — `Validations.CheckEach` ×4** (`Bunnings.Common.Validation`) | Validation is called on every request payload; the 4 near-identical fold loops are the largest clean (A) block of code in the repo. Representative of the "fold over collection" pattern; biggest code-reduction win. |
| 3 | **A9 — `StringSimilarityExtensions` upper-case fill** (`Bunnings.Common.String`) | Foundation component, char-span SIMD-friendly transform, already allocation-optimized — clean A/B to measure whether ZLinq `Select` beats a hand-rolled span loop. Representative of "span transform" category. |
| 4 | **A2+A3 — `BaseValidationService` merge + `ValidationFailureException.TryGetException`** | Simple `Aggregate`/`Where`+`Aggregate` on per-request validation data; small but easy, zero-risk benchmark baseline for ZLinq `Aggregate`. |
| 5 | **A10/A11 — `MultipartStream.GetStreams`/`Length`** (`Bunnings.Common.Stream`) | Representative "span projection/aggregate" case; also the only production file with an explicit `ForCanBeConvertedToForeach … avoid allocating an enumerator` suppression ZLinq would let you delete. |

---

## Overall assessment

**Concentration:** the win is **concentrated, and modest**. Of 19 (A) files, ~8 hold the 90% of the value (Topic, CheckEach, BaseValidationService, ValidationFailureException, the two grouping sites, StringSimilarity, MultipartStream). The other ~11 sites (A12–A24) are one-liner copies/lookups where plain `AddRange`/`Contains`/`FirstOrDefault` achieves the same with no new dependency.

**Why the win is smaller than the pattern count suggests:**
1. The codebase already does zero-alloc by hand: `ArrayPool`, `StringBuilderInstancePool`, `stackalloc`, index loops with explicit `// Avoid Enumerator allocation` comments. ZLinq largely *re-expresses* what is already allocation-free — the gain is readability, not allocs, for most (A) sites.
2. The genuinely perf-critical hot loops (Levenshtein DP, Solace batching, pipeline executors, stream transforms, JSON writers) are all (B): stateful, async, IO, or side-effect — precisely the territory ZLinq's sync in-memory span LINQ does not cover.
3. **Bunnings.Common.Json has no ZLinq opportunity at all.** Its converters are `Utf8JsonReader`/`Utf8JsonWriter`-based; ZLinq's LINQ-to-Tree operates over `JsonNode` axes, and the repo contains no manual `JsonNode` tree traversal to replace. The only "JSON" loop (AttributeWriterHelper) is a side-effect write (B3).

**Is ZLinq integration earned?** Not repo-wide. It is *marginally* earned as a targeted opt-in in 3–4 Foundation/Validation/PubSub files (Topic, CheckEach, StringSimilarity, ValidationService), where it removes real boilerplate on per-event/per-request paths. As a blanket dependency of `Bunnings.Common` it would touch mostly (B) code it can't help and mostly trivial (A) code where `AddRange` suffices. Recommend: benchmark the top-5 first; if Topic/CheckEach show a measurable win, adopt ZLinq per-component rather than repo-wide.

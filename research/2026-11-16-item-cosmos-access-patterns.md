# Item repo — Cosmos migration access-pattern research

Date: 2026-11-16 · Repo: `/home/bmazzarol/work/item` (read-only, no code changed)

Question under review (captain): *"if there are many cases where all the parts of the item are loaded
all at once, it might make sense to add locations to scope before messages"* — and whether co-locating
everything in one container beats separate containers.

Parts of the item family and where they live today:

| Part | Store | Key / name shape | Evidence |
|---|---|---|---|
| Item doc | Cosmos `item` container | doc id `{itemNumber}`, PK `{country}_{itemNumber}` | `src/dotnet/Item.Core.ItemQuery/ItemQueryService.cs` (`ReadItemAsync(itemNumber, new PartitionKey($"{countryCode}_{itemNumber}"))`); ingest: `src/dotnet/Item.Core.Item/ItemTransform.cs:24-25` |
| Enriched | Blob `item` container (read); **Cosmos `item` in flight** (#43, branch `enriched-cosmos-write-path`) | blob `{country}/{itemNumber}_enriched.json`; branch writes Cosmos doc id `{itemNumber}_enriched`, PK `{country}_{itemNumber}_enriched`, `docType="enriched"` | read: `src/dotnet/Item.Core.ItemQuery/ItemEnrichedRepository.cs`; write (branch): `src/dotnet/Item.Core.EnrichedItem/EnrichedItemTransform.cs` (branch `enriched-cosmos-write-path`); `EnrichedItemState.cs` blob builder |
| Locations | Blob `itemlocation` container (separate storage account) | blob `{country}/{locationCode}/{itemNumber}.json` — one doc per (item × location) | read: `src/dotnet/Item.Core.ItemQuery/ItemLocationQueryService.cs` (`BlobNameBuilder`); write: `src/dotnet/Item.Bkg.ItemLocation/Module.cs:106-107`, `src/dotnet/Item.Core.ItemLocation/ItemLocationState.cs:47-48` |
| Messages | Table Storage | PK `{locationCode}_{itemNumber}`, RowKey `{messageId}`, 1 row per message | read: `src/dotnet/Item.Bkg.ItemMessage/Services/TableStorageItemMessageStore.cs` (`GetMessagesAsync`); write: `src/dotnet/Item.Bkg.ItemMessage/Services/ItemMessageTransform.cs` |
| Status (catalog) | Cosmos `status` container, single PK `ItemStatus`, 1-hour in-memory cache | — | `src/dotnet/Item.Core.ItemQuery/ItemStatusQueryService.cs`; `src/dotnet/Item.Core/Containers.cs` |
| Location metadata (zone/region) | external Location API, in-memory cached | — | `src/dotnet/Item.Core.ItemQuery/Location/LocationApiResolver.cs` |

Constants: `src/dotnet/Item.Core/BlobContainers.cs` (`item`, `itemlocation`), `src/dotnet/Item.Core/Containers.cs` (`item`, `status`),
`src/dotnet/Item.Core/ItemStorageSettings.cs` (item, itemlocation, message connection strings — separate accounts).

## 1. API surface → parts matrix

All operations in `src/dotnet/Item.Api.Index/Operations/*/Operation.cs`; pipelines in `src/dotnet/Item.Api.*/Pipeline.cs`.

| Endpoint (route) | Item doc | Enriched | Locations | Messages | Status | Other | Load shape |
|---|---|---|---|---|---|---|---|
| `GET /discovery` | – | – | – | – | – | – | none |
| `GET /detail/{country}/{itemNumber}` (GetItemDetail) | ✅ point read | ✅ blob read | – | – | – | – | 2-part, single item |
| `GET /details/{country}?itemNumbers=` (GetItemDetails) | ✅ 1 Cosmos query | ✅ N blob reads, parallel (`Task.WhenAll`) | – | – | – | – | 2-part, batched |
| `GET /locations/{country}?itemNumbers=&locationCodes=[&includeItemDetail][&includeMessages]` (GetItemLocations) | opt. | opt. | ✅ N×M blob reads | opt. | cached | opt. Location API HTTP | **only endpoint that can load all 4 parts in one request** |
| `GET /locations/{country}/messages?…` (GetItemLocationsMessages) | – (no includeItemDetail param; `Pipeline.cs` passes only `includeMessages`, default true) | – | ✅ N×M blob reads | ✅ | cached | – | 2-part (locations+messages) |
| `GET /locations/{country}/search?…` (SearchItemLocations) | – | – | ✅ N×M blob reads | opt. | cached | – | same code path as above (`ItemLocationQueryService.GetItemLocationsAsync`) |
| `GET /itemStatus` (GetItemStatusList) | – | – | – | – | ✅ | – | 1-part |
| `GET /list/{country}` (ListItems) | ✅ full-country cross-partition query | ✅ per-item blob reads, **sequential** in loop | – | – | – | – | 2-part, unbounded batch |
| `POST /search/{country}` (SearchItems) | details=true only | via `CoveoSearchProvider`/`VertexSearchProvider` → `ItemQueryService.GetItemsAsync` | – | – | – | external search engine (Coveo/Vertex) | 2-part when details=true |
| `POST /recommendations/{country}` (Recommendations) | details=true only | via `ItemQueryService` (injected into `VertexRecommendationProvider`) | – | – | – | external Vertex | 2-part when details=true |

Evidence for SearchItems/Recommendations details path: `src/dotnet/Item.Core.ItemQuery/CoveoSearchProvider.cs:31`
(`itemQueryService.GetItemsAsync`), `src/dotnet/Item.Core.Vertex/VertexRecommendationProvider.cs:18`,
`src/dotnet/Item.Core.Vertex/VertexSearchProvider.cs:18`.

Batch limits: `src/dotnet/Item.Api.Index/RequestValidation.cs` — `MaxItemLocationRecords = 100`,
`IsWithinLocationProductLimit`: **itemCount × locationCount ≤ 100** (N×M bounded per request).
Item numbers alone ≤ 100 (`Item.Api.GetItemLocations/Request.Validator.cs`, `Item.Api.GetItemDetails/Request.Validator.cs`).

### Combined-load counts (10 data endpoints)

- **All 4 parts in one request: 1 endpoint** — GetItemLocations (`includeItemDetail=true&includeMessages=true`; `Item.Api.GetItemLocations/Pipeline.cs`).
- **Locations + messages (2 parts, always together): 3 endpoints** — GetItemLocations (messages), GetItemLocationsMessages, SearchItemLocations. Note there is **no endpoint that returns messages without locations or locations without messages** — the two always travel together.
- **Item + enriched (2 parts): 4 endpoints** — GetItemDetail, GetItemDetails, ListItems, SearchItems/Recommendations (details=true, via provider).
- **Single-part: 2 endpoints** — GetItemStatusList, Discovery (no data).

So "all parts at once" is exactly **one code path**: `ItemLocationQueryService.GetItemLocationsAsync`, called by 3 of the 10 endpoints.

## 2. Round-trip analysis — the combined path (GetItemLocations, N items × M locations, N×M ≤ 100)

`src/dotnet/Item.Core.ItemQuery/ItemLocationQueryService.cs` `GetItemLocationsAsync`:

1. `GetSellableStatusesAsync` → status container, but singleton 1-hour cache (`ItemStatusQueryService`, `Module.cs` comment "Singleton: the status cache … must be shared across requests"). Warm: **0 round trips**; cold: 1.
2. `includeItemDetail` → `_itemQueryService.GetItemsAsync` = **1 Cosmos cross-partition query** + **N enriched blob downloads** (parallel, `ItemQueryService.GetItemsAsync` `Task.WhenAll`) + Location API (in-memory cached: warm 0, cold 1 HTTP).
3. Per (item, location) pair, `ReadLocationAsync` = **1 blob download** (`{cc}/{location}/{item}.json`, 404 ⇒ null) — all N×M in `Task.WhenAll`.
4. `includeMessages` → per pair `_messageStore.GetMessagesAsync` = **1 Table Storage partition query** each — also inside the N×M fan-out.

**Today (warm): 1 Cosmos query + N enriched blobs + N×M location blobs + N×M table queries.**
Worst case (N=1, M=100): 1 + 1 + 100 + 100 = **202 storage ops**. Typical (N=1, M=10): **21 ops**.
(Non-combined endpoints: GetItemDetail = 2 ops; GetItemDetails(N) = 1 + N; ListItems = 1 + N *sequential*.)

### Design (a) — #43 + #76 as proposed (item container holds item + enriched; dedicated `itemMessage` Cosmos container, PK `{locationCode}_{itemNumber}`; locations still blob)

- Item: 1 Cosmos query (unchanged).
- Enriched: #43 branch writes enriched as a **separate doc with its own partition** (PK `{cc}_{item}_enriched`, `EnrichedItemTransform.cs` on branch). Read = N Cosmos point reads (or 1 batched cross-partition query). Slightly cheaper than blob; same round-trip shape.
- Locations: **unchanged, still N×M blob downloads** — this is the dominant cost of the combined path.
- Messages: 1 Cosmos query over the `itemMessage` container fanning out over the N×M `{location}_{item}` partitions (replaces N×M table queries).

**(a): 1 + N + N×M + 1.** For N=1, M=10: 12 ops (was 21). The N×M blob term remains.

### Design (b) — everything co-located in the `item` container

Requires: locations as docs with PK `{cc}_{item}` (id e.g. `{location}_{item}`), messages with PK `{cc}_{item}`
(id e.g. `{location}_{item}_{messageId}`), and enriched PK changed from #43's `{cc}_{item}_enriched` to `{cc}_{item}`
(id `{item}_enriched`) — otherwise a partition-scoped read on the item PK misses enriched.

- Combined path: 1 cross-partition query (or N PK-hinted partition queries `WHERE c.partitionKey=@pk AND …`)
  returns item + enriched + all location docs + all message docs per item. **~1–N round trips total.**
- What it **breaks/costs**:
  1. **Scan-cost regression on every item-only cross-partition query.** `GetItemsAsync`
     (`WHERE c.countryCode=@cc AND ARRAY_CONTAINS(@itemNumbers, c.itemNumber)`), `ListItemsAsync`
     (full-country `WHERE c.countryCode=@cc`), SearchItems/Recommendations details all match by
     `itemNumber` only — with 10s of location docs + messages per partition (a wide item lives in ~400–500
     stores), each partition scanned returns every doc then filters. `ListItems` (no OData) = whole-country
     scan of a container that now holds the entire location universe. Cosmos charges RU for full-partition
     scans regardless of predicates; a `docType`/`id` filter does not reduce that. `ListItemsAsync` today is
     already the heaviest endpoint (`ItemQueryService.ListItemsAsync`), and it would multiply.
  2. **Hot partitions on writes.** Location ingestion is per (item × location) extract events
     (`Item.Bkg.ItemLocation/Module.cs`); under (b) up to ~500 doc upserts per partition per extract cycle
     concentrate on item partitions, against the 20,000 RU/partition write ceiling. Today those writes go to a
     separate storage account with no partition ceiling.
  3. **#43 must change PK** from `{cc}_{item}_enriched` to `{cc}_{item}` — cheap while in flight (flag-gated
     write + backfill only, `EnrichedItem__CosmosWriteEnabled` in branch), a rewrite after cutover.
  4. One container's RU/throughput budget now carries item reads + enriched + the full location/message volume
     (autoscale copes, but cost model and blast radius change; a location-extract spike starves item reads).
- Point reads are unaffected: GetItemDetail still `ReadItemAsync(itemNumber, {cc}_{item})` — O(1) regardless of partition contents.

## 3. Location data specifics

- **Domain**: `Item.Core.ItemLocation/Domain/ItemLocation.cs` (XSD-generated). One doc per (country × location × item):
  `ItemNumber`, `LocationCode`, `ItemStatus`, `IsOnRecall`, `IsRanged`, `Availability`, `PrimaryBarcode`,
  `InStoreLocations[]` (aisle/bay/sequence), `Suppliers[]` (with domestic lead times), `ReplenishmentDetails`
  (planner code, stocking DCs, min/max). Small JSON — low KB.
- **Count shape**: 1 doc per *ranged* (item, store) pair. Ingest events are per-pair (`ItemLocationMessage` =
  header + single `ItemLocation`, `ItemLocation.cs`). Wide item → ~400 (AU) + ~120 (NZ) docs.
- **Blob read pattern**: whole file per (item, location) pair — `blobClient.DownloadAsync` on
  `{cc}/{location}/{item}.json` (`ItemLocationQueryService.ReadLocationAsync`); 404 → null (not ranged there).
  Always both item and location are known from the request; **no endpoint reads "all locations of an item"** —
  every location read is for explicit `(itemNumbers × locationCodes)` pairs (N×M ≤ 100).
- **Cosmos shape candidates**:
  - PK `{cc}_{item}`, id `{location}_{item}`: all of an item's stores in one partition → combined request = N
    PK-hinted partition queries (one per distinct item) returning location docs (+ messages if co-located).
    Batched read cost ∝ partition size (scans all stores of the item even if M is small), but N is tiny (≤100,
    usually 1–10).
  - PK `{cc}_{location}_{item}` (or `#item`): per-pair partitions → N×M point reads, or 1 cross-partition query
    for the batch. No per-item partition, no hot-partition write concentration; loses the "one query per item"
    shape.
  - Query patterns from code: only per-pair reads today; the only batch shape is the N×M fan-out in
    `GetItemLocationsAsync`. No search on location fields is served from storage (SearchItemLocations is the same
    fan-out + in-memory OData; `Item.Api.SearchItemLocations/Pipeline.cs`).

## 4. RECOMMENDATION

**Scope locations before (or alongside) messages; do NOT put locations into the `item` container.** Use a
**dedicated `itemLocation` Cosmos container with PK `{country}_{itemNumber}`, doc id `{locationCode}_{itemNumber}`**
(same PK scheme as the item container), and let messages follow #76 into `itemMessage` — or co-locate messages in
the same `itemLocation` container under the same PK (both are supported by the access patterns; co-location saves
one container and one round trip).

Evidence:

1. **The captain's premise is half right.** There is exactly one "all parts at once" path
   (`ItemLocationQueryService.GetItemLocationsAsync`, 3 endpoints) and it is a fan-out of N×M ≤ 100 pair reads.
   Co-locating in one container does reduce it to ~1–N round trips — but so does a dedicated container with the
   same item PK: N PK-hinted partition queries (locations) + 1 query (messages) + 1 query (items) ≈ **N+2 round
   trips**, vs **2·N·M + N + 1 today** and **N·M + 2 under design (a)**. For the typical N=1, M=10 call: 3 vs 21
   vs 12. The win from moving locations to Cosmos is obtained *without* putting them in the item container.
2. **Locations and messages always travel together** (no endpoint splits them), so one `itemLocation`-shaped
   Cosmos store for both is the natural boundary.
3. **Keeping locations out of the `item` container avoids a real regression**: item-only cross-partition queries
   (`ListItemsAsync` full-country scan, `GetItemsAsync`, search details) would otherwise scan every item's
   hundreds of location/message docs per partition (RU for scan, not filter). This is the concrete "what breaks"
   for design (b), and it affects the highest-volume endpoints (ListItems, SearchItems).
4. **#43's in-flight design already establishes the right pattern**: same container family, separate partition per
   doc type (enriched PK `{cc}_{item}_enriched`). The in-branch comment in `EnrichedItemTransform.cs`
   ("future doc types e.g. 'message', 'embedding' coexist as new values") anticipates multi-doc-type containers,
   but the PK choice shows the team's intent is *partition isolation per part*, not one mega-partition.
   Mirroring that (dedicated location container, own PK) is the consistent move.
5. **Write-side risk stays contained**: location extract upserts concentrate on item partitions under (b)
   (≤ ~500 docs/partition/extract vs the 20k RU/partition ceiling). A dedicated `itemLocation` container takes
   that load on its own partitions/RU budget instead of coupling it to item reads on the shared `item` container.
6. **Migration cost is low**: location blob doc body is already the JSON we'd store (same XSD entity,
   `StripPropertiesJsonConverter`); the swap is upsert client (Blobs → Cosmos, cf. #43's
   `AddCosmosUpserter`) + read client (`ReadLocationAsync` blob download → point/partition query), with the
   existing 404 ⇒ null semantics mapping to Cosmos 404.

Container layout summary:

```
Cosmos `item`         : item doc (PK {cc}_{item}) + enriched doc (PK {cc}_{item}_enriched, #43 as-is)
Cosmos `itemLocation` : location doc (PK {cc}_{item}, id {location}_{item})   ← NEW, before messages
Cosmos `itemMessage`  : message doc (PK {location}_{item} per #76, or {cc}_{item} to co-locate with locations)
Cosmos `status`       : unchanged (cached catalog)
```

If per-item partitioning of locations proves too hot during full extracts (watch 20k RU/partition), fall back to
PK `{cc}_{location}_{item}` in the same container — the read pattern (per-pair point reads / one batched
cross-partition query) still beats the blob fan-out, and no other endpoint is affected.

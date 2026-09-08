# Food Truck API

A small read-only ASP.NET Core API that finds approved San Francisco food trucks
near a location, optionally filtered by a food preference, ordered by distance.

Built for [buhlergroup/dev-challenge-01](https://github.com/buhlergroup/dev-challenge-01).

---

## The endpoint

```
GET /api/food-trucks?latitude={lat}&longitude={lon}&amountOfResults={n}&food={preference}
```

| Parameter        | Required | Rules                                             | Default |
|------------------|----------|---------------------------------------------------|---------|
| `latitude`       | yes      | number, −90 to 90                                 | —       |
| `longitude`      | yes      | number, −180 to 180                               | —       |
| `amountOfResults`| no       | integer, 1 to `MaxAmountOfResults` (50)           | 10      |
| `food`           | no       | text, ≤ 100 chars; blank means "no preference"    | —       |

### Example

```
GET /api/food-trucks?latitude=37.7955&longitude=-122.3937&food=tacos&amountOfResults=3
```

```json
{
  "query": { "latitude": 37.7955, "longitude": -122.3937, "amountOfResults": 3, "food": "tacos" },
  "count": 3,
  "results": [
    {
      "name": "Senor Sisig",
      "facilityType": "Truck",
      "address": "SPEAR ST",
      "latitude": 37.7929,
      "longitude": -122.3944,
      "distanceKm": 0.479,
      "foodItems": "Senor Sisig: Filipino fusion food: tacos: burritos: nachos",
      "matchScore": 1.0
    }
  ]
}
```

- `matchScore` (0–1) and `query.food` appear only when a `food` preference was supplied.
- Results are always ordered by `distanceKm` ascending. `food` is a filter, not the sort key.
- An empty `results` array with `200` is a valid answer (nothing matched).

### Errors

Validation failures return **one** [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)
problem document listing **every** problem at once:

```json
// GET /api/food-trucks?amountOfResults=0
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "latitude": ["latitude is required and must be a number between -90 and 90."],
    "longitude": ["longitude is required and must be a number between -180 and 180."],
    "amountOfResults": ["amountOfResults must be between 1 and 50."]
  }
}
```

`429` (rate limit) and `500` (unexpected) also return problem documents.

Interactive docs: **Swagger UI at `/swagger`** (Development environment only).

**`GET /health`** — liveness/readiness probe. `200 Healthy` once the dataset is
loaded, `503` otherwise. Not rate-limited, not part of the API surface.

---

## Running it

### Prerequisites

- .NET SDK 8.0 (pinned in `global.json`). Or open the repo in the provided
  `.devcontainer` (VS Code → "Reopen in Container").
- **Network access on the first build** — `SmartComponents.LocalEmbeddings` downloads
  the ~22 MiB embedding model then (cached afterwards; it never leaves the machine at runtime).

### Commands

```bash
dotnet run --project src/FoodTruckApi                     # http://localhost:5065
dotnet run --project src/FoodTruckApi --launch-profile https   # + https://localhost:7007
dotnet test                                               # 112 tests
```

Then open `http://localhost:5065/swagger`, or:

```bash
curl "http://localhost:5065/api/food-trucks?latitude=37.7955&longitude=-122.3937&food=tacos"
```

---

## Data source

The data is **San Francisco's Mobile Food Facility Permit dataset**, the CSV dump
provided with the challenge (`Mobile_Food_Facility_Permit.csv`). It is used
**unmodified** and **read-only**.

It is **compiled into the assembly as an embedded resource**, so the app ships as
a single artifact and the data cannot be swapped or tampered with at runtime. The
loader parses it once at startup into an immutable in-memory snapshot; at ~160
rows there is no database and no index.

The `IFoodTruckRepository` seam means a live source (the SF Socrata open-data API)
could be added later as an alternative implementation with no change to the rest
of the app. That is out of scope here — the challenge ships a static dump and asks
for a read-only POC.

### Which rows are kept

A row is included only if **all** of:

| Filter                          | Rows |
|---------------------------------|------|
| `FacilityType = Truck`          | drops 54 push carts + 8 blank |
| `Status = APPROVED`             | drops requested / expired / suspended |
| valid, non-zero `Latitude`/`Longitude` | drops rows with no usable location |

→ **158 trucks** out of 481. Skipped rows are counted and logged at startup.

**`ExpirationDate` is deliberately not used as a filter.** Every `APPROVED` permit
in this snapshot expired in 2023–2024, so filtering by permit validity date would
return zero results. The dataset is treated as a point-in-time snapshot and
`Status = APPROVED` is taken as the authority.

---

## How food matching works

The `FoodItems` field is real-world messy — `"Tacos: Burritos"`, but also
`"Everything"`, `"everything except for hot dogs"`, `"Multiple Food Trucks & Food
Types"`, `"MOMO Spicy Noodle: POPO's Noodle"`. Matching a query well against that
needs more than string comparison, so there are two problems solved separately.

### 1. Structure — `FoodOffering` (parsed once at load)

`FoodOfferingParser` turns each listing into:

| Field | From |
|---|---|
| `Terms` | the listed foods, normalized + Porter2-stemmed (so `tacos` ↔ `taco`) |
| `ServesEverything` | catch-all phrasing — `Everything`, `Multiple … Food Types` |
| `Excludes` | the negated part of `everything except / but <X>` (run through the same normalizer) |

So `everything except for hot dogs` → `ServesEverything`, `Excludes = ["dog"]`.
A search for `tacos` matches it (0.85); searches for `hot dogs`, `frankfurter` or
`bratwurst` do not — the exclusion check goes through `FoodAliases`, so synonyms
of the ruled-out food are ruled out too.

### 2. Similarity — `HybridFoodMatcher`

For a listed-food match it takes the strongest of three signals:

| Signal | Example | Score |
|---|---|---|
| exact stem | `burritos` ↔ `Burritos` | `1.0` |
| **alias** (`FoodAliases`, ~16 curated groups) | `bbq` ↔ `barbecue`, `mexican` ↔ a taco truck | `0.9` |
| **fuzzy** (`FuzzySharp.WeightedRatio`, floored at ~0.55) | `burito` → `burrito` ≈ `0.92` | `0.55–0.89` |
| **semantic** (on-device `bge-micro-v2` embedding, cosine) | `italian` → a pizza/pasta listing | scaled from `SemanticFloor`→`SemanticStrong` |

Terms shorter than 4 characters must match exactly or via an alias — short tokens
collide too easily under fuzzy matching (`ice` is inside `rice`).

Each truck's listing is embedded **once at startup** (`FoodEmbeddingIndex`,
~150 ms for the whole dataset); the query is embedded once per request. The final
score is `max(catch-all, lexical, semantic)`, and a truck is returned when it is
≥ `FoodMatching:MatchThreshold` (default `0.7`).

### Limits

- The embedding model is tiny (22 MiB, quantized) — it reliably bridges near-paraphrases
  but not loose conceptual leaps; the alias table carries the common cuisine/synonym cases.
- The exclusion covers the excluded word and its aliases, but not a semantic leap the
  alias table doesn't know about.
- `IFoodMatcher` / `IFoodEmbedder` are the seams — a stronger (or hosted) model is a
  DI swap.

---

## Configuration

`appsettings.json` (all sections validated at startup — the app refuses to start
on invalid values):

```jsonc
{
  "FoodTruckSearch": {
    "DefaultAmountOfResults": 10,   // used when the caller omits amountOfResults
    "MaxAmountOfResults": 50        // upper bound; DefaultAmountOfResults must not exceed it
  },
  "FoodMatching": {
    "MatchThreshold": 0.7,          // 0..1; overall bar for including a truck
    "SemanticFloor": 0.58,          // cosine at/below this adds no semantic score
    "SemanticStrong": 0.72          // cosine at/above this is a full semantic match
  },
  "RateLimiting": {
    "PermitLimit": 100,             // requests per window, per client
    "WindowSeconds": 60,
    "ClientIdentifierHeader": ""    // e.g. "X-Forwarded-For" when behind a trusted proxy;
                                    // empty => partition by the direct connection IP
  },
  "Cors": {
    "AllowedOrigins": []            // empty => no CORS headers (same-origin only)
  }
}
```

---

## Architecture

Single project, layered with dependency inversion at the seams that matter. It is
**Clean-Architecture-influenced**, not a textbook implementation — one project
instead of one per layer, no MediatR/CQRS.

```
Api            controllers, request/response contracts, validation, Result → HTTP
  │
Application    FindFoodTrucksHandler, FindFoodTrucksQuery, and the ports:
  │              IFoodTruckRepository, IFoodMatcher, IFoodEmbedder, IDistanceCalculator
  │
Domain         FoodTruck, FoodOffering, Coordinate, Result / Error   (references nothing)
  ▲
Infrastructure adapters: CsvFoodTruckRepository, HybridFoodMatcher,
               LocalFoodEmbedder + FoodEmbeddingIndex, HaversineDistanceCalculator
```

`Program.cs` is ~15 lines; the DI registration and middleware pipeline live in
`Startup/ServiceRegistration.cs` and `Startup/RequestPipeline.cs`, each grouped by
concern.

Key decisions:

- **Result pattern, not exceptions**, for expected failures. A hand-rolled
  `Result` / `Result<T>` carries a *list* of `Error`, each with an `ErrorType`
  category. `ResultActionExtensions` is the single place that maps a failure to
  an HTTP status code — the inner layers never mention HTTP.
- **All input validation is owned by the app** (`FindFoodTrucksRequestValidator`),
  not by `[ApiController]` model binding, so every problem is collected into one
  response.
- **Fail fast at startup** — the dataset is loaded, the embedding index is built,
  and the options are validated during startup, not on the first request.
- **Haversine** great-circle distance; no spatial index needed at this size.

Packages: `CsvHelper`, `Porter2Stemmer`, `FuzzySharp`, `SmartComponents.LocalEmbeddings`
(preview — on-device embeddings), `Swashbuckle`.

---

## Security / hardening

- **Rate limiting** — fixed window per client; `429` + `Retry-After` + problem body.
  Partitions by the direct connection IP, or by a configured header
  (`RateLimiting:ClientIdentifierHeader`) when behind a trusted reverse proxy.
- **Security headers** — `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`,
  `Referrer-Policy: no-referrer`, `X-Permitted-Cross-Domain-Policies: none`; the
  `Server` header is suppressed.
- **HSTS** + HTTPS redirection (HSTS outside Development).
- **CORS** off by default; opt-in per origin via config.
- **Problem-details** for unhandled exceptions — no stack traces in Production.
- Every CSV field is treated as untrusted input: explicit typed parsing,
  malformed rows skipped and counted.
- Swagger UI is exposed in Development only.

---

## Tests

```bash
dotnet test
```

112 xUnit tests: `Result` / `Coordinate` invariants, the CSV loader (real dataset →
exactly 158 trucks, quoted commas preserved), Haversine against known reference
distances, the offering parser (catch-all + exclusions), the hybrid matcher
(alias, fuzzy, catch-all, and one real-model semantic case), the handler
(ordering, threshold, limits — with a stub matcher), request validation, and full
HTTP integration via `WebApplicationFactory` (validation, config overrides, rate
limiting, security headers, CORS, logging, health check). Integration tests swap
in a stub embedder so they don't load the model.

---

## Project layout

```
src/FoodTruckApi/            the API
tests/FoodTruckApi.Tests/    the tests
Mobile_Food_Facility_Permit.csv   the SF dataset (embedded at build time)
global.json                  pins the SDK to 8.0
```

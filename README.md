# 🔗 Shortly — URL Link Shortener

A small but complete URL shortener: turn long URLs into short links, track
clicks, resolve to platform-specific destinations, and manage links. Built with
an **ASP.NET Core (.NET 9)** backend and a **React + TypeScript (Vite)** frontend.

---

## Table of contents
- [Features](#features)
- [Architecture](#architecture)
- [Getting started](#getting-started)
- [API contract](#api-contract)
- [Design decisions](#design-decisions)
- [Testing](#testing)
- [How I'd extend it](#how-id-extend-it)

---

## Features

| # | Capability | Notes |
|---|------------|-------|
| 1 | Create short link | Auto-generated code or custom alias; URL validated |
| 2 | View access stats | Click count, created time, last accessed time |
| 3 | Disable / delete | Disabled links stop redirecting but are kept; delete removes them |
| 4 | Platform-specific destinations | iOS / Android / default, decided at redirect time from the User-Agent |
| 5 | Pluggable short-code generation | Strategy pattern — add new generators without touching existing code |

Bonus: responsive mobile-first UI (Tailwind CSS, light/dark), copy-to-clipboard,
QR codes (`qrcode.react`), helpful validation errors, thread-safe in-memory store
with a swappable data layer.

---

## Architecture

A **vertical-slice** layout inside a single API project — deliberately chosen
over a 4-project Clean Architecture split because the scope is small and the
assignment explicitly values *sound judgment over over-engineering*. The design
patterns that matter (Strategy, Repository, Validator) still live behind
interfaces; they just aren't spread across separate assemblies.

```
backend/
  src/UrlShortener.Api/
    Core/                        # domain + abstractions (no web concerns)
      Link.cs                    # entity: destinations, status, atomic click count
      Platform.cs, LinkStatus.cs # enums (state modeled as enums, not bools)
      Result.cs                  # Result<T> / Error — business failures without exceptions
      ShortCodes/                # pluggable code generation (Strategy + resolver)
        IShortCodeGenerator.cs
        RandomShortCodeGenerator.cs
        CustomAliasGenerator.cs
        ShortCodeGeneratorResolver.cs
        AliasRules.cs            # alias format + reserved-name rules
      Persistence/               # Repository abstraction + in-memory impl
        ILinkRepository.cs
        InMemoryLinkRepository.cs
      Platform/                  # User-Agent -> Platform detection
        IPlatformDetector.cs
        UserAgentPlatformDetector.cs
      Validation/                # URL validation
        IUrlValidator.cs
        UrlValidator.cs
    Features/                    # one folder per feature slice
      Links/                     # create, list, stats, disable/enable, delete
        LinkService.cs           # application logic
        LinksEndpoints.cs        # minimal-API endpoints
        Contracts.cs             # request/response DTOs
        LinkMapper.cs, ShortUrlOptions.cs
      Redirect/
        RedirectEndpoints.cs     # GET /{shortCode} -> 302
    Common/
      ResultExtensions.cs        # maps Error -> RFC 7807 ProblemDetails
    Program.cs                   # DI wiring
  tests/UrlShortener.UnitTests/  # xUnit + FluentAssertions + NSubstitute (87)

frontend/                        # Vite + React + TypeScript SPA (Tailwind CSS v4)
  src/
    api.ts                       # typed fetch client (throws ApiError from ProblemDetails)
    hooks/useLinks.ts            # TanStack Query: list query + create/status/delete mutations
    App.tsx                      # 3 sections: create form, links table, per-link actions
    components/CreateLinkForm.tsx, LinkCard.tsx, Logo.tsx
```

### Frontend server state — TanStack Query
The links list is **server state**, not UI state: it's fetched, mutated
(create / disable / delete), and — crucially — the click counts change on the
server whenever someone follows a link. TanStack Query fits that shape directly:
one `useLinks` query owns `isLoading` / `isError` / refetch; each mutation
exposes `isPending` / `error` for inline loading and error handling and
`invalidateQueries(['links'])` re-syncs the list after every change. A short
`staleTime` + refetch-on-focus keeps stats fresh when the user returns from
clicking a link — behavior I'd otherwise hand-roll in a custom hook. For four
endpoints it's a small dependency, but it removes exactly the loading/error/
refetch boilerplate this UI is mostly made of; a bespoke `useReducer` hook would
be the leaner alternative if avoiding the dependency mattered.

---

## Getting started

**Prerequisites:** .NET 9 SDK, Node.js 20+ (Tailwind CSS v4).

### 1. Backend (terminal A)

```bash
cd backend
dotnet run --project src/UrlShortener.Api --urls "http://localhost:5000"
```

API is now at `http://localhost:5000`. OpenAPI doc: `http://localhost:5000/openapi/v1.json`.

### 2. Frontend (terminal B)

```bash
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`. The dev server talks to the API on `:5000`
(configurable via `VITE_API_BASE`).

### Custom short domain (optional)

The base URL shown in short links is configurable — no real domain needed. Set
`ShortUrl:BaseUrl` in `backend/src/UrlShortener.Api/appsettings.json` (e.g.
`"https://gul.fy"`) and, if you want it to resolve locally, map `gul.fy` to
`127.0.0.1` in your hosts file and run Kestrel on that host. By default it uses
`http://localhost:5000` and treats the domain as a display value.

---

## API contract

Base URL: `http://localhost:5000`. All bodies are JSON. Errors use
[RFC 7807 ProblemDetails](https://datatracker.ietf.org/doc/html/rfc7807).

### Create a link — `POST /api/links`

```jsonc
// request
{
  "url": "https://www.google.co.th",   // required, absolute http/https
  "customAlias": "mylink",              // optional; auto-generated if omitted
  "destinations": {                      // optional platform overrides
    "android": "https://download.gulf.co.th/android.apk",
    "ios": "https://download.gulf.co.th/iphone.ipa"
  }
}
```

```jsonc
// 201 Created  (Location: /api/links/{shortCode})
{
  "shortCode": "HsQy5",
  "shortUrl": "http://localhost:5000/HsQy5",
  "destinations": { "default": "https://www.google.co.th/", "android": "…", "ios": "…" },
  "status": "Active",
  "clickCount": 0,
  "createdAt": "2026-07-13T10:00:00Z",
  "lastAccessedAt": null
}
```

| Status | When |
|--------|------|
| `201 Created` | success |
| `400 Bad Request` | invalid URL, malformed alias, unknown platform key |
| `409 Conflict` | custom alias already taken |

### List links — `GET /api/links`
`200 OK` → array of link objects, newest first.

### Get stats — `GET /api/links/{shortCode}`
`200 OK` → single link object · `404 Not Found` if missing.

### Enable / disable — `PATCH /api/links/{shortCode}`
```json
{ "status": "Disabled" }   // or "Active"
```
`200 OK` → updated link · `404 Not Found`. `PATCH` is used because it is a
partial update of one field, and the same endpoint re-enables a link.

### Delete — `DELETE /api/links/{shortCode}`
`204 No Content` · `404 Not Found`.

### Redirect — `GET /{shortCode}`
Detects the platform from the `User-Agent`, redirects to the matching
destination, and increments the click count.

| Status | When |
|--------|------|
| `302 Found` | active link — `Location` header holds the destination |
| `404 Not Found` | missing, disabled, or deleted link (no redirect) |

---

## Design decisions

### Pluggable short-code generation (Strategy + resolver)
`IShortCodeGenerator` has two implementations — `RandomShortCodeGenerator` and
`CustomAliasGenerator`. A `ShortCodeGeneratorResolver` picks the first one whose
`CanHandle` returns true (registration order = priority; the catch-all random
generator is last). **Adding a third strategy is a new class + one DI line — no
existing code changes** (Open/Closed).

### Why random codes, not Base62 of an auto-increment ID
Sequential-ID codes are enumerable (`/5` → try `/4`, `/6`), leak how many links
exist, and need a central counter to coordinate across instances. A
crypto-random 7-char code (`RandomNumberGenerator`, ~56⁷ space) is
unpredictable and needs no coordination. The trade-off — no built-in uniqueness
guarantee — is handled by collision retry.

### Collision handling
Uniqueness is enforced in one atomic place: `ILinkRepository.TryAddAsync`
(`ConcurrentDictionary.TryAdd`). On collision:
- **Random** generator → retry with a fresh code (up to 5 attempts).
- **Custom alias** → deterministic, so a collision is a real `409 Conflict`, no retry.

The policy is expressed by `IShortCodeGenerator.AllowRetryOnCollision`, so the
service never type-checks concrete generators.

### 302, not 301, for redirects
`301` is permanent and gets cached by browsers/proxies — which would **break
click counting** (later visits never reach the server) and platform routing. A
`302` is re-evaluated every visit, so every click is counted, the platform is
re-detected, and disabling a link takes effect immediately.

### Platform detection is its own seam
`IPlatformDetector` parses the User-Agent independently of link logic, so it's
unit-testable and swappable for a richer device-detection library. Unknown or
missing agents (bots, curl) fall back to `Default` so a link always resolves.

### State as an enum, atomic click counting
`LinkStatus` is an enum (not an `IsDisabled` bool) so the redirect makes one
check and new states (e.g. `Expired`) can be added later. Click counting uses
`Interlocked.Increment`, verified by a 10k-parallel-visit test.

### Swappable storage
Everything goes through `ILinkRepository`. The in-memory implementation uses a
case-insensitive `ConcurrentDictionary`; swapping in EF Core means one new class
and one DI registration — the service layer is untouched.

### Business errors as `Result`, not exceptions
The service returns `Result<T>` / `Result` carrying a typed `Error`
(`Validation` / `Conflict` / `NotFound`) instead of throwing for expected
failures. Callers must handle both outcomes explicitly, the happy path stays
free of exception control flow, and the endpoint maps `Error` → RFC 7807
ProblemDetails (`ToProblem`). Exceptions are reserved for genuinely unexpected
faults, which fall through to a default 500 ProblemDetails.

### Validation & edge cases
`UrlValidator` accepts only absolute `http`/`https` URLs — rejecting missing
schemes, `javascript:`, `ftp:`, `file:`, and relative paths. The service adds
two more guards: a destination may not point back at the shortener's own domain
(redirect loop), and a custom alias may not be malformed or a **reserved name**
(`api`, `openapi`, `health`, …) that would shadow a real route.

---

## Testing

```bash
cd backend
dotnet test
```

**87 unit tests** (xUnit + FluentAssertions + NSubstitute) cover the core logic
the assignment calls out:
- **short-code generation** — length, alphabet, fallback, retry policy, resolver selection
- **URL validation** — accepts http/https, rejects `javascript:`, `ftp:`, `file:`, relative, empty
- custom alias validation, reserved-name and self-loop rejection, case-insensitive uniqueness
- **click counting** — including 10k concurrent visits (atomicity) and last-accessed tracking
- **disable / delete** — re-enable, and that disabled/deleted links neither count nor redirect
- platform detection and platform-aware destination resolution

### Tests that prove the design is decoupled
`LinkServiceDecouplingTests` swaps each collaborator for a mock to show the
service depends only on abstractions and its behavior flows through those seams:

| Test | What it proves |
|------|----------------|
| `Create_uses_the_code_produced_by_the_injected_generator` | Code generation is fully delegated to `IShortCodeGenerator` — a brand-new strategy works untouched (Strategy / Open-Closed) |
| `Create_retries_…_when_the_repository_reports_a_collision` | Uniqueness is owned by `ILinkRepository`; the retry loop runs against the abstraction, not in-memory internals |
| `Create_does_not_retry_a_deterministic_generator_…` | `AllowRetryOnCollision` drives the decision — no concrete-type check |
| `Redirect_writes_the_click_back_through_the_repository` | Click-count persistence is a seam (`UpdateAsync`), swappable for a DB |
| `Create_rejects_when_the_url_validator_rejects` | URL validation is delegated to `IUrlValidator`, and storage is never touched on failure |
| `Resolve_selects_a_newly_added_strategy_…` | The resolver picks a newly registered generator purely via `CanHandle` |

---

## How I'd extend it

- **Real database** — implement `ILinkRepository` with EF Core; add a unique
  index on the short code to replace the in-memory atomic check.
- **Scale click counting** — move increments off the redirect hot path to an
  async queue / event stream and aggregate (eventual consistency).
- **Richer analytics** — record per-visit events (referrer, geo, timestamp)
  instead of a single counter.
- **More generators** — word-pair codes, hash-based, vanity domains — each a new
  `IShortCodeGenerator`.
- **Link expiry & auth** — add an `Expired` status and per-user ownership.

---

## AI usage

This project was built with an agentic AI assistant. See [`ai-logs/`](./ai-logs)
for the session log.

# 🔗 Shortly — URL Link Shortener

บริการย่อลิงก์ขนาดเล็กแต่ครบถ้วน: เปลี่ยน URL ยาว ๆ ให้เป็นลิงก์สั้น นับจำนวนคลิก
รองรับปลายทางตามแพลตฟอร์ม และจัดการลิงก์ได้ พัฒนาด้วย backend **ASP.NET Core (.NET 9)**
และ frontend **React + TypeScript (Vite)**

---

## สารบัญ
- [ความสามารถ (Features)](#ความสามารถ-features)
- [สถาปัตยกรรม (Architecture)](#สถาปัตยกรรม-architecture)
- [เริ่มต้นใช้งาน (Getting started)](#เริ่มต้นใช้งาน-getting-started)
- [API contract](#api-contract)
- [การตัดสินใจด้านการออกแบบ (Design decisions)](#การตัดสินใจด้านการออกแบบ-design-decisions)
- [การทดสอบ (Testing)](#การทดสอบ-testing)
- [แนวทางต่อยอด (How I'd extend it)](#แนวทางต่อยอด-how-id-extend-it)

---

## ความสามารถ (Features)

| # | ความสามารถ | รายละเอียด |
|---|------------|-------|
| 1 | สร้างลิงก์สั้น | auto-generate code หรือ custom alias; validate URL |
| 2 | ดูสถิติการเข้าถึง | จำนวนคลิก, เวลาที่สร้าง, เวลาเข้าถึงล่าสุด |
| 3 | Disable / delete | disabled = หยุด redirect แต่เก็บ record ไว้; delete = ลบทิ้ง |
| 4 | ปลายทางตามแพลตฟอร์ม | iOS / Android / default ตัดสินใจตอน redirect จาก User-Agent |
| 5 | Pluggable short-code generation | Strategy pattern — เพิ่มวิธี gen ใหม่ได้โดยไม่แก้โค้ดเดิม |

**Bonus:** UI responsive แบบ mobile-first (Tailwind CSS, light/dark), copy-to-clipboard,
QR code (`qrcode.react`), error message ที่เป็นประโยชน์, in-memory store ที่ thread-safe
และสลับเป็น database จริงได้

---

## สถาปัตยกรรม (Architecture)

ใช้โครงแบบ **vertical-slice** ภายใน API project เดียว — เลือกแทนการแยกเป็น Clean Architecture
4 project เพราะ scope เล็ก และโจทย์เน้น *sound judgment มากกว่า over-engineering*
design pattern ที่สำคัญ (Strategy, Repository, Validator) ยังอยู่หลัง interface
เพียงแต่ไม่ได้กระจายไปคนละ assembly

```
backend/
  src/UrlShortener.Api/
    Core/                        # domain + abstractions (ไม่มี web concern)
      Link.cs                    # entity: destinations, status, atomic click count
      Platform.cs, LinkStatus.cs # enums (state เป็น enum ไม่ใช่ bool)
      Result.cs                  # Result<T> / Error — business failure โดยไม่ throw
      ShortCodes/                # pluggable code generation (Strategy + resolver)
        IShortCodeGenerator.cs
        RandomShortCodeGenerator.cs
        CustomAliasGenerator.cs
        ShortCodeGeneratorResolver.cs
        AliasRules.cs            # กฎ format alias + reserved-name
      Persistence/               # Repository abstraction + in-memory impl
        ILinkRepository.cs
        InMemoryLinkRepository.cs
      Platform/                  # User-Agent -> Platform detection
        IPlatformDetector.cs
        UserAgentPlatformDetector.cs
      Validation/                # URL validation
        IUrlValidator.cs
        UrlValidator.cs
    Features/                    # หนึ่ง folder ต่อหนึ่ง feature slice
      Links/                     # create, list, stats, disable/enable, delete
        LinkService.cs           # application logic
        LinksEndpoints.cs        # minimal-API endpoints
        Contracts.cs             # request/response DTOs
        LinkMapper.cs, ShortUrlOptions.cs
      Redirect/
        RedirectEndpoints.cs     # GET /{shortCode} -> 302
    Common/
      ResultExtensions.cs        # map Error -> RFC 7807 ProblemDetails
    Program.cs                   # DI wiring
  tests/UrlShortener.UnitTests/  # xUnit + FluentAssertions + NSubstitute (87)

frontend/                        # Vite + React + TypeScript SPA (Tailwind CSS v4)
  src/
    api.ts                       # typed fetch client (throw ApiError จาก ProblemDetails)
    hooks/useLinks.ts            # TanStack Query: list query + create/status/delete mutations
    App.tsx                      # 3 ส่วน: create form, links table, per-link actions
    components/CreateLinkForm.tsx, LinkCard.tsx, Logo.tsx
```

### Frontend server state — TanStack Query
links list คือ **server state** ไม่ใช่ UI state: ถูก fetch, mutate (create / disable / delete)
และที่สำคัญคือ click count เปลี่ยนฝั่ง server ทุกครั้งที่มีคนกดลิงก์ TanStack Query เหมาะกับ
รูปแบบนี้ตรง ๆ: query `useLinks` ตัวเดียวคุม `isLoading` / `isError` / refetch; แต่ละ mutation
มี `isPending` / `error` สำหรับ loading และ error แบบ inline และ `invalidateQueries(['links'])`
sync list ใหม่หลังทุกการเปลี่ยนแปลง ตั้ง `staleTime` สั้น + refetch-on-focus ทำให้สถิติสดใหม่
เมื่อผู้ใช้กลับมาที่ tab หลังกดลิงก์ — ซึ่งถ้าทำ custom hook เองต้องเขียน logic นี้เพิ่ม
สำหรับ 4 endpoints ถือเป็น dependency ที่เล็ก แต่ตัด boilerplate เรื่อง loading/error/refetch
ที่เป็นเนื้อหาหลักของ UI นี้ออกไปได้; ถ้าต้องการเลี่ยง dependency ก็เขียน `useReducer` hook
เองแทนได้

---

## เริ่มต้นใช้งาน (Getting started)

**สิ่งที่ต้องมี:** .NET 9 SDK, Node.js 20+ (Tailwind CSS v4)

### 1. Backend (terminal A)

```bash
cd backend
dotnet run --project src/UrlShortener.Api --urls "http://localhost:5000"
```

API รันที่ `http://localhost:5000` · OpenAPI doc: `http://localhost:5000/openapi/v1.json`

### 2. Frontend (terminal B)

```bash
cd frontend
npm install
npm run dev
```

เปิด `http://localhost:5173` · dev server คุยกับ API ที่ `:5000` (ปรับได้ผ่าน `VITE_API_BASE`)

### Custom short domain (optional)

base URL ที่แสดงในลิงก์สั้นปรับค่าได้ — ไม่ต้องมีโดเมนจริง ตั้ง `ShortUrl:BaseUrl`
ใน `backend/src/UrlShortener.Api/appsettings.json` (เช่น `"https://gul.fy"`) และถ้าอยากให้
resolve บนเครื่อง local ให้ชี้ `gul.fy` ไปที่ `127.0.0.1` ในไฟล์ hosts แล้วรัน Kestrel บน host นั้น
ค่า default ใช้ `http://localhost:5000` และมองโดเมนเป็นแค่ค่าที่ใช้แสดงผล

---

## API contract

Base URL: `http://localhost:5000` · body เป็น JSON ทั้งหมด · error ใช้
[RFC 7807 ProblemDetails](https://datatracker.ietf.org/doc/html/rfc7807)

### สร้างลิงก์ — `POST /api/links`

```jsonc
// request
{
  "url": "https://www.google.co.th",   // required, absolute http/https
  "customAlias": "mylink",              // optional; auto-generate ถ้าไม่ส่ง
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

| Status | เมื่อไหร่ |
|--------|------|
| `201 Created` | สำเร็จ |
| `400 Bad Request` | URL ไม่ถูกต้อง, alias ผิด format, platform key ไม่รู้จัก |
| `409 Conflict` | custom alias ถูกใช้ไปแล้ว |

### List ลิงก์ — `GET /api/links`
`200 OK` → array ของ link objects เรียงใหม่สุดก่อน

### ดูสถิติ — `GET /api/links/{shortCode}`
`200 OK` → link object เดียว · `404 Not Found` ถ้าไม่มี

### Enable / disable — `PATCH /api/links/{shortCode}`
```json
{ "status": "Disabled" }   // หรือ "Active"
```
`200 OK` → link ที่ update แล้ว · `404 Not Found` — ใช้ `PATCH` เพราะเป็น partial update
ของ field เดียว และใช้ endpoint เดียวกันในการ re-enable

### Delete — `DELETE /api/links/{shortCode}`
`204 No Content` · `404 Not Found`

### Redirect — `GET /{shortCode}`
detect platform จาก `User-Agent`, redirect ไปปลายทางที่ตรงกัน แล้วเพิ่ม click count

| Status | เมื่อไหร่ |
|--------|------|
| `302 Found` | ลิงก์ active — header `Location` คือปลายทาง |
| `404 Not Found` | ไม่มี / disabled / ถูกลบ (ไม่ redirect) |

---

## การตัดสินใจด้านการออกแบบ (Design decisions)

### Pluggable short-code generation (Strategy + resolver)
`IShortCodeGenerator` มี 2 implementation — `RandomShortCodeGenerator` และ
`CustomAliasGenerator` โดย `ShortCodeGeneratorResolver` เลือกตัวแรกที่ `CanHandle`
คืน true (ลำดับการ register = ลำดับความสำคัญ; random generator ที่รับทุกงานอยู่ท้ายสุด)
**เพิ่ม strategy ที่ 3 = เขียน class ใหม่ + register 1 บรรทัด ไม่ต้องแก้โค้ดเดิม** (Open/Closed)

### ทำไมใช้ random code ไม่ใช่ Base62 ของ auto-increment ID
code ที่มาจาก sequential ID เดาได้ (`/5` → ลอง `/4`, `/6`), leak จำนวนลิงก์ในระบบ,
และต้องมี central counter คอย coordinate ข้าม instance ส่วน crypto-random 7 ตัวอักษร
(`RandomNumberGenerator`, พื้นที่ ~56⁷) เดาไม่ได้และไม่ต้อง coordinate — trade-off คือ
ไม่มีการันตี unique ในตัว จึงจัดการด้วย collision retry

### Collision handling
uniqueness ถูก enforce ที่จุดเดียวแบบ atomic: `ILinkRepository.TryAddAsync`
(`ConcurrentDictionary.TryAdd`) เมื่อชน:
- **Random** generator → retry ด้วย code ใหม่ (สูงสุด 5 ครั้ง)
- **Custom alias** → deterministic ดังนั้นการชนคือ `409 Conflict` จริง ไม่ retry

policy นี้แสดงผ่าน `IShortCodeGenerator.AllowRetryOnCollision` ทำให้ service
ไม่ต้อง type-check concrete generator เลย

### 302 ไม่ใช่ 301 สำหรับ redirect
`301` เป็น permanent และถูก cache โดย browser/proxy — ซึ่งจะ **ทำให้ click counting พัง**
(การเข้าครั้งถัด ๆ ไปไม่วิ่งถึง server) และ platform routing ก็พังด้วย ส่วน `302`
ถูกประเมินใหม่ทุกครั้ง ดังนั้นทุกคลิกถูกนับ, platform ถูก detect ใหม่, และการ disable
มีผลทันที

### Platform detection เป็น seam แยกของตัวเอง
`IPlatformDetector` parse User-Agent แยกจาก link logic จึง unit-test ได้และสลับไปใช้
library device-detection ที่ละเอียดกว่าได้ กรณี agent ไม่รู้จัก/ไม่มี (bot, curl)
จะ fallback เป็น `Default` เสมอเพื่อให้ลิงก์ resolve ได้ตลอด

### State เป็น enum, click counting แบบ atomic
`LinkStatus` เป็น enum (ไม่ใช่ bool `IsDisabled`) ทำให้ redirect เช็คจุดเดียว
และเพิ่ม state ใหม่ (เช่น `Expired`) ได้ภายหลัง การนับคลิกใช้ `Interlocked.Increment`
ยืนยันด้วย test 10k-parallel-visit

### Storage สลับได้
ทุกอย่างผ่าน `ILinkRepository` implementation แบบ in-memory ใช้ case-insensitive
`ConcurrentDictionary`; สลับไป EF Core = class ใหม่ 1 ตัว + register DI 1 บรรทัด —
service layer ไม่ต้องแตะ

### Business error เป็น `Result` ไม่ใช่ exception
service คืน `Result<T>` / `Result` ที่ถือ `Error` แบบ typed (`Validation` / `Conflict` /
`NotFound`) แทนการ throw สำหรับ failure ที่คาดไว้ — caller ต้อง handle ทั้งสองทางชัดเจน,
happy path ไม่มี exception control flow, และ endpoint map `Error` → RFC 7807 ProblemDetails
(`ToProblem`) ส่วน exception เก็บไว้สำหรับ fault ที่ไม่คาดคิดจริง ๆ ซึ่งจะตกไปที่ default
500 ProblemDetails

### Validation & edge cases
`UrlValidator` รับเฉพาะ absolute `http`/`https` URL — ปฏิเสธ missing scheme, `javascript:`,
`ftp:`, `file:`, และ relative path ส่วน service เพิ่มอีก 2 ด่าน: ปลายทางห้ามชี้กลับมาที่
โดเมนของ shortener เอง (redirect loop), และ custom alias ห้ามผิด format หรือเป็น
**reserved name** (`api`, `openapi`, `health`, …) ที่จะบัง route จริง

---

## การทดสอบ (Testing)

```bash
cd backend
dotnet test
```

**87 unit tests** (xUnit + FluentAssertions + NSubstitute) ครอบคลุม core logic
ตามที่โจทย์ระบุ:
- **short-code generation** — length, alphabet, fallback, retry policy, resolver selection
- **URL validation** — รับ http/https, ปฏิเสธ `javascript:`, `ftp:`, `file:`, relative, empty
- custom alias validation, ปฏิเสธ reserved-name และ self-loop, uniqueness แบบ case-insensitive
- **click counting** — รวม 10k concurrent visits (atomicity) และ last-accessed tracking
- **disable / delete** — re-enable, และลิงก์ที่ disabled/deleted ไม่นับคลิกและไม่ redirect
- platform detection และการเลือกปลายทางตามแพลตฟอร์ม

### Test ที่พิสูจน์ว่า design decoupled จริง
`LinkServiceDecouplingTests` สลับ collaborator แต่ละตัวเป็น mock เพื่อแสดงว่า service
พึ่งแค่ abstraction และ behavior ไหลผ่าน seam เหล่านั้น:

| Test | พิสูจน์อะไร |
|------|----------------|
| `Create_uses_the_code_produced_by_the_injected_generator` | code generation delegate ให้ `IShortCodeGenerator` เต็มที่ — strategy ใหม่ทำงานได้โดยไม่แก้อะไร (Strategy / Open-Closed) |
| `Create_retries_…_when_the_repository_reports_a_collision` | uniqueness เป็นของ `ILinkRepository`; retry loop วิ่งผ่าน abstraction ไม่ใช่ internals ของ in-memory |
| `Create_does_not_retry_a_deterministic_generator_…` | `AllowRetryOnCollision` เป็นตัวขับการตัดสินใจ — ไม่มี concrete-type check |
| `Redirect_writes_the_click_back_through_the_repository` | click-count persistence เป็น seam (`UpdateAsync`) สลับเป็น DB ได้ |
| `Create_rejects_when_the_url_validator_rejects` | URL validation delegate ให้ `IUrlValidator` และไม่แตะ storage เมื่อ fail |
| `Resolve_selects_a_newly_added_strategy_…` | resolver เลือก generator ที่เพิ่งเพิ่มผ่าน `CanHandle` ล้วน ๆ |

---

## แนวทางต่อยอด (How I'd extend it)

- **Database จริง** — implement `ILinkRepository` ด้วย EF Core; เพิ่ม unique index
  บน short code แทน atomic check ของ in-memory
- **Scale click counting** — ย้าย increment ออกจาก redirect hot path ไปเป็น
  async queue / event stream แล้ว aggregate (eventual consistency)
- **Analytics ละเอียดขึ้น** — เก็บ event ต่อ visit (referrer, geo, timestamp)
  แทน counter ตัวเดียว
- **Generator เพิ่ม** — word-pair codes, hash-based, vanity domains — แต่ละอันเป็น
  `IShortCodeGenerator` ใหม่
- **Link expiry & auth** — เพิ่ม status `Expired` และ ownership ต่อ user

---

## การใช้ AI

โปรเจกต์นี้พัฒนาด้วย agentic AI assistant ดู session log ได้ที่ [`ai-logs/`](./ai-logs)

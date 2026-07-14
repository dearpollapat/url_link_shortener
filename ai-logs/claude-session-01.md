นี่คือโจทย์สัมภาษณ์ ช่วยวิเคราะห์ว่า requirement ข้อไหนบ้างที่เป็นข้อที่ผู้ตรวจตั้งใจใส่มาเพื่อดูว่าออกแบบเป็นหรือไม่ และแต่ละข้อควรตอบด้วย design pattern อะไร อธิบายเหตุผล อย่าเพิ่งเขียนโค้ด
วิเคราะห์โจทย์ — ข้อไหนคือ "กับดักดีไซน์" ที่ผู้ตรวจจงใจใส่มา + pattern ที่ควรใช้

สรุปเร็ว
5 capabilities แต่ 3 ข้อแรก (create/stats/disable-delete) เป็น CRUD ธรรมดา — วัด "ทำเป็นไหม". ข้อ 4 กับ 5 คือของจริง — วัด "ออกแบบเป็นไหม". README section 2 ข้อ 4-5 + evaluation criteria เขียนชัด "extensible seams", "pluggable", "value sound judgment over pattern-dropping".

ข้อที่เป็นกับดักดีไซน์ (จงใจใส่)
1. Pluggable short-code generation (ข้อ 5) — สำคัญสุด
โจทย์ใช้คำว่า "pluggable" ตรงๆ = จงใจ. ต้องมีอย่างน้อย 2 วิธี: auto-generate + custom alias.

Pattern: Strategy + Factory

IShortCodeGenerator interface → RandomCodeGenerator, CustomAliasGenerator (อนาคตต่อ SequentialGenerator, HashBasedGenerator)
Factory/resolver เลือก strategy ตาม input (มี alias → custom, ไม่มี → random)
เหตุผล: open/closed — เพิ่มวิธี gen ใหม่ไม่แตะ service เดิม. เป็น seam ที่ผู้สัมภาษณ์จะถาม "ถ้าเพิ่มแบบที่ 3 ทำไง"
เลี่ยง over-engineer: ไม่ต้องทำ plugin loader/DI แบบ dynamic assembly. แค่ register strategies ใน DI พอ.

2. Platform-specific destination (ข้อ 4)
ลิงก์เดียว → หลายปลายทางตาม iOS/Android/default. ตัดสินใจตอน redirect.

Pattern: Strategy (resolver) + Chain/dictionary lookup

แยก 2 ส่วน: (a) IPlatformDetector อ่าน User-Agent → enum Platform; (b) destination resolver map platform → URL
Data model: link มี destinations: List<{platform, url}> + default ไม่ใช่ field originalUrl เดี่ยว
เหตุผล: separation of concerns — การ detect platform (parsing UA) แยกจาก business logic (เลือก URL). ทดสอบแยกได้. UA parsing เป็นจุดที่ผู้สัมภาษณ์ชอบถาม edge case (bot, unknown UA → ต้อง fallback default)
3. Storage layer (bonus แต่โจทย์เน้น "swappable")
"Structure the data layer so it could be swapped for a real database later" — เขียนชัดว่าดู abstraction.

Pattern: Repository

ILinkRepository → InMemoryLinkRepository (ตอนนี้), EfLinkRepository (อนาคต)
เหตุผล: ไม่ผูก service กับ storage. In-memory ก็พอส่ง แต่ interface ต้องมี. ระวัง thread-safety ของ in-memory (ConcurrentDictionary) — จุดที่ถูกถามเรื่อง concurrency
ข้อที่เป็น CRUD แต่มี hidden design concern
4. Click counting (redirect increment)
ดูเหมือนง่าย แต่มี concurrency trap: หลาย request เข้าพร้อมกัน → race condition นับพลาด.

เหตุผล: จุดนี้ผู้สัมภาษณ์ถามเรื่อง scaling — "1000 req/s นับยังไงไม่ให้ lock ตาย" → คำตอบระดับ design: atomic increment ตอนนี้, อนาคตแยก write path (queue/event/fire-and-forget), eventual consistency.

5. Disable vs Delete
เป็น state ไม่ใช่แค่ลบ row. Disabled = เก็บ record แต่ไม่ redirect.

Pattern: soft-delete / state field (enum Status: Active/Disabled) ไม่ใช่ boolean หลายตัว

เหตุผล: redirect logic เช็ค status เดียว. ขยาย state ได้ (Expired, RateLimited). Delete = soft delete หรือ hard — เตรียมอธิบาย trade-off
Mapping สรุป
Requirement	จงใจทดสอบ	Pattern
Short-code gen (ข้อ 5)	★★★	Strategy + Factory
Platform destination (ข้อ 4)	★★★	Strategy resolver + UA detector แยก
Storage swappable	★★	Repository
Click counting	★ (hidden)	Atomic increment + คุยเรื่อง async/scale
Disable/Delete	★ (hidden)	State enum / soft-delete
Cross-cutting: URL validation แยกเป็น IUrlValidator (testable, โจทย์สั่งเทสตรง), และ layering ชัด Controller → Service → Repository.

จุดที่ผู้สัมภาษณ์จะเจาะ (เตรียมตอบ)
"เพิ่ม gen แบบที่ 3 ยังไง" → Strategy ตอบได้ทันที
"UA ไม่รู้จัก / bot" → fallback default
"click count race" → atomic ตอนนี้, async ทีหลัง
"custom alias ชนกัน" → uniqueness check + error ชัด
"in-memory หายตอน restart" → ยอมรับ, บอก Repository swap ได้
อยากให้ผมร่าง architecture/folder structure หรือเริ่ม scaffold โปรเจกต์ต่อไหม?

ช่วยเสนอโครงสร้างโปรเจกต์ 2 แบบ: (A) Clean Architecture แยก 4 layer (B) Vertical Slice / Minimal API แบบเรียบง่าย พร้อมข้อดีข้อเสียของแต่ละแบบสำหรับโจทย์ขนาด 4-8 ชม. ที่เน้น 'sound judgment over over-engineering' แล้วแนะนำว่าควรเลือกอันไหนเพราะอะไร
โครงสร้างโปรเจกต์ 2 แบบ สำหรับโจทย์ 4-8 ชม.

แบบ A: Clean Architecture (4 layer)

src/
  UrlShortener.Domain/          # ไม่พึ่งใคร
    Entities/Link.cs
    Enums/LinkStatus.cs, Platform.cs
    ValueObjects/ShortCode.cs
  UrlShortener.Application/      # business logic + interfaces
    Interfaces/ILinkRepository.cs, IShortCodeGenerator.cs
    Services/LinkService.cs
    Strategies/RandomCodeGenerator.cs, CustomAliasGenerator.cs
    Validation/UrlValidator.cs
  UrlShortener.Infrastructure/   # implement interfaces
    Persistence/InMemoryLinkRepository.cs
    Platform/UserAgentPlatformDetector.cs
  UrlShortener.Api/              # controllers, DI, Program.cs
    Controllers/LinksController.cs, RedirectController.cs
tests/
  UrlShortener.UnitTests/
ข้อดี

Dependency rule ชัด — Domain ไม่รู้จัก DB/web. ตรงกับ "clear separation of concerns"
โชว์ SOLID เต็มๆ, ผู้สัมภาษณ์เห็น seam ทันที
Repository/Strategy อยู่ที่ควรอยู่ตาม textbook
ข้อเสีย

4 projects สำหรับ ~10 ไฟล์จริง = boilerplate เยอะ, ratio โครงสร้างต่อโค้ดจริงสูง
เสี่ยงโดนมองว่า over-engineer — โจทย์เตือนตรงๆ "value sound judgment over pattern-dropping"
เวลาหมดไปกับ project reference/DI wiring แทน feature
แบบ B: Vertical Slice / Minimal API

src/UrlShortener.Api/
  Features/
    Links/
      CreateLink.cs        # endpoint + request/response + handler ในไฟล์เดียว
      GetLinkStats.cs
      DisableLink.cs
      DeleteLink.cs
    Redirect/
      RedirectLink.cs
  Core/
    Link.cs, LinkStatus.cs, Platform.cs
    ShortCodeGeneration/   # IShortCodeGenerator + strategies
    ILinkRepository.cs, InMemoryLinkRepository.cs
    UrlValidator.cs, PlatformDetector.cs
  Program.cs
tests/UrlShortener.UnitTests/
ข้อดี

1 project, feature อยู่รวมกัน — อ่านง่าย, เปิดไฟล์เดียวเห็นทั้ง slice
เหมาะ scope เล็ก, เดโม่เร็ว, ratio โค้ดจริงสูง
ยังใส่ Strategy/Repository/Validator ได้ครบ — pattern ไม่หาย แค่ไม่แตก project
ข้อเสีย

Layer boundary หลวมกว่า — วินัยขึ้นกับคนเขียน ไม่ใช่ compiler บังคับ
ถ้าใส่ MediatR/CQRS full = over-engineer อีกแบบ (ไม่ต้อง)
โตมากๆ แล้ว Core/ อาจรก (แต่โจทย์นี้ไม่โต)
แนะนำ: แบบ B (Vertical Slice, 1 project)
เหตุผล:

โจทย์เตือนเรื่อง over-engineer 2 ที่ (section 5 + evaluation). 4 projects สำหรับ ~10 ไฟล์คือสัญญาณ over-engineer ที่ผู้ตรวจจับได้
Pattern ที่วัดจริง (Strategy/Repository/Validator) ใส่ได้ครบใน B — boundary ที่ผู้ตรวจดูคือ interface/abstraction ไม่ใช่จำนวน .csproj
เวลา 4-8 ชม. ควรลงกับ feature + test + UI ไม่ใช่ wiring
คุยในสัมภาษณ์ได้ — "เลือก B เพราะ scope เล็ก, แต่ถ้าโตเป็นทีม/หลาย bounded context ผมจะ split เป็น A" = โชว์ judgment ตรงเป๊ะกับที่เขาให้คะแนน
จุดกลาง (ถ้าอยากโชว์ layering หน่อย): B แต่แยก UrlShortener.Api + UrlShortener.Tests เป็น 2 project, ข้างในจัด folder แบบ layer หลวมๆ (Core/Features). ได้ทั้งเรียบง่าย + เห็น boundary โดยไม่ over-split.

ประเด็นเถียงได้ในสัมภาษณ์: "ทำไมไม่ Clean Architecture" → ตอบ scope + judgment. เตรียมได้เลย.

เริ่ม scaffold แบบ B ต่อไหม?

เลือกแบบ B ได้เลย
ออกแบบ domain model และ API contract ให้ครบก่อน (endpoint, request/response schema, HTTP status code, error format) ครอบคลุม: create link, custom alias, get stats, disable, delete, redirect ที่แยกตาม platform ให้อธิบายว่าทำไมเลือก status code แต่ละตัว (โดยเฉพาะ redirect ควรเป็น 301 หรือ 302 และผลกระทบต่อการนับ click)
Domain model + API contract แบบ B

Domain Model

Link
├─ Id              : Guid           # internal id
├─ ShortCode       : string         # unique, indexed (เช่น "HsQy5" หรือ custom alias)
├─ Destinations    : Destination[]  # หลายปลายทางตาม platform
├─ Status          : LinkStatus     # Active | Disabled
├─ ClickCount      : long           # atomic increment
├─ CreatedAt       : DateTimeOffset
└─ LastAccessedAt  : DateTimeOffset?  # null = ยังไม่เคยถูกเข้า

Destination
├─ Platform : Platform   # Default | iOS | Android
└─ Url      : string

enum LinkStatus  { Active, Disabled }
enum Platform    { Default, iOS, Android }
เหตุผลออกแบบ

Destinations เป็น array ของ {Platform, Url} แทน field OriginalUrl เดี่ยว — รองรับข้อ 4 ตั้งแต่ model. Default บังคับมีเสมอ (fallback)
Status เป็น enum ไม่ใช่ bool IsDisabled — ขยาย state ได้ (Expired ฯลฯ), redirect เช็คจุดเดียว
ClickCount เป็น long — atomic op ตอนนับ
LastAccessedAt nullable — แยก "ไม่เคยเข้า" ออกจาก "เข้าเมื่อ epoch"
ไม่มี hard Delete field — delete = ลบออกจาก store จริง (คุย soft-delete trade-off ได้)
API Contract
Base: http://localhost:5000 · Short base URL: configurable (ShortUrl:BaseUrl)

1. Create link — POST /api/links
Request:


{
  "url": "https://www.google.co.th",
  "customAlias": "mylink",              // optional
  "destinations": {                      // optional, platform-specific
    "android": "https://download.gulf.co.th/android.apk",
    "ios": "https://download.gulf.co.th/iphone.ipa"
  }
}
url = default destination (บังคับ). destinations = override เพิ่มเติม
ไม่มี customAlias → auto-generate (Strategy)
Response 201 Created + header Location: /api/links/{shortCode}:


{
  "shortCode": "HsQy5",
  "shortUrl": "https://gul.fy/HsQy5",
  "destinations": {
    "default": "https://www.google.co.th",
    "android": "https://download.gulf.co.th/android.apk",
    "ios": "https://download.gulf.co.th/iphone.ipa"
  },
  "status": "Active",
  "clickCount": 0,
  "createdAt": "2026-07-13T10:00:00Z"
}
Status codes

Code	เมื่อไหร่
201 Created	สำเร็จ — resource ใหม่ + Location header (REST idiom)
400 Bad Request	url ไม่ valid, destinations ว่าง, alias ผิด format
409 Conflict	customAlias ชนของเดิม — สื่อ "resource state conflict" ชัดกว่า 400
2. Get stats — GET /api/links/{shortCode}
Response 200 OK:


{
  "shortCode": "HsQy5",
  "shortUrl": "https://gul.fy/HsQy5",
  "destinations": { "default": "...", "android": "...", "ios": "..." },
  "status": "Active",
  "clickCount": 42,
  "createdAt": "2026-07-13T10:00:00Z",
  "lastAccessedAt": "2026-07-13T12:30:00Z"
}
404 Not Found ถ้าไม่มี shortCode
3. List links (bonus, ช่วย UI) — GET /api/links
200 OK + array. ใส่ไว้เพราะ UI ต้องโชว์ตาราง.

4. Disable — PATCH /api/links/{shortCode}

{ "status": "Disabled" }
200 OK คืน object ที่ update แล้ว
PATCH เพราะ partial update (แก้ field เดียว) ไม่ใช่ replace ทั้ง resource (PUT)
ใช้ PATCH กับ status field รองรับ re-enable ("Active") ด้วย — endpoint เดียว
404 ถ้าไม่มี
5. Delete — DELETE /api/links/{shortCode}
204 No Content สำเร็จ — ไม่มี body ตอบ (DELETE idiom)
404 ถ้าไม่มี
idempotent: ลบซ้ำ → 404 (ยอมรับได้)
6. Redirect — GET /{shortCode} (root, ไม่ใช่ /api)
อ่าน User-Agent → detect platform → เลือก destination → redirect + increment click
สำเร็จ: 302 Found + Location: <destination>
disabled/deleted/ไม่มี: 404 Not Found (ไม่ redirect)
301 vs 302 — จุดสำคัญ
เลือก 302 (Found / temporary redirect) — ไม่ใช่ 301

เหตุผล:

Click counting พังถ้าใช้ 301. 301 = permanent → browser/proxy/CDN cache ไว้. ครั้งต่อไป user ไม่ยิงมา server เลย → นับ click ไม่ได้. นี่คือ trade-off ที่ผู้สัมภาษณ์จงใจถาม
302 ไม่ cache (default) → ทุกครั้งวิ่งผ่าน server → นับครบ + ใช้ platform detection ได้ทุกครั้ง
Platform-specific ต้องการ 302 — ปลายทางเปลี่ยนตาม UA/เวลา ไม่ใช่ permanent mapping. 301 ขัดกับ concept นี้โดยตรง
ยัง disable/delete ได้จริง — ถ้า 301 cache แล้ว disable link, browser ยัง redirect ไปที่เก่า (ผิด requirement)
ทางเลือกเข้ม: 302 หรือ 307 Temporary Redirect. 307 รับประกันไม่เปลี่ยน method (แต่เป็น GET อยู่แล้ว ไม่ต่าง). ใช้ 302 พอ, มาตรฐานสุด.

Trade-off ที่แลก: 302 = ทุก click โหลด server (301 เร็วกว่าเพราะ cache). แต่โจทย์ ต้องนับ click + platform routing → correctness > latency. อนาคต scale: แยก analytics ออกเป็น async event.

Error format (RFC 7807 Problem Details)
.NET built-in รองรับ ใช้เลย:


{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Conflict",
  "status": 409,
  "detail": "Alias 'mylink' is already in use.",
  "errors": { "customAlias": ["Alias already taken"] }  // validation only
}
มาตรฐาน, .NET ProblemDetails ให้ฟรี, frontend parse ง่าย
validation errors (400) ใช้ ValidationProblemDetails — map field → messages
สรุป status code เหตุผล
Endpoint	สำเร็จ	เหตุผล
Create	201	resource ใหม่ + Location
Get stats	200	อ่านปกติ
Disable	200 (PATCH)	partial update, คืน state ใหม่
Delete	204	ไม่มี body
Redirect	302	ไม่ cache → นับ click + platform routing ได้
ไม่เจอ	404	ทุก endpoint
Alias ชน	409	state conflict ชัดกว่า 400
Input ผิด	400	validation
เริ่ม scaffold โปรเจกต์ + implement ตาม contract นี้ต่อไหม?

ออกแบบระบบ short-code generation ที่ pluggable ตาม requirement ข้อ 5 ต้องรองรับอย่างน้อย: auto-generated code และ custom alias และต้องเพิ่มวิธีใหม่ได้โดยไม่แก้โค้ดเดิม (Open/Closed Principle) ให้เขียน interface + implementation + วิธีเลือก strategy ตอน runtime อธิบายว่าจะจัดการ collision อย่างไร และทำไมไม่ใช้ Base62 จาก auto-increment ID
Pluggable short-code generation

หลักออกแบบ: แยก 3 ความรับผิดชอบ
Generate candidate — สร้าง code (strategy ต่างกัน)
Select strategy — เลือก strategy ตอน runtime
Ensure uniqueness — จัดการ collision (แยกออกจาก generator)
แยกกันเพราะ collision handling เหมือนกันทุก strategy — ไม่ควรอยู่ในแต่ละ generator (DRY + single responsibility).

1. Interface

public sealed record GenerationRequest(string DefaultUrl, string? CustomAlias);

public interface IShortCodeGenerator
{
    /// Strategy name — สำหรับ logging / อธิบายว่าใครถูกเลือก
    string Name { get; }

    /// runtime selection: strategy รับงานนี้ได้ไหม
    bool CanHandle(GenerationRequest request);

    /// สร้าง candidate 1 ตัว (ยังไม่การันตี unique — ชั้นบนเช็ค)
    string Generate(GenerationRequest request);
}
CanHandle = จุดที่ทำให้ OCP ได้จริง. เพิ่ม strategy ใหม่ → มันบอกเองว่ารับงานไหน, resolver ไม่ต้องแก้.

2. Implementations

// วิธี A: auto-generated (default fallback)
public sealed class RandomShortCodeGenerator : IShortCodeGenerator
{
    // ตัด 0/O/1/l/I ออก กันสับสนตอนพิมพ์มือ
    private const string Alphabet = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int Length = 7;

    public string Name => "random";

    // รับงานเสมอ — เป็น fallback ตัวสุดท้าย
    public bool CanHandle(GenerationRequest request) => true;

    public string Generate(GenerationRequest request)
    {
        Span<char> buffer = stackalloc char[Length];
        for (int i = 0; i < Length; i++)
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        return new string(buffer);
    }
}

// วิธี B: custom alias
public sealed class CustomAliasGenerator : IShortCodeGenerator
{
    public string Name => "custom-alias";

    // รับเฉพาะตอนมี alias
    public bool CanHandle(GenerationRequest request)
        => !string.IsNullOrWhiteSpace(request.CustomAlias);

    public string Generate(GenerationRequest request)
    {
        var alias = request.CustomAlias!.Trim();
        if (!IsValidAlias(alias))
            throw new InvalidAliasException(alias);
        return alias;
    }

    private static bool IsValidAlias(string alias) =>
        alias.Length is >= 3 and <= 30 &&
        alias.All(c => char.IsLetterOrDigit(c) || c is '-' or '_');
}
RandomNumberGenerator (crypto RNG) ไม่ใช่ Random — code เดาไม่ได้.

3. Runtime selection (Resolver)

public sealed class ShortCodeGeneratorResolver
{
    private readonly IReadOnlyList<IShortCodeGenerator> _generators;

    // DI ฉีด strategy ทั้งหมดเข้ามาเป็น IEnumerable
    public ShortCodeGeneratorResolver(IEnumerable<IShortCodeGenerator> generators)
        => _generators = generators.ToList();

    public IShortCodeGenerator Resolve(GenerationRequest request)
        => _generators.First(g => g.CanHandle(request));
}
DI registration — order สำคัญ (custom ก่อน random fallback):


services.AddSingleton<IShortCodeGenerator, CustomAliasGenerator>();
services.AddSingleton<IShortCodeGenerator, RandomShortCodeGenerator>(); // ตัวท้าย = fallback
services.AddSingleton<ShortCodeGeneratorResolver>();
OCP ได้จริง: เพิ่มวิธีที่ 3 (เช่น WordPairGenerator — "blue-tiger") → เขียน class ใหม่ + AddSingleton 1 บรรทัด. Resolver, service, generator เดิม ไม่แตะ.

4. Collision handling (แยกออกมาที่ service)

public sealed class LinkService
{
    private const int MaxRetries = 5;

    public async Task<Link> CreateAsync(GenerationRequest request)
    {
        var generator = _resolver.Resolve(request);
        var isCustom = generator is CustomAliasGenerator;

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            var code = generator.Generate(request);

            // atomic: repo คืน false ถ้าชน (ConcurrentDictionary.TryAdd)
            if (await _repository.TryReserveAsync(code, request))
                return await _repository.GetAsync(code);

            // custom alias ชน = user เลือกเอง → ไม่ retry, โยน 409 ทันที
            if (isCustom)
                throw new AliasAlreadyExistsException(code);

            // random ชน = ซวย → regenerate ใหม่
        }
        throw new ShortCodeGenerationException("Exhausted retries.");
    }
}
Collision strategy ต่างกันตาม generator:

กรณี	จัดการ
Random ชน	retry regenerate (ใหม่ทุกครั้ง random) สูงสุด 5 ครั้ง → ชนต่อ = โยน error
Custom alias ชน	ไม่ retry (gen เดิมได้ code เดิมเสมอ) → 409 Conflict ทันที
ทำไม check-then-reserve ต้อง atomic: 2 request สร้าง alias เดียวกันพร้อมกัน. ถ้า check แล้วค่อย add แยกขั้น → race. TryReserveAsync ใช้ ConcurrentDictionary.TryAdd (atomic) → คนที่ 2 ได้ false → 409. DB จริงใช้ unique constraint แทน.

Collision probability (random): 7 chars × 56 alphabet ≈ 56⁷ ≈ 1.7×10¹² combos. ที่ 1M links, ชน ~0.0006% — retry 5 ครั้งเหลือเศษ.

ทำไม ไม่ ใช้ Base62 จาก auto-increment ID
Base62-from-counter = แปลง ID 1,2,3... เป็น "1","2"..."a","b" — สั้น, unique การันตี, ไม่ต้องเช็ค collision. แต่ไม่เลือกเพราะ:

เดาได้ / enumerable — code เรียงตาม ID. เห็น gul.fy/5 ก็ลอง /4, /6 ได้หมด → scrape ลิงก์คนอื่น, privacy leak. URL shortener สาธารณะห้ามเดาได้
Leak business metrics — code ล่าสุด decode กลับเป็นจำนวน link ทั้งระบบ → คู่แข่งรู้ growth rate
ต้อง central monotonic counter — in-memory ตอนนี้โอเค, แต่พอ scale หลาย instance/shard → ต้อง coordinate counter (contention, single point). Random ไม่ต้อง coordinate เลย
ผูกกับ storage ID — code = f(persistence id) → เปลี่ยน DB/re-seed แล้วพัง. Random แยก code ออกจาก id → Repository swap ง่าย (ตรงกับ requirement swappable)
Custom alias ไม่เข้า scheme — ยังไงก็ต้องมีกลไกที่ 2 อยู่ดี → Strategy จัดการทั้งคู่ด้วย pattern เดียว สะอาดกว่า
Trade-off ที่ยอมแลก: เสีย uniqueness การันตี (ต้อง retry) + code ยาวกว่า (7 vs ~2-4 ตอนแรก). แต่ได้ unpredictability + no coordination — คุ้มกว่าสำหรับ public shortener.

เมื่อไหร่ Base62-ID เหมาะ: internal system ที่ predictability ไม่แคร์ + อยาก code สั้นสุด + มี central DB sequence อยู่แล้ว. เตรียมตอบข้อนี้ในสัมภาษณ์ได้.

พร้อม scaffold แล้ว. เริ่มสร้างโปรเจกต์จริง (Program.cs + Core + Features + tests) ตาม design ทั้งหมดนี้ต่อไหม?

สร้างโครงสร้างโปรเจกต์ได้เลย
Bash Check dotnet and node versions
IN
dotnet --version && node --version && npm --version

OUT
9.0.314
v21.2.0
10.2.4
Bash List project dir and git status
IN
ls -la e:/coding/url_link_shortener/url_link_shortener 2>/dev/null; echo "---"; git -C e:/coding/url_link_shortener/url_link_shortener status

OUT
total 4
drwxr-xr-x 1 Pollapat 197121 0 Jul 13 23:08 .
drwxr-xr-x 1 Pollapat 197121 0 Jul 13 22:22 ..
drwxr-xr-x 1 Pollapat 197121 0 Jul 13 23:08 .claude
drwxr-xr-x 1 Pollapat 197121 0 Jul 13 22:26 .git
---
On branch main

No commits yet

nothing to commit (create/copy files and use "git add" to track)
dotnet 9, node 21 พร้อม. Repo ว่าง. สร้าง scaffold — backend (.NET) + frontend (React/Vite) + docs.

เริ่มจาก .NET solution + projects:

Bash Scaffold .NET solution, API and test projects
IN
cd e:/coding/url_link_shortener/url_link_shortener && \
mkdir -p backend frontend ai-logs && \
cd backend && \
dotnet new sln -n UrlShortener && \
dotnet new webapi -n UrlShortener.Api -o src/UrlShortener.Api --use-minimal-apis false && \
dotnet new xunit -n UrlShortener.UnitTests -o tests/UrlShortener.UnitTests && \
dotnet sln add src/UrlShortener.Api/UrlShortener.Api.csproj && \
dotnet sln add tests/UrlShortener.UnitTests/UrlShortener.UnitTests.csproj && \
dotnet add tests/UrlShortener.UnitTests/UrlShortener.UnitTests.csproj reference src/UrlShortener.Api/UrlShortener.Api.csproj && \
echo "=== DONE ==="

OUT
The template "Solution File" was created successfully.

The template "ASP.NET Core Web API" was created successfully.

Processing post-creation actions...
Restoring E:\coding\url_link_shortener\url_link_shortener\backend\src\UrlShortener.Api\UrlShortener.Api.csproj:
  Determining projects to restore...
  Restored E:\coding\url_link_shortener\url_link_shortener\backend\src\UrlShortener.Api\UrlShortener.Api.csproj (in 4.44 sec).
Restore succeeded.


The template "xUnit Test Project" was created successfully.

Processing post-creation actions...
Restoring E:\coding\url_link_shortener\url_link_shortener\backend\tests\UrlShortener.UnitTests\UrlShortener.UnitTests.csproj:
  Determining projects to restore...
  Restored E:\coding\url_link_shortener\url_link_shortener\backend\tests\UrlShortener.UnitTests\UrlShortener.UnitTests.csproj (in 5.31 sec).
Restore succeeded.


Project `src\UrlShortener.Api\UrlShortener.Api.csproj` added to the solution.
Project `tests\UrlShortener.UnitTests\UrlShortener.UnitTests.csproj` added to the solution.
Reference `..\..\src\UrlShortener.Api\UrlShortener.Api.csproj` added to the project.
=== DONE ===
Solution สร้างแล้ว. ดูไฟล์ที่ generate มา แล้วเคลียร์ template ทิ้ง:

Bash List generated project files
IN
cd e:/coding/url_link_shortener/url_link_shortener/backend && find src tests -type f -not -path '*/obj/*' -not -path '*/bin/*' | sort

OUT
src/UrlShortener.Api/Controllers/WeatherForecastController.cs
src/UrlShortener.Api/Program.cs
src/UrlShortener.Api/Properties/launchSettings.json
src/UrlShortener.Api/UrlShortener.Api.csproj
src/UrlShortener.Api/UrlShortener.Api.http
src/UrlShortener.Api/WeatherForecast.cs
src/UrlShortener.Api/appsettings.Development.json
src/UrlShortener.Api/appsettings.json
tests/UrlShortener.UnitTests/UnitTest1.cs
tests/UrlShortener.UnitTests/UrlShortener.UnitTests.csproj
สร้าง scaffold ครบทั้งระบบ — ตั้ง todo list ก่อน:

Update Todos

Clean template files (WeatherForecast, UnitTest1)

Build Core domain (Link, Destination, enums)

Build short-code generation (interface, generators, resolver)

Build repository (ILinkRepository, InMemory)

Build platform detection + URL validation

Build LinkService + DTOs + exceptions

Build Features/endpoints (controllers)

Wire Program.cs DI + appsettings + CORS

Write unit tests

Build succeeds + tests pass

Scaffold React frontend (Vite + TS)

Write README + design notes + .gitignore
Bash Remove template files and create folder structure
IN
cd e:/coding/url_link_shortener/url_link_shortener/backend && \
rm -f src/UrlShortener.Api/Controllers/WeatherForecastController.cs \
      src/UrlShortener.Api/WeatherForecast.cs \
      src/UrlShortener.Api/UrlShortener.Api.http \
      tests/UrlShortener.UnitTests/UnitTest1.cs && \
mkdir -p src/UrlShortener.Api/Core/ShortCodes \
         src/UrlShortener.Api/Core/Persistence \
         src/UrlShortener.Api/Core/Platform \
         src/UrlShortener.Api/Core/Validation \
         src/UrlShortener.Api/Features/Links \
         src/UrlShortener.Api/Features/Redirect \
         tests/UrlShortener.UnitTests/ShortCodes \
         tests/UrlShortener.UnitTests/Services \
         tests/UrlShortener.UnitTests/Validation && \
echo "cleaned + dirs ready"

OUT
cleaned + dirs ready
Read UrlShortener.Api.csproj
Read Program.cs
csproj: net9.0, Nullable+ImplicitUsings. เริ่มเขียน Core domain:

Write LinkStatus.cs
12 lines
namespace UrlShortener.Api.Core;

/// <summary>
/// Lifecycle state of a link. Modeled as an enum rather than a boolean flag so
/// new states (e.g. Expired) can be added without changing the redirect check.
/// </summary>
public enum LinkStatus
{
    Active,
    Disabled
}
Write Platform.cs
13 lines
namespace UrlShortener.Api.Core;

/// <summary>
/// Visitor platform, decided at redirect time from the User-Agent header.
/// <c>Default</c> is the fallback destination that every link must have.
/// </summary>
public enum Platform
{
    Default,
    iOS,
    Android
}
Write Link.cs
56 lines
namespace UrlShortener.Api.Core;

/// <summary>
/// A shortened link and its platform-specific destinations.
/// Click counting mutates <see cref="ClickCount"/> and <see cref="LastAccessedAt"/>;
/// those writes go through <see cref="RegisterVisit"/> so the concurrency
/// guarantee lives in one place.
/// </summary>
public sealed class Link
{
    private long _clickCount;

    public Link(string shortCode, IReadOnlyDictionary<Platform, string> destinations, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
            throw new ArgumentException("Short code is required.", nameof(shortCode));
        if (destinations is null || destinations.Count == 0)
            throw new ArgumentException("At least one destination is required.", nameof(destinations));
        if (!destinations.ContainsKey(Platform.Default))
            throw new ArgumentException("A Default destination is required.", nameof(destinations));

        ShortCode = shortCode;
        Destinations = destinations;
        CreatedAt = createdAt;
        Status = LinkStatus.Active;
    }

    public string ShortCode { get; }

    public IReadOnlyDictionary<Platform, string> Destinations { get; }

    public LinkStatus Status { get; private set; }

    public long ClickCount => Interlocked.Read(ref _clickCount);

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? LastAccessedAt { get; private set; }

    /// <summary>Resolves the destination for a platform, falling back to Default.</summary>
    public string ResolveDestination(Platform platform) =>
        Destinations.TryGetValue(platform, out var url) ? url : Destinations[Platform.Default];

    /// <summary>
    /// Atomically increments the click count and records the access time.
    /// Safe to call concurrently from multiple redirect requests.
    /// </summary>
    public void RegisterVisit(DateTimeOffset at)
    {
        Interlocked.Increment(ref _clickCount);
        LastAccessedAt = at;
    }

    public void SetStatus(LinkStatus status) => Status = status;
}
Core domain เสร็จ. ต่อ short-code generation:

Update Todos

Clean template files (WeatherForecast, UnitTest1)

Build Core domain (Link, Destination, enums)

Build short-code generation (interface, generators, resolver)

Build repository (ILinkRepository, InMemory)

Build platform detection + URL validation

Build LinkService + DTOs + exceptions

Build Features/endpoints (controllers)

Wire Program.cs DI + appsettings + CORS

Write unit tests

Build succeeds + tests pass

Scaffold React frontend (Vite + TS)

Write README + design notes + .gitignore
Write GenerationRequest.cs
5 lines
namespace UrlShortener.Api.Core.ShortCodes;

/// <summary>Input a generator needs to produce a short code.</summary>
public sealed record GenerationRequest(string? CustomAlias);
Write IShortCodeGenerator.cs
23 lines
namespace UrlShortener.Api.Core.ShortCodes;

/// <summary>
/// A strategy for producing short codes. New strategies are added by
/// implementing this interface and registering it in DI — no existing code
/// changes (Open/Closed). The resolver selects one at runtime via
/// <see cref="CanHandle"/>.
/// </summary>
public interface IShortCodeGenerator
{
    /// <summary>Identifier used for diagnostics and to tell strategies apart.</summary>
    string Name { get; }

    /// <summary>Whether this strategy is responsible for the given request.</summary>
    bool CanHandle(GenerationRequest request);

    /// <summary>
    /// Produces a candidate code. Uniqueness is NOT guaranteed here — the
    /// caller reserves the code and retries on collision.
    /// </summary>
    string Generate(GenerationRequest request);
}
Write RandomShortCodeGenerator.cs
29 lines
using System.Security.Cryptography;

namespace UrlShortener.Api.Core.ShortCodes;

/// <summary>
/// Default strategy: a cryptographically-random code. Unpredictable (unlike a
/// Base62-of-sequential-id scheme) so links can't be enumerated, and it needs
/// no central counter to coordinate. Handles any request, so it is the
/// fallback and must be registered last.
/// </summary>
public sealed class RandomShortCodeGenerator : IShortCodeGenerator
{
    // Omits look-alike characters (0/O, 1/l/I) to stay readable when typed by hand.
    private const string Alphabet = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 7;

    public string Name => "random";

    public bool CanHandle(GenerationRequest request) => true;

    public string Generate(GenerationRequest request)
    {
        Span<char> buffer = stackalloc char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        return new string(buffer);
    }
}
Write CustomAliasGenerator.cs
30 lines
using System.Text.RegularExpressions;

namespace UrlShortener.Api.Core.ShortCodes;

/// <summary>
/// Strategy for user-supplied aliases. Only handles requests that carry an
/// alias, and validates its shape. Because it always returns the same code for
/// the same input, a collision here is a real conflict (409) — the caller must
/// not retry it.
/// </summary>
public sealed partial class CustomAliasGenerator : IShortCodeGenerator
{
    public string Name => "custom-alias";

    public bool CanHandle(GenerationRequest request) =>
        !string.IsNullOrWhiteSpace(request.CustomAlias);

    public string Generate(GenerationRequest request)
    {
        var alias = request.CustomAlias!.Trim();
        if (!AliasPattern().IsMatch(alias))
            throw new InvalidAliasException(alias);
        return alias;
    }

    // 3–30 chars: letters, digits, hyphen, underscore.
    [GeneratedRegex("^[A-Za-z0-9_-]{3,30}$")]
    private static partial Regex AliasPattern();
}
Write ShortCodeGeneratorResolver.cs
23 lines
namespace UrlShortener.Api.Core.ShortCodes;

/// <summary>
/// Picks the first registered generator that can handle a request. Registration
/// order matters: specific strategies (e.g. custom alias) come first, the
/// catch-all random generator last.
/// </summary>
public sealed class ShortCodeGeneratorResolver
{
    private readonly IReadOnlyList<IShortCodeGenerator> _generators;

    public ShortCodeGeneratorResolver(IEnumerable<IShortCodeGenerator> generators)
    {
        _generators = generators.ToList();
        if (_generators.Count == 0)
            throw new InvalidOperationException("No short-code generators are registered.");
    }

    public IShortCodeGenerator Resolve(GenerationRequest request) =>
        _generators.FirstOrDefault(g => g.CanHandle(request))
        ?? throw new InvalidOperationException("No generator can handle the request.");
}
Exceptions ทั้งชุด:

Write DomainExceptions.cs
19 lines
namespace UrlShortener.Api.Core;

/// <summary>Base type for expected, user-facing domain errors mapped to HTTP responses.</summary>
public abstract class DomainException(string message) : Exception(message);

/// <summary>Input failed validation (bad URL, malformed request). Maps to 400.</summary>
public sealed class ValidationException(string message) : DomainException(message);

/// <summary>A custom alias has an invalid shape. Maps to 400.</summary>
public sealed class InvalidAliasException(string alias)
    : DomainException($"Alias '{alias}' is invalid. Use 3–30 letters, digits, '-' or '_'.");

/// <summary>A custom alias is already taken. Maps to 409.</summary>
public sealed class AliasAlreadyExistsException(string alias)
    : DomainException($"Alias '{alias}' is already in use.");

/// <summary>Random generation could not find a free code after several tries. Maps to 500.</summary>
public sealed class ShortCodeGenerationException(string message) : DomainException(message);
Repository — interface + in-memory (ConcurrentDictionary, atomic reserve):

Update Todos

Clean template files (WeatherForecast, UnitTest1)

Build Core domain (Link, Destination, enums)

Build short-code generation (interface, generators, resolver)

Build repository (ILinkRepository, InMemory)

Build platform detection + URL validation

Build LinkService + DTOs + exceptions

Build Features/endpoints (controllers)

Wire Program.cs DI + appsettings + CORS

Write unit tests

Build succeeds + tests pass

Scaffold React frontend (Vite + TS)

Write README + design notes + .gitignore
Write ILinkRepository.cs
24 lines
namespace UrlShortener.Api.Core.Persistence;

/// <summary>
/// Storage abstraction for links. The current implementation is in-memory;
/// this seam lets it be swapped for a database (EF Core, etc.) without touching
/// the service layer. Async by design so a real, I/O-bound store fits the same
/// contract.
/// </summary>
public interface ILinkRepository
{
    /// <summary>
    /// Atomically reserves <paramref name="link"/> under its short code.
    /// Returns false if the code is already taken — this is the single point
    /// where uniqueness is enforced, so it must be atomic under concurrency.
    /// </summary>
    Task<bool> TryAddAsync(Link link, CancellationToken ct = default);

    Task<Link?> GetAsync(string shortCode, CancellationToken ct = default);

    Task<IReadOnlyCollection<Link>> GetAllAsync(CancellationToken ct = default);

    Task<bool> RemoveAsync(string shortCode, CancellationToken ct = default);
}
Write InMemoryLinkRepository.cs
27 lines
using System.Collections.Concurrent;

namespace UrlShortener.Api.Core.Persistence;

/// <summary>
/// Thread-safe in-memory store. Short codes are matched case-insensitively so
/// "MyLink" and "mylink" cannot both be reserved. Registered as a singleton so
/// state survives across requests for the process lifetime.
/// </summary>
public sealed class InMemoryLinkRepository : ILinkRepository
{
    private readonly ConcurrentDictionary<string, Link> _links =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> TryAddAsync(Link link, CancellationToken ct = default) =>
        Task.FromResult(_links.TryAdd(link.ShortCode, link));

    public Task<Link?> GetAsync(string shortCode, CancellationToken ct = default) =>
        Task.FromResult(_links.GetValueOrDefault(shortCode));

    public Task<IReadOnlyCollection<Link>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyCollection<Link>>(_links.Values.ToList());

    public Task<bool> RemoveAsync(string shortCode, CancellationToken ct = default) =>
        Task.FromResult(_links.TryRemove(shortCode, out _));
}
Platform detection + URL validation:

Write IPlatformDetector.cs
12 lines
namespace UrlShortener.Api.Core;

/// <summary>
/// Decides the visitor platform from the User-Agent header. Kept separate from
/// the link/redirect logic so User-Agent parsing can be tested and swapped
/// (e.g. for a richer device-detection library) on its own.
/// </summary>
public interface IPlatformDetector
{
    Platform Detect(string? userAgent);
}
Write UserAgentPlatformDetector.cs
28 lines
namespace UrlShortener.Api.Core;

/// <summary>
/// Lightweight User-Agent sniffing. Deliberately simple substring matching —
/// enough for iOS vs Android vs everything-else. Unknown or missing agents
/// (bots, curl) fall back to <see cref="Platform.Default"/> so a link always
/// resolves.
/// </summary>
public sealed class UserAgentPlatformDetector : IPlatformDetector
{
    public Platform Detect(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return Platform.Default;

        // iPadOS reports "Macintosh" in some modes; "iphone"/"ipad"/"ipod" are the reliable markers.
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            return Platform.Android;

        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPod", StringComparison.OrdinalIgnoreCase))
            return Platform.iOS;

        return Platform.Default;
    }
}
Write IUrlValidator.cs
11 lines
namespace UrlShortener.Api.Core.Validation;

/// <summary>Validates and normalizes destination URLs before a link is created.</summary>
public interface IUrlValidator
{
    /// <summary>
    /// Returns true and the normalized absolute URL when valid; false otherwise.
    /// </summary>
    bool TryNormalize(string? url, out string normalized);
}
Write UrlValidator.cs
31 lines
namespace UrlShortener.Api.Core.Validation;

/// <summary>
/// Accepts absolute http/https URLs only. Rejects other schemes (javascript:,
/// file:, ftp:) so a short link can't be used to smuggle a non-web destination.
/// </summary>
public sealed class UrlValidator : IUrlValidator
{
    public bool TryNormalize(string? url, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(url))
            return false;

        var candidate = url.Trim();

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        if (string.IsNullOrEmpty(uri.Host))
            return false;

        normalized = uri.ToString();
        return true;
    }
}
LinkService + DTOs. เพิ่ม collision policy ลง interface ก่อน (ให้ service ไม่ผูก concrete type — คง OCP):

Edit IShortCodeGenerator.cs
Added 7 lines
Edit RandomShortCodeGenerator.cs
Added 2 lines
Edit CustomAliasGenerator.cs
Added 2 lines
Todo update + DTOs/service:

Update Todos

Clean template files (WeatherForecast, UnitTest1)

Build Core domain (Link, Destination, enums)

Build short-code generation (interface, generators, resolver)

Build repository (ILinkRepository, InMemory)

Build platform detection + URL validation

Build LinkService + DTOs + exceptions

Build Features/endpoints (controllers)

Wire Program.cs DI + appsettings + CORS

Write unit tests

Build succeeds + tests pass

Scaffold React frontend (Vite + TS)

Write README + design notes + .gitignore
Write Contracts.cs
26 lines
using UrlShortener.Api.Core;

namespace UrlShortener.Api.Features.Links;

/// <summary>Request body for creating a link.</summary>
/// <param name="Url">The default destination. Required, must be an absolute http/https URL.</param>
/// <param name="CustomAlias">Optional user-chosen short code; auto-generated when omitted.</param>
/// <param name="Destinations">Optional platform overrides, e.g. { "android": "...", "ios": "..." }.</param>
public sealed record CreateLinkRequest(
    string Url,
    string? CustomAlias = null,
    Dictionary<string, string>? Destinations = null);

/// <summary>Request body for enabling/disabling a link.</summary>
public sealed record UpdateStatusRequest(LinkStatus Status);

/// <summary>Public view of a link, including stats.</summary>
public sealed record LinkResponse(
    string ShortCode,
    string ShortUrl,
    Dictionary<string, string> Destinations,
    string Status,
    long ClickCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastAccessedAt);
Write ShortUrlOptions.cs
13 lines
namespace UrlShortener.Api.Features.Links;

/// <summary>
/// Configurable base URL for generated short links (e.g. "https://gul.fy").
/// Bound from the "ShortUrl" section so the display domain isn't hard-coded.
/// </summary>
public sealed class ShortUrlOptions
{
    public const string SectionName = "ShortUrl";

    public string BaseUrl { get; set; } = "http://localhost:5000";
}
เพิ่ม UpdateAsync ใน repository (ให้ status change/visit persist ผ่าน seam — DB จริงต่อได้):

Edit ILinkRepository.cs
Added 7 lines
Edit InMemoryLinkRepository.cs
Added 8 lines
Now the service:

Write LinkService.cs
109 lines
using UrlShortener.Api.Core;
using UrlShortener.Api.Core.Persistence;
using UrlShortener.Api.Core.ShortCodes;
using UrlShortener.Api.Core.Validation;

namespace UrlShortener.Api.Features.Links;

/// <summary>
/// Application logic for links: creation (with pluggable code generation and
/// collision handling), stats, status changes, deletion, and redirect
/// resolution. Depends only on abstractions so storage, code generation, URL
/// validation, and the clock are all swappable and testable.
/// </summary>
public sealed class LinkService(
    ILinkRepository repository,
    ShortCodeGeneratorResolver resolver,
    IUrlValidator urlValidator,
    TimeProvider clock)
{
    private const int MaxGenerationAttempts = 5;

    public async Task<Link> CreateAsync(CreateLinkRequest request, CancellationToken ct = default)
    {
        var destinations = BuildDestinations(request);
        var genRequest = new GenerationRequest(request.CustomAlias);
        var generator = resolver.Resolve(genRequest);

        for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            var code = generator.Generate(genRequest); // may throw InvalidAliasException
            var link = new Link(code, destinations, clock.GetUtcNow());

            if (await repository.TryAddAsync(link, ct))
                return link;

            // Deterministic strategies (custom alias) can't escape a collision by retrying.
            if (!generator.AllowRetryOnCollision)
                throw new AliasAlreadyExistsException(code);
        }

        throw new ShortCodeGenerationException(
            $"Could not generate a unique short code after {MaxGenerationAttempts} attempts.");
    }

    public Task<Link?> GetAsync(string shortCode, CancellationToken ct = default) =>
        repository.GetAsync(shortCode, ct);

    public Task<IReadOnlyCollection<Link>> GetAllAsync(CancellationToken ct = default) =>
        repository.GetAllAsync(ct);

    /// <summary>Enables or disables a link. Returns null if it does not exist.</summary>
    public async Task<Link?> SetStatusAsync(string shortCode, LinkStatus status, CancellationToken ct = default)
    {
        var link = await repository.GetAsync(shortCode, ct);
        if (link is null)
            return null;

        link.SetStatus(status);
        await repository.UpdateAsync(link, ct);
        return link;
    }

    public Task<bool> DeleteAsync(string shortCode, CancellationToken ct = default) =>
        repository.RemoveAsync(shortCode, ct);

    /// <summary>
    /// Resolves the destination for a visit and records the click. Returns null
    /// when the link is missing or disabled, so the caller responds 404 rather
    /// than redirecting.
    /// </summary>
    public async Task<string?> ResolveForRedirectAsync(string shortCode, Platform platform, CancellationToken ct = default)
    {
        var link = await repository.GetAsync(shortCode, ct);
        if (link is null || link.Status != LinkStatus.Active)
            return null;

        link.RegisterVisit(clock.GetUtcNow());
        await repository.UpdateAsync(link, ct);
        return link.ResolveDestination(platform);
    }

    private Dictionary<Platform, string> BuildDestinations(CreateLinkRequest request)
    {
        if (!urlValidator.TryNormalize(request.Url, out var defaultUrl))
            throw new ValidationException("A valid absolute http/https URL is required.");

        var destinations = new Dictionary<Platform, string> { [Platform.Default] = defaultUrl };

        if (request.Destinations is null)
            return destinations;

        foreach (var (key, value) in request.Destinations)
        {
            if (!Enum.TryParse<Platform>(key, ignoreCase: true, out var platform))
                throw new ValidationException($"Unknown platform '{key}'. Valid values: default, ios, android.");

            if (platform == Platform.Default)
                continue; // the default destination always comes from Url

            if (!urlValidator.TryNormalize(value, out var url))
                throw new ValidationException($"Destination for '{key}' is not a valid absolute http/https URL.");

            destinations[platform] = url;
        }

        return destinations;
    }
}
Mapper (Link → LinkResponse ด้วย base URL):

Write LinkMapper.cs
19 lines
using UrlShortener.Api.Core;

namespace UrlShortener.Api.Features.Links;

/// <summary>Maps the <see cref="Link"/> domain model to its API representation.</summary>
public static class LinkMapper
{
    public static LinkResponse ToResponse(this Link link, string baseUrl) => new(
        ShortCode: link.ShortCode,
        ShortUrl: $"{baseUrl.TrimEnd('/')}/{link.ShortCode}",
        Destinations: link.Destinations.ToDictionary(
            kvp => kvp.Key.ToString().ToLowerInvariant(),
            kvp => kvp.Value),
        Status: link.Status.ToString(),
        ClickCount: link.ClickCount,
        CreatedAt: link.CreatedAt,
        LastAccessedAt: link.LastAccessedAt);
}
Endpoints (Minimal API, vertical slice) + exception handler. สร้าง Common folder:

Bash Create Common folder
IN
mkdir -p e:/coding/url_link_shortener/url_link_shortener/backend/src/UrlShortener.Api/Common && echo ok

OUT
ok
Write DomainExceptionHandler.cs
52 lines
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Core;

namespace UrlShortener.Api.Common;

/// <summary>
/// Translates <see cref="DomainException"/>s into RFC 7807 ProblemDetails
/// responses with the right status code, keeping HTTP concerns out of the
/// service layer. Unhandled exceptions fall through to the default 500 handler.
/// </summary>
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
            return false; // not ours — let the default handler deal with it

        var status = exception switch
        {
            AliasAlreadyExistsException => StatusCodes.Status409Conflict,
            ValidationException or InvalidAliasException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = domainException,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = ReasonPhrases.GetTitle(status),
                Detail = domainException.Message
            }
        });
    }

    private static class ReasonPhrases
    {
        public static string GetTitle(int status) => status switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status409Conflict => "Conflict",
            _ => "Server Error"
        };
    }
}
Write LinksEndpoints.cs
79 lines
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UrlShortener.Api.Core;

namespace UrlShortener.Api.Features.Links;

/// <summary>Management endpoints for links, grouped under /api/links.</summary>
public static class LinksEndpoints
{
    public static IEndpointRouteBuilder MapLinksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/links").WithTags("Links");

        // Create
        group.MapPost("/", async (
            CreateLinkRequest request,
            LinkService service,
            IOptions<ShortUrlOptions> options,
            CancellationToken ct) =>
        {
            var link = await service.CreateAsync(request, ct);
            var response = link.ToResponse(options.Value.BaseUrl);
            return Results.Created($"/api/links/{link.ShortCode}", response);
        });

        // List
        group.MapGet("/", async (
            LinkService service,
            IOptions<ShortUrlOptions> options,
            CancellationToken ct) =>
        {
            var links = await service.GetAllAsync(ct);
            var baseUrl = options.Value.BaseUrl;
            return Results.Ok(links
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => l.ToResponse(baseUrl)));
        });

        // Stats for one link
        group.MapGet("/{shortCode}", async (
            string shortCode,
            LinkService service,
            IOptions<ShortUrlOptions> options,
            CancellationToken ct) =>
        {
            var link = await service.GetAsync(shortCode, ct);
            return link is null
                ? Results.NotFound()
                : Results.Ok(link.ToResponse(options.Value.BaseUrl));
        });

        // Enable / disable
        group.MapPatch("/{shortCode}", async (
            string shortCode,
            UpdateStatusRequest request,
            LinkService service,
            IOptions<ShortUrlOptions> options,
            CancellationToken ct) =>
        {
            var link = await service.SetStatusAsync(shortCode, request.Status, ct);
            return link is null
                ? Results.NotFound()
                : Results.Ok(link.ToResponse(options.Value.BaseUrl));
        });

        // Delete
        group.MapDelete("/{shortCode}", async (
            string shortCode,
            LinkService service,
            CancellationToken ct) =>
        {
            var removed = await service.DeleteAsync(shortCode, ct);
            return removed ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }
}
Write RedirectEndpoints.cs
35 lines
using UrlShortener.Api.Core;
using UrlShortener.Api.Features.Links;

namespace UrlShortener.Api.Features.Redirect;

/// <summary>
/// The public redirect endpoint, mapped at the root so short links look like
/// {baseUrl}/{shortCode}.
/// </summary>
public static class RedirectEndpoints
{
    public static IEndpointRouteBuilder MapRedirectEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{shortCode}", async (
            string shortCode,
            HttpContext http,
            LinkService service,
            IPlatformDetector platformDetector,
            CancellationToken ct) =>
        {
            var platform = platformDetector.Detect(http.Request.Headers.UserAgent);
            var destination = await service.ResolveForRedirectAsync(shortCode, platform, ct);

            // 302 (temporary) — never cached, so every visit hits the server:
            // the click is always counted and platform routing is re-evaluated.
            // 404 for missing/disabled links means they do not redirect.
            return destination is null
                ? Results.NotFound()
                : Results.Redirect(destination, permanent: false);
        }).ExcludeFromDescription();

        return app;
    }
}
Wire Program.cs — DI, options, exception handler, CORS, endpoints:

Update Todos

Clean template files (WeatherForecast, UnitTest1)

Build Core domain (Link, Destination, enums)

Build short-code generation (interface, generators, resolver)

Build repository (ILinkRepository, InMemory)

Build platform detection + URL validation

Build LinkService + DTOs + exceptions

Build Features/endpoints (minimal API)

Wire Program.cs DI + appsettings + CORS

Write unit tests

Build succeeds + tests pass

Scaffold React frontend (Vite + TS)

Write README + design notes + .gitignore
Write Program.cs
59 lines
using UrlShortener.Api.Common;
using UrlShortener.Api.Core;
using UrlShortener.Api.Core.Persistence;
using UrlShortener.Api.Core.ShortCodes;
using UrlShortener.Api.Core.Validation;
using UrlShortener.Api.Features.Links;
using UrlShortener.Api.Features.Redirect;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCors = "frontend";

// --- Configuration ---
builder.Services.Configure<ShortUrlOptions>(
    builder.Configuration.GetSection(ShortUrlOptions.SectionName));

// --- Core services ---
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ILinkRepository, InMemoryLinkRepository>();
builder.Services.AddSingleton<IUrlValidator, UrlValidator>();
builder.Services.AddSingleton<IPlatformDetector, UserAgentPlatformDetector>();

// Short-code generators. Registration order defines resolution priority:
// specific strategies first, the catch-all random generator last.
builder.Services.AddSingleton<IShortCodeGenerator, CustomAliasGenerator>();
builder.Services.AddSingleton<IShortCodeGenerator, RandomShortCodeGenerator>();
builder.Services.AddSingleton<ShortCodeGeneratorResolver>();

builder.Services.AddScoped<LinkService>();

// --- Error handling (RFC 7807 ProblemDetails) ---
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// --- API surface ---
builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddPolicy(FrontendCors, policy =>
    policy.WithOrigins(
            builder.Configuration.GetValue<string>("Frontend:Origin") ?? "http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors(FrontendCors);

app.MapLinksEndpoints();
app.MapRedirectEndpoints();

app.Run();

// Exposes the implicit Program class to the test project (WebApplicationFactory).
public partial class Program;
appsettings — เพิ่ม ShortUrl + Frontend config:

Read appsettings.json
Read launchSettings.json
Write appsettings.json
16 lines
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ShortUrl": {
    "BaseUrl": "http://localhost:5000"
  },
  "Frontend": {
    "Origin": "http://localhost:5173"
  }
}
Edit launchSettings.json
Modified
Build เช็ค error ก่อนเขียน test:

Bash Build API project
IN
cd e:/coding/url_link_shortener/url_link_shortener/backend && dotnet build src/UrlShortener.Api/UrlShortener.Api.csproj -v q 2>&1 | tail -30

OUT
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:04.67
Build ผ่าน. เขียน unit tests — core logic ตามที่โจทย์สั่ง (short-code gen, URL validation, click counting, disable/delete, platform, collision):

Update Todos

Clean template files (WeatherForecast, UnitTest1)

Build Core domain (Link, Destination, enums)

Build short-code generation (interface, generators, resolver)

Build repository (ILinkRepository, InMemory)

Build platform detection + URL validation

Build LinkService + DTOs + exceptions

Build Features/endpoints (minimal API)

Wire Program.cs DI + appsettings + CORS

Write unit tests

Build succeeds + tests pass

Scaffold React frontend (Vite + TS)

Write README + design notes + .gitignore
Read UrlShortener.UnitTests.csproj
Plain xUnit. เขียน tests — เพิ่ม FixedTimeProvider stub เอง (ไม่พึ่ง external package):

Write FixedTimeProvider.cs
12 lines
namespace UrlShortener.UnitTests;

/// <summary>A controllable clock for deterministic time assertions in tests.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
}
Write RandomShortCodeGeneratorTests.cs
53 lines
using UrlShortener.Api.Core.ShortCodes;

namespace UrlShortener.UnitTests.ShortCodes;

public class RandomShortCodeGeneratorTests
{
    private readonly RandomShortCodeGenerator _generator = new();
    private static readonly GenerationRequest AnyRequest = new(CustomAlias: null);

    [Fact]
    public void Generate_produces_a_seven_character_code()
    {
        var code = _generator.Generate(AnyRequest);
        Assert.Equal(7, code.Length);
    }

    [Fact]
    public void Generate_uses_only_the_unambiguous_alphabet()
    {
        const string forbidden = "0O1lI";
        for (var i = 0; i < 100; i++)
        {
            var code = _generator.Generate(AnyRequest);
            Assert.DoesNotContain(code, c => forbidden.Contains(c));
        }
    }

    [Fact]
    public void Generate_is_effectively_unique_across_many_calls()
    {
        var codes = Enumerable.Range(0, 10_000)
            .Select(_ => _generator.Generate(AnyRequest))
            .ToHashSet();

        // A handful of collisions in 10k draws would still be acceptable, but with
        // a ~56^7 space we expect none.
        Assert.Equal(10_000, codes.Count);
    }

    [Fact]
    public void CanHandle_is_always_true_so_it_acts_as_the_fallback()
    {
        Assert.True(_generator.CanHandle(AnyRequest));
        Assert.True(_generator.CanHandle(new GenerationRequest("anything")));
    }

    [Fact]
    public void AllowRetryOnCollision_is_true()
    {
        Assert.True(_generator.AllowRetryOnCollision);
    }
}
Write CustomAliasGeneratorTests.cs
45 lines
using UrlShortener.Api.Core;
using UrlShortener.Api.Core.ShortCodes;

namespace UrlShortener.UnitTests.ShortCodes;

public class CustomAliasGeneratorTests
{
    private readonly CustomAliasGenerator _generator = new();

    [Theory]
    [InlineData("mylink")]
    [InlineData("My-Link_1")]
    [InlineData("abc")]
    [InlineData("a-very-long-but-valid-alias-30c")]
    public void Generate_returns_valid_alias_trimmed(string alias)
    {
        var code = _generator.Generate(new GenerationRequest($"  {alias}  "));
        Assert.Equal(alias, code);
    }

    [Theory]
    [InlineData("ab")]              // too short
    [InlineData("has space")]       // space not allowed
    [InlineData("bad!char")]        // punctuation not allowed
    [InlineData("way-too-long-alias-that-exceeds-thirty-characters")]
    public void Generate_rejects_invalid_alias(string alias)
    {
        Assert.Throws<InvalidAliasException>(() => _generator.Generate(new GenerationRequest(alias)));
    }

    [Fact]
    public void CanHandle_only_when_alias_present()
    {
        Assert.True(_generator.CanHandle(new GenerationRequest("mylink")));
        Assert.False(_generator.CanHandle(new GenerationRequest(null)));
        Assert.False(_generator.CanHandle(new GenerationRequest("   ")));
    }

    [Fact]
    public void AllowRetryOnCollision_is_false()
    {
        Assert.False(_generator.AllowRetryOnCollision);
    }
}
Edit CustomAliasGeneratorTests.cs
Modified
Write ShortCodeGeneratorResolverTests.cs
31 lines
using UrlShortener.Api.Core.ShortCodes;

namespace UrlShortener.UnitTests.ShortCodes;

public class ShortCodeGeneratorResolverTests
{
    // Mirrors the DI registration order: custom alias first, random fallback last.
    private readonly ShortCodeGeneratorResolver _resolver = new(
        [new CustomAliasGenerator(), new RandomShortCodeGenerator()]);

    [Fact]
    public void Resolve_picks_custom_alias_when_alias_is_supplied()
    {
        var generator = _resolver.Resolve(new GenerationRequest("mylink"));
        Assert.Equal("custom-alias", generator.Name);
    }

    [Fact]
    public void Resolve_falls_back_to_random_when_no_alias()
    {
        var generator = _resolver.Resolve(new GenerationRequest(null));
        Assert.Equal("random", generator.Name);
    }

    [Fact]
    public void Constructor_throws_when_no_generators_registered()
    {
        Assert.Throws<InvalidOperationException>(() => new ShortCodeGeneratorResolver([]));
    }
}
Write UrlValidatorTests.cs
42 lines
using UrlShortener.Api.Core.Validation;

namespace UrlShortener.UnitTests.Validation;

public class UrlValidatorTests
{
    private readonly UrlValidator _validator = new();

    [Theory]
    [InlineData("https://www.google.co.th")]
    [InlineData("http://example.com/path?q=1")]
    [InlineData("https://sub.domain.io:8443/a/b")]
    public void TryNormalize_accepts_absolute_http_urls(string url)
    {
        Assert.True(_validator.TryNormalize(url, out var normalized));
        Assert.False(string.IsNullOrEmpty(normalized));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("example.com")]                 // missing scheme -> not absolute
    [InlineData("/relative/path")]
    [InlineData("javascript:alert(1)")]          // dangerous scheme
    [InlineData("ftp://files.example.com")]      // non-web scheme
    [InlineData("file:///etc/passwd")]
    public void TryNormalize_rejects_invalid_or_unsafe_urls(string? url)
    {
        Assert.False(_validator.TryNormalize(url, out var normalized));
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void TryNormalize_trims_whitespace()
    {
        Assert.True(_validator.TryNormalize("  https://example.com  ", out var normalized));
        Assert.StartsWith("https://example.com", normalized);
    }
}
Write UserAgentPlatformDetectorTests.cs
37 lines
using UrlShortener.Api.Core;

namespace UrlShortener.UnitTests.Platform;

public class UserAgentPlatformDetectorTests
{
    private readonly UserAgentPlatformDetector _detector = new();

    [Theory]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36")]
    [InlineData("Dalvik/2.1.0 (Linux; U; Android 13)")]
    public void Detect_returns_Android_for_android_agents(string userAgent)
    {
        Assert.Equal(Api.Core.Platform.Android, _detector.Detect(userAgent));
    }

    [Theory]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X)")]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 16_0 like Mac OS X)")]
    [InlineData("Mozilla/5.0 (iPod touch; CPU iPhone OS 15_0 like Mac OS X)")]
    public void Detect_returns_iOS_for_apple_mobile_agents(string userAgent)
    {
        Assert.Equal(Api.Core.Platform.iOS, _detector.Detect(userAgent));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64)")]
    [InlineData("curl/8.4.0")]
    [InlineData("Googlebot/2.1")]
    public void Detect_falls_back_to_Default_for_unknown_or_missing_agents(string? userAgent)
    {
        Assert.Equal(Api.Core.Platform.Default, _detector.Detect(userAgent));
    }
}
Domain Link tests + LinkService tests (core behavior — click counting, disable/delete, platform routing, collision):

Write LinkTests.cs
64 lines
using UrlShortener.Api.Core;

namespace UrlShortener.UnitTests;

public class LinkTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 13, 10, 0, 0, TimeSpan.Zero);

    private static Link BuildLink(params (Platform, string)[] destinations)
    {
        var map = destinations.ToDictionary(d => d.Item1, d => d.Item2);
        return new Link("abc123", map, Now);
    }

    [Fact]
    public void Constructor_requires_a_default_destination()
    {
        var withoutDefault = new Dictionary<Platform, string> { [Platform.iOS] = "https://a.io" };
        Assert.Throws<ArgumentException>(() => new Link("abc", withoutDefault, Now));
    }

    [Fact]
    public void New_link_is_active_with_zero_clicks_and_no_last_access()
    {
        var link = BuildLink((Platform.Default, "https://a.com"));

        Assert.Equal(LinkStatus.Active, link.Status);
        Assert.Equal(0, link.ClickCount);
        Assert.Null(link.LastAccessedAt);
    }

    [Fact]
    public void ResolveDestination_falls_back_to_default_for_unmapped_platform()
    {
        var link = BuildLink(
            (Platform.Default, "https://default.com"),
            (Platform.Android, "https://android.com"));

        Assert.Equal("https://android.com", link.ResolveDestination(Platform.Android));
        Assert.Equal("https://default.com", link.ResolveDestination(Platform.iOS));
    }

    [Fact]
    public void RegisterVisit_increments_count_and_records_time()
    {
        var link = BuildLink((Platform.Default, "https://a.com"));
        link.RegisterVisit(Now);

        Assert.Equal(1, link.ClickCount);
        Assert.Equal(Now, link.LastAccessedAt);
    }

    [Fact]
    public void RegisterVisit_counts_are_atomic_under_concurrency()
    {
        var link = BuildLink((Platform.Default, "https://a.com"));
        const int visits = 10_000;

        Parallel.For(0, visits, _ => link.RegisterVisit(Now));

        Assert.Equal(visits, link.ClickCount);
    }
}
Now the LinkService tests (the biggest — full behavior):

Write LinkServiceTests.cs
204 lines
using UrlShortener.Api.Core;
using UrlShortener.Api.Core.Persistence;
using UrlShortener.Api.Core.ShortCodes;
using UrlShortener.Api.Core.Validation;
using UrlShortener.Api.Features.Links;

namespace UrlShortener.UnitTests.Services;

public class LinkServiceTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 13, 10, 0, 0, TimeSpan.Zero);

    private readonly FixedTimeProvider _clock = new(Start);
    private readonly ILinkRepository _repository = new InMemoryLinkRepository();
    private readonly LinkService _service;

    public LinkServiceTests()
    {
        var resolver = new ShortCodeGeneratorResolver(
            [new CustomAliasGenerator(), new RandomShortCodeGenerator()]);
        _service = new LinkService(_repository, resolver, new UrlValidator(), _clock);
    }

    // --- Create ---

    [Fact]
    public async Task Create_returns_active_link_with_zero_clicks()
    {
        var link = await _service.CreateAsync(new CreateLinkRequest("https://example.com"));

        Assert.Equal(LinkStatus.Active, link.Status);
        Assert.Equal(0, link.ClickCount);
        Assert.Equal(Start, link.CreatedAt);
        Assert.Equal(7, link.ShortCode.Length); // auto-generated
    }

    [Fact]
    public async Task Create_uses_custom_alias_when_supplied()
    {
        var link = await _service.CreateAsync(new CreateLinkRequest("https://example.com", "mylink"));
        Assert.Equal("mylink", link.ShortCode);
    }

    [Fact]
    public async Task Create_rejects_invalid_url()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(new CreateLinkRequest("not-a-url")));
    }

    [Fact]
    public async Task Create_rejects_invalid_alias()
    {
        await Assert.ThrowsAsync<InvalidAliasException>(
            () => _service.CreateAsync(new CreateLinkRequest("https://example.com", "no good!")));
    }

    [Fact]
    public async Task Create_with_duplicate_alias_throws_conflict()
    {
        await _service.CreateAsync(new CreateLinkRequest("https://example.com", "dup"));

        await Assert.ThrowsAsync<AliasAlreadyExistsException>(
            () => _service.CreateAsync(new CreateLinkRequest("https://other.com", "dup")));
    }

    [Fact]
    public async Task Create_duplicate_alias_is_case_insensitive()
    {
        await _service.CreateAsync(new CreateLinkRequest("https://example.com", "MyLink"));

        await Assert.ThrowsAsync<AliasAlreadyExistsException>(
            () => _service.CreateAsync(new CreateLinkRequest("https://other.com", "mylink")));
    }

    [Fact]
    public async Task Create_stores_platform_destinations()
    {
        var request = new CreateLinkRequest(
            "https://example.com",
            Destinations: new Dictionary<string, string>
            {
                ["android"] = "https://example.com/android.apk",
                ["ios"] = "https://example.com/app.ipa"
            });

        var link = await _service.CreateAsync(request);

        Assert.Equal("https://example.com/android.apk", link.ResolveDestination(Platform.Android));
        Assert.Equal("https://example.com/app.ipa", link.ResolveDestination(Platform.iOS));
        Assert.StartsWith("https://example.com", link.ResolveDestination(Platform.Default));
    }

    [Fact]
    public async Task Create_rejects_unknown_platform_key()
    {
        var request = new CreateLinkRequest(
            "https://example.com",
            Destinations: new Dictionary<string, string> { ["windows"] = "https://example.com/win" });

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));
    }

    // --- Redirect / click counting ---

    [Fact]
    public async Task Redirect_increments_click_and_records_access_time()
    {
        var link = await _service.CreateAsync(new CreateLinkRequest("https://example.com"));
        _clock.Advance(TimeSpan.FromMinutes(5));

        var destination = await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default);

        Assert.StartsWith("https://example.com", destination);
        var reloaded = await _service.GetAsync(link.ShortCode);
        Assert.Equal(1, reloaded!.ClickCount);
        Assert.Equal(Start.AddMinutes(5), reloaded.LastAccessedAt);
    }

    [Fact]
    public async Task Redirect_selects_destination_by_platform()
    {
        var link = await _service.CreateAsync(new CreateLinkRequest(
            "https://example.com",
            Destinations: new Dictionary<string, string>
            {
                ["android"] = "https://example.com/android.apk",
                ["ios"] = "https://example.com/app.ipa"
            }));

        Assert.Equal("https://example.com/android.apk",
            await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Android));
        Assert.Equal("https://example.com/app.ipa",
            await _service.ResolveForRedirectAsync(link.ShortCode, Platform.iOS));
        Assert.StartsWith("https://example.com",
            await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default));
    }

    [Fact]
    public async Task Redirect_returns_null_for_missing_link()
    {
        Assert.Null(await _service.ResolveForRedirectAsync("nope", Platform.Default));
    }

    [Fact]
    public async Task Redirect_on_disabled_link_returns_null_and_does_not_count()
    {
        var link = await _service.CreateAsync(new CreateLinkRequest("https://example.com"));
        await _service.SetStatusAsync(link.ShortCode, LinkStatus.Disabled);

        var destination = await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default);

        Assert.Null(destination);
        var reloaded = await _service.GetAsync(link.ShortCode);
        Assert.Equal(0, reloaded!.ClickCount);
    }

    // --- Disable / enable / delete ---

    [Fact]
    public async Task SetStatus_can_disable_then_re_enable()
    {
        var link = await _service.CreateAsync(new CreateLinkRequest("https://example.com"));

        await _service.SetStatusAsync(link.ShortCode, LinkStatus.Disabled);
        Assert.Null(await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default));

        await _service.SetStatusAsync(link.ShortCode, LinkStatus.Active);
        Assert.NotNull(await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default));
    }

    [Fact]
    public async Task SetStatus_returns_null_for_missing_link()
    {
        Assert.Null(await _service.SetStatusAsync("nope", LinkStatus.Disabled));
    }

    [Fact]
    public async Task Delete_removes_link_and_stops_redirects()
    {
        var link = await _service.CreateAsync(new CreateLinkRequest("https://example.com"));

        Assert.True(await _service.DeleteAsync(link.ShortCode));
        Assert.Null(await _service.GetAsync(link.ShortCode));
        Assert.Null(await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default));
    }

    [Fact]
    public async Task Delete_returns_false_for_missing_link()
    {
        Assert.False(await _service.DeleteAsync("nope"));
    }

    [Fact]
    public async Task Deleted_alias_can_be_reused()
    {
        var link = await _service.CreateAsync(new CreateLinkRequest("https://example.com", "reuse"));
        await _service.DeleteAsync(link.ShortCode);

        var recreated = await _service.CreateAsync(new CreateLinkRequest("https://other.com", "reuse"));
        Assert.Equal("reuse", recreated.ShortCode);
    }
}
Run tests:

Bash Run all unit tests
IN
cd e:/coding/url_link_shortener/url_link_shortener/backend && dotnet test -v q 2>&1 | tail -40

OUT
E:\coding\url_link_shortener\url_link_shortener\backend\tests\UrlShortener.UnitTests\LinkTests.cs(9,43): error CS0118: 'Platform' is a namespace but is used like a type [E:\coding\url_link_shortener\url_link_shortener\backend\tests\UrlShortener.UnitTests\UrlShortener.UnitTests.csproj]
Namespace UnitTests.Platform ชนกับ enum Platform. เปลี่ยนชื่อ namespace test:

Edit UserAgentPlatformDetectorTests.cs
Modified
Bash Re-run all unit tests
IN
cd e:/coding/url_link_shortener/url_link_shortener/backend && dotnet test -v q 2>&1 | tail -30

OUT
Test run for E:\coding\url_link_shortener\url_link_shortener\backend\tests\UrlShortener.UnitTests\bin\Debug\net9.0\UrlShortener.UnitTests.dll (.NETCoreApp,Version=v9.0)
VSTest version 17.14.1 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    63, Skipped:     0, Total:    63, Duration: 105 ms - UrlShortener.UnitTests.dll (net9.0)
63 tests ผ่านหมด. Backend เสร็จ. ต่อ frontend:

Update Todos

Clean template files (WeatherForecast, UnitTest1)

Build Core domain (Link, Destination, enums)

Build short-code generation (interface, generators, resolver)

Build repository (ILinkRepository, InMemory)

Build platform detection + URL validation

Build LinkService + DTOs + exceptions

Build Features/endpoints (minimal API)

Wire Program.cs DI + appsettings + CORS

Write unit tests

Build succeeds + tests pass (63 pass)

Scaffold React frontend (Vite + TS)

Write README + design notes + .gitignore
Scaffold React + Vite + TS:

Bash Scaffold Vite React TS frontend
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && npm create vite@latest . -- --template react-ts 2>&1 | tail -15

OUT
                                                                                                                                               ^^^^^^^^^
SyntaxError: The requested module 'node:util' does not provide an export named 'styleText'
    at ModuleJob._instantiate (node:internal/modules/esm/module_job:132:21)
    at async ModuleJob.run (node:internal/modules/esm/module_job:214:5)
    at async ModuleLoader.import (node:internal/modules/esm/loader:329:24)
    at async loadESM (node:internal/process/esm_loader:28:7)
    at async handleMainPromise (node:internal/modules/run_main:113:12)

Node.js v21.2.0
npm ERR! code 1
npm ERR! path E:\coding\url_link_shortener\url_link_shortener\frontend
npm ERR! command failed
npm ERR! command C:\Windows\system32\cmd.exe /d /s /c create-vite . --template react-ts

npm ERR! A complete log of this run can be found in: C:\Users\Pollapat\AppData\Local\npm-cache\_logs\2026-07-13T16_24_40_263Z-debug-0.log
Node 21.2 ขาด styleText ที่ create-vite ใหม่ต้องใช้. Pin version เก่า:

Bash Scaffold with pinned create-vite 5
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && npm create vite@5.5.5 . -- --template react-ts 2>&1 | tail -15

OUT
npm WARN exec The following package was not found and will be installed: create-vite@5.5.5

Scaffolding project in E:\coding\url_link_shortener\url_link_shortener\frontend...

Done. Now run:

  npm install
  npm run dev
Scaffolded. ดูไฟล์ที่ได้:

Bash List frontend files
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && find . -type f -not -path './node_modules/*' | sort

OUT
./.gitignore
./README.md
./eslint.config.js
./index.html
./package.json
./public/vite.svg
./src/App.css
./src/App.tsx
./src/assets/react.svg
./src/index.css
./src/main.tsx
./src/vite-env.d.ts
./tsconfig.app.json
./tsconfig.json
./tsconfig.node.json
./vite.config.ts
Bash Show generated package.json
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && cat package.json

OUT
{
  "name": "frontend",
  "private": true,
  "version": "0.0.0",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "tsc -b && vite build",
    "lint": "eslint .",
    "preview": "vite preview"
  },
  "dependencies": {
    "react": "^18.3.1",
    "react-dom": "^18.3.1"
  },
  "devDependencies": {
    "@eslint/js": "^9.13.0",
    "@types/react": "^18.3.12",
    "@types/react-dom": "^18.3.1",
    "@vitejs/plugin-react": "^4.3.3",
    "eslint": "^9.13.0",
    "eslint-plugin-react-hooks": "^5.0.0",
    "eslint-plugin-react-refresh": "^0.4.14",
    "globals": "^15.11.0",
    "typescript": "~5.6.2",
    "typescript-eslint": "^8.11.0",
    "vite": "^5.4.10"
  }
}
Vite 5 / React 18 — ดี. ติดตั้ง deps + qrcode (bonus):

Bash Install frontend deps + qrcode.react
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && npm install 2>&1 | tail -5 && npm install qrcode.react@4 2>&1 | tail -5

OUT
To address all issues (including breaking changes), run:
  npm audit fix --force

Run `npm audit` for details.

To address all issues (including breaking changes), run:
  npm audit fix --force

Run `npm audit` for details.
Bash Confirm qrcode.react installed
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && node -e "const p=require('./package.json'); console.log('qrcode.react', p.dependencies['qrcode.react']); console.log('installed ok')"

OUT
qrcode.react ^4.2.0
installed ok
สร้าง React app จริง. ก่อนอื่นเพิ่ม JSON string-enum converter ที่ backend (PATCH รับ "Disabled" เป็น enum):

Edit Program.cs
Added 1 line
Edit Program.cs
Added 4 lines
Frontend code — API client + types:

Write api.ts
71 lines
// API client for the URL Shortener backend. The base URL is configurable via
// VITE_API_BASE so the frontend can point at a different host in other setups.
const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:5000';

export type LinkStatus = 'Active' | 'Disabled';

export interface LinkResponse {
  shortCode: string;
  shortUrl: string;
  destinations: Record<string, string>;
  status: LinkStatus;
  clickCount: number;
  createdAt: string;
  lastAccessedAt: string | null;
}

export interface CreateLinkRequest {
  url: string;
  customAlias?: string;
  destinations?: Record<string, string>;
}

/** Error carrying the human-readable detail from an RFC 7807 ProblemDetails body. */
export class ApiError extends Error {
  constructor(message: string, readonly status: number) {
    super(message);
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  });

  if (!response.ok) {
    let detail = `Request failed (${response.status})`;
    try {
      const problem = await response.json();
      detail = problem.detail ?? problem.title ?? detail;
    } catch {
      /* non-JSON error body — keep the default message */
    }
    throw new ApiError(detail, response.status);
  }

  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export const api = {
  list: () => request<LinkResponse[]>('/api/links'),

  create: (body: CreateLinkRequest) =>
    request<LinkResponse>('/api/links', {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  setStatus: (shortCode: string, status: LinkStatus) =>
    request<LinkResponse>(`/api/links/${encodeURIComponent(shortCode)}`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    }),

  remove: (shortCode: string) =>
    request<void>(`/api/links/${encodeURIComponent(shortCode)}`, {
      method: 'DELETE',
    }),
};
Bash Show main.tsx and index.html
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && echo "=== main.tsx ===" && cat src/main.tsx && echo "=== index.html ===" && cat index.html

OUT
=== main.tsx ===
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
=== index.html ===
<!doctype html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <link rel="icon" type="image/svg+xml" href="/vite.svg" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Vite + React + TS</title>
  </head>
  <body>
    <div id="root"></div>
    <script type="module" src="/src/main.tsx"></script>
  </body>
</html>
Edit index.html
Modified
CreateLinkForm component:

Write CreateLinkForm.tsx
107 lines
import { useState, type FormEvent } from 'react';
import { api, ApiError, type CreateLinkRequest, type LinkResponse } from '../api';

interface Props {
  onCreated: (link: LinkResponse) => void;
}

export function CreateLinkForm({ onCreated }: Props) {
  const [url, setUrl] = useState('');
  const [customAlias, setCustomAlias] = useState('');
  const [androidUrl, setAndroidUrl] = useState('');
  const [iosUrl, setIosUrl] = useState('');
  const [showPlatforms, setShowPlatforms] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSubmitting(true);

    const destinations: Record<string, string> = {};
    if (androidUrl.trim()) destinations.android = androidUrl.trim();
    if (iosUrl.trim()) destinations.ios = iosUrl.trim();

    const body: CreateLinkRequest = { url: url.trim() };
    if (customAlias.trim()) body.customAlias = customAlias.trim();
    if (Object.keys(destinations).length > 0) body.destinations = destinations;

    try {
      const link = await api.create(body);
      onCreated(link);
      setUrl('');
      setCustomAlias('');
      setAndroidUrl('');
      setIosUrl('');
      setShowPlatforms(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="card create-form" onSubmit={handleSubmit}>
      <label className="field">
        <span>Long URL</span>
        <input
          type="url"
          required
          placeholder="https://www.example.com/very/long/path"
          value={url}
          onChange={(e) => setUrl(e.target.value)}
        />
      </label>

      <label className="field">
        <span>Custom alias <em>(optional)</em></span>
        <input
          type="text"
          placeholder="my-link"
          value={customAlias}
          onChange={(e) => setCustomAlias(e.target.value)}
        />
      </label>

      <button
        type="button"
        className="link-button"
        onClick={() => setShowPlatforms((v) => !v)}
      >
        {showPlatforms ? '− Hide' : '+ Add'} platform-specific destinations
      </button>

      {showPlatforms && (
        <div className="platform-fields">
          <label className="field">
            <span>Android destination</span>
            <input
              type="url"
              placeholder="https://example.com/app.apk"
              value={androidUrl}
              onChange={(e) => setAndroidUrl(e.target.value)}
            />
          </label>
          <label className="field">
            <span>iOS destination</span>
            <input
              type="url"
              placeholder="https://example.com/app.ipa"
              value={iosUrl}
              onChange={(e) => setIosUrl(e.target.value)}
            />
          </label>
        </div>
      )}

      {error && <p className="error" role="alert">{error}</p>}

      <button type="submit" className="primary" disabled={submitting}>
        {submitting ? 'Shortening…' : 'Shorten'}
      </button>
    </form>
  );
}
LinkCard component (stats, copy, QR, disable/enable, delete):

Write LinkCard.tsx
126 lines
import { useState } from 'react';
import { QRCodeCanvas } from 'qrcode.react';
import { api, ApiError, type LinkResponse } from '../api';

interface Props {
  link: LinkResponse;
  onChanged: (link: LinkResponse) => void;
  onDeleted: (shortCode: string) => void;
}

const PLATFORM_LABELS: Record<string, string> = {
  default: 'Default',
  android: 'Android',
  ios: 'iOS',
};

export function LinkCard({ link, onChanged, onDeleted }: Props) {
  const [showQr, setShowQr] = useState(false);
  const [copied, setCopied] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isActive = link.status === 'Active';

  async function copy() {
    await navigator.clipboard.writeText(link.shortUrl);
    setCopied(true);
    setTimeout(() => setCopied(false), 1500);
  }

  async function toggleStatus() {
    setBusy(true);
    setError(null);
    try {
      const updated = await api.setStatus(link.shortCode, isActive ? 'Disabled' : 'Active');
      onChanged(updated);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to update.');
    } finally {
      setBusy(false);
    }
  }

  async function remove() {
    if (!confirm(`Delete ${link.shortCode}? This cannot be undone.`)) return;
    setBusy(true);
    setError(null);
    try {
      await api.remove(link.shortCode);
      onDeleted(link.shortCode);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to delete.');
      setBusy(false);
    }
  }

  const platformDestinations = Object.entries(link.destinations).filter(([p]) => p !== 'default');

  return (
    <article className={`card link-card ${isActive ? '' : 'is-disabled'}`}>
      <div className="link-card-main">
        <div className="link-card-info">
          <div className="short-row">
            <a href={link.shortUrl} target="_blank" rel="noreferrer" className="short-url">
              {link.shortUrl.replace(/^https?:\/\//, '')}
            </a>
            <span className={`badge ${isActive ? 'badge-active' : 'badge-disabled'}`}>
              {link.status}
            </span>
          </div>
          <div className="dest" title={link.destinations.default}>
            → {link.destinations.default}
          </div>
          {platformDestinations.length > 0 && (
            <ul className="platform-list">
              {platformDestinations.map(([platform, dest]) => (
                <li key={platform}>
                  <span className="platform-tag">{PLATFORM_LABELS[platform] ?? platform}</span>
                  <span className="platform-dest">{dest}</span>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="stats">
          <div className="stat">
            <span className="stat-value">{link.clickCount}</span>
            <span className="stat-label">clicks</span>
          </div>
        </div>
      </div>

      <dl className="meta">
        <div>
          <dt>Created</dt>
          <dd>{new Date(link.createdAt).toLocaleString()}</dd>
        </div>
        <div>
          <dt>Last visited</dt>
          <dd>{link.lastAccessedAt ? new Date(link.lastAccessedAt).toLocaleString() : '—'}</dd>
        </div>
      </dl>

      {error && <p className="error" role="alert">{error}</p>}

      <div className="actions">
        <button onClick={copy}>{copied ? 'Copied ✓' : 'Copy'}</button>
        <button onClick={() => setShowQr((v) => !v)}>{showQr ? 'Hide QR' : 'QR'}</button>
        <button onClick={toggleStatus} disabled={busy}>
          {isActive ? 'Disable' : 'Enable'}
        </button>
        <button className="danger" onClick={remove} disabled={busy}>
          Delete
        </button>
      </div>

      {showQr && (
        <div className="qr">
          <QRCodeCanvas value={link.shortUrl} size={140} />
        </div>
      )}
    </article>
  );
}
App.tsx:

Write App.tsx
Write failed
import { useEffect, useState } from 'react';
import { api, type LinkResponse } from './api';
import { CreateLinkForm } from './components/CreateLinkForm';
import { LinkCard } from './components/LinkCard';

export default function App() {
  const [links, setLinks] = useState<LinkResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    api
      .list()
      .then(setLinks)
      .catch(() => setLoadError('Could not reach the API. Is the backend running on :5000?'))
      .finally(() => setLoading(false));
  }, []);

  function handleCreated(link: LinkResponse) {
    setLinks((prev) => [link, ...prev]);
  }

  function handleChanged(updated: LinkResponse) {
    setLinks((prev) => prev.map((l) => (l.shortCode === updated.shortCode ? updated : l)));
  }

  function handleDeleted(shortCode: string) {
    setLinks((prev) => prev.filter((l) => l.shortCode !== shortCode));
  }

  return (
    <div className="app">
      <header className="app-header">
        <h1>🔗 Shortly</h1>
        <p>Turn long URLs into short, trackable links.</p>
      </header>

      <main className="container">
        <CreateLinkForm onCreated={handleCreated} />

        <section className="links-section">
          <h2>Your links {links.length > 0 && <span className="count">{links.length}</span>}</h2>

          {loading && <p className="muted">Loading…</p>}
          {loadError && <p className="error">{loadError}</p>}
          {!loading && !loadError && links.length === 0 && (
            <p className="muted empty">No links yet — create your first one above.</p>
          )}

          <div className="link-list">
            {links.map((link) => (
              <LinkCard
                key={link.shortCode}
                link={link}
                onChanged={handleChanged}
                onDeleted={handleDeleted}
              />
            ))}
          </div>
        </section>
      </main>

      <footer className="app-footer">
        <span>Full Stack Assignment · .NET + React</span>
      </footer>
    </div>
  );
}
Read App.tsx
Write App.tsx
69 lines
import { useEffect, useState } from 'react';
import { api, type LinkResponse } from './api';
import { CreateLinkForm } from './components/CreateLinkForm';
import { LinkCard } from './components/LinkCard';

export default function App() {
  const [links, setLinks] = useState<LinkResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    api
      .list()
      .then(setLinks)
      .catch(() => setLoadError('Could not reach the API. Is the backend running on :5000?'))
      .finally(() => setLoading(false));
  }, []);

  function handleCreated(link: LinkResponse) {
    setLinks((prev) => [link, ...prev]);
  }

  function handleChanged(updated: LinkResponse) {
    setLinks((prev) => prev.map((l) => (l.shortCode === updated.shortCode ? updated : l)));
  }

  function handleDeleted(shortCode: string) {
    setLinks((prev) => prev.filter((l) => l.shortCode !== shortCode));
  }

  return (
    <div className="app">
      <header className="app-header">
        <h1>🔗 Shortly</h1>
        <p>Turn long URLs into short, trackable links.</p>
      </header>

      <main className="container">
        <CreateLinkForm onCreated={handleCreated} />

        <section className="links-section">
          <h2>Your links {links.length > 0 && <span className="count">{links.length}</span>}</h2>

          {loading && <p className="muted">Loading…</p>}
          {loadError && <p className="error">{loadError}</p>}
          {!loading && !loadError && links.length === 0 && (
            <p className="muted empty">No links yet — create your first one above.</p>
          )}

          <div className="link-list">
            {links.map((link) => (
              <LinkCard
                key={link.shortCode}
                link={link}
                onChanged={handleChanged}
                onDeleted={handleDeleted}
              />
            ))}
          </div>
        </section>
      </main>

      <footer className="app-footer">
        <span>Full Stack Assignment · .NET + React</span>
      </footer>
    </div>
  );
}
ลบ App.css ที่ไม่ใช้ + เขียน index.css (responsive, theme-aware):

Bash Remove unused App.css and assets
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && rm -f src/App.css src/assets/react.svg && rmdir src/assets 2>/dev/null; echo cleaned

OUT
cleaned
Write index.css
Write failed
:root {
  --bg: #f5f6f8;
  --surface: #ffffff;
  --border: #e4e7ec;
  --text: #1a1d21;
  --muted: #6b7280;
  --primary: #4f46e5;
  --primary-hover: #4338ca;
  --danger: #dc2626;
  --success: #16a34a;
  --radius: 12px;
  --shadow: 0 1px 3px rgba(16, 24, 40, 0.06), 0 1px 2px rgba(16, 24, 40, 0.04);

  font-family: system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif;
  color: var(--text);
  background: var(--bg);
}

@media (prefers-color-scheme: dark) {
  :root {
    --bg: #0f1115;
    --surface: #191c22;
    --border: #2a2f38;
    --text: #e7e9ee;
    --muted: #9099a8;
    --primary: #7c74ff;
    --primary-hover: #948dff;
    --shadow: 0 1px 3px rgba(0, 0, 0, 0.4);
  }
}

* {
  box-sizing: border-box;
}

body {
  margin: 0;
  min-height: 100vh;
  background: var(--bg);
}

.app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.app-header {
  text-align: center;
  padding: 2.5rem 1rem 1.5rem;
}

.app-header h1 {
  margin: 0;
  font-size: 2rem;
  letter-spacing: -0.02em;
}

.app-header p {
  margin: 0.4rem 0 0;
  color: var(--muted);
}

.container {
  width: 100%;
  max-width: 720px;
  margin: 0 auto;
  padding: 0 1rem 3rem;
  flex: 1;
}

.card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  box-shadow: var(--shadow);
  padding: 1.25rem;
}

/* --- Create form --- */
.create-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  font-size: 0.9rem;
  font-weight: 500;
}

.field em {
  color: var(--muted);
  font-weight: 400;
}

input {
  width: 100%;
  padding: 0.65rem 0.75rem;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--bg);
  color: var(--text);
  font-size: 0.95rem;
}

input:focus {
  outline: 2px solid var(--primary);
  outline-offset: -1px;
  border-color: var(--primary);
}

.platform-fields {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding: 0.75rem;
  border: 1px dashed var(--border);
  border-radius: 8px;
}

button {
  cursor: pointer;
  font-family: inherit;
  font-size: 0.9rem;
  border-radius: 8px;
  border: 1px solid var(--border);
  background: var(--surface);
  color: var(--text);
  padding: 0.55rem 0.9rem;
  transition: background 0.15s, border-color 0.15s;
}

button:hover:not(:disabled) {
  border-color: var(--primary);
}

button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

button.primary {
  background: var(--primary);
  border-color: var(--primary);
  color: #fff;
  font-weight: 600;
  padding: 0.7rem;
}

button.primary:hover:not(:disabled) {
  background: var(--primary-hover);
}

button.link-button {
  align-self: flex-start;
  background: none;
  border: none;
  color: var(--primary);
  padding: 0;
  font-weight: 500;
}

button.danger {
  color: var(--danger);
}

button.danger:hover:not(:disabled) {
  border-color: var(--danger);
}

/* --- Links section --- */
.links-section {
  margin-top: 2rem;
}

.links-section h2 {
  font-size: 1.15rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.count {
  font-size: 0.8rem;
  background: var(--primary);
  color: #fff;
  border-radius: 999px;
  padding: 0.1rem 0.55rem;
}

.muted {
  color: var(--muted);
}

.empty {
  text-align: center;
  padding: 2rem;
  border: 1px dashed var(--border);
  border-radius: var(--radius);
}

.link-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-top: 1rem;
}

/* --- Link card --- */
.link-card {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.link-card.is-disabled {
  opacity: 0.65;
}

.link-card-main {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  align-items: flex-start;
}

.link-card-info {
  min-width: 0;
  flex: 1;
}

.short-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.short-url {
  font-weight: 600;
  font-size: 1.05rem;
  color: var(--primary);
  text-decoration: none;
  word-break: break-all;
}

.short-url:hover {
  text-decoration: underline;
}

.badge {
  font-size: 0.7rem;
  font-weight: 600;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.badge-active {
  background: color-mix(in srgb, var(--success) 15%, transparent);
  color: var(--success);
}

.badge-disabled {
  background: color-mix(in srgb, var(--muted) 20%, transparent);
  color: var(--muted);
}

.dest {
  color: var(--muted);
  font-size: 0.85rem;
  margin-top: 0.3rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.platform-list {
  list-style: none;
  padding: 0;
  margin: 0.6rem 0 0;
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}

.platform-list li {
  display: flex;
  gap: 0.5rem;
  align-items: center;
  font-size: 0.8rem;
}

.platform-tag {
  background: var(--bg);
  border: 1px solid var(--border);
  border-radius: 6px;
  padding: 0.05rem 0.4rem;
  font-weight: 600;
  flex-shrink: 0;
}

.platform-dest {
  color: var(--muted);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.stats {
  flex-shrink: 0;
}

.stat {
  display: flex;
  flex-direction: column;
  align-items: center;
  min-width: 64px;
  padding: 0.5rem 0.75rem;
  background: var(--bg);
  border-radius: 10px;
}

.stat-value {
  font-size: 1.5rem;
  font-weight: 700;
  line-height: 1;
}

.stat-label {
  font-size: 0.7rem;
  color: var(--muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.meta {
  display: flex;
  gap: 1.5rem;
  margin: 0;
  padding-top: 0.5rem;
  border-top: 1px solid var(--border);
  font-size: 0.8rem;
}

.meta dt {
  color: var(--muted);
}

.meta dd {
  margin: 0.1rem 0 0;
}

.actions {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.qr {
  align-self: center;
  padding: 0.85rem;
  background: #fff;
  border-radius: 10px;
}

.error {
  color: var(--danger);
  font-size: 0.85rem;
  margin: 0;
}

.app-footer {
  text-align: center;
  padding: 1.5rem;
  color: var(--muted);
  font-size: 0.8rem;
  border-top: 1px solid var(--border);
}

@media (max-width: 520px) {
  .link-card-main {
    flex-direction: column;
  }
  .stat {
    flex-direction: row;
    gap: 0.4rem;
    align-items: baseline;
  }
}
Read index.css
Write index.css
396 lines
:root {
  --bg: #f5f6f8;
  --surface: #ffffff;
  --border: #e4e7ec;
  --text: #1a1d21;
  --muted: #6b7280;
  --primary: #4f46e5;
  --primary-hover: #4338ca;
  --danger: #dc2626;
  --success: #16a34a;
  --radius: 12px;
  --shadow: 0 1px 3px rgba(16, 24, 40, 0.06), 0 1px 2px rgba(16, 24, 40, 0.04);

  font-family: system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif;
  color: var(--text);
  background: var(--bg);
}

@media (prefers-color-scheme: dark) {
  :root {
    --bg: #0f1115;
    --surface: #191c22;
    --border: #2a2f38;
    --text: #e7e9ee;
    --muted: #9099a8;
    --primary: #7c74ff;
    --primary-hover: #948dff;
    --shadow: 0 1px 3px rgba(0, 0, 0, 0.4);
  }
}

* {
  box-sizing: border-box;
}

body {
  margin: 0;
  min-height: 100vh;
  background: var(--bg);
}

.app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.app-header {
  text-align: center;
  padding: 2.5rem 1rem 1.5rem;
}

.app-header h1 {
  margin: 0;
  font-size: 2rem;
  letter-spacing: -0.02em;
}

.app-header p {
  margin: 0.4rem 0 0;
  color: var(--muted);
}

.container {
  width: 100%;
  max-width: 720px;
  margin: 0 auto;
  padding: 0 1rem 3rem;
  flex: 1;
}

.card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  box-shadow: var(--shadow);
  padding: 1.25rem;
}

/* --- Create form --- */
.create-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  font-size: 0.9rem;
  font-weight: 500;
}

.field em {
  color: var(--muted);
  font-weight: 400;
}

input {
  width: 100%;
  padding: 0.65rem 0.75rem;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--bg);
  color: var(--text);
  font-size: 0.95rem;
}

input:focus {
  outline: 2px solid var(--primary);
  outline-offset: -1px;
  border-color: var(--primary);
}

.platform-fields {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding: 0.75rem;
  border: 1px dashed var(--border);
  border-radius: 8px;
}

button {
  cursor: pointer;
  font-family: inherit;
  font-size: 0.9rem;
  border-radius: 8px;
  border: 1px solid var(--border);
  background: var(--surface);
  color: var(--text);
  padding: 0.55rem 0.9rem;
  transition: background 0.15s, border-color 0.15s;
}

button:hover:not(:disabled) {
  border-color: var(--primary);
}

button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

button.primary {
  background: var(--primary);
  border-color: var(--primary);
  color: #fff;
  font-weight: 600;
  padding: 0.7rem;
}

button.primary:hover:not(:disabled) {
  background: var(--primary-hover);
}

button.link-button {
  align-self: flex-start;
  background: none;
  border: none;
  color: var(--primary);
  padding: 0;
  font-weight: 500;
}

button.danger {
  color: var(--danger);
}

button.danger:hover:not(:disabled) {
  border-color: var(--danger);
}

/* --- Links section --- */
.links-section {
  margin-top: 2rem;
}

.links-section h2 {
  font-size: 1.15rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.count {
  font-size: 0.8rem;
  background: var(--primary);
  color: #fff;
  border-radius: 999px;
  padding: 0.1rem 0.55rem;
}

.muted {
  color: var(--muted);
}

.empty {
  text-align: center;
  padding: 2rem;
  border: 1px dashed var(--border);
  border-radius: var(--radius);
}

.link-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-top: 1rem;
}

/* --- Link card --- */
.link-card {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.link-card.is-disabled {
  opacity: 0.65;
}

.link-card-main {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  align-items: flex-start;
}

.link-card-info {
  min-width: 0;
  flex: 1;
}

.short-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.short-url {
  font-weight: 600;
  font-size: 1.05rem;
  color: var(--primary);
  text-decoration: none;
  word-break: break-all;
}

.short-url:hover {
  text-decoration: underline;
}

.badge {
  font-size: 0.7rem;
  font-weight: 600;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.badge-active {
  background: color-mix(in srgb, var(--success) 15%, transparent);
  color: var(--success);
}

.badge-disabled {
  background: color-mix(in srgb, var(--muted) 20%, transparent);
  color: var(--muted);
}

.dest {
  color: var(--muted);
  font-size: 0.85rem;
  margin-top: 0.3rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.platform-list {
  list-style: none;
  padding: 0;
  margin: 0.6rem 0 0;
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}

.platform-list li {
  display: flex;
  gap: 0.5rem;
  align-items: center;
  font-size: 0.8rem;
}

.platform-tag {
  background: var(--bg);
  border: 1px solid var(--border);
  border-radius: 6px;
  padding: 0.05rem 0.4rem;
  font-weight: 600;
  flex-shrink: 0;
}

.platform-dest {
  color: var(--muted);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.stats {
  flex-shrink: 0;
}

.stat {
  display: flex;
  flex-direction: column;
  align-items: center;
  min-width: 64px;
  padding: 0.5rem 0.75rem;
  background: var(--bg);
  border-radius: 10px;
}

.stat-value {
  font-size: 1.5rem;
  font-weight: 700;
  line-height: 1;
}

.stat-label {
  font-size: 0.7rem;
  color: var(--muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.meta {
  display: flex;
  gap: 1.5rem;
  margin: 0;
  padding-top: 0.5rem;
  border-top: 1px solid var(--border);
  font-size: 0.8rem;
}

.meta dt {
  color: var(--muted);
}

.meta dd {
  margin: 0.1rem 0 0;
}

.actions {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.qr {
  align-self: center;
  padding: 0.85rem;
  background: #fff;
  border-radius: 10px;
}

.error {
  color: var(--danger);
  font-size: 0.85rem;
  margin: 0;
}

.app-footer {
  text-align: center;
  padding: 1.5rem;
  color: var(--muted);
  font-size: 0.8rem;
  border-top: 1px solid var(--border);
}

@media (max-width: 520px) {
  .link-card-main {
    flex-direction: column;
  }
  .stat {
    flex-direction: row;
    gap: 0.4rem;
    align-items: baseline;
  }
}
Type-check + build frontend เพื่อยืนยันไม่มี error:

Bash Type-check and build frontend
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && npm run build 2>&1 | tail -20

OUT
> frontend@0.0.0 build
> tsc -b && vite build

vite v5.4.21 building for production...
transforming...
✓ 35 modules transformed.
rendering chunks...
computing gzip size...
dist/index.html                   0.47 kB │ gzip:  0.31 kB
dist/assets/index-fLzU9yV-.css    4.91 kB │ gzip:  1.55 kB
dist/assets/index-D1_rkwqy.js   165.57 kB │ gzip: 54.12 kB
✓ built in 616ms
Frontend build ผ่าน. เหลือ verify end-to-end จริง + README + .gitignore. ทดสอบ backend รันจริง — start แล้วยิง API:

Update Todos

Clean template files (WeatherForecast, UnitTest1)

Build Core domain (Link, Destination, enums)

Build short-code generation (interface, generators, resolver)

Build repository (ILinkRepository, InMemory)

Build platform detection + URL validation

Build LinkService + DTOs + exceptions

Build Features/endpoints (minimal API)

Wire Program.cs DI + appsettings + CORS

Write unit tests

Build succeeds + tests pass (63 pass)

Scaffold React frontend (Vite + TS) + build passes

E2E smoke test backend (run + curl full flow)

Write README + design notes + .gitignore
Bash Start backend in background
IN
cd e:/coding/url_link_shortener/url_link_shortener/backend && ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/UrlShortener.Api --urls "http://localhost:5000" > "$TEMP/urlsvc.log" 2>&1 &
echo "started pid $!"

OUT
started pid 2000
Bash Wait for backend readiness
IN
for i in $(seq 1 20); do
  if curl -s -o /dev/null http://localhost:5000/api/links 2>/dev/null; then echo "UP after ${i}s"; break; fi
  sleep 1
done
curl -s http://localhost:5000/api/links; echo " <- initial list"

OUT
UP after 1s
[] <- initial list
Backend รันแล้ว. ยิงทดสอบ full flow — create, custom alias, platform redirect, click count, disable, delete, error cases:

Bash Test create, alias, conflict, validation
IN
B=http://localhost:5000
echo "=== 1. Create (auto code) ==="
curl -s -X POST $B/api/links -H "Content-Type: application/json" \
  -d '{"url":"https://www.google.co.th"}' -w "\nHTTP %{http_code}\n"

echo "=== 2. Create with custom alias + platform destinations ==="
curl -s -X POST $B/api/links -H "Content-Type: application/json" \
  -d '{"url":"https://gulf.co.th","customAlias":"gulf","destinations":{"android":"https://download.gulf.co.th/android.apk","ios":"https://download.gulf.co.th/iphone.ipa"}}' \
  -w "\nHTTP %{http_code}\n"

echo "=== 3. Duplicate alias -> 409 ==="
curl -s -X POST $B/api/links -H "Content-Type: application/json" \
  -d '{"url":"https://x.com","customAlias":"gulf"}' -w "\nHTTP %{http_code}\n"

echo "=== 4. Invalid URL -> 400 ==="
curl -s -X POST $B/api/links -H "Content-Type: application/json" \
  -d '{"url":"not-a-url"}' -w "\nHTTP %{http_code}\n"

OUT
=== 1. Create (auto code) ===
{"shortCode":"HVMwnKE","shortUrl":"http://localhost:5000/HVMwnKE","destinations":{"default":"https://www.google.co.th/"},"status":"Active","clickCount":0,"createdAt":"2026-07-13T16:32:46.6778799+00:00","lastAccessedAt":null}
HTTP 201
=== 2. Create with custom alias + platform destinations ===
{"shortCode":"gulf","shortUrl":"http://localhost:5000/gulf","destinations":{"default":"https://gulf.co.th/","android":"https://download.gulf.co.th/android.apk","ios":"https://download.gulf.co.th/iphone.ipa"},"status":"Active","clickCount":0,"createdAt":"2026-07-13T16:32:46.7427518+00:00","lastAccessedAt":null}
HTTP 201
=== 3. Duplicate alias -> 409 ===
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.10","title":"Conflict","status":409,"detail":"Alias 'gulf' is already in use.","traceId":"00-11debfb81bae045f8ae6801374a01ad0-5c51bc433f0ff74b-00"}
HTTP 409
=== 4. Invalid URL -> 400 ===
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Bad Request","status":400,"detail":"A valid absolute http/https URL is required.","traceId":"00-93b64d54a68e7a3890c764575f66f949-ed10151a1b10b817-00"}
HTTP 400
Create/alias/409/400 ครบ. ต่อ redirect platform-aware + click count + disable/delete:

Bash Test redirect platform, clicks, disable, delete
IN
B=http://localhost:5000
echo "=== 5. Redirect as Android (302 + Location) ==="
curl -s -o /dev/null -D - -A "Mozilla/5.0 (Linux; Android 14)" $B/gulf | grep -iE "^HTTP|^location"
echo "=== 6. Redirect as iPhone ==="
curl -s -o /dev/null -D - -A "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X)" $B/gulf | grep -iE "^HTTP|^location"
echo "=== 7. Redirect as Desktop (default) ==="
curl -s -o /dev/null -D - -A "Mozilla/5.0 (Windows NT 10.0)" $B/gulf | grep -iE "^HTTP|^location"

echo "=== 8. Click count after 3 visits ==="
curl -s $B/api/links/gulf | grep -oE '"clickCount":[0-9]+'

echo "=== 9. Disable then redirect -> 404 ==="
curl -s -X PATCH $B/api/links/gulf -H "Content-Type: application/json" -d '{"status":"Disabled"}' -w " HTTP %{http_code}\n" -o /dev/null
curl -s -o /dev/null -w "redirect after disable: HTTP %{http_code}\n" $B/gulf

echo "=== 10. Click count unchanged after disabled visit ==="
curl -s $B/api/links/gulf | grep -oE '"clickCount":[0-9]+'

echo "=== 11. Delete -> 204, then GET -> 404 ==="
curl -s -X DELETE $B/api/links/gulf -o /dev/null -w "delete: HTTP %{http_code}\n"
curl -s -o /dev/null -w "get after delete: HTTP %{http_code}\n" $B/api/links/gulf

OUT
=== 5. Redirect as Android (302 + Location) ===
HTTP/1.1 302 Found
Location: https://download.gulf.co.th/android.apk
=== 6. Redirect as iPhone ===
HTTP/1.1 302 Found
Location: https://download.gulf.co.th/iphone.ipa
=== 7. Redirect as Desktop (default) ===
HTTP/1.1 302 Found
Location: https://gulf.co.th/
=== 8. Click count after 3 visits ===
"clickCount":3
=== 9. Disable then redirect -> 404 ===
 HTTP 200
redirect after disable: HTTP 404
=== 10. Click count unchanged after disabled visit ===
"clickCount":3
=== 11. Delete -> 204, then GET -> 404 ===
delete: HTTP 204
get after delete: HTTP 404
E2E ผ่านครบ — platform routing (Android→apk, iPhone→ipa, desktop→default), 302, click count=3, disable→404 (count คงที่), delete→204→404. หยุด backend แล้วเขียน README + .gitignore:

Bash Stop background backend
IN
taskkill //F //PID 2000 2>/dev/null; taskkill //F //IM UrlShortener.Api.exe 2>/dev/null; echo "stopped"; sleep 1

OUT
SUCCESS: The process "UrlShortener.Api.exe" with PID 28532 has been terminated.
stopped
Update Todos

Clean template files (WeatherForecast, UnitTest1)

Build Core domain (Link, Destination, enums)

Build short-code generation (interface, generators, resolver)

Build repository (ILinkRepository, InMemory)

Build platform detection + URL validation

Build LinkService + DTOs + exceptions

Build Features/endpoints (minimal API)

Wire Program.cs DI + appsettings + CORS

Write unit tests

Build succeeds + tests pass (63 pass)

Scaffold React frontend (Vite + TS) + build passes

E2E smoke test backend (all flows pass)

Write README + design notes + .gitignore
Root .gitignore:

Write .gitignore
17 lines
# --- .NET ---
[Bb]in/
[Oo]bj/
*.user
*.suo
.vs/

# --- Node / frontend ---
node_modules/
dist/
*.local

# --- Editor / OS ---
.idea/
.DS_Store
Thumbs.db
Main README — API contract, run/test, design notes:

Write README.md
273 lines
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

Bonus: responsive UI (light/dark), copy-to-clipboard, QR codes, helpful
validation errors, thread-safe in-memory store with a swappable data layer.

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
      DomainExceptions.cs        # typed errors mapped to HTTP status codes
      ShortCodes/                # pluggable code generation (Strategy + resolver)
        IShortCodeGenerator.cs
        RandomShortCodeGenerator.cs
        CustomAliasGenerator.cs
        ShortCodeGeneratorResolver.cs
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
      DomainExceptionHandler.cs  # maps DomainException -> RFC 7807 ProblemDetails
    Program.cs                   # DI wiring
  tests/UrlShortener.UnitTests/  # xUnit tests (63)

frontend/                        # Vite + React + TypeScript SPA
  src/
    api.ts                       # typed API client
    App.tsx
    components/CreateLinkForm.tsx, LinkCard.tsx
```

---

## Getting started

**Prerequisites:** .NET 9 SDK, Node.js 18+.

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

### Errors as typed exceptions
The service throws `DomainException` subtypes (`ValidationException`,
`AliasAlreadyExistsException`, …). A single `IExceptionHandler` maps them to
ProblemDetails responses, keeping HTTP concerns out of the domain.

---

## Testing

```bash
cd backend
dotnet test
```

**63 unit tests** cover the core logic the assignment calls out:
- short-code generation (length, alphabet, fallback, retry policy)
- URL validation (accepts http/https, rejects `javascript:`, `ftp:`, relative, empty)
- custom alias validation and case-insensitive uniqueness
- click counting (including 10k concurrent visits) and last-accessed tracking
- disable / re-enable / delete behavior and that disabled links don't count or redirect
- platform detection and platform-aware destination resolution

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
ai-logs placeholder (README ชี้ไปที่นี่):

Write README.md
9 lines
# AI session logs

This project was built with an agentic AI coding assistant (Claude Code).

Export the chat / session transcript here before submitting (e.g.
`session-01.md`) so reviewers can see how the tool was directed — from the
initial design discussion (project structure, API contract, short-code
generation strategy, 301-vs-302 reasoning) through implementation and testing.
Final check — full solution build + test หลังแก้ Program.cs:

Bash Final full test run
IN
cd e:/coding/url_link_shortener/url_link_shortener/backend && dotnet test -v q 2>&1 | tail -6

OUT
VSTest version 17.14.1 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    63, Skipped:     0, Total:    63, Duration: 31 ms - UrlShortener.UnitTests.dll (net9.0)
Update Todos

Clean template files (WeatherForecast, UnitTest1)

Build Core domain (Link, Destination, enums)

Build short-code generation (interface, generators, resolver)

Build repository (ILinkRepository, InMemory)

Build platform detection + URL validation

Build LinkService + DTOs + exceptions

Build Features/endpoints (minimal API)

Wire Program.cs DI + appsettings + CORS

Write unit tests

Build succeeds + tests pass (63 pass)

Scaffold React frontend (Vite + TS) + build passes

E2E smoke test backend (all flows pass)

Write README + design notes + .gitignore
เสร็จครบ. โปรเจกต์รันได้จริง ทดสอบผ่านทุก flow.

สรุปสิ่งที่สร้าง
โครงสร้าง (แบบ B — vertical slice, 1 API project):


backend/   .NET 9 API + xUnit tests
frontend/  Vite + React + TS
ai-logs/   ที่ใส่ session log
README.md  API contract + design notes
Backend — pattern ที่วางไว้เชื่อมกับที่คุยไว้:

Strategy + Resolver — IShortCodeGenerator.cs, random + custom-alias, เพิ่มวิธีใหม่ = class ใหม่ + 1 บรรทัด DI (OCP)
Collision policy อยู่บน interface (AllowRetryOnCollision) — service ไม่ผูก concrete type
Repository — ILinkRepository.cs, in-memory ConcurrentDictionary (case-insensitive, atomic TryAdd)
Platform detector แยก seam, unknown→Default
Interlocked click count, LinkStatus enum, ProblemDetails error mapping
ยืนยันด้วยการรันจริง (curl):

Flow	ผล
Create auto/custom	201
Duplicate alias	409
Invalid URL	400
Redirect Android→apk / iPhone→ipa / desktop→default	302 ถูกทุกตัว
Click count	นับ 3 ครั้งถูก
Disable → redirect	404, count คงที่
Delete → get	204 → 404
Tests: 63 ผ่านหมด — ครอบคลุม short-code gen, URL validation, click counting (รวม 10k concurrent), disable/delete, platform routing, collision.

Frontend: create form (+ platform destinations), link cards (stats, copy, QR, disable/enable, delete), responsive + dark mode. npm run build ผ่าน.

ยังไม่ได้ทำ (ตั้งใจเว้น)
ยังไม่ commit — repo ยังไม่มี commit เลย บอกได้ถ้าจะให้ commit
ai-logs ยังเป็น placeholder — ต้อง export session log ใส่เองตอนส่ง
ยังไม่ได้เปิด frontend ดูด้วยตาจริง (build ผ่าน + API ยิงผ่าน) — อยากให้ start คู่กันเช็ค UI ไหม
อยากให้ commit หรือรัน frontend ให้ดู UI ต่อไหม?

ออกแบบส่วน platform detection และ platform-specific destination ให้ data model รองรับ default/iOS/Android ได้ และให้แยก platform detection ออกมาเป็น service ที่ test ได้ (ไม่ผูกกับ HttpContext ตรง ๆ) อธิบายว่าทำไมต้องแยก และ User-Agent parsing มีข้อจำกัดอะไรบ้าง
ส่วนนี้สร้างไว้แล้ว. อธิบายว่าออกแบบยังไง + ทำไมแยก + ข้อจำกัด UA — อ้างโค้ดจริง.

Data model รองรับ default/iOS/Android
Platform.cs — enum Default | iOS | Android.

Link.cs เก็บเป็น IReadOnlyDictionary<Platform, string> ไม่ใช่ field OriginalUrl เดี่ยว:

constructor บังคับมี Platform.Default เสมอ (throw ถ้าไม่มี) → ทุกลิงก์มี fallback การันตี
ResolveDestination(platform) — คืน override ถ้ามี, ไม่มีก็ตก Default:

public string ResolveDestination(Platform platform) =>
    Destinations.TryGetValue(platform, out var url) ? url : Destinations[Platform.Default];
ทำไม dict ไม่ใช่ field แยก (IosUrl, AndroidUrl): เพิ่ม platform ใหม่ (เช่น Windows, Mac) = เพิ่ม enum value อย่างเดียว ไม่ต้องแก้ schema/entity. Resolve logic เดิมใช้ได้เลย.

Platform detection แยกเป็น service — ไม่ผูก HttpContext
IPlatformDetector.cs — รับ string? ล้วน:


public interface IPlatformDetector
{
    Platform Detect(string? userAgent);   // ไม่รู้จัก HttpContext เลย
}
UserAgentPlatformDetector.cs — pure function, input string → output enum.

HttpContext อยู่แค่ที่ endpoint (RedirectEndpoints.cs) — ดึง header ตรงนั้นแล้วส่งเข้า detector:


var platform = platformDetector.Detect(http.Request.Headers.UserAgent);
Test (UserAgentPlatformDetectorTests.cs) — ส่ง string ตรงๆ ไม่ต้อง mock HttpContext.

ทำไมต้องแยก
Testable — mock HttpContext/HttpRequest/IHeaderDictionary เจ็บปวด. รับ string? → test เป็น [InlineData] ธรรมดา. ตอนนี้ test 12 เคส (android/ios variants, bot, curl, null) เขียนสั้นมาก
Single responsibility — UA parsing (string logic) แยกจาก HTTP plumbing แยกจาก business (เลือก URL). แก้ทีละชั้นได้
Swappable — วันนี้ substring match. อยากเปลี่ยนเป็น library device-detection (UAParser, ua-parser) → เปลี่ยน implementation ของ IPlatformDetector ตัวเดียว, endpoint/service/test เดิมไม่แตะ (Dependency Inversion)
Reusable — ถ้าอนาคต detect จากที่อื่น (Client-Hints header, query param ?platform=ios) → detector รับ string ได้เหมือนเดิม
ข้อจำกัด User-Agent parsing (สำคัญ — เตรียมตอบสัมภาษณ์)
ปลอมได้ — UA เป็น string ที่ client ส่งมา แก้ได้อิสระ. ห้ามใช้ตัดสินใจ security-sensitive
ไม่เสถียร / เปลี่ยนตลอด — vendor เปลี่ยน format เรื่อยๆ. substring match วันนี้ผ่าน พรุ่งนี้อาจพลาด
iPadOS โกหก — Safari บน iPad ตั้งแต่ iOS 13 รายงานตัวเป็น Macintosh (desktop UA) → detect เป็น Default ไม่ใช่ iOS. code เขียน comment เตือนไว้ตรงนี้แล้ว
Substring กว้างเกิน/แคบเกิน — เช่นบาง Android WebView / in-app browser (Facebook, Line) มี UA แปลก อาจ match พลาด
Bot/ไม่มี UA — crawler, curl, health check ไม่มี/มี UA แปลก → design ให้ตก Default เสมอ (ไม่ throw, ลิงก์ resolve ได้ตลอด)
กำลังจะ deprecated — เบราว์เซอร์ freeze UA string แล้วดันไปใช้ User-Agent Client Hints (Sec-CH-UA-Platform) แทน → ระยะยาวควรอ่าน Client-Hints ก่อน แล้ว fallback UA
ทางแก้ที่ design รองรับอยู่แล้ว: เพราะ detector รับ string? และซ่อนหลัง interface — ยกระดับเป็น Client-Hints หรือ library ได้โดยไม่กระทบส่วนอื่น. นี่คือเหตุผลหลักที่แยก seam ไว้.

ของนี้ implement + test ผ่านแล้ว (redirect Android→apk, iPhone→ipa, desktop→default ยืนยันด้วย curl รอบก่อน). อยากให้ผมยกระดับ detector เป็นอ่าน Client-Hints ก่อน fallback UA เลยไหม หรือพอแค่นี้?

สร้าง repository abstraction ที่ตอนนี้ใช้ in-memory (thread-safe ด้วย ConcurrentDictionary) แต่สลับไปใช้ EF Core / DB จริงได้โดยไม่แตะ business logic ระวังเรื่อง race condition ตอน increment click count และตอนสร้าง code ซ้ำ อธิบายว่าจุดไหนบ้างที่จะพังถ้ามี concurrent request
Repository abstraction สร้างไว้แล้ว — แต่คำถามเรื่อง concurrency ทำให้ผมเจอ bug จริงในโค้ดปัจจุบัน ตอน review. อธิบาย + แก้ให้.

ขอดูไฟล์ก่อน:

Read InMemoryLinkRepository.cs
เจอ bug ที่บรรทัด 28. แก้:

Edit InMemoryLinkRepository.cs
Added 3 lines
เพิ่ม test ยืนยัน fix (delete แล้ว UpdateAsync ต้องไม่ปลุกลิงก์คืนชีพ):

Write InMemoryLinkRepositoryTests.cs
54 lines
using UrlShortener.Api.Core;
using UrlShortener.Api.Core.Persistence;

namespace UrlShortener.UnitTests.Persistence;

public class InMemoryLinkRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 13, 10, 0, 0, TimeSpan.Zero);

    private static Link BuildLink(string code = "abc123") =>
        new(code, new Dictionary<Platform, string> { [Platform.Default] = "https://a.com" }, Now);

    private readonly InMemoryLinkRepository _repo = new();

    [Fact]
    public async Task TryAdd_returns_false_on_duplicate_code()
    {
        Assert.True(await _repo.TryAddAsync(BuildLink("dup")));
        Assert.False(await _repo.TryAddAsync(BuildLink("dup")));
    }

    [Fact]
    public async Task TryAdd_is_case_insensitive()
    {
        Assert.True(await _repo.TryAddAsync(BuildLink("MyLink")));
        Assert.False(await _repo.TryAddAsync(BuildLink("mylink")));
    }

    [Fact]
    public async Task Update_after_delete_does_not_resurrect_the_link()
    {
        // Models the redirect race: a request reads the link and writes back its
        // click count (UpdateAsync) after a concurrent DeleteAsync removed it.
        var link = BuildLink();
        await _repo.TryAddAsync(link);
        await _repo.RemoveAsync(link.ShortCode);

        await _repo.UpdateAsync(link); // stale write-back from the in-flight visit

        Assert.Null(await _repo.GetAsync(link.ShortCode));
    }

    [Fact]
    public async Task Concurrent_TryAdd_of_same_code_admits_exactly_one()
    {
        const int racers = 50;
        var links = Enumerable.Range(0, racers).Select(_ => BuildLink("race")).ToArray();

        var results = await Task.WhenAll(links.Select(l => _repo.TryAddAsync(l)));

        Assert.Equal(1, results.Count(won => won));
    }
}
Bash Run tests after resurrection fix
IN
cd e:/coding/url_link_shortener/url_link_shortener/backend && dotnet test -v q 2>&1 | tail -6

OUT
VSTest version 17.14.1 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    67, Skipped:     0, Total:    67, Duration: 30 ms - UrlShortener.UnitTests.dll (net9.0)
67 tests ผ่าน (เพิ่ม 4). ทีนี้อธิบายครบ.

Repository abstraction
ILinkRepository.cs — async ทุก method (I/O-bound DB เข้า contract เดิมได้). Business logic (LinkService.cs) พึ่ง interface อย่างเดียว. สลับเป็น EF Core = เขียน EfLinkRepository + เปลี่ยน 1 บรรทัด DI ใน Program.cs, service/endpoint/test ไม่แตะ.

จุดที่จะพังถ้ามี concurrent request
1. สร้าง code ซ้ำ (duplicate short code)
ตอนนี้ (in-memory) — ปลอดภัย. uniqueness บังคับที่จุดเดียว atomic:


_links.TryAdd(link.ShortCode, link);   // atomic compare-and-set
2 request สร้าง alias เดียวกันพร้อมกัน → คนแรก true, คนที่สอง false → 409. ไม่มี check-then-act.

จุดพัง (ถ้าเขียน EF Core ผิด): naive จะทำ


if (await db.Links.AnyAsync(x => x.Code == code)) return false;  // ← check
db.Links.Add(link); await db.SaveChangesAsync();                  // ← act
2 request ผ่าน AnyAsync พร้อมกันก่อนใคร save → insert ซ้ำทั้งคู่ (TOCTOU race).
แก้: unique constraint/index ที่ column Code แล้ว catch DbUpdateException → แปลงเป็น 409/retry. ให้ DB เป็นคน enforce ไม่ใช่โค้ด — เหมือน TryAdd เป็นคน enforce ในตอนนี้.

2. Increment click count
ตอนนี้ (in-memory) — ปลอดภัย. Link.cs ใช้ Interlocked.Increment (atomic), ยืนยันด้วย test 10k parallel visit.

จุดพัง (ถ้าเขียน EF Core ผิด): read-modify-write


var link = await db.Links.FindAsync(code);  // clicks = 100
link.ClickCount++;                           // 101 ในหน่วยความจำ
await db.SaveChangesAsync();
100 request พร้อมกันอ่าน 100 เท่ากันหมด → เขียนกลับ 101 ทับกัน → lost update, ได้ 101 แทน 200.
แก้: atomic ที่ DB ไม่ใช่ใน memory:


await db.Links.Where(x => x.Code == code)
    .ExecuteUpdateAsync(s => s.SetProperty(x => x.ClickCount, x => x.ClickCount + 1));
// SQL: UPDATE Links SET ClickCount = ClickCount + 1 WHERE Code = @code
DB จัดการ row lock ให้ = ไม่หาย.

3. Resurrection race (bug ที่เพิ่งเจอ+แก้)
จุดพังจริงในโค้ดเดิม: redirect flow ทำ GetAsync → RegisterVisit → UpdateAsync. เดิม UpdateAsync ใช้ _links[code] = link (add-or-update). ถ้า DELETE แทรกระหว่าง Get กับ Update → delete ลบไปแล้ว, UpdateAsync ดัน insert กลับ → ลิงก์ที่ลบแล้วฟื้นคืนชีพ.
แก้แล้ว: เปลี่ยนเป็น TryUpdate(code, link, link) — เขียนเฉพาะเมื่อ key ยังอยู่ ถ้าถูกลบแล้วคืน false เฉยๆ (test Update_after_delete_does_not_resurrect_the_link คุม).

เทียบ EF Core: ปัญหาเดียวกันจะโผล่เป็น — write-back ของ visit ที่ค้างอยู่ไป update row ที่ถูกลบ. ถ้าใช้ ExecuteUpdateAsync ... WHERE Code=@code แบบข้อ 2 → row ไม่มีแล้ว, affected rows = 0, ไม่ฟื้น. ปลอดภัยโดยธรรมชาติ.

สรุปหลักการที่ยึด
ปัญหา	หลักการ	in-memory	EF Core
Code ซ้ำ	enforce ที่ store, atomic	TryAdd	unique index + catch
Click count	atomic increment, ไม่ read-modify-write	Interlocked	ExecuteUpdateAsync SET x=x+1
Resurrection	เขียนเฉพาะถ้ายังมีอยู่	TryUpdate	WHERE Code=@code (0 rows)
แก่นเดียวกันทุกช่อง: อย่าตัดสินใจใน memory แล้วค่อยเขียน — ให้ storage เป็นคน enforce แบบ atomic. ตอนนี้ 67 tests ผ่าน, resurrection bug แก้แล้ว.

อยากให้ผมเขียน EfLinkRepository (SQLite in-memory) จริงเลยไหม เพื่อพิสูจน์ว่า swap ได้โดยไม่แตะ service — หรือพอแค่ in-memory ตามที่โจทย์บอกว่าเพียงพอ?

เขียน service layer สำหรับ business logic (create/disable/delete/resolve) พร้อม URL validation ที่ครอบคลุม edge case: URL ไม่มี scheme, javascript: scheme, URL ชี้กลับมาที่ short domain ตัวเอง (loop), alias ที่ชนกับ reserved route เช่น /api และ alias ที่มีอักขระพิเศษ ให้ใช้ Result pattern แทนการ throw exception สำหรับ business error
Refactor ใหญ่ — เปลี่ยน business error จาก throw เป็น Result pattern + เพิ่ม edge case (loop, reserved route, scheme). ตั้ง todo:

Update Todos

Add Result/Error type (Core/Result.cs)

Add AliasRules (format regex + reserved routes)

Simplify CustomAliasGenerator (no throw, validation moves to service)

Rewrite LinkService with Result + self-loop + reserved checks

Add Error->IResult mapper, update endpoints

Remove DomainExceptions + handler, update Program.cs

Update + add tests (edge cases), all pass

E2E smoke test new validation
Write Result.cs
70 lines
namespace UrlShortener.Api.Core;

/// <summary>Category of a business error, mapped to an HTTP status at the edge.</summary>
public enum ErrorType
{
    Validation,
    Conflict,
    NotFound
}

/// <summary>An expected, user-facing failure — carried by <see cref="Result"/> instead of thrown.</summary>
public sealed record Error(ErrorType Type, string Message)
{
    public static Error Validation(string message) => new(ErrorType.Validation, message);
    public static Error Conflict(string message) => new(ErrorType.Conflict, message);
    public static Error NotFound(string message) => new(ErrorType.NotFound, message);
}

/// <summary>Outcome of an operation with no return value.</summary>
public readonly struct Result
{
    private readonly Error? _error;

    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public Error Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Outcome of an operation that yields a <typeparamref name="T"/> on success.</summary>
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    public Error Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, default, error);

    // Let handlers `return value;` or `return Error.Validation(...);` directly.
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
Write AliasRules.cs
28 lines
using System.Text.RegularExpressions;

namespace UrlShortener.Api.Core.ShortCodes;

/// <summary>
/// Rules a short code must satisfy regardless of how it was produced. Applied by
/// the service to every candidate, so both custom aliases and (defensively)
/// auto-generated codes are checked in one place.
/// </summary>
public static partial class AliasRules
{
    // Names that would shadow real routes served at the root, so a short code
    // must never take them. Matched case-insensitively (routing is too).
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "api", "openapi", "swagger", "health", "healthz",
        "assets", "static", "public", "favicon.ico", "robots.txt", "sitemap.xml"
    };

    /// <summary>3–30 chars: letters, digits, hyphen, underscore.</summary>
    [GeneratedRegex("^[A-Za-z0-9_-]{3,30}$")]
    public static partial Regex WellFormed();

    public static bool IsWellFormed(string code) => WellFormed().IsMatch(code);

    public static bool IsReserved(string code) => Reserved.Contains(code);
}
Simplify CustomAliasGenerator (validation ย้ายไป service):

Write CustomAliasGenerator.cs
20 lines
namespace UrlShortener.Api.Core.ShortCodes;

/// <summary>
/// Strategy for user-supplied aliases. It only produces the candidate (the
/// trimmed alias); shape and reserved-name rules are enforced centrally by the
/// service via <see cref="AliasRules"/>. Because the code is deterministic, a
/// collision is a real conflict — <see cref="AllowRetryOnCollision"/> is false.
/// </summary>
public sealed class CustomAliasGenerator : IShortCodeGenerator
{
    public string Name => "custom-alias";

    public bool AllowRetryOnCollision => false;

    public bool CanHandle(GenerationRequest request) =>
        !string.IsNullOrWhiteSpace(request.CustomAlias);

    public string Generate(GenerationRequest request) => request.CustomAlias!.Trim();
}
Rewrite LinkService + ลบ exception plumbing:

Bash Remove domain exception files
IN
cd e:/coding/url_link_shortener/url_link_shortener/backend && rm -f src/UrlShortener.Api/Core/DomainExceptions.cs src/UrlShortener.Api/Common/DomainExceptionHandler.cs && echo "removed exception files"

OUT
removed exception files
Write LinkService.cs
172 lines
using Microsoft.Extensions.Options;
using UrlShortener.Api.Core;
using UrlShortener.Api.Core.Persistence;
using UrlShortener.Api.Core.ShortCodes;
using UrlShortener.Api.Core.Validation;

namespace UrlShortener.Api.Features.Links;

/// <summary>
/// Application logic for links: creation (pluggable code generation, validation,
/// collision handling), stats, status changes, deletion, and redirect
/// resolution. Business failures are returned as <see cref="Result"/>/<see
/// cref="Error"/> rather than thrown, so callers handle them explicitly and the
/// happy path stays free of exception control flow.
/// </summary>
public sealed class LinkService
{
    private const int MaxGenerationAttempts = 5;

    private readonly ILinkRepository _repository;
    private readonly ShortCodeGeneratorResolver _resolver;
    private readonly IUrlValidator _urlValidator;
    private readonly TimeProvider _clock;
    private readonly string _shortDomainAuthority;

    public LinkService(
        ILinkRepository repository,
        ShortCodeGeneratorResolver resolver,
        IUrlValidator urlValidator,
        IOptions<ShortUrlOptions> shortUrlOptions,
        TimeProvider clock)
    {
        _repository = repository;
        _resolver = resolver;
        _urlValidator = urlValidator;
        _clock = clock;

        // Authority (host:port) of our own short domain, used to reject links
        // that would point back at us and cause a redirect loop.
        _shortDomainAuthority = Uri.TryCreate(shortUrlOptions.Value.BaseUrl, UriKind.Absolute, out var baseUri)
            ? baseUri.Authority
            : string.Empty;
    }

    public async Task<Result<Link>> CreateAsync(CreateLinkRequest request, CancellationToken ct = default)
    {
        var destinationsResult = BuildDestinations(request);
        if (destinationsResult.IsFailure)
            return destinationsResult.Error;

        var destinations = destinationsResult.Value;
        var genRequest = new GenerationRequest(request.CustomAlias);
        var generator = _resolver.Resolve(genRequest);
        var isDeterministic = !generator.AllowRetryOnCollision; // custom alias

        for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            var code = generator.Generate(genRequest);

            if (!AliasRules.IsWellFormed(code))
            {
                if (isDeterministic)
                    return Error.Validation("Alias must be 3–30 characters: letters, digits, '-' or '_'.");
                continue; // random code was somehow malformed — try again (defensive)
            }

            if (AliasRules.IsReserved(code))
            {
                if (isDeterministic)
                    return Error.Validation($"'{code}' is a reserved name and cannot be used as an alias.");
                continue;
            }

            var link = new Link(code, destinations, _clock.GetUtcNow());

            if (await _repository.TryAddAsync(link, ct))
                return link;

            // Deterministic strategies can't escape a collision by retrying.
            if (isDeterministic)
                return Error.Conflict($"Alias '{code}' is already in use.");
        }

        return Error.Validation("Could not generate a unique short code. Please try again.");
    }

    public Task<Link?> GetAsync(string shortCode, CancellationToken ct = default) =>
        _repository.GetAsync(shortCode, ct);

    public Task<IReadOnlyCollection<Link>> GetAllAsync(CancellationToken ct = default) =>
        _repository.GetAllAsync(ct);

    public async Task<Result<Link>> SetStatusAsync(string shortCode, LinkStatus status, CancellationToken ct = default)
    {
        var link = await _repository.GetAsync(shortCode, ct);
        if (link is null)
            return Error.NotFound($"Link '{shortCode}' was not found.");

        link.SetStatus(status);
        await _repository.UpdateAsync(link, ct);
        return link;
    }

    public async Task<Result> DeleteAsync(string shortCode, CancellationToken ct = default)
    {
        var removed = await _repository.RemoveAsync(shortCode, ct);
        return removed ? Result.Success() : Error.NotFound($"Link '{shortCode}' was not found.");
    }

    /// <summary>
    /// Resolves the destination for a visit and records the click. Returns null
    /// when the link is missing or disabled, so the caller responds 404 rather
    /// than redirecting. (Binary outcome with no message — a plain nullable is
    /// clearer here than a Result.)
    /// </summary>
    public async Task<string?> ResolveForRedirectAsync(string shortCode, Platform platform, CancellationToken ct = default)
    {
        var link = await _repository.GetAsync(shortCode, ct);
        if (link is null || link.Status != LinkStatus.Active)
            return null;

        link.RegisterVisit(_clock.GetUtcNow());
        await _repository.UpdateAsync(link, ct);
        return link.ResolveDestination(platform);
    }

    private Result<Dictionary<Platform, string>> BuildDestinations(CreateLinkRequest request)
    {
        var defaultResult = ValidateDestination(request.Url);
        if (defaultResult.IsFailure)
            return defaultResult.Error;

        var destinations = new Dictionary<Platform, string> { [Platform.Default] = defaultResult.Value };

        if (request.Destinations is null)
            return destinations;

        foreach (var (key, value) in request.Destinations)
        {
            if (!Enum.TryParse<Platform>(key, ignoreCase: true, out var platform))
                return Error.Validation($"Unknown platform '{key}'. Valid values: default, ios, android.");

            if (platform == Platform.Default)
                continue; // the default destination always comes from Url

            var result = ValidateDestination(value);
            if (result.IsFailure)
                return Error.Validation($"Destination for '{key}': {result.Error.Message}");

            destinations[platform] = result.Value;
        }

        return destinations;
    }

    private Result<string> ValidateDestination(string? url)
    {
        if (!_urlValidator.TryNormalize(url, out var normalized))
            return Error.Validation("A valid absolute http/https URL is required.");

        // Reject links that resolve back to our own domain to avoid redirect loops.
        var uri = new Uri(normalized);
        if (!string.IsNullOrEmpty(_shortDomainAuthority) &&
            string.Equals(uri.Authority, _shortDomainAuthority, StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation("Destination cannot point back to the shortener's own domain.");
        }

        return normalized;
    }
}
Error→IResult mapper + update endpoints:

Write ResultExtensions.cs
18 lines
using UrlShortener.Api.Core;

namespace UrlShortener.Api.Common;

/// <summary>Maps a domain <see cref="Error"/> to an HTTP result (RFC 7807 ProblemDetails).</summary>
public static class ResultExtensions
{
    public static IResult ToProblem(this Error error) => error.Type switch
    {
        ErrorType.Validation => Results.Problem(
            detail: error.Message, statusCode: StatusCodes.Status400BadRequest, title: "Bad Request"),
        ErrorType.Conflict => Results.Problem(
            detail: error.Message, statusCode: StatusCodes.Status409Conflict, title: "Conflict"),
        ErrorType.NotFound => Results.NotFound(),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
}
Write LinksEndpoints.cs
81 lines
using Microsoft.Extensions.Options;
using UrlShortener.Api.Common;

namespace UrlShortener.Api.Features.Links;

/// <summary>Management endpoints for links, grouped under /api/links.</summary>
public static class LinksEndpoints
{
    public static IEndpointRouteBuilder MapLinksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/links").WithTags("Links");

        // Create
        group.MapPost("/", async (
            CreateLinkRequest request,
            LinkService service,
            IOptions<ShortUrlOptions> options,
            CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            if (result.IsFailure)
                return result.Error.ToProblem();

            var response = result.Value.ToResponse(options.Value.BaseUrl);
            return Results.Created($"/api/links/{result.Value.ShortCode}", response);
        });

        // List
        group.MapGet("/", async (
            LinkService service,
            IOptions<ShortUrlOptions> options,
            CancellationToken ct) =>
        {
            var links = await service.GetAllAsync(ct);
            var baseUrl = options.Value.BaseUrl;
            return Results.Ok(links
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => l.ToResponse(baseUrl)));
        });

        // Stats for one link
        group.MapGet("/{shortCode}", async (
            string shortCode,
            LinkService service,
            IOptions<ShortUrlOptions> options,
            CancellationToken ct) =>
        {
            var link = await service.GetAsync(shortCode, ct);
            return link is null
                ? Results.NotFound()
                : Results.Ok(link.ToResponse(options.Value.BaseUrl));
        });

        // Enable / disable
        group.MapPatch("/{shortCode}", async (
            string shortCode,
            UpdateStatusRequest request,
            LinkService service,
            IOptions<ShortUrlOptions> options,
            CancellationToken ct) =>
        {
            var result = await service.SetStatusAsync(shortCode, request.Status, ct);
            return result.IsFailure
                ? result.Error.ToProblem()
                : Results.Ok(result.Value.ToResponse(options.Value.BaseUrl));
        });

        // Delete
        group.MapDelete("/{shortCode}", async (
            string shortCode,
            LinkService service,
            CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(shortCode, ct);
            return result.IsFailure ? result.Error.ToProblem() : Results.NoContent();
        });

        return app;
    }
}
Update Program.cs — ลบ DomainExceptionHandler registration:

Edit Program.cs
Removed 1 line
Edit Program.cs
Added 1 line
Build API เช็ค error:

Bash Build API after refactor
IN
cd e:/coding/url_link_shortener/url_link_shortener/backend && dotnet build src/UrlShortener.Api/UrlShortener.Api.csproj -v q 2>&1 | tail -20

OUT
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.22
Tests — update สำหรับ Result + เพิ่ม edge cases. เริ่ม CustomAliasGenerator (ไม่ throw แล้ว):

Write CustomAliasGeneratorTests.cs
29 lines
using UrlShortener.Api.Core.ShortCodes;

namespace UrlShortener.UnitTests.ShortCodes;

public class CustomAliasGeneratorTests
{
    private readonly CustomAliasGenerator _generator = new();

    [Fact]
    public void Generate_returns_the_trimmed_alias()
    {
        Assert.Equal("mylink", _generator.Generate(new GenerationRequest("  mylink  ")));
    }

    [Fact]
    public void CanHandle_only_when_alias_present()
    {
        Assert.True(_generator.CanHandle(new GenerationRequest("mylink")));
        Assert.False(_generator.CanHandle(new GenerationRequest(null)));
        Assert.False(_generator.CanHandle(new GenerationRequest("   ")));
    }

    [Fact]
    public void AllowRetryOnCollision_is_false()
    {
        Assert.False(_generator.AllowRetryOnCollision);
    }
}
AliasRules tests (format + reserved):

Write AliasRulesTests.cs
46 lines
using UrlShortener.Api.Core.ShortCodes;

namespace UrlShortener.UnitTests.ShortCodes;

public class AliasRulesTests
{
    [Theory]
    [InlineData("mylink")]
    [InlineData("My-Link_1")]
    [InlineData("abc")]
    [InlineData("abcdefghijklmnopqrstuvwxyz1234")] // 30 chars
    public void IsWellFormed_accepts_valid_aliases(string alias)
    {
        Assert.True(AliasRules.IsWellFormed(alias));
    }

    [Theory]
    [InlineData("ab")]                 // too short
    [InlineData("has space")]
    [InlineData("bad!char")]
    [InlineData("with.dot")]
    [InlineData("way-too-long-alias-that-exceeds-thirty-chars")]
    public void IsWellFormed_rejects_invalid_aliases(string alias)
    {
        Assert.False(AliasRules.IsWellFormed(alias));
    }

    [Theory]
    [InlineData("api")]
    [InlineData("API")]      // case-insensitive
    [InlineData("openapi")]
    [InlineData("health")]
    public void IsReserved_flags_route_shadowing_names(string alias)
    {
        Assert.True(AliasRules.IsReserved(alias));
    }

    [Theory]
    [InlineData("mylink")]
    [InlineData("gulf")]
    public void IsReserved_allows_normal_aliases(string alias)
    {
        Assert.False(AliasRules.IsReserved(alias));
    }
}
Rewrite LinkServiceTests — Result assertions + edge cases (loop, reserved, scheme):

Write LinkServiceTests.cs
259 lines
using Microsoft.Extensions.Options;
using UrlShortener.Api.Core;
using UrlShortener.Api.Core.Persistence;
using UrlShortener.Api.Core.ShortCodes;
using UrlShortener.Api.Core.Validation;
using UrlShortener.Api.Features.Links;

namespace UrlShortener.UnitTests.Services;

public class LinkServiceTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 13, 10, 0, 0, TimeSpan.Zero);
    private const string ShortDomain = "http://localhost:5000";

    private readonly FixedTimeProvider _clock = new(Start);
    private readonly LinkService _service;

    public LinkServiceTests()
    {
        var resolver = new ShortCodeGeneratorResolver(
            [new CustomAliasGenerator(), new RandomShortCodeGenerator()]);
        var options = Options.Create(new ShortUrlOptions { BaseUrl = ShortDomain });
        _service = new LinkService(new InMemoryLinkRepository(), resolver, new UrlValidator(), options, _clock);
    }

    // Convenience: create and unwrap a link expected to succeed.
    private async Task<Link> Create(CreateLinkRequest request)
    {
        var result = await _service.CreateAsync(request);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        return result.Value;
    }

    // --- Create (happy path) ---

    [Fact]
    public async Task Create_returns_active_link_with_zero_clicks()
    {
        var link = await Create(new CreateLinkRequest("https://example.com"));

        Assert.Equal(LinkStatus.Active, link.Status);
        Assert.Equal(0, link.ClickCount);
        Assert.Equal(Start, link.CreatedAt);
        Assert.Equal(7, link.ShortCode.Length);
    }

    [Fact]
    public async Task Create_uses_custom_alias_when_supplied()
    {
        var link = await Create(new CreateLinkRequest("https://example.com", "mylink"));
        Assert.Equal("mylink", link.ShortCode);
    }

    [Fact]
    public async Task Create_stores_platform_destinations()
    {
        var link = await Create(new CreateLinkRequest(
            "https://example.com",
            Destinations: new Dictionary<string, string>
            {
                ["android"] = "https://example.com/android.apk",
                ["ios"] = "https://example.com/app.ipa"
            }));

        Assert.Equal("https://example.com/android.apk", link.ResolveDestination(Platform.Android));
        Assert.Equal("https://example.com/app.ipa", link.ResolveDestination(Platform.iOS));
    }

    // --- Create (validation edge cases, returned as Result failures) ---

    [Fact]
    public async Task Create_rejects_url_without_scheme()
    {
        var result = await _service.CreateAsync(new CreateLinkRequest("example.com"));
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Create_rejects_javascript_scheme()
    {
        var result = await _service.CreateAsync(new CreateLinkRequest("javascript:alert(1)"));
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Create_rejects_link_pointing_back_to_short_domain()
    {
        var result = await _service.CreateAsync(new CreateLinkRequest($"{ShortDomain}/somewhere"));
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Contains("own domain", result.Error.Message);
    }

    [Fact]
    public async Task Create_rejects_platform_destination_pointing_back_to_short_domain()
    {
        var result = await _service.CreateAsync(new CreateLinkRequest(
            "https://example.com",
            Destinations: new Dictionary<string, string> { ["ios"] = $"{ShortDomain}/x" }));
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Create_rejects_reserved_alias()
    {
        var result = await _service.CreateAsync(new CreateLinkRequest("https://example.com", "api"));
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Contains("reserved", result.Error.Message);
    }

    [Fact]
    public async Task Create_rejects_alias_with_special_characters()
    {
        var result = await _service.CreateAsync(new CreateLinkRequest("https://example.com", "bad!alias"));
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Create_rejects_unknown_platform_key()
    {
        var result = await _service.CreateAsync(new CreateLinkRequest(
            "https://example.com",
            Destinations: new Dictionary<string, string> { ["windows"] = "https://example.com/win" }));
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Create_with_duplicate_alias_returns_conflict()
    {
        await Create(new CreateLinkRequest("https://example.com", "dup"));

        var result = await _service.CreateAsync(new CreateLinkRequest("https://other.com", "dup"));
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Create_duplicate_alias_is_case_insensitive()
    {
        await Create(new CreateLinkRequest("https://example.com", "MyLink"));

        var result = await _service.CreateAsync(new CreateLinkRequest("https://other.com", "mylink"));
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    // --- Redirect / click counting ---

    [Fact]
    public async Task Redirect_increments_click_and_records_access_time()
    {
        var link = await Create(new CreateLinkRequest("https://example.com"));
        _clock.Advance(TimeSpan.FromMinutes(5));

        var destination = await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default);

        Assert.StartsWith("https://example.com", destination);
        var reloaded = await _service.GetAsync(link.ShortCode);
        Assert.Equal(1, reloaded!.ClickCount);
        Assert.Equal(Start.AddMinutes(5), reloaded.LastAccessedAt);
    }

    [Fact]
    public async Task Redirect_selects_destination_by_platform()
    {
        var link = await Create(new CreateLinkRequest(
            "https://example.com",
            Destinations: new Dictionary<string, string>
            {
                ["android"] = "https://example.com/android.apk",
                ["ios"] = "https://example.com/app.ipa"
            }));

        Assert.Equal("https://example.com/android.apk",
            await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Android));
        Assert.Equal("https://example.com/app.ipa",
            await _service.ResolveForRedirectAsync(link.ShortCode, Platform.iOS));
        Assert.StartsWith("https://example.com",
            await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default));
    }

    [Fact]
    public async Task Redirect_returns_null_for_missing_link()
    {
        Assert.Null(await _service.ResolveForRedirectAsync("nope", Platform.Default));
    }

    [Fact]
    public async Task Redirect_on_disabled_link_returns_null_and_does_not_count()
    {
        var link = await Create(new CreateLinkRequest("https://example.com"));
        await _service.SetStatusAsync(link.ShortCode, LinkStatus.Disabled);

        var destination = await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default);

        Assert.Null(destination);
        var reloaded = await _service.GetAsync(link.ShortCode);
        Assert.Equal(0, reloaded!.ClickCount);
    }

    // --- Disable / enable / delete ---

    [Fact]
    public async Task SetStatus_can_disable_then_re_enable()
    {
        var link = await Create(new CreateLinkRequest("https://example.com"));

        await _service.SetStatusAsync(link.ShortCode, LinkStatus.Disabled);
        Assert.Null(await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default));

        await _service.SetStatusAsync(link.ShortCode, LinkStatus.Active);
        Assert.NotNull(await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default));
    }

    [Fact]
    public async Task SetStatus_returns_not_found_for_missing_link()
    {
        var result = await _service.SetStatusAsync("nope", LinkStatus.Disabled);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Delete_removes_link_and_stops_redirects()
    {
        var link = await Create(new CreateLinkRequest("https://example.com"));

        var result = await _service.DeleteAsync(link.ShortCode);

        Assert.True(result.IsSuccess);
        Assert.Null(await _service.GetAsync(link.ShortCode));
        Assert.Null(await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default));
    }

    [Fact]
    public async Task Delete_returns_not_found_for_missing_link()
    {
        var result = await _service.DeleteAsync("nope");
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Deleted_alias_can_be_reused()
    {
        var link = await Create(new CreateLinkRequest("https://example.com", "reuse"));
        await _service.DeleteAsync(link.ShortCode);

        var recreated = await Create(new CreateLinkRequest("https://other.com", "reuse"));
        Assert.Equal("reuse", recreated.ShortCode);
    }
}
Update Todos

Add Result/Error type (Core/Result.cs)

Add AliasRules (format regex + reserved routes)

Simplify CustomAliasGenerator (no throw, validation moves to service)

Rewrite LinkService with Result + self-loop + reserved checks

Add Error->IResult mapper, update endpoints

Remove DomainExceptions + handler, update Program.cs

Update + add tests (edge cases), all pass

E2E smoke test new validation
Bash Run all tests after Result refactor
IN
cd e:/coding/url_link_shortener/url_link_shortener/backend && dotnet test -v q 2>&1 | tail -15

OUT
Test run for E:\coding\url_link_shortener\url_link_shortener\backend\tests\UrlShortener.UnitTests\bin\Debug\net9.0\UrlShortener.UnitTests.dll (.NETCoreApp,Version=v9.0)
VSTest version 17.14.1 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    79, Skipped:     0, Total:    79, Duration: 28 ms - UrlShortener.UnitTests.dll (net9.0)
79 tests ผ่าน. Smoke test validation ใหม่แบบรันจริง:

Bash Start backend for validation smoke test
IN
cd e:/coding/url_link_shortener/url_link_shortener/backend && ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/UrlShortener.Api --urls "http://localhost:5000" > "$TEMP/urlsvc2.log" 2>&1 &
echo "pid $!"
for i in $(seq 1 20); do curl -s -o /dev/null http://localhost:5000/api/links 2>/dev/null && { echo "UP ${i}s"; break; }; sleep 1; done

OUT
pid 764
UP 1s
Bash Smoke test all validation edge cases
IN
B=http://localhost:5000
t() { printf '%-42s' "$1"; shift; curl -s -o /tmp/body -w "%{http_code}" -X POST $B/api/links -H "Content-Type: application/json" -d "$1"; echo " | $(cat /tmp/body | grep -oE '"detail":"[^"]*"' || echo ok)"; }

t "no scheme (example.com)"        '{"url":"example.com"}'
t "javascript: scheme"            '{"url":"javascript:alert(1)"}'
t "ftp: scheme"                   '{"url":"ftp://x.com"}'
t "self-loop (own domain)"        '{"url":"http://localhost:5000/abc"}'
t "reserved alias api"            '{"url":"https://ok.com","customAlias":"api"}'
t "special char alias"            '{"url":"https://ok.com","customAlias":"bad!alias"}'
t "platform dest self-loop"       '{"url":"https://ok.com","destinations":{"ios":"http://localhost:5000/x"}}'
t "valid link"                    '{"url":"https://www.google.co.th","customAlias":"goog"}'

OUT
no scheme (example.com)                   400 | "detail":"A valid absolute http/https URL is required."
javascript: scheme                        400 | "detail":"A valid absolute http/https URL is required."
ftp: scheme                               400 | "detail":"A valid absolute http/https URL is required."
self-loop (own domain)                    400 | "detail":"Destination cannot point back to the shortener's own domain."
reserved alias api                        400 | "detail":"'api' is a reserved name and cannot be used as an alias."
special char alias                        400 | "detail":"Alias must be 3–30 characters: letters, digits, '-' or '_'."
platform dest self-loop                   400 | "detail":"Destination for 'ios': Destination cannot point back to the shortener's own domain."
valid link                                201 | ok
ทุก edge case ตอบถูก + message ชัด. หยุด backend:

Bash Stop background backend
IN
taskkill //F //PID 764 2>/dev/null; taskkill //F //IM UrlShortener.Api.exe 2>/dev/null; echo stopped

OUT
SUCCESS: The process "UrlShortener.Api.exe" with PID 22968 has been terminated.
stopped
README — แก้ section error ให้ตรงกับ Result pattern:

Edit README.md
Added 10 lines
Update Todos

Add Result/Error type (Core/Result.cs)

Add AliasRules (format regex + reserved routes)

Simplify CustomAliasGenerator (no throw, validation moves to service)

Rewrite LinkService with Result + self-loop + reserved checks

Add Error->IResult mapper, update endpoints

Remove DomainExceptions + handler, update Program.cs

Update + add tests (edge cases), all pass (79)

E2E smoke test new validation (all pass)
เสร็จ. Refactor เป็น Result pattern + edge case ครบ.

สิ่งที่เปลี่ยน
Result pattern แทน throw:

Result.cs — Result, Result<T>, Error(ErrorType, Message) — มี implicit operator ให้ return link; / return Error.Validation(...) เขียนสั้น
ลบ DomainException + DomainExceptionHandler ทิ้ง — business error ไม่ throw แล้ว
ResultExtensions.cs — Error.ToProblem() map เป็น ProblemDetails ที่ edge (Validation→400, Conflict→409, NotFound→404)
Service คืน Result<Link> (create/setStatus), Result (delete). Redirect ยังคืน string? (binary ไม่มี message → 404 เฉยๆ, Result เกินจำเป็น)
Edge case validation (แยก concern ชัด):

URL validation อยู่ที่ UrlValidator.cs — no scheme / javascript: / ftp: / file: / relative → reject
Self-loop + reserved อยู่ที่ LinkService.cs (business rule) — compare Uri.Authority กับ short domain
Alias format + reserved route ย้ายจาก generator มารวมที่ AliasRules.cs — CustomAliasGenerator เหลือแค่ produce candidate (SRP ชัดขึ้น)
ยืนยัน (รันจริง)
Input	ผล
example.com (no scheme)	400
javascript: / ftp:	400
self-loop localhost:5000/x	400 "point back to own domain"
alias api (reserved)	400 "reserved name"
alias bad!alias	400 "3–30 characters..."
platform dest self-loop	400 (nested message)
valid	201
79 tests ผ่าน (เพิ่ม 12: reserved, loop, scheme edge, Result outcomes).

จุดคุยในสัมภาษณ์
ทำไม Result ไม่ throw: business error เป็น expected flow — throw ทำให้ control flow ซ่อน + performance cost. Result บังคับ caller handle. เก็บ exception ไว้เฉพาะ unexpected (→500)
ทำไม redirect ยังใช้ string?: outcome binary ไม่มี message ให้สื่อ — Result เกินจำเป็น (judgment ไม่ dogmatic)
self-loop: กัน redirect loop ตอน redirect time
reserved: กัน alias ชน route จริง — แต่หมายเหตุ: alias regex ห้าม . อยู่แล้ว ดังนั้น dotted reserved (favicon.ico) moot, ตัวจริงที่สำคัญคือ api
อยากให้ทำ EfLinkRepository พิสูจน์ swap, หรือ commit งานทั้งหมดตอนนี้ไหม?

เขียน unit tests ครอบคลุม 4 หัวข้อที่โจทย์ระบุ: short-code generation, URL validation, click counting, disable/delete behavior ใช้ xUnit + FluentAssertions + NSubstitute เน้น test ที่มีความหมาย ไม่ใช่ test ที่ทำให้ coverage สวย และช่วยชี้ให้เห็นด้วยว่า test ตัวไหนบ้างที่พิสูจน์ว่า design ของเรา decoupled จริง
เพิ่ม FluentAssertions + NSubstitute (pin version ที่ free license — FA 6.x Apache-2.0, เลี่ยง 8.x ที่เป็น commercial):

Bash Add FluentAssertions and NSubstitute packages
IN
cd e:/coding/url_link_shortener/url_link_shortener/backend/tests/UrlShortener.UnitTests && \
dotnet add package FluentAssertions --version 6.12.2 2>&1 | tail -2 && \
dotnet add package NSubstitute --version 5.3.0 2>&1 | tail -2

OUT
info : Writing assets file to disk. Path: E:\coding\url_link_shortener\url_link_shortener\backend\tests\UrlShortener.UnitTests\obj\project.assets.json
log  : Restored E:\coding\url_link_shortener\url_link_shortener\backend\tests\UrlShortener.UnitTests\UrlShortener.UnitTests.csproj (in 3.59 sec).
info : Writing assets file to disk. Path: E:\coding\url_link_shortener\url_link_shortener\backend\tests\UrlShortener.UnitTests\obj\project.assets.json
log  : Restored E:\coding\url_link_shortener\url_link_shortener\backend\tests\UrlShortener.UnitTests\UrlShortener.UnitTests.csproj (in 3.76 sec).
เพิ่ม global usings ใน csproj:

Edit UrlShortener.UnitTests.csproj
Added 2 lines
แปลง test เป็น FluentAssertions ทีละไฟล์ + เพิ่มไฟล์ decoupling (NSubstitute). เริ่ม generation topic:

Write RandomShortCodeGeneratorTests.cs
49 lines
using UrlShortener.Api.Core.ShortCodes;

namespace UrlShortener.UnitTests.ShortCodes;

public class RandomShortCodeGeneratorTests
{
    private readonly RandomShortCodeGenerator _generator = new();
    private static readonly GenerationRequest AnyRequest = new(CustomAlias: null);

    [Fact]
    public void Generate_produces_a_seven_character_code()
    {
        _generator.Generate(AnyRequest).Should().HaveLength(7);
    }

    [Fact]
    public void Generate_uses_only_the_unambiguous_alphabet()
    {
        const string forbidden = "0O1lI";

        var codes = Enumerable.Range(0, 100).Select(_ => _generator.Generate(AnyRequest));

        codes.Should().OnlyContain(code => !code.Any(forbidden.Contains));
    }

    [Fact]
    public void Generate_is_effectively_unique_across_many_calls()
    {
        var codes = Enumerable.Range(0, 10_000)
            .Select(_ => _generator.Generate(AnyRequest))
            .ToList();

        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void CanHandle_is_always_true_so_it_acts_as_the_fallback()
    {
        _generator.CanHandle(AnyRequest).Should().BeTrue();
        _generator.CanHandle(new GenerationRequest("anything")).Should().BeTrue();
    }

    [Fact]
    public void AllowRetryOnCollision_is_true()
    {
        _generator.AllowRetryOnCollision.Should().BeTrue();
    }
}
Write CustomAliasGeneratorTests.cs
29 lines
using UrlShortener.Api.Core.ShortCodes;

namespace UrlShortener.UnitTests.ShortCodes;

public class CustomAliasGeneratorTests
{
    private readonly CustomAliasGenerator _generator = new();

    [Fact]
    public void Generate_returns_the_trimmed_alias()
    {
        _generator.Generate(new GenerationRequest("  mylink  ")).Should().Be("mylink");
    }

    [Fact]
    public void CanHandle_only_when_alias_present()
    {
        _generator.CanHandle(new GenerationRequest("mylink")).Should().BeTrue();
        _generator.CanHandle(new GenerationRequest(null)).Should().BeFalse();
        _generator.CanHandle(new GenerationRequest("   ")).Should().BeFalse();
    }

    [Fact]
    public void AllowRetryOnCollision_is_false()
    {
        _generator.AllowRetryOnCollision.Should().BeFalse();
    }
}
Write ShortCodeGeneratorResolverTests.cs
48 lines
using UrlShortener.Api.Core.ShortCodes;

namespace UrlShortener.UnitTests.ShortCodes;

public class ShortCodeGeneratorResolverTests
{
    // Mirrors the DI registration order: custom alias first, random fallback last.
    private readonly ShortCodeGeneratorResolver _resolver = new(
        [new CustomAliasGenerator(), new RandomShortCodeGenerator()]);

    [Fact]
    public void Resolve_picks_custom_alias_when_alias_is_supplied()
    {
        _resolver.Resolve(new GenerationRequest("mylink")).Name.Should().Be("custom-alias");
    }

    [Fact]
    public void Resolve_falls_back_to_random_when_no_alias()
    {
        _resolver.Resolve(new GenerationRequest(null)).Name.Should().Be("random");
    }

    [Fact]
    public void Resolve_selects_a_newly_added_strategy_without_changing_the_resolver()
    {
        // A third strategy that claims requests whose alias starts with "vip-".
        // The resolver picks it purely via CanHandle — proving Open/Closed:
        // extending the set of generators needs no change to the resolver.
        var vip = Substitute.For<IShortCodeGenerator>();
        vip.Name.Returns("vip");
        vip.CanHandle(Arg.Is<GenerationRequest>(r => r.CustomAlias?.StartsWith("vip-") == true))
            .Returns(true);

        var resolver = new ShortCodeGeneratorResolver(
            [vip, new CustomAliasGenerator(), new RandomShortCodeGenerator()]);

        resolver.Resolve(new GenerationRequest("vip-gold")).Name.Should().Be("vip");
        resolver.Resolve(new GenerationRequest("normal")).Name.Should().Be("custom-alias");
    }

    [Fact]
    public void Constructor_throws_when_no_generators_registered()
    {
        var act = () => new ShortCodeGeneratorResolver([]);
        act.Should().Throw<InvalidOperationException>();
    }
}
Write AliasRulesTests.cs
46 lines
using UrlShortener.Api.Core.ShortCodes;

namespace UrlShortener.UnitTests.ShortCodes;

public class AliasRulesTests
{
    [Theory]
    [InlineData("mylink")]
    [InlineData("My-Link_1")]
    [InlineData("abc")]
    [InlineData("abcdefghijklmnopqrstuvwxyz1234")] // 30 chars
    public void IsWellFormed_accepts_valid_aliases(string alias)
    {
        AliasRules.IsWellFormed(alias).Should().BeTrue();
    }

    [Theory]
    [InlineData("ab")]                 // too short
    [InlineData("has space")]
    [InlineData("bad!char")]
    [InlineData("with.dot")]
    [InlineData("way-too-long-alias-that-exceeds-thirty-chars")]
    public void IsWellFormed_rejects_invalid_aliases(string alias)
    {
        AliasRules.IsWellFormed(alias).Should().BeFalse();
    }

    [Theory]
    [InlineData("api")]
    [InlineData("API")]      // case-insensitive
    [InlineData("openapi")]
    [InlineData("health")]
    public void IsReserved_flags_route_shadowing_names(string alias)
    {
        AliasRules.IsReserved(alias).Should().BeTrue();
    }

    [Theory]
    [InlineData("mylink")]
    [InlineData("gulf")]
    public void IsReserved_allows_normal_aliases(string alias)
    {
        AliasRules.IsReserved(alias).Should().BeFalse();
    }
}
Validation topic:

Write UrlValidatorTests.cs
42 lines
using UrlShortener.Api.Core.Validation;

namespace UrlShortener.UnitTests.Validation;

public class UrlValidatorTests
{
    private readonly UrlValidator _validator = new();

    [Theory]
    [InlineData("https://www.google.co.th")]
    [InlineData("http://example.com/path?q=1")]
    [InlineData("https://sub.domain.io:8443/a/b")]
    public void TryNormalize_accepts_absolute_http_urls(string url)
    {
        _validator.TryNormalize(url, out var normalized).Should().BeTrue();
        normalized.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("example.com")]                 // missing scheme -> not absolute
    [InlineData("/relative/path")]
    [InlineData("javascript:alert(1)")]          // dangerous scheme
    [InlineData("ftp://files.example.com")]      // non-web scheme
    [InlineData("file:///etc/passwd")]
    public void TryNormalize_rejects_invalid_or_unsafe_urls(string? url)
    {
        _validator.TryNormalize(url, out var normalized).Should().BeFalse();
        normalized.Should().BeEmpty();
    }

    [Fact]
    public void TryNormalize_trims_whitespace()
    {
        _validator.TryNormalize("  https://example.com  ", out var normalized).Should().BeTrue();
        normalized.Should().StartWith("https://example.com");
    }
}
Write UserAgentPlatformDetectorTests.cs
37 lines
using UrlShortener.Api.Core;

namespace UrlShortener.UnitTests.PlatformDetection;

public class UserAgentPlatformDetectorTests
{
    private readonly UserAgentPlatformDetector _detector = new();

    [Theory]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36")]
    [InlineData("Dalvik/2.1.0 (Linux; U; Android 13)")]
    public void Detect_returns_Android_for_android_agents(string userAgent)
    {
        _detector.Detect(userAgent).Should().Be(Platform.Android);
    }

    [Theory]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X)")]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 16_0 like Mac OS X)")]
    [InlineData("Mozilla/5.0 (iPod touch; CPU iPhone OS 15_0 like Mac OS X)")]
    public void Detect_returns_iOS_for_apple_mobile_agents(string userAgent)
    {
        _detector.Detect(userAgent).Should().Be(Platform.iOS);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64)")]
    [InlineData("curl/8.4.0")]
    [InlineData("Googlebot/2.1")]
    public void Detect_falls_back_to_Default_for_unknown_or_missing_agents(string? userAgent)
    {
        _detector.Detect(userAgent).Should().Be(Platform.Default);
    }
}
Click counting topic (Link domain):

Write LinkTests.cs
68 lines
using UrlShortener.Api.Core;

namespace UrlShortener.UnitTests;

public class LinkTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 13, 10, 0, 0, TimeSpan.Zero);

    private static Link BuildLink(params (Platform, string)[] destinations)
    {
        var map = destinations.ToDictionary(d => d.Item1, d => d.Item2);
        return new Link("abc123", map, Now);
    }

    [Fact]
    public void Constructor_requires_a_default_destination()
    {
        var withoutDefault = new Dictionary<Platform, string> { [Platform.iOS] = "https://a.io" };

        var act = () => new Link("abc", withoutDefault, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void New_link_is_active_with_zero_clicks_and_no_last_access()
    {
        var link = BuildLink((Platform.Default, "https://a.com"));

        link.Status.Should().Be(LinkStatus.Active);
        link.ClickCount.Should().Be(0);
        link.LastAccessedAt.Should().BeNull();
    }

    [Fact]
    public void ResolveDestination_falls_back_to_default_for_unmapped_platform()
    {
        var link = BuildLink(
            (Platform.Default, "https://default.com"),
            (Platform.Android, "https://android.com"));

        link.ResolveDestination(Platform.Android).Should().Be("https://android.com");
        link.ResolveDestination(Platform.iOS).Should().Be("https://default.com");
    }

    [Fact]
    public void RegisterVisit_increments_count_and_records_time()
    {
        var link = BuildLink((Platform.Default, "https://a.com"));

        link.RegisterVisit(Now);

        link.ClickCount.Should().Be(1);
        link.LastAccessedAt.Should().Be(Now);
    }

    [Fact]
    public void RegisterVisit_counts_are_atomic_under_concurrency()
    {
        var link = BuildLink((Platform.Default, "https://a.com"));
        const int visits = 10_000;

        Parallel.For(0, visits, _ => link.RegisterVisit(Now));

        link.ClickCount.Should().Be(visits);
    }
}
Write InMemoryLinkRepositoryTests.cs
54 lines
using UrlShortener.Api.Core;
using UrlShortener.Api.Core.Persistence;

namespace UrlShortener.UnitTests.Persistence;

public class InMemoryLinkRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 13, 10, 0, 0, TimeSpan.Zero);

    private static Link BuildLink(string code = "abc123") =>
        new(code, new Dictionary<Platform, string> { [Platform.Default] = "https://a.com" }, Now);

    private readonly InMemoryLinkRepository _repo = new();

    [Fact]
    public async Task TryAdd_returns_false_on_duplicate_code()
    {
        (await _repo.TryAddAsync(BuildLink("dup"))).Should().BeTrue();
        (await _repo.TryAddAsync(BuildLink("dup"))).Should().BeFalse();
    }

    [Fact]
    public async Task TryAdd_is_case_insensitive()
    {
        (await _repo.TryAddAsync(BuildLink("MyLink"))).Should().BeTrue();
        (await _repo.TryAddAsync(BuildLink("mylink"))).Should().BeFalse();
    }

    [Fact]
    public async Task Update_after_delete_does_not_resurrect_the_link()
    {
        // Models the redirect race: a request reads the link and writes back its
        // click count (UpdateAsync) after a concurrent DeleteAsync removed it.
        var link = BuildLink();
        await _repo.TryAddAsync(link);
        await _repo.RemoveAsync(link.ShortCode);

        await _repo.UpdateAsync(link); // stale write-back from the in-flight visit

        (await _repo.GetAsync(link.ShortCode)).Should().BeNull();
    }

    [Fact]
    public async Task Concurrent_TryAdd_of_same_code_admits_exactly_one()
    {
        const int racers = 50;
        var links = Enumerable.Range(0, racers).Select(_ => BuildLink("race")).ToArray();

        var results = await Task.WhenAll(links.Select(l => _repo.TryAddAsync(l)));

        results.Count(won => won).Should().Be(1);
    }
}
LinkServiceTests → FluentAssertions:

Write LinkServiceTests.cs
266 lines
using Microsoft.Extensions.Options;
using UrlShortener.Api.Core;
using UrlShortener.Api.Core.Persistence;
using UrlShortener.Api.Core.ShortCodes;
using UrlShortener.Api.Core.Validation;
using UrlShortener.Api.Features.Links;

namespace UrlShortener.UnitTests.Services;

/// <summary>
/// Behavior of the service against a real in-memory store — end-to-end for the
/// core user journey (create, stats, disable/enable, delete, redirect).
/// Decoupling from those collaborators is proven separately with mocks in
/// <see cref="LinkServiceDecouplingTests"/>.
/// </summary>
public class LinkServiceTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 13, 10, 0, 0, TimeSpan.Zero);
    private const string ShortDomain = "http://localhost:5000";

    private readonly FixedTimeProvider _clock = new(Start);
    private readonly LinkService _service;

    public LinkServiceTests()
    {
        var resolver = new ShortCodeGeneratorResolver(
            [new CustomAliasGenerator(), new RandomShortCodeGenerator()]);
        var options = Options.Create(new ShortUrlOptions { BaseUrl = ShortDomain });
        _service = new LinkService(new InMemoryLinkRepository(), resolver, new UrlValidator(), options, _clock);
    }

    private async Task<Link> Create(CreateLinkRequest request)
    {
        var result = await _service.CreateAsync(request);
        result.IsSuccess.Should().BeTrue(because: result.IsFailure ? result.Error.Message : "");
        return result.Value;
    }

    // --- Create (happy path) ---

    [Fact]
    public async Task Create_returns_active_link_with_zero_clicks()
    {
        var link = await Create(new CreateLinkRequest("https://example.com"));

        link.Status.Should().Be(LinkStatus.Active);
        link.ClickCount.Should().Be(0);
        link.CreatedAt.Should().Be(Start);
        link.ShortCode.Should().HaveLength(7);
    }

    [Fact]
    public async Task Create_uses_custom_alias_when_supplied()
    {
        var link = await Create(new CreateLinkRequest("https://example.com", "mylink"));
        link.ShortCode.Should().Be("mylink");
    }

    [Fact]
    public async Task Create_stores_platform_destinations()
    {
        var link = await Create(new CreateLinkRequest(
            "https://example.com",
            Destinations: new Dictionary<string, string>
            {
                ["android"] = "https://example.com/android.apk",
                ["ios"] = "https://example.com/app.ipa"
            }));

        link.ResolveDestination(Platform.Android).Should().Be("https://example.com/android.apk");
        link.ResolveDestination(Platform.iOS).Should().Be("https://example.com/app.ipa");
    }

    // --- Create (validation edge cases, returned as Result failures) ---

    [Theory]
    [InlineData("example.com")]            // no scheme
    [InlineData("javascript:alert(1)")]    // dangerous scheme
    [InlineData("ftp://x.com")]            // non-web scheme
    public async Task Create_rejects_invalid_url_scheme(string url)
    {
        var result = await _service.CreateAsync(new CreateLinkRequest(url));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Create_rejects_link_pointing_back_to_short_domain()
    {
        var result = await _service.CreateAsync(new CreateLinkRequest($"{ShortDomain}/somewhere"));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("own domain");
    }

    [Fact]
    public async Task Create_rejects_platform_destination_pointing_back_to_short_domain()
    {
        var result = await _service.CreateAsync(new CreateLinkRequest(
            "https://example.com",
            Destinations: new Dictionary<string, string> { ["ios"] = $"{ShortDomain}/x" }));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Create_rejects_reserved_alias()
    {
        var result = await _service.CreateAsync(new CreateLinkRequest("https://example.com", "api"));

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("reserved");
    }

    [Fact]
    public async Task Create_rejects_alias_with_special_characters()
    {
        var result = await _service.CreateAsync(new CreateLinkRequest("https://example.com", "bad!alias"));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Create_rejects_unknown_platform_key()
    {
        var result = await _service.CreateAsync(new CreateLinkRequest(
            "https://example.com",
            Destinations: new Dictionary<string, string> { ["windows"] = "https://example.com/win" }));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Create_with_duplicate_alias_returns_conflict()
    {
        await Create(new CreateLinkRequest("https://example.com", "dup"));

        var result = await _service.CreateAsync(new CreateLinkRequest("https://other.com", "dup"));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Create_duplicate_alias_is_case_insensitive()
    {
        await Create(new CreateLinkRequest("https://example.com", "MyLink"));

        var result = await _service.CreateAsync(new CreateLinkRequest("https://other.com", "mylink"));

        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    // --- Redirect / click counting ---

    [Fact]
    public async Task Redirect_increments_click_and_records_access_time()
    {
        var link = await Create(new CreateLinkRequest("https://example.com"));
        _clock.Advance(TimeSpan.FromMinutes(5));

        var destination = await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default);

        destination.Should().StartWith("https://example.com");
        var reloaded = await _service.GetAsync(link.ShortCode);
        reloaded!.ClickCount.Should().Be(1);
        reloaded.LastAccessedAt.Should().Be(Start.AddMinutes(5));
    }

    [Fact]
    public async Task Redirect_selects_destination_by_platform()
    {
        var link = await Create(new CreateLinkRequest(
            "https://example.com",
            Destinations: new Dictionary<string, string>
            {
                ["android"] = "https://example.com/android.apk",
                ["ios"] = "https://example.com/app.ipa"
            }));

        (await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Android))
            .Should().Be("https://example.com/android.apk");
        (await _service.ResolveForRedirectAsync(link.ShortCode, Platform.iOS))
            .Should().Be("https://example.com/app.ipa");
        (await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default))
            .Should().StartWith("https://example.com");
    }

    [Fact]
    public async Task Redirect_returns_null_for_missing_link()
    {
        (await _service.ResolveForRedirectAsync("nope", Platform.Default)).Should().BeNull();
    }

    [Fact]
    public async Task Redirect_on_disabled_link_returns_null_and_does_not_count()
    {
        var link = await Create(new CreateLinkRequest("https://example.com"));
        await _service.SetStatusAsync(link.ShortCode, LinkStatus.Disabled);

        var destination = await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default);

        destination.Should().BeNull();
        (await _service.GetAsync(link.ShortCode))!.ClickCount.Should().Be(0);
    }

    // --- Disable / enable / delete ---

    [Fact]
    public async Task SetStatus_can_disable_then_re_enable()
    {
        var link = await Create(new CreateLinkRequest("https://example.com"));

        await _service.SetStatusAsync(link.ShortCode, LinkStatus.Disabled);
        (await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default)).Should().BeNull();

        await _service.SetStatusAsync(link.ShortCode, LinkStatus.Active);
        (await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default)).Should().NotBeNull();
    }

    [Fact]
    public async Task SetStatus_returns_not_found_for_missing_link()
    {
        var result = await _service.SetStatusAsync("nope", LinkStatus.Disabled);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Delete_removes_link_and_stops_redirects()
    {
        var link = await Create(new CreateLinkRequest("https://example.com"));

        var result = await _service.DeleteAsync(link.ShortCode);

        result.IsSuccess.Should().BeTrue();
        (await _service.GetAsync(link.ShortCode)).Should().BeNull();
        (await _service.ResolveForRedirectAsync(link.ShortCode, Platform.Default)).Should().BeNull();
    }

    [Fact]
    public async Task Delete_returns_not_found_for_missing_link()
    {
        var result = await _service.DeleteAsync("nope");

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Deleted_alias_can_be_reused()
    {
        var link = await Create(new CreateLinkRequest("https://example.com", "reuse"));
        await _service.DeleteAsync(link.ShortCode);

        var recreated = await Create(new CreateLinkRequest("https://other.com", "reuse"));
        recreated.ShortCode.Should().Be("reuse");
    }
}
สร้างไฟล์ decoupling tests (NSubstitute) — พิสูจน์ service พึ่ง abstraction ล้วน:

Write LinkServiceDecouplingTests.cs
153 lines
using Microsoft.Extensions.Options;
using UrlShortener.Api.Core;
using UrlShortener.Api.Core.Persistence;
using UrlShortener.Api.Core.ShortCodes;
using UrlShortener.Api.Core.Validation;
using UrlShortener.Api.Features.Links;

namespace UrlShortener.UnitTests.Services;

/// <summary>
/// These tests use mocks (NSubstitute) in place of the real collaborators to
/// prove the service is decoupled: it talks only to the abstractions
/// (ILinkRepository, IShortCodeGenerator, IUrlValidator) and its behavior is
/// driven entirely through those seams — never through a concrete type.
/// </summary>
public class LinkServiceDecouplingTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 13, 10, 0, 0, TimeSpan.Zero);

    private static LinkService Build(
        ILinkRepository repository,
        IShortCodeGenerator generator,
        IUrlValidator validator)
    {
        var resolver = new ShortCodeGeneratorResolver([generator]);
        var options = Options.Create(new ShortUrlOptions { BaseUrl = "http://localhost:5000" });
        return new LinkService(repository, resolver, validator, options, new FixedTimeProvider(Start));
    }

    private static IUrlValidator PassThroughValidator()
    {
        // Accepts anything, echoing the input back as the normalized URL.
        var validator = Substitute.For<IUrlValidator>();
        validator.TryNormalize(Arg.Any<string?>(), out Arg.Any<string>())
            .Returns(ci => { ci[1] = (string?)ci[0] ?? ""; return true; });
        return validator;
    }

    // --- Strategy seam: the service uses whatever generator it's given ---

    [Fact]
    public async Task Create_uses_the_code_produced_by_the_injected_generator()
    {
        // A generator the service has never heard of — proves code generation is
        // fully delegated through IShortCodeGenerator (Strategy / Open-Closed).
        var generator = Substitute.For<IShortCodeGenerator>();
        generator.CanHandle(Arg.Any<GenerationRequest>()).Returns(true);
        generator.AllowRetryOnCollision.Returns(true);
        generator.Generate(Arg.Any<GenerationRequest>()).Returns("PLUGIN1");

        var repo = Substitute.For<ILinkRepository>();
        repo.TryAddAsync(Arg.Any<Link>(), Arg.Any<CancellationToken>()).Returns(true);

        var service = Build(repo, generator, PassThroughValidator());

        var result = await service.CreateAsync(new CreateLinkRequest("https://example.com"));

        result.IsSuccess.Should().BeTrue();
        result.Value.ShortCode.Should().Be("PLUGIN1");
        generator.Received(1).Generate(Arg.Any<GenerationRequest>());
    }

    // --- Repository seam: persistence goes through ILinkRepository ---

    [Fact]
    public async Task Create_reserves_the_code_through_the_repository()
    {
        var repo = Substitute.For<ILinkRepository>();
        repo.TryAddAsync(Arg.Any<Link>(), Arg.Any<CancellationToken>()).Returns(true);

        var service = Build(repo, new RandomShortCodeGenerator(), PassThroughValidator());

        await service.CreateAsync(new CreateLinkRequest("https://example.com"));

        await repo.Received(1).TryAddAsync(Arg.Any<Link>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_retries_with_a_new_code_when_the_repository_reports_a_collision()
    {
        // Repository (not the service) owns uniqueness: it rejects twice, then
        // accepts. The random generator's retry policy drives the loop entirely
        // through the abstraction.
        var repo = Substitute.For<ILinkRepository>();
        repo.TryAddAsync(Arg.Any<Link>(), Arg.Any<CancellationToken>())
            .Returns(false, false, true);

        var service = Build(repo, new RandomShortCodeGenerator(), PassThroughValidator());

        var result = await service.CreateAsync(new CreateLinkRequest("https://example.com"));

        result.IsSuccess.Should().BeTrue();
        await repo.Received(3).TryAddAsync(Arg.Any<Link>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_does_not_retry_a_deterministic_generator_and_returns_conflict()
    {
        // Custom alias never changes, so a collision is final: exactly one
        // reservation attempt, then Conflict. Proves AllowRetryOnCollision is
        // what drives the decision — not a concrete generator check.
        var repo = Substitute.For<ILinkRepository>();
        repo.TryAddAsync(Arg.Any<Link>(), Arg.Any<CancellationToken>()).Returns(false);

        var service = Build(repo, new CustomAliasGenerator(), PassThroughValidator());

        var result = await service.CreateAsync(new CreateLinkRequest("https://example.com", "mylink"));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        await repo.Received(1).TryAddAsync(Arg.Any<Link>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Redirect_writes_the_click_back_through_the_repository()
    {
        // Click-count persistence is a seam: after a visit the service must
        // hand the updated link back to the repository.
        var link = new Link("code01",
            new Dictionary<Platform, string> { [Platform.Default] = "https://example.com" }, Start);
        var repo = Substitute.For<ILinkRepository>();
        repo.GetAsync("code01", Arg.Any<CancellationToken>()).Returns(link);

        var service = Build(repo, new RandomShortCodeGenerator(), PassThroughValidator());

        var destination = await service.ResolveForRedirectAsync("code01", Platform.Default);

        destination.Should().Be("https://example.com");
        link.ClickCount.Should().Be(1);
        await repo.Received(1).UpdateAsync(link, Arg.Any<CancellationToken>());
    }

    // --- Validation seam: URL validation is delegated to IUrlValidator ---

    [Fact]
    public async Task Create_rejects_when_the_url_validator_rejects()
    {
        var validator = Substitute.For<IUrlValidator>();
        validator.TryNormalize(Arg.Any<string?>(), out Arg.Any<string>())
            .Returns(ci => { ci[1] = string.Empty; return false; });

        var repo = Substitute.For<ILinkRepository>();
        var service = Build(repo, new RandomShortCodeGenerator(), validator);

        var result = await service.CreateAsync(new CreateLinkRequest("anything-the-validator-says-no"));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        // Validation failed before any storage was touched.
        await repo.DidNotReceive().TryAddAsync(Arg.Any<Link>(), Arg.Any<CancellationToken>());
    }
}
Update Todos

Add FluentAssertions + NSubstitute packages

Convert existing tests to FluentAssertions

Add NSubstitute decoupling tests

Run full suite, all pass
Bash Run full test suite with FA + NSubstitute
IN
cd e:/coding/url_link_shortener/url_link_shortener/backend && dotnet test -v q 2>&1 | tail -20

OUT
E:\coding\url_link_shortener\url_link_shortener\backend\tests\UrlShortener.UnitTests\ShortCodes\ShortCodeGeneratorResolverTests.cs(31,54): error CS8072: An expression tree lambda may not contain a null propagating operator. [E:\coding\url_link_shortener\url_link_shortener\backend\tests\UrlShortener.UnitTests\UrlShortener.UnitTests.csproj]
?. ใน expression tree ไม่ได้. แก้:

Edit ShortCodeGeneratorResolverTests.cs
Modified
Bash Re-run full test suite
IN
cd e:/coding/url_link_shortener/url_link_shortener/backend && dotnet test -v q 2>&1 | tail -20

OUT
Test run for E:\coding\url_link_shortener\url_link_shortener\backend\tests\UrlShortener.UnitTests\bin\Debug\net9.0\UrlShortener.UnitTests.dll (.NETCoreApp,Version=v9.0)
VSTest version 17.14.1 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    87, Skipped:     0, Total:    87, Duration: 320 ms - UrlShortener.UnitTests.dll (net9.0)
87 tests ผ่านหมด. อัปเดต README testing section:

Edit README.md
Added 14 lines
Update Todos

Add FluentAssertions + NSubstitute packages

Convert existing tests to FluentAssertions

Add NSubstitute decoupling tests

Run full suite, all pass (87)

Update README testing section
เสร็จ. Test suite ยกระดับเป็น xUnit + FluentAssertions + NSubstitute, 87 tests ผ่าน.

ครอบคลุม 4 หัวข้อที่โจทย์ระบุ
หัวข้อ	ไฟล์	เนื้อหา
Short-code generation	Random/CustomAlias/Resolver/AliasRules Tests	length, alphabet (ตัด 0/O/1/l/I), uniqueness 10k, fallback, retry policy, reserved
URL validation	UrlValidatorTests	รับ http/https, ปฏิเสธ javascript:/ftp:/file:/relative/empty
Click counting	LinkTests, LinkServiceTests	increment, last-accessed, 10k concurrent atomic, disabled ไม่นับ
Disable/delete	LinkServiceTests	disable→ไม่ redirect, re-enable, delete→404, reuse alias หลังลบ
Test ที่พิสูจน์ว่า design decoupled จริง (NSubstitute)
ไฟล์ LinkServiceDecouplingTests.cs — สลับ collaborator เป็น mock พิสูจน์ service พึ่งแค่ abstraction:

Create_uses_the_code_produced_by_the_injected_generator — ยัด fake IShortCodeGenerator คืน "PLUGIN1" → link ได้ code นั้น. พิสูจน์ Strategy/OCP: generator ที่ service ไม่เคยรู้จักก็ทำงานได้
Create_retries_…_repository_reports_a_collision — mock repo คืน false,false,true → verify TryAddAsync ถูกเรียก 3 ครั้ง. พิสูจน์ retry loop วิ่งผ่าน interface ไม่ใช่ internals ของ InMemory
Create_does_not_retry_a_deterministic_generator — custom alias ชน → verify เรียก 1 ครั้ง + Conflict. พิสูจน์ AllowRetryOnCollision เป็นตัวขับ ไม่ใช่ type-check
Redirect_writes_the_click_back_through_the_repository — verify UpdateAsync ถูกเรียกหลัง visit. พิสูจน์ click persistence เป็น seam สลับ DB ได้
Create_rejects_when_the_url_validator_rejects — mock validator ปฏิเสธ → verify DidNotReceive().TryAddAsync. พิสูจน์ validation delegate ไป IUrlValidator + fail ก่อนแตะ storage
Resolve_selects_a_newly_added_strategy — resolver เลือก generator ตัวใหม่ผ่าน CanHandle ล้วน
หลักที่ยึด: ถ้า mock แล้ว test ยัง drive behavior ได้ = seam จริง. ถ้าต้อง new concrete = coupling. ทุกตัวข้างบนใช้ Received()/DidNotReceive() ยืนยัน interaction ผ่าน interface — ไม่ใช่แค่เช็ค output สวยๆ

หมายเหตุ
Pin FluentAssertions 6.12.2 (Apache-2.0 ฟรี) เลี่ยง 8.x ที่เป็น commercial license — จุดที่ควรรู้ถ้า repo นี้ใช้เชิงพาณิชย์
LinkServiceTests (real InMemory) กับ DecouplingTests (mock) แยกกัน — behavior vs decoupling คนละเจตนา
อยากให้ commit งานทั้งหมด หรือทำ EfLinkRepository พิสูจน์ swap ต่อไหม?

สร้าง React + TypeScript + Vite frontend มี 3 ส่วน: form สร้างลิงก์ (พร้อม optional custom alias และ optional platform-specific URLs), ตารางแสดงลิงก์ทั้งหมดพร้อมสถิติ, และปุ่ม disable/delete จัดการ loading/error state ให้เรียบร้อย ใช้ TanStack Query หรือ custom hook อธิบายเหตุผลที่เลือก
Frontend มีอยู่แล้ว (custom hook แบบ useState/useEffect). ยกระดับตามที่ขอ — เลือก TanStack Query เพราะมีเหตุผลจริง (อธิบายท้าย). ติดตั้งก่อน:

Bash Install TanStack Query
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && npm install @tanstack/react-query@5 2>&1 | tail -3 && node -e "console.log('tanstack', require('./package.json').dependencies['@tanstack/react-query'])"

OUT
  npm audit fix --force

Run `npm audit` for details.
tanstack ^5.101.2
main.tsx — wrap QueryClientProvider:

Write main.tsx
Write failed
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import './index.css';
import App from './App.tsx';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Click counts change server-side (via redirects), so treat data as
      // quickly stale and refetch when the user returns to the tab.
      staleTime: 10_000,
      retry: 1,
    },
  },
});

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>
  </StrictMode>,
);
Read main.tsx
Write main.tsx
25 lines
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import './index.css';
import App from './App.tsx';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Click counts change server-side (via redirects), so treat data as
      // quickly stale and refetch when the user returns to the tab.
      staleTime: 10_000,
      retry: 1,
    },
  },
});

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>
  </StrictMode>,
);
Hooks — query + mutations (invalidate on success):

Write useLinks.ts
38 lines
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, type CreateLinkRequest, type LinkStatus } from '../api';

const LINKS_KEY = ['links'] as const;

/** All links + their stats. Owns loading/error/refetch state. */
export function useLinks() {
  return useQuery({
    queryKey: LINKS_KEY,
    queryFn: () => api.list(),
  });
}

export function useCreateLink() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateLinkRequest) => api.create(body),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: LINKS_KEY }),
  });
}

export function useSetStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { shortCode: string; status: LinkStatus }) =>
      api.setStatus(vars.shortCode, vars.status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: LINKS_KEY }),
  });
}

export function useDeleteLink() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (shortCode: string) => api.remove(shortCode),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: LINKS_KEY }),
  });
}
App.tsx — ใช้ useLinks (loading/error/empty):

Write App.tsx
45 lines
import { useLinks } from './hooks/useLinks';
import { CreateLinkForm } from './components/CreateLinkForm';
import { LinkCard } from './components/LinkCard';

export default function App() {
  const { data: links, isLoading, isError } = useLinks();

  return (
    <div className="app">
      <header className="app-header">
        <h1>🔗 Shortly</h1>
        <p>Turn long URLs into short, trackable links.</p>
      </header>

      <main className="container">
        <CreateLinkForm />

        <section className="links-section">
          <h2>
            Your links {links && links.length > 0 && <span className="count">{links.length}</span>}
          </h2>

          {isLoading && <p className="muted">Loading…</p>}
          {isError && (
            <p className="error">Could not reach the API. Is the backend running on :5000?</p>
          )}
          {links && links.length === 0 && (
            <p className="muted empty">No links yet — create your first one above.</p>
          )}

          <div className="link-list">
            {links?.map((link) => (
              <LinkCard key={link.shortCode} link={link} />
            ))}
          </div>
        </section>
      </main>

      <footer className="app-footer">
        <span>Full Stack Assignment · .NET + React</span>
      </footer>
    </div>
  );
}
CreateLinkForm — ใช้ useCreateLink mutation (isPending/error):

Write CreateLinkForm.tsx
105 lines
import { useState, type FormEvent } from 'react';
import { ApiError, type CreateLinkRequest } from '../api';
import { useCreateLink } from '../hooks/useLinks';

export function CreateLinkForm() {
  const [url, setUrl] = useState('');
  const [customAlias, setCustomAlias] = useState('');
  const [androidUrl, setAndroidUrl] = useState('');
  const [iosUrl, setIosUrl] = useState('');
  const [showPlatforms, setShowPlatforms] = useState(false);

  const createLink = useCreateLink();

  function handleSubmit(event: FormEvent) {
    event.preventDefault();

    const destinations: Record<string, string> = {};
    if (androidUrl.trim()) destinations.android = androidUrl.trim();
    if (iosUrl.trim()) destinations.ios = iosUrl.trim();

    const body: CreateLinkRequest = { url: url.trim() };
    if (customAlias.trim()) body.customAlias = customAlias.trim();
    if (Object.keys(destinations).length > 0) body.destinations = destinations;

    createLink.mutate(body, {
      onSuccess: () => {
        setUrl('');
        setCustomAlias('');
        setAndroidUrl('');
        setIosUrl('');
        setShowPlatforms(false);
      },
    });
  }

  const errorMessage =
    createLink.error instanceof ApiError
      ? createLink.error.message
      : createLink.isError
        ? 'Something went wrong.'
        : null;

  return (
    <form className="card create-form" onSubmit={handleSubmit}>
      <label className="field">
        <span>Long URL</span>
        <input
          type="url"
          required
          placeholder="https://www.example.com/very/long/path"
          value={url}
          onChange={(e) => setUrl(e.target.value)}
        />
      </label>

      <label className="field">
        <span>Custom alias <em>(optional)</em></span>
        <input
          type="text"
          placeholder="my-link"
          value={customAlias}
          onChange={(e) => setCustomAlias(e.target.value)}
        />
      </label>

      <button
        type="button"
        className="link-button"
        onClick={() => setShowPlatforms((v) => !v)}
      >
        {showPlatforms ? '− Hide' : '+ Add'} platform-specific destinations
      </button>

      {showPlatforms && (
        <div className="platform-fields">
          <label className="field">
            <span>Android destination</span>
            <input
              type="url"
              placeholder="https://example.com/app.apk"
              value={androidUrl}
              onChange={(e) => setAndroidUrl(e.target.value)}
            />
          </label>
          <label className="field">
            <span>iOS destination</span>
            <input
              type="url"
              placeholder="https://example.com/app.ipa"
              value={iosUrl}
              onChange={(e) => setIosUrl(e.target.value)}
            />
          </label>
        </div>
      )}

      {errorMessage && <p className="error" role="alert">{errorMessage}</p>}

      <button type="submit" className="primary" disabled={createLink.isPending}>
        {createLink.isPending ? 'Shortening…' : 'Shorten'}
      </button>
    </form>
  );
}
LinkCard — ใช้ useSetStatus + useDeleteLink mutations:

Write LinkCard.tsx
117 lines
import { useState } from 'react';
import { QRCodeCanvas } from 'qrcode.react';
import { ApiError, type LinkResponse } from '../api';
import { useDeleteLink, useSetStatus } from '../hooks/useLinks';

interface Props {
  link: LinkResponse;
}

const PLATFORM_LABELS: Record<string, string> = {
  default: 'Default',
  android: 'Android',
  ios: 'iOS',
};

export function LinkCard({ link }: Props) {
  const [showQr, setShowQr] = useState(false);
  const [copied, setCopied] = useState(false);

  const setStatus = useSetStatus();
  const deleteLink = useDeleteLink();

  const isActive = link.status === 'Active';
  const busy = setStatus.isPending || deleteLink.isPending;

  const error =
    setStatus.error instanceof ApiError
      ? setStatus.error.message
      : deleteLink.error instanceof ApiError
        ? deleteLink.error.message
        : null;

  async function copy() {
    await navigator.clipboard.writeText(link.shortUrl);
    setCopied(true);
    setTimeout(() => setCopied(false), 1500);
  }

  function toggleStatus() {
    setStatus.mutate({ shortCode: link.shortCode, status: isActive ? 'Disabled' : 'Active' });
  }

  function remove() {
    if (!confirm(`Delete ${link.shortCode}? This cannot be undone.`)) return;
    deleteLink.mutate(link.shortCode);
  }

  const platformDestinations = Object.entries(link.destinations).filter(([p]) => p !== 'default');

  return (
    <article className={`card link-card ${isActive ? '' : 'is-disabled'}`}>
      <div className="link-card-main">
        <div className="link-card-info">
          <div className="short-row">
            <a href={link.shortUrl} target="_blank" rel="noreferrer" className="short-url">
              {link.shortUrl.replace(/^https?:\/\//, '')}
            </a>
            <span className={`badge ${isActive ? 'badge-active' : 'badge-disabled'}`}>
              {link.status}
            </span>
          </div>
          <div className="dest" title={link.destinations.default}>
            → {link.destinations.default}
          </div>
          {platformDestinations.length > 0 && (
            <ul className="platform-list">
              {platformDestinations.map(([platform, dest]) => (
                <li key={platform}>
                  <span className="platform-tag">{PLATFORM_LABELS[platform] ?? platform}</span>
                  <span className="platform-dest">{dest}</span>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="stats">
          <div className="stat">
            <span className="stat-value">{link.clickCount}</span>
            <span className="stat-label">clicks</span>
          </div>
        </div>
      </div>

      <dl className="meta">
        <div>
          <dt>Created</dt>
          <dd>{new Date(link.createdAt).toLocaleString()}</dd>
        </div>
        <div>
          <dt>Last visited</dt>
          <dd>{link.lastAccessedAt ? new Date(link.lastAccessedAt).toLocaleString() : '—'}</dd>
        </div>
      </dl>

      {error && <p className="error" role="alert">{error}</p>}

      <div className="actions">
        <button onClick={copy}>{copied ? 'Copied ✓' : 'Copy'}</button>
        <button onClick={() => setShowQr((v) => !v)}>{showQr ? 'Hide QR' : 'QR'}</button>
        <button onClick={toggleStatus} disabled={busy}>
          {isActive ? 'Disable' : 'Enable'}
        </button>
        <button className="danger" onClick={remove} disabled={busy}>
          Delete
        </button>
      </div>

      {showQr && (
        <div className="qr">
          <QRCodeCanvas value={link.shortUrl} size={140} />
        </div>
      )}
    </article>
  );
}
Build เช็ค type + bundle:

Bash Type-check and build frontend
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && npm run build 2>&1 | tail -12

OUT
> frontend@0.0.0 build
> tsc -b && vite build

vite v5.4.21 building for production...
transforming...
✓ 83 modules transformed.
rendering chunks...
computing gzip size...
dist/index.html                   0.47 kB │ gzip:  0.31 kB
dist/assets/index-fLzU9yV-.css    4.91 kB │ gzip:  1.55 kB
dist/assets/index-BXB0Y6Y6.js   207.24 kB │ gzip: 66.22 kB
✓ built in 766ms
Build ผ่าน. อัปเดต README (frontend structure + เหตุผล TanStack):

Edit README.md
Added 14 lines
Runtime check — vite dev boot ไม่ error:

Bash Boot vite dev server and check it serves
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && (npm run dev > "$TEMP/vite.log" 2>&1 &) ; echo "starting vite..."
for i in $(seq 1 15); do curl -s -o /dev/null http://localhost:5173 2>/dev/null && { echo "vite UP ${i}s"; break; }; sleep 1; done
echo "--- log ---"; cat "$TEMP/vite.log" | tail -8
curl -s http://localhost:5173 | grep -oE '<title>[^<]*</title>|/src/main.tsx'

OUT
starting vite...
vite UP 1s
--- log ---
> frontend@0.0.0 dev
> vite


  VITE v5.4.21  ready in 357 ms

  ➜  Local:   http://localhost:5173/
  ➜  Network: use --host to expose
<title>Shortly · URL Shortener</title>
/src/main.tsx
Vite serve ได้. หยุด dev server:

Bash Stop vite dev server by port
IN
PID=$(netstat -ano | grep ':5173' | grep LISTENING | awk '{print $NF}' | head -1); [ -n "$PID" ] && taskkill //F //PID $PID 2>/dev/null; echo "stopped vite (pid $PID)"

OUT
SUCCESS: The process with PID 37076 has been terminated.
stopped vite (pid 37076)
Update Todos

Install TanStack Query

Wrap app in QueryClientProvider (main.tsx)

Create useLinks hooks (query + mutations)

Refactor App/CreateLinkForm/LinkCard to hooks

Build passes + dev server boots

Update README frontend section
เสร็จ. Frontend ยกเป็น TanStack Query, build ผ่าน, dev server boot ได้.

3 ส่วนตามที่ขอ
Form สร้างลิงก์ — CreateLinkForm.tsx: URL + optional custom alias + toggle platform-specific (Android/iOS). ใช้ useCreateLink → isPending (ปุ่ม "Shortening…"), error (จาก ProblemDetails ผ่าน ApiError)
ตารางลิงก์ + สถิติ — App.tsx + LinkCard.tsx: clicks, created, last-visited, platform destinations, badge สถานะ. useLinks คุม isLoading/isError/empty
Disable/delete — LinkCard ใช้ useSetStatus + useDeleteLink → isPending disable ปุ่ม, error inline, confirm ก่อนลบ
โครงสร้าง
api.ts — fetch layer (คงเดิม, throw ApiError จาก ProblemDetails)
hooks/useLinks.ts — 1 query + 3 mutation, ทุก mutation invalidateQueries(['links']) sync list อัตโนมัติ
Component เหลือแค่ presentation — ไม่มี useState/useEffect จัดการ server state เอง
ทำไมเลือก TanStack Query (ไม่ใช่ custom hook)
เหตุผลจริง ไม่ใช่ตามเทรนด์:

links list = server state ไม่ใช่ UI state — fetch/mutate/cache. TanStack ออกแบบมาตรงนี้พอดี
Click count เปลี่ยนฝั่ง server (คนกดลิงก์ redirect) — staleTime สั้น + refetch-on-focus → กลับมาที่ tab แล้วสถิติอัปเดตเอง. custom hook ต้อง hand-roll logic นี้
Loading/error/refetch คือ boilerplate หลักของ UI นี้ — isLoading/isError/isPending/error ได้ฟรี ไม่ต้องเขียน try/catch/setState ซ้ำทุก component
Mutation → invalidate — เขียนบรรทัดเดียว list re-sync ไม่ต้อง manual splice/map state เอง (แบบเดิมที่ทำ)
Trade-off ที่ยอมรับ (เตรียมตอบสัมภาษณ์): 4 endpoints + dependency 66KB gzip อาจดู "หนัก". ถ้าซีเรียสเรื่อง bundle/dependency → custom useReducer hook ทำได้ แต่ต้องเขียน loading/error/refetch/invalidate เอง ~เท่าๆ กับที่ประหยัดไป. สำหรับ UI ที่แทบทั้งหมดคือ server-state sync — TanStack คุ้ม. นี่คือ judgment call ไม่ใช่ dogma

ยังไม่ได้ทำ
ยังไม่เปิด browser ดู UI จริง (build type-check + dev boot ผ่าน, API layer เดิมทดสอบแล้ว) — driver headless ทำ interaction จริงไม่ได้ตรงนี้
optimistic update ไม่ได้ทำ — ใช้ invalidate refetch ธรรมดา (ง่ายกว่า, ถูกต้องกว่าสำหรับ scope นี้; optimistic เป็น extension ที่คุยได้)
อยากให้เปิดทั้ง stack ดู UI จริงในเบราว์เซอร์ หรือ commit งานตอนนี้ไหม?

เพิ่ม copy-to-clipboard และ QR code (ใช้ qrcode.react) แล้วทำให้ responsive สวยงามด้วย Tailwind เน้น mobile-first
Copy + QR มีอยู่แล้ว. งานหลัก: migrate เป็น Tailwind mobile-first. ติดตั้ง Tailwind v4 (+ Vite plugin):

Bash Install Tailwind v4 and Vite plugin
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && npm install -D tailwindcss@4 @tailwindcss/vite@4 2>&1 | tail -3 && node -e "const d=require('./package.json').devDependencies; console.log('tailwind', d.tailwindcss, '| plugin', d['@tailwindcss/vite'])"

OUT
  npm audit fix --force

Run `npm audit` for details.
tailwind ^4.3.2 | plugin ^4.3.2
Read vite.config.ts
Write vite.config.ts
9 lines
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
})
index.css → Tailwind import + base:

Write index.css
9 lines
@import "tailwindcss";

/* Enable the built-in dark: variant to follow the OS theme. */
@variant dark (&:where(.dark, .dark *), @media (prefers-color-scheme: dark));

html {
  color-scheme: light dark;
}
Write index.css
7 lines
@import "tailwindcss";

/* Tailwind v4's dark: variant follows prefers-color-scheme by default. */
html {
  color-scheme: light dark;
}
Now App.tsx — Tailwind mobile-first:

Write App.tsx
58 lines
import { useLinks } from './hooks/useLinks';
import { CreateLinkForm } from './components/CreateLinkForm';
import { LinkCard } from './components/LinkCard';

export default function App() {
  const { data: links, isLoading, isError } = useLinks();

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100">
      <header className="px-4 pt-10 pb-6 text-center sm:pt-14">
        <h1 className="text-3xl font-bold tracking-tight sm:text-4xl">🔗 Shortly</h1>
        <p className="mt-2 text-slate-500 dark:text-slate-400">
          Turn long URLs into short, trackable links.
        </p>
      </header>

      <main className="mx-auto w-full max-w-2xl px-4 pb-20">
        <CreateLinkForm />

        <section className="mt-8">
          <h2 className="mb-4 flex items-center gap-2 text-lg font-semibold">
            Your links
            {links && links.length > 0 && (
              <span className="rounded-full bg-indigo-600 px-2 py-0.5 text-xs font-semibold text-white">
                {links.length}
              </span>
            )}
          </h2>

          {isLoading && <p className="text-slate-500 dark:text-slate-400">Loading…</p>}

          {isError && (
            <p className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-600 dark:bg-red-950/40 dark:text-red-400">
              Could not reach the API. Is the backend running on :5000?
            </p>
          )}

          {links && links.length === 0 && (
            <p className="rounded-xl border border-dashed border-slate-300 px-4 py-10 text-center text-slate-500 dark:border-slate-700 dark:text-slate-400">
              No links yet — create your first one above.
            </p>
          )}

          <div className="flex flex-col gap-4">
            {links?.map((link) => (
              <LinkCard key={link.shortCode} link={link} />
            ))}
          </div>
        </section>
      </main>

      <footer className="border-t border-slate-200 py-6 text-center text-xs text-slate-400 dark:border-slate-800">
        Full Stack Assignment · .NET + React
      </footer>
    </div>
  );
}
CreateLinkForm — Tailwind:

Write CreateLinkForm.tsx
129 lines
import { useState, type FormEvent } from 'react';
import { ApiError, type CreateLinkRequest } from '../api';
import { useCreateLink } from '../hooks/useLinks';

const inputClass =
  'w-full rounded-lg border border-slate-300 bg-slate-50 px-3 py-2.5 text-base ' +
  'text-slate-900 outline-none transition focus:border-indigo-500 focus:ring-2 ' +
  'focus:ring-indigo-500/30 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100';

const labelClass = 'flex flex-col gap-1.5 text-sm font-medium';

export function CreateLinkForm() {
  const [url, setUrl] = useState('');
  const [customAlias, setCustomAlias] = useState('');
  const [androidUrl, setAndroidUrl] = useState('');
  const [iosUrl, setIosUrl] = useState('');
  const [showPlatforms, setShowPlatforms] = useState(false);

  const createLink = useCreateLink();

  function handleSubmit(event: FormEvent) {
    event.preventDefault();

    const destinations: Record<string, string> = {};
    if (androidUrl.trim()) destinations.android = androidUrl.trim();
    if (iosUrl.trim()) destinations.ios = iosUrl.trim();

    const body: CreateLinkRequest = { url: url.trim() };
    if (customAlias.trim()) body.customAlias = customAlias.trim();
    if (Object.keys(destinations).length > 0) body.destinations = destinations;

    createLink.mutate(body, {
      onSuccess: () => {
        setUrl('');
        setCustomAlias('');
        setAndroidUrl('');
        setIosUrl('');
        setShowPlatforms(false);
      },
    });
  }

  const errorMessage =
    createLink.error instanceof ApiError
      ? createLink.error.message
      : createLink.isError
        ? 'Something went wrong.'
        : null;

  return (
    <form
      onSubmit={handleSubmit}
      className="flex flex-col gap-4 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-800 dark:bg-slate-900 sm:p-6"
    >
      <label className={labelClass}>
        <span>Long URL</span>
        <input
          type="url"
          required
          placeholder="https://www.example.com/very/long/path"
          value={url}
          onChange={(e) => setUrl(e.target.value)}
          className={inputClass}
        />
      </label>

      <label className={labelClass}>
        <span>
          Custom alias <span className="font-normal text-slate-400">(optional)</span>
        </span>
        <input
          type="text"
          placeholder="my-link"
          value={customAlias}
          onChange={(e) => setCustomAlias(e.target.value)}
          className={inputClass}
        />
      </label>

      <button
        type="button"
        onClick={() => setShowPlatforms((v) => !v)}
        className="self-start text-sm font-medium text-indigo-600 hover:underline dark:text-indigo-400"
      >
        {showPlatforms ? '− Hide' : '+ Add'} platform-specific destinations
      </button>

      {showPlatforms && (
        <div className="flex flex-col gap-3 rounded-xl border border-dashed border-slate-300 p-4 dark:border-slate-700">
          <label className={labelClass}>
            <span>Android destination</span>
            <input
              type="url"
              placeholder="https://example.com/app.apk"
              value={androidUrl}
              onChange={(e) => setAndroidUrl(e.target.value)}
              className={inputClass}
            />
          </label>
          <label className={labelClass}>
            <span>iOS destination</span>
            <input
              type="url"
              placeholder="https://example.com/app.ipa"
              value={iosUrl}
              onChange={(e) => setIosUrl(e.target.value)}
              className={inputClass}
            />
          </label>
        </div>
      )}

      {errorMessage && (
        <p role="alert" className="text-sm text-red-600 dark:text-red-400">
          {errorMessage}
        </p>
      )}

      <button
        type="submit"
        disabled={createLink.isPending}
        className="rounded-lg bg-indigo-600 px-4 py-3 font-semibold text-white transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {createLink.isPending ? 'Shortening…' : 'Shorten'}
      </button>
    </form>
  );
}
LinkCard — Tailwind, mobile-first (stats stack บนมือถือ, ข้างขวาบน desktop):

Write LinkCard.tsx
156 lines
import { useState } from 'react';
import { QRCodeCanvas } from 'qrcode.react';
import { ApiError, type LinkResponse } from '../api';
import { useDeleteLink, useSetStatus } from '../hooks/useLinks';

interface Props {
  link: LinkResponse;
}

const PLATFORM_LABELS: Record<string, string> = {
  default: 'Default',
  android: 'Android',
  ios: 'iOS',
};

const actionBtn =
  'rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium ' +
  'transition hover:border-indigo-500 disabled:cursor-not-allowed disabled:opacity-60 ' +
  'dark:border-slate-700 dark:bg-slate-800';

export function LinkCard({ link }: Props) {
  const [showQr, setShowQr] = useState(false);
  const [copied, setCopied] = useState(false);

  const setStatus = useSetStatus();
  const deleteLink = useDeleteLink();

  const isActive = link.status === 'Active';
  const busy = setStatus.isPending || deleteLink.isPending;

  const error =
    setStatus.error instanceof ApiError
      ? setStatus.error.message
      : deleteLink.error instanceof ApiError
        ? deleteLink.error.message
        : null;

  async function copy() {
    await navigator.clipboard.writeText(link.shortUrl);
    setCopied(true);
    setTimeout(() => setCopied(false), 1500);
  }

  function toggleStatus() {
    setStatus.mutate({ shortCode: link.shortCode, status: isActive ? 'Disabled' : 'Active' });
  }

  function remove() {
    if (!confirm(`Delete ${link.shortCode}? This cannot be undone.`)) return;
    deleteLink.mutate(link.shortCode);
  }

  const platformDestinations = Object.entries(link.destinations).filter(([p]) => p !== 'default');

  return (
    <article
      className={`flex flex-col gap-3 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm transition dark:border-slate-800 dark:bg-slate-900 sm:p-5 ${
        isActive ? '' : 'opacity-60'
      }`}
    >
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <a
              href={link.shortUrl}
              target="_blank"
              rel="noreferrer"
              className="break-all text-base font-semibold text-indigo-600 hover:underline dark:text-indigo-400"
            >
              {link.shortUrl.replace(/^https?:\/\//, '')}
            </a>
            <span
              className={`rounded-full px-2 py-0.5 text-[0.65rem] font-semibold uppercase tracking-wide ${
                isActive
                  ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-400'
                  : 'bg-slate-200 text-slate-500 dark:bg-slate-800 dark:text-slate-400'
              }`}
            >
              {link.status}
            </span>
          </div>

          <div
            className="mt-1 truncate text-sm text-slate-500 dark:text-slate-400"
            title={link.destinations.default}
          >
            → {link.destinations.default}
          </div>

          {platformDestinations.length > 0 && (
            <ul className="mt-2 flex flex-col gap-1">
              {platformDestinations.map(([platform, dest]) => (
                <li key={platform} className="flex items-center gap-2 text-xs">
                  <span className="shrink-0 rounded-md border border-slate-200 bg-slate-50 px-1.5 py-0.5 font-semibold dark:border-slate-700 dark:bg-slate-800">
                    {PLATFORM_LABELS[platform] ?? platform}
                  </span>
                  <span className="truncate text-slate-500 dark:text-slate-400">{dest}</span>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="flex shrink-0 items-baseline gap-2 rounded-xl bg-slate-50 px-3 py-2 dark:bg-slate-800 sm:flex-col sm:items-center sm:gap-0">
          <span className="text-2xl font-bold leading-none">{link.clickCount}</span>
          <span className="text-[0.65rem] uppercase tracking-wider text-slate-400">clicks</span>
        </div>
      </div>

      <dl className="flex gap-6 border-t border-slate-200 pt-3 text-xs dark:border-slate-800">
        <div>
          <dt className="text-slate-400">Created</dt>
          <dd className="mt-0.5">{new Date(link.createdAt).toLocaleString()}</dd>
        </div>
        <div>
          <dt className="text-slate-400">Last visited</dt>
          <dd className="mt-0.5">
            {link.lastAccessedAt ? new Date(link.lastAccessedAt).toLocaleString() : '—'}
          </dd>
        </div>
      </dl>

      {error && (
        <p role="alert" className="text-sm text-red-600 dark:text-red-400">
          {error}
        </p>
      )}

      <div className="flex flex-wrap gap-2">
        <button onClick={copy} className={actionBtn}>
          {copied ? 'Copied ✓' : 'Copy'}
        </button>
        <button onClick={() => setShowQr((v) => !v)} className={actionBtn}>
          {showQr ? 'Hide QR' : 'QR'}
        </button>
        <button onClick={toggleStatus} disabled={busy} className={actionBtn}>
          {isActive ? 'Disable' : 'Enable'}
        </button>
        <button
          onClick={remove}
          disabled={busy}
          className={`${actionBtn} text-red-600 hover:border-red-500 dark:text-red-400`}
        >
          Delete
        </button>
      </div>

      {showQr && (
        <div className="self-center rounded-xl bg-white p-3">
          <QRCodeCanvas value={link.shortUrl} size={140} />
        </div>
      )}
    </article>
  );
}
Build เช็ค Tailwind compile + types:

Bash Build with Tailwind
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && npm run build 2>&1 | tail -12

OUT
> frontend@0.0.0 build
> tsc -b && vite build

vite v5.4.21 building for production...
transforming...
✓ 83 modules transformed.
rendering chunks...
computing gzip size...
dist/index.html                   0.47 kB │ gzip:  0.30 kB
dist/assets/index-Rhi4pcxf.css   15.19 kB │ gzip:  3.96 kB
dist/assets/index-DhWR0xqt.js   210.06 kB │ gzip: 66.97 kB
✓ built in 744ms
Tailwind compile ผ่าน. ยืนยัน utility classes ถูก scan เข้า bundle จริง (กัน content-scan พลาด):

Bash Verify Tailwind classes present in built CSS
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && CSS=$(ls dist/assets/index-*.css); for c in "bg-indigo-600" "sm:flex-row" "dark:bg-slate-900" "opacity-60" "tracking-wider"; do grep -q "$c" "$CSS" && echo "✓ $c" || echo "✗ MISSING $c"; done

OUT
✓ bg-indigo-600
✗ MISSING sm:flex-row
✗ MISSING dark:bg-slate-900
✓ opacity-60
✓ tracking-wider
Bash Check escaped variant classes and media queries
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && CSS=$(ls dist/assets/index-*.css); \
echo "escaped variants (colon -> \\:):"; \
grep -oc 'sm\\:flex-row' "$CSS" | sed 's/^/  sm:flex-row count=/'; \
grep -oc 'dark\\:bg-slate-900' "$CSS" | sed 's/^/  dark:bg-slate-900 count=/'; \
echo "media queries present:"; grep -oc '@media' "$CSS" | sed 's/^/  @media count=/'; \
grep -oc 'prefers-color-scheme' "$CSS" | sed 's/^/  prefers-color-scheme count=/'

OUT
escaped variants (colon -> \:):
  sm:flex-row count=0
  dark:bg-slate-900 count=0
media queries present:
  @media count=1
  prefers-color-scheme count=1
CSS แปลกๆ — media query น้อยเกิน. ดูของจริงว่า v4 emit variants ยังไง:

Bash Check for variant substrings in built CSS
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && CSS=$(ls dist/assets/index-*.css); \
echo "=== raw checks ==="; \
grep -oc 'flex-row' "$CSS" | sed 's/^/flex-row substrings=/'; \
grep -oc 'slate-900' "$CSS" | sed 's/^/slate-900 substrings=/'; \
grep -oc 'min-width' "$CSS" | sed 's/^/min-width=/'; \
grep -oc 'flex-col' "$CSS" | sed 's/^/flex-col=/'; \
echo "=== file size ==="; wc -c "$CSS"

OUT
=== raw checks ===
flex-row substrings=1
slate-900 substrings=1
min-width=1
flex-col=1
=== file size ===
15185 dist/assets/index-Rhi4pcxf.css
Bash Count actual occurrences with fixed-string grep
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && CSS=$(ls dist/assets/index-*.css); \
echo "occurrences (not lines — file is minified to 1 line):"; \
printf '  min-width blocks: '; grep -o 'min-width' "$CSS" | wc -l; \
printf '  prefers-color-scheme: '; grep -o 'prefers-color-scheme' "$CSS" | wc -l; \
printf '  .sm\\:flex-row selector: '; grep -oF '.sm\:flex-row' "$CSS" | wc -l; \
printf '  .dark\\:bg-slate-900 selector: '; grep -oF 'dark\:bg-slate-900' "$CSS" | wc -l

OUT
occurrences (not lines — file is minified to 1 line):
  min-width blocks: 2
  prefers-color-scheme: 1
  .sm\:flex-row selector: 1
  .dark\:bg-slate-900 selector: 1
ยืนยันแล้ว — .sm\:flex-row, .dark\:bg-slate-900 มีจริง, 2 min-width media query, prefers-color-scheme ครบ (ก่อนหน้าเข้าใจผิดเพราะ CSS minify เป็นบรรทัดเดียว + grep escape ผิด). Tailwind ทำงานถูกต้อง.

อัปเดต README ระบุ Tailwind:

Edit README.md
Added 1 line
Edit README.md
Modified
Update Todos

Install Tailwind v4 + Vite plugin

Configure vite + index.css for Tailwind

Convert App/CreateLinkForm/LinkCard to Tailwind (mobile-first)

Verify build + classes generated in bundle

Update README
เสร็จ. Frontend เป็น Tailwind v4 mobile-first, build ผ่าน, utility classes เข้า bundle ครบ.

สิ่งที่ทำ
Copy + QR (มีอยู่แล้ว คงไว้/polish):

Copy — navigator.clipboard.writeText + feedback "Copied ✓" 1.5s
QR — qrcode.react QRCodeCanvas toggle ต่อลิงก์, พื้นขาวเสมอ (สแกนได้ทั้ง dark mode)
Tailwind v4 migration:

ติดตั้ง tailwindcss@4 + @tailwindcss/vite@4, plugin ใน vite.config.ts, @import "tailwindcss" ใน index.css
ลบ CSS เขียนมือ 400 บรรทัดทิ้ง → utility classes ล้วน
Dark mode ผ่าน dark: variant (follow OS prefers-color-scheme อัตโนมัติ ไม่ต้อง config)
Mobile-first (base = มือถือ, sm: ขยายจอใหญ่):

Card: flex-col → sm:flex-row (stats stack ใต้ข้อมูลบนมือถือ, ย้ายไปขวาบน desktop)
Stat block: items-baseline แนวนอนมือถือ → sm:flex-col แนวตั้ง desktop
Container max-w-2xl, padding p-4 → sm:p-5/6, ปุ่ม flex-wrap ไม่ล้นจอแคบ
Input text-base (16px กัน iOS zoom ตอน focus), touch target py-2.5/3
ยืนยัน
npm run build ผ่าน — CSS 15KB (gzip 3.96KB)
Grep bundle: .sm\:flex-row, .dark\:bg-slate-900, 2 min-width media query, prefers-color-scheme — ครบ (content scan ไม่พลาด)
หมายเหตุ
ยังไม่เปิด browser ดูจริง (build + type-check + class-in-bundle ผ่าน) — headless ไม่มี screenshot tool. อยากให้เปิด full stack (backend :5000 + vite :5173) ให้ทดสอบเองไหม
Design ใช้ indigo accent, slate neutrals, rounded-2xl cards, badge สี emerald/slate ตามสถานะ
อยากให้เปิด stack ดู UI, หรือ commit งานทั้งหมด (backend+frontend+tests+README) ตอนนี้เลยไหม?

e:\coding\url_link_shortener\url_link_shortener\frontend\package-lock.json
ยกตัวอย่าง url ที่ผิดให้หน่อย ฉันจะดูการโยน error
URL/alias ที่ทำให้ error — เรียงตามที่ระบบปฏิเสธ:

400 Bad Request (validation)
URL ผิด:

Input	เหตุผล
example.com	ไม่มี scheme (ไม่ absolute)
javascript:alert(1)	scheme อันตราย
ftp://files.com	ไม่ใช่ http/https
file:///etc/passwd	scheme ต้องห้าม
not a url	parse ไม่ได้
/relative/path	relative
"" / เว้นวรรค	ว่าง
http://localhost:5000/abc	ชี้กลับ short domain เอง (loop)
Alias ผิด:

Input	เหตุผล
ab	สั้นไป (<3)
bad!alias / has space / with.dot	อักขระพิเศษ
api / openapi / health	reserved route
ยาวเกิน 30 ตัว	เกิน limit
409 Conflict
custom alias ที่มีอยู่แล้ว (เช่นสร้าง gulf ซ้ำ)
ยิงเทสเร็ว (curl)
ต้อง start backend ก่อน: dotnet run --project backend/src/UrlShortener.Api --urls "http://localhost:5000"


# no scheme -> 400
curl -i -X POST http://localhost:5000/api/links -H "Content-Type: application/json" -d '{"url":"example.com"}'

# javascript scheme -> 400
curl -i -X POST http://localhost:5000/api/links -H "Content-Type: application/json" -d '{"url":"javascript:alert(1)"}'

# self-loop -> 400
curl -i -X POST http://localhost:5000/api/links -H "Content-Type: application/json" -d '{"url":"http://localhost:5000/x"}'

# reserved alias -> 400
curl -i -X POST http://localhost:5000/api/links -H "Content-Type: application/json" -d '{"url":"https://ok.com","customAlias":"api"}'
ดูใน UI: พิมพ์ example.com (ไม่มี https://) ในช่อง Long URL แล้วกด Shorten → error แดงใต้ฟอร์ม "A valid absolute http/https URL is required."

หมายเหตุ: ช่อง input เป็น type="url" → browser อาจ block ก่อนถึง backend สำหรับบางเคส. อยากเทส backend ตรงๆ ใช้ curl ชัวร์กว่า. ให้ผม start backend แล้วยิงให้ดูเลยไหม?


image.png
451×340
ปรับสี theme ui ให้เป็นสี green และ darkblue ให้หน่อย และนำภาพนี้ไปเป็น logo ให้ด้วย
สีในโลโก้: green ~#4EA866, dark blue ~#0A4A6E. ทำ 3 อย่าง — สร้าง logo SVG, ตั้ง brand color ใน theme, replace indigo accent.

โลโก้ SVG (public + component):

Write logo.svg
5 lines
<svg viewBox="0 0 100 80" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="Shortly logo">
  <path d="M49 73 C40 56 27 44 12 34 C26 47 40 59 49 73 Z" fill="#0a4a6e"/>
  <path d="M49 73 C55 51 72 25 92 9 C83 39 68 57 49 73 Z" fill="#4ea866"/>
</svg>
Write Logo.tsx
14 lines
interface Props {
  className?: string;
}

/** Two-leaf checkmark mark in the brand navy + green. */
export function Logo({ className = 'h-10 w-auto' }: Props) {
  return (
    <svg viewBox="0 0 100 80" className={className} role="img" aria-label="Shortly logo">
      <path d="M49 73 C40 56 27 44 12 34 C26 47 40 59 49 73 Z" fill="#0a4a6e" />
      <path d="M49 73 C55 51 72 25 92 9 C83 39 68 57 49 73 Z" fill="#4ea866" />
    </svg>
  );
}
favicon:

Edit index.html
Modified
brand colors ใน theme:

Write index.css
16 lines
@import "tailwindcss";

/* Brand palette (from the leaf logo): green + dark blue. */
@theme {
  --color-brand-green: #4ea866;
  --color-brand-green-dark: #3f8f54;
  --color-brand-navy: #0a4a6e;
  --color-brand-navy-dark: #073650;
  --color-brand-navy-light: #2b7fb0;
}

/* Tailwind v4's dark: variant follows prefers-color-scheme by default. */
html {
  color-scheme: light dark;
}
App.tsx — logo + navy title + navy badge:

Edit App.tsx
Added 1 line
Edit App.tsx
Added 5 lines
Edit App.tsx
Modified
CreateLinkForm — green accent (focus ring, link, submit):

Edit CreateLinkForm.tsx
Modified
Edit CreateLinkForm.tsx
Modified
Edit CreateLinkForm.tsx
Modified
LinkCard — navy links, green accents, green active badge:

Edit LinkCard.tsx
Modified
Edit LinkCard.tsx
Modified
Edit LinkCard.tsx
Modified
ลบ vite.svg เดิม + build เช็ค brand color compile:

Bash Remove old favicon and build
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && rm -f public/vite.svg && npm run build 2>&1 | tail -8

OUT
transforming...
✓ 84 modules transformed.
rendering chunks...
computing gzip size...
dist/index.html                   0.47 kB │ gzip:  0.31 kB
dist/assets/index-tbei9E7j.css   15.05 kB │ gzip:  3.91 kB
dist/assets/index-FMNmKXkP.js   210.48 kB │ gzip: 67.13 kB
✓ built in 768ms
Bash Verify brand colors and logo in build output
IN
cd e:/coding/url_link_shortener/url_link_shortener/frontend && CSS=$(ls dist/assets/index-*.css); \
for c in "#4ea866" "#0a4a6e" "brand-green" "brand-navy"; do printf '  %-14s ' "$c:"; grep -oF "$c" "$CSS" | head -1 | grep -q . && echo present || echo MISSING; done; \
echo "logo.svg in dist:"; ls dist/logo.svg 2>/dev/null && echo ok || echo MISSING

OUT
  #4ea866:       present
  #0a4a6e:       present
  brand-green:   present
  brand-navy:    present
logo.svg in dist:
dist/logo.svg
ok
เสร็จ. Theme เป็น green + dark blue, logo จากภาพติดตั้งแล้ว. Build ผ่าน, brand color + logo เข้า bundle ครบ.

ที่ทำ
Logo:

public/logo.svg — สร้างใหม่เป็นใบไม้ 2 ใบ (navy #0a4a6e + green #4ea866) รูปเช็ค ตามภาพ
Logo.tsx — inline SVG component, scalable
ใช้ใน header คู่กับชื่อ "Shortly" (แทน emoji 🔗 เดิม)
ตั้งเป็น favicon ด้วย (index.html) + ลบ vite.svg เดิมทิ้ง
Brand theme (index.css @theme):


brand-green: #4ea866  (+ dark #3f8f54)
brand-navy:  #0a4a6e  (+ dark, light #2b7fb0)
Replace indigo → brand:

จุด	สีใหม่
ปุ่ม Shorten, focus ring, "add platform" link	green
ชื่อ Shortly (header), count badge	navy
short-url link	navy (light: #2b7fb0 ใน dark mode)
active badge	green tint
hover border ปุ่ม action	green
delete	แดง (คงเดิม)
ยืนยัน
build ผ่าน, CSS 15KB
grep bundle: #4ea866, #0a4a6e, brand-green, brand-navy present; logo.svg copy เข้า dist
โลโก้ที่ผมวาดเป็น approximation ของภาพ (bezier 2 ใบ) — ใกล้เคียงแต่ไม่ pixel-perfect. ถ้าอยากได้เป๊ะ ส่งไฟล์ SVG/PNG ต้นฉบับมา ผม swap ให้ตรงเลย.

อยากให้เปิด stack ดู UI จริง หรือ commit งานทั้งหมดตอนนี้ไหม?

ตรวจสอบไฟล์ README.md อีกรอบว่าครบถ้วนทุก list ในไฟล์โจทย์สัมภาษณ์เรียบร้อยแล้ว
Read README.md
อ่านครบแล้ว. เทียบกับโจทย์ — required ครบทุก list แต่เจอ ข้อมูลไม่ตรงกับโค้ดจริง หลังจาก refactor (README อ้างไฟล์ที่ลบไปแล้ว). แก้ให้ตรง:

Edit README.md
Added 1 line
Edit README.md
Modified
Edit README.md
Modified
ยืนยันว่า tree ใน README ตรงกับไฟล์จริง:

Bash List actual source files to verify README tree
IN
cd e:/coding/url_link_shortener/url_link_shortener/backend && echo "=== backend src ===" && find src/UrlShortener.Api -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' | sort
echo "=== frontend src ===" && find ../frontend/src -type f -not -path '*/node_modules/*' | sort
echo "=== ai-logs ===" && ls ../ai-logs

OUT
=== backend src ===
src/UrlShortener.Api/Common/ResultExtensions.cs
src/UrlShortener.Api/Core/Link.cs
src/UrlShortener.Api/Core/LinkStatus.cs
src/UrlShortener.Api/Core/Persistence/ILinkRepository.cs
src/UrlShortener.Api/Core/Persistence/InMemoryLinkRepository.cs
src/UrlShortener.Api/Core/Platform.cs
src/UrlShortener.Api/Core/Platform/IPlatformDetector.cs
src/UrlShortener.Api/Core/Platform/UserAgentPlatformDetector.cs
src/UrlShortener.Api/Core/Result.cs
src/UrlShortener.Api/Core/ShortCodes/AliasRules.cs
src/UrlShortener.Api/Core/ShortCodes/CustomAliasGenerator.cs
src/UrlShortener.Api/Core/ShortCodes/GenerationRequest.cs
src/UrlShortener.Api/Core/ShortCodes/IShortCodeGenerator.cs
src/UrlShortener.Api/Core/ShortCodes/RandomShortCodeGenerator.cs
src/UrlShortener.Api/Core/ShortCodes/ShortCodeGeneratorResolver.cs
src/UrlShortener.Api/Core/Validation/IUrlValidator.cs
src/UrlShortener.Api/Core/Validation/UrlValidator.cs
src/UrlShortener.Api/Features/Links/Contracts.cs
src/UrlShortener.Api/Features/Links/LinkMapper.cs
src/UrlShortener.Api/Features/Links/LinkService.cs
src/UrlShortener.Api/Features/Links/LinksEndpoints.cs
src/UrlShortener.Api/Features/Links/ShortUrlOptions.cs
src/UrlShortener.Api/Features/Redirect/RedirectEndpoints.cs
src/UrlShortener.Api/Program.cs
=== frontend src ===
../frontend/src/App.tsx
../frontend/src/api.ts
../frontend/src/components/CreateLinkForm.tsx
../frontend/src/components/LinkCard.tsx
../frontend/src/components/Logo.tsx
../frontend/src/hooks/useLinks.ts
../frontend/src/index.css
../frontend/src/main.tsx
../frontend/src/vite-env.d.ts
=== ai-logs ===
README.md
ตรวจครบ. README tree ตรงกับไฟล์จริงทุกไฟล์แล้ว. สรุปเทียบทีละ list ในโจทย์:

Section 3 (REQUIRED) — ครบ
โจทย์ต้องมี	README	สถานะ
Project code (.NET + React, repo เดียว, รันได้, มีขั้นตอน)	Getting started	✅
Unit tests ครอบคลุม core logic	Testing	✅
— short-code generation	✅ ระบุชัด	
— URL validation	✅	
— click counting	✅ (+ 10k concurrent)	
— disable/delete behavior	✅	
API contract	API contract section	✅
how to run & test	Getting started + Testing	✅
key design decisions	Design decisions (9 หัวข้อ)	✅
how you'd extend	How I'd extend it	✅
Section 2 — 5 capabilities ครบใน Features table
create+alias ✅ · stats ✅ · disable/delete ✅ · platform-specific ✅ · pluggable code ✅

Section 8 (Submission) — เกือบครบ
install/run/test + API contract ✅
AI session logs ⚠️ — ai-logs/ มีแค่ README placeholder ยังไม่มี log จริง
ที่แก้รอบนี้ (README ไม่ตรงโค้ดหลัง refactor)
ลบ DomainExceptions.cs → ใส่ Result.cs (ตรงกับที่เปลี่ยนเป็น Result pattern)
DomainExceptionHandler.cs → ResultExtensions.cs
เพิ่ม AliasRules.cs, Logo.tsx ใน tree
test count 63 → 87
Node 18+ → 20+ (Tailwind v4)
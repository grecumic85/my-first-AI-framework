# SOLID and software engineering in concrete code

SOLID is used to make changes local and dependencies visible. It is not a claim that every type needs an interface or that inheritance is always wrong.

## Single responsibility

| Type | Owns | Kept elsewhere |
|---|---|---|
| BrowserFactory | Driver configuration/creation | Session disposal, page selectors and assertions |
| BrowserSessions | Driver lifetime and current-session selection | Widget behavior |
| BrowserWait | Readiness predicates and polling | Business outcomes |
| MonthNavigator | Bounded month traversal | Site markup and range business rules |
| GlobalSqaPage | Iframe, jQuery date-picker markup and format | Browser startup |
| BearerHandler | Authorization header | Retry and auditing |
| ReadRetryHandler | Read-request retry policy | Authentication acquisition |
| HttpAuditHandler | Request outcome metadata | Response/body serialization |
| Repository | Query/write mechanics | Browser state and NUnit |
| TestScope | Composition and owned lifetime | New application's workflows |

TestScope deliberately has several collaborators because composition is its responsibility. It is not a dumping ground for new-app business helpers. Site pages contain Expect methods in these examples, mixing page operations and observable postconditions for readability; larger suites may separate assertion objects without changing the navigation contract.

## Open/closed

New token providers can implement ITokenProvider and be used by ApiClientFactory. A custom browser factory can implement IBrowserFactory and be passed to BrowserSessions. A new calendar implementation can implement IMonthCalendar and reuse MonthNavigator without changing its algorithm. Generated service additions use manifest entries.

This does not make every extension edit-free: TestScope has a concrete authentication-mode switch and constructs BrowserFactory. Adding a default auth mode requires composition/settings changes. Extensibility is placed at useful seams rather than promised universally.

## Liskov substitution

![Calendar interface and implementations](diagrams/10-calendar-classes.svg)

An IMonthCalendar implementation must report the first day of the currently visible month; MoveMonth(-1/+1) must finish at exactly the adjacent month or fail; ClickDay must select an available day in the displayed month or fail. It must not silently choose an adjacent-month duplicate or set a hidden model value.

DomMonthCalendar implements that contract through WebDriver and condition waits. The local FakeCalendar validates traversal, no-op behavior, budget rejection, stuck movement and overshoot detection. It is a test double for the navigation algorithm, not proof of website correctness.

A disabled date may throw through readiness timeout; the interface does not promise every civil date is selectable. Range selection and time editing are therefore separate site capabilities. Substitution is a behavioral contract and needs tests; merely implementing an interface is insufficient.

## Interface segregation

IReadRepository and IWriteRepository let read-only consumers avoid a write API. IElementWait exposes common element conditions without all concrete wait extensions. IMonthCalendar does not impose text-entry, reset, range or timezone methods. IHttpEmulator and IEmulatorAdmin remain distinct because they represent different services.

A useful interface is one a consumer needs. Avoid an interface for every DTO or a giant IFramework that exposes all tools.

## Dependency inversion

High-level helpers receive transport/provider abstractions: HttpClient, HttpMessageHandler, ITokenProvider, IDbContextFactory, IWebDriver and IBrowserFactory. MonthNavigator depends on IMonthCalendar, not CSS or Selenium. Generated clients accept HttpClient instead of finding a global singleton.

The composition root selects concrete implementations. The page adapters still use concrete BrowserWait, and TestScope uses concrete local result publishing. These are practical limits; introduce further ports when a real substitution need appears.

## Other engineering practices

| Practice | Concrete example | Why it matters |
|---|---|---|
| Composition over inheritance | HTTP DelegatingHandlers and calendar selector delegates | Add policies without deep base-class chains |
| Page Object / Component | SitePages and DomMonthCalendar | Localize markup and formatting changes |
| Strategy | IMonthCalendar, ITokenProvider, IBrowserFactory | Swap behavior at stable boundaries |
| Factory | DatabaseFactory, BrowserFactory | Keep construction decisions explicit |
| Adapter | MailHog, admin emulator, publishers | Translate external protocols into focused methods |
| Dependency lifecycle | IAsyncDisposable and owned scopes | Teardown is part of correctness |
| Compensation | CleanupScope reverse order | Undo owned setup when later steps fail |
| Design by contract | Date/mode/path validation; exact month transitions | Fail close to an invalid assumption |
| DRY | One scenario catalog for NUnit and runner | Fix behavior once; avoid diverging test bodies |
| Separation of pure and effectful code | CalendarDates/MonthNavigator vs DOM adapter | Fast local checks plus real browser verification |
| Reproducible inputs | Versioned contracts, package locks, recorded seed | Explain and reproduce relevant inputs |
| Observability | Structured events, PNG/HTML, per-case JSON | Diagnose timeouts and preserve failure context |
| Least implicit state | No global current driver/token/entity registry | Prevent order-dependent tests |

## Failure semantics

Assertions and infrastructure failures are not converted to success. Missing widgets produce timeouts with the locator/expected condition. Invalid selectors surface immediately. External site consent is handled through normal controls; overlays are not removed by script. No CAPTCHA or browser security interstitial is bypassed.

HTTP retries and state polling differ: retry repeats a read request; polling repeats an observation until a predicate holds. Replaying a write is not a polling strategy. Register compensation only after success, and use unique names so teardown removes only owned resources.

## What this architecture does not prove

The starter is not a full domain-driven design, CQRS/event-sourcing implementation or microservice platform. It does not establish browser compatibility merely by offering three driver factories. It does not prove production database behavior with SQLite. It does not make public websites deterministic. These boundaries keep the claims reviewable.

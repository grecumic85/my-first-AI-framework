# Source analysis and extraction map

The source was inspected read-only. The repository contains 4,761 tracked files, a .NET 6-oriented framework, NUnit/Reqnroll tests, Selenium 4.39, generated Swagger/Refit service families, EF Core 7/Npgsql repositories, HTTP emulators and reporting tools. The archive is a refactored starter, not a copy of the original domain test suite. Handwritten starter implementations adapt the original behaviors to neutral interfaces and sample contracts.

Paths below are relative to `Phoenix.AutomatedTests/` in the source unless otherwise stated. The report records representative implementations reviewed for each capability; it does not claim a line-by-line audit of every business test or generated DTO.

| Source and observed behavior | Starter destination and adaptation |
|---|---|
| `Framework/Settings.cs`: common, DB/emulator, local JSON and environment overrides | `Automation.Core/Settings.cs`: typed groups, consistent precedence, environment prefix, validation, no private URLs |
| `Framework/Utils/WaitUtils.cs`, `Retry.cs`: condition polling and selective API retries | `Poll.cs`, `HttpPipeline.cs`: cancellable deadline polling and bounded read-only retries; no mutable global error list |
| `API.Common/Generators/generate-codegen.ps1`, `generate-Optic-API.ps1`, Centric/Sedna generation scripts | `tools/ApiGenerator`, `contracts/`, `scripts/Generate-Api.ps1`: multi-service manifest, pinned NSwag, injected HttpClient, generated interfaces/DTOs |
| `API.Common/AccessTokenClient.cs`: token endpoint and multiple authentication flows | `Tokens.cs`: client-credentials and externally supplied bearer tokens, serialized cache refresh; app-specific login flows require new-app configuration |
| `API.Common/Refit/ErrorHandlingMessageHandler.cs`, `CustomApiException.cs`, generated client infrastructure | `HttpPipeline.cs`, `JsonApiClient.cs`, generated exceptions: response validation, metadata audit, authentication and cancellation |
| `PageObjects/BasePage.cs`, `Common/BaseHasElements.cs` | `BasePage`, `PageComponent`, `ItemsPage`: injected browser/waits, explicit navigation, locator-based components |
| `PageObjects/Common/Wait.cs`, `WebDriverWaitExtensions.cs` | `BrowserWait.cs`: presence, visibility, text predicates, enabled/disabled, disappearance, stable lists, stable unobscured clickability, document/URL readiness |
| `Wait.cs`: animation by position/height and overlay checks | Bounding-box stability includes location and size; avoids sleeps nested inside nested waits |
| `WebDriverExtensions.cs`: pending requests, JS actions, uploads, drag/drop, browser storage/download helpers | `NetworkTracker.cs`, `BrowserActions.cs`, `Downloads.cs`; explicit instrumentation scope, managed Grid downloads and standard WebDriver APIs |
| `WebDriverManager.cs`: lazy driver creation, multi-session switching, pooled browsers, Grid capacity | `BrowserSessions.cs`, `GridCapacity.cs`: lazy creation, multiple isolated sessions, guaranteed teardown attempts, browser-specific capacity probe; cross-test pooling removed |
| `Screenshoter.cs`, `DownloadedFileUtils.cs`, request log hooks | `BrowserDiagnostics`, `ArtifactDirectory`, `JsonEventSink`, downloads: per-test paths, failure PNG/HTML, completed-file checks, ZIP inspection |
| `Database.Optic/Phoenix/GenericRepository.cs`: save operations and wait-for-update | Generic `Repository<TContext,TEntity>`: detached reads and fresh contexts every poll; optional writes; asynchronous EF operations |
| `Database.Optic/*/*DbContext.cs`, generated entities and repository selects; `Database.Optic/Readme.md` | Sample context, PostgreSQL/SQLite factories, transactions; `Scaffold-Database.ps1` generates new schema code without connection strings or force-overwriting handwritten code |
| `Tests.Api/Principal Integration/Common/BaseWireMockTest.cs` | `HttpEmulator`: request/response mappings, delayed responses, stateful fault sequences, request capture/count polling, negative observation window, owned random-port server |
| Same WireMock base class: derive mapping from a Refit request | `RequestCapture`: transport capture from any HttpClient-based generated client; returns method/path/query/body without a network call |
| `Steps.Common/Components/Emulator/EmulatorClient.cs` and benchmark/settings models | `AdminEmulatorClient`: transaction-settings CRUD, benchmark CRUD/filtering and owned cleanup with neutral DTOs; demo implements the sample contract |
| `Steps.Common/Helpers/TestDataTracker.cs`, `DeleteTestDataAttribute.cs` | `CleanupScope`: per-test reverse-order asynchronous compensation, aggregation, no domain-specific static dictionaries |
| `Steps.Common/Steps/PortCallStepsCreate.cs`, flow settings and data population scripts | `DataFlow`, `TestData`: composable setup steps, compensation, seeded choices and unique resource names; business lifecycle transitions are extension work |
| `Tests.UI.OneCape/ReqnrollHooks.cs`, `HookStepsCommon.cs`, test attributes | `AutomationTest`, `TestScope`, BDD hooks: runner lifecycle, no-browser scenarios, category filtering, case-ID metadata and local reports |
| `Steps.Common/Steps/EmailSteps.cs` | `MailhogClient`: recipient/subject polling, search and owned-message deletion; create a unique recipient per test |
| `Steps.UI/Common/Utils/PdfUtils.cs` | `Automation.Documents/PdfText.cs`: disposable PDF text extraction via PdfPig, independently tested; no application PDFs or commercial license files |
| `Framework/Components/Zephyr`, `TestRail`, `Jira` | Explicit TestRail and Zephyr Enterprise publishers, Jira status reader, generic webhook publisher; no account details or automatic remote posting |
| `Framework/Components/Scheduler/SchedulerModule.cs` | `SchedulerClient`: injected endpoint/client, awaited trigger, response checks and cancellation |
| `Tests.UI.LPA.Performance`, `PerfData` | `PerformanceProbe` and `DataFlow`: sequential latency statistics and data setup primitives; application-specific performance journeys are not transferable |
| Root `selenium-grid-4.yml`, `bitbucket-pipelines.yml`, CI trigger scripts | Local Compose services, locked dependencies, GitHub Actions validation and portable PowerShell entry points; no company CI identifiers |

## Changes that matter when migrating

- Swagger Codegen 2.x, hand-edited generated RestSharp code and Java-based scripts are replaced with NSwag C# generation. This preserves API generation as a capability, not binary/source compatibility with the old generated SDK. Fix specifications or add wrappers instead of restoring manual edits after generation. APIs described by OpenAPI 3.1 need compatibility review with the pinned generator.
- Framework runtime moves from .NET 6 to .NET 8, and EF Core/provider versions are upgraded together. Business EF entities are regenerated from the new app's database; source database mappings are deliberately not renamed and reused.
- The old application emulator is a separate server not implemented by this repository. The generic admin client and sample server preserve its control pattern, but payload shapes and routes must match your new emulator. Generic settings stored by the demo are not a financial transaction simulator.
- WireMock's in-process .NET server is used instead of requiring a shared remote emulator. For a remote application, host an emulator reachable from that application and implement the same `IHttpEmulator` boundary or use WireMock's admin API. A per-test loopback URL is useful only when the caller can reach it.
- Catch-all retry, nested waits, ignored HTTP status codes, indefinite/shared state, raw API body logging and global emulator resets are not retained.
- The original UI library has many custom widgets tied to specific CSS/frameworks. The starter carries the page/component pattern and interaction primitives; dropdowns, grids and editors should be implemented against the new app's markup.

## Deliberately excluded or requiring application-specific work

Old page selectors, API DTOs/endpoints, users, role IDs, financial/shipping workflows, fixtures/uploads, database entities, private URLs, credentials, production-like datasets, Jira/Zephyr IDs, CI tokens and trigger scripts are excluded. Only neutral examples are bundled.

The source includes an Android APK and emulator-launch batch file in upload test data. Those are application artifacts, not a portable mobile automation implementation for a new web app. They are not included. This starter's emulator features are the HTTP/WireMock and admin-service mechanisms actually used by the reviewed web/API framework.

Specialized business document generation, image/OCR comparison, vendor SDKs, application-specific performance scripts, password-grant login/impersonation and full custom-widget libraries need new-app contracts and are not advertised as complete here. PDF text extraction, core browser/file operations and timing primitives are included.

The archive contains source and dependency manifests, not `.git` history or installed third-party binaries. Initialize a fresh Git repository after extraction. See `NOTICE.md` for provenance and dependencies.

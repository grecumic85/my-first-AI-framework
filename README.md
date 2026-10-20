# Web Automation Starter

A reusable C# automation repository adapted . It includes source, generated API code, a local demonstration application, NUnit and Reqnroll examples, and an extraction report. The original application's business objects, private environments, credentials, generated domain SDKs and database schemas are not required.

Start with the [offline architecture handbook](docs/handbook.html), [architecture reading guide](docs/ARCHITECTURE.md), and [validation results](docs/VALIDATION.md). The expanded documentation includes high- and low-level designs, 12 diagrams, architecture decisions, concrete SOLID examples, and a new-app implementation recipe.

## Public date-picker examples

The starter includes **19 Selenium scenarios** for Lastest, GlobalSQA, Practice Automation and Automation Testing UK. They cover leap years, month/year boundaries, disabled weekends, reversed ranges, reset isolation, iframe/dropdown/icon controls, committed values, and midnight/23:59 time entry. All 19 passed in headless Chrome during preparation. See the [complete runbook and coverage matrix](docs/DATE-PICKERS.md).

```powershell
dotnet restore WebAutomation.sln --locked-mode --configfile NuGet.Config
dotnet build WebAutomation.sln -c Release --no-restore
dotnet run --project tools/DatePickerRunner -c Release --no-build -- --self-test
dotnet run --project tools/DatePickerRunner -c Release --no-build -- --live
```

Use `--live --site globalsqa` or `--live --case UK-05` for focused runs. The runner and NUnit fixture share the same scenario bodies. Public cases are opt-in; the default framework checks do not require these sites. An explicit manual GitHub Actions workflow is included.

## Quick start

Prerequisites: .NET SDK 8.0 or a newer SDK capable of building `net8.0`, and the .NET/ASP.NET Core 8 runtime. Chrome is needed only for UI tests. Public NuGet access is needed for the first restore. Java, a production application, PostgreSQL and Docker are not needed for the default tests.

Run these commands from the extracted repository root:

```powershell
dotnet restore WebAutomation.sln --locked-mode --configfile NuGet.Config
dotnet build WebAutomation.sln --no-restore
dotnet run --project tools/SmokeCheck --no-build --no-restore
dotnet test WebAutomation.sln --no-build --no-restore --filter 'TestCategory!=UI'
```

Default tests start and stop their own random-port sample HTTP servers and WireMock instances. Database tests create separate SQLite files below `artifacts`. Nothing connects to the original system. The standalone `SmokeCheck` executable validates the core features without test discovery; add `-- --ui` to its `dotnet run` command to include headless Chrome. All nine standalone checks passed during preparation. NUnit test discovery encountered a Windows assembly-access error; see the validation report before interpreting test-suite status.

To regenerate the bundled API client:

```powershell
dotnet run --project tools/ApiGenerator -- contracts/services.json
dotnet build WebAutomation.sln
```

To run real browser examples against a self-hosted local sample:

```powershell
$env:AUTOMATION_RUN_UI = '1'
dotnet test tests/Automation.Tests --filter 'TestCategory=UI'
Remove-Item Env:AUTOMATION_RUN_UI
```

Selenium Manager resolves the appropriate local driver and may download it. These sample UI tests require a local browser; a Grid container cannot reach a loopback-only test server. [Grid setup](docs/EXTENDING.md#selenium-grid) explains the change needed for remote application tests.

To explore the sample by hand:

```powershell
dotnet run --project samples/SampleApp --urls http://localhost:5080
```

Open `http://localhost:5080/`. This is a disposable demonstration app with in-memory data and unauthenticated local admin endpoints; it is not a production web application.

## Use with your new application

1. Copy `settings.local.example.json` to `settings.local.json`; set the application's web and API base URLs. Both must end in `/`.
2. Put your OpenAPI documents under `contracts/`, update `contracts/services.json`, and run the generator. Put custom service methods in another project or partial-class files, outside generated `.g.cs` files.
3. Add page objects based on `BasePage` and reusable components based on locators. Use the `ItemsPage` example as a starting point.
4. Add tests inheriting `AutomationTest`, or Reqnroll bindings with one `TestScope` per scenario. Register cleanup immediately after creating a resource.
5. For database access, scaffold the new schema and compose typed repositories with a context factory. Explicitly enable writes only for intended test-data operations.
6. Configure and instantiate the integrations your application needs. Result publishing is local JSON by default; external publishers are never automatically enabled.

The demonstration tests deliberately target their own sample app even when URLs are configured. Add your application's tests in a separate test project; do not mistake passing sample tests for coverage of your app.

## Configuration

Precedence: defaults -> `settings.json` -> `settings.{AUTOMATION_ENVIRONMENT}.json` -> `settings.local.json` -> `AUTOMATION_` environment variables. The environment defaults to `local`.

```powershell
$env:AUTOMATION_ApiBaseUrl = 'https://your-app.example/api/'
$env:AUTOMATION_Auth__Mode = 'ClientCredentials'
$env:AUTOMATION_Auth__TokenEndpoint = 'https://identity.example/connect/token'
$env:AUTOMATION_Auth__ClientId = 'automation-client'
# Set AUTOMATION_Auth__ClientSecret via your local environment or CI secret store.
```

`AutomationTest` reads configuration from the test output directory. Link `settings*.json` into new test projects with `CopyToOutputDirectory`, or construct `AutomationSettings` from an explicit directory. The sample NUnit project copies only the safe shared defaults; set environment variables or explicitly link your local overrides when using that project. No secrets are committed.

## What's included

| Area | Implementation |
|---|---|
| SOLID architecture | Small interfaces, injected dependencies, separate infrastructure libraries, explicit test ownership |
| API generation | Manifest-driven NSwag generator; generated interfaces, clients and DTOs; sample contract and client |
| HTTP | OAuth client-credentials cache, static tokens, cancellation, bounded read retries, metadata-only audit log, typed errors |
| Selenium | Chrome/Edge/Firefox, local/Grid, multiple sessions, pages/components, explicit waits, DOM recovery, overlay and animation checks |
| Database | EF Core/Npgsql, SQLite example, generic repositories, fresh-context polling, rollback/commit transactions, schema scaffold script |
| Emulation | Isolated WireMock servers, faults/delays/state sequences, request journal and assertions, generated-call capture; admin settings/benchmarks client |
| Test lifecycle | NUnit and Reqnroll, `NoBrowser`, reverse-order cleanup, unique artifacts, failure screenshots/HTML, local result JSON |
| Supporting tools | MailHog, PDF text extraction, downloads/ZIP inspection, uploads/drag-and-drop/windows/frames, Grid capacity, scheduler trigger |
| Reporting | TestRail, Zephyr Enterprise, Jira status lookup, generic webhook adapters |
| Test data and timing | Unique names, seeded data, compensating setup flows, sequential latency measurement |
| Delivery | Locked dependency files, Docker Compose, GitHub Actions, scripts and onboarding documentation |

Target framework is `net8.0` to match a runtime available in the inspected workstation. This is a compiled and smoke-checked compatibility baseline, not a promise of the newest dependencies or an indefinite support window. Choose the supported .NET baseline for your new app before long-term adoption and update SDK, EF/provider, generator and CI versions together.

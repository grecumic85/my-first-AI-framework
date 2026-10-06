# Extending the starter

## API generation and services

Add an entry to `contracts/services.json`:

```json
[
  { "name": "Sample", "spec": "sample.openapi.json" },
  { "name": "Identity", "spec": "identity.openapi.json" }
]
```

The generator reads local OpenAPI documents, validates service names and path containment, generates all clients in memory, then writes `src/Automation.Generated/{Name}.g.cs`. It does not download private contracts, update external docs, modify handmade files or silently delete removed-service outputs. If removing a service, remove its old `.g.cs` file deliberately. Review generated changes and compile clients before committing. Commit the specs, manifest, generated source and lock files together.

```csharp
using var http = ApiClientFactory.Create(baseUri, tokenProvider, eventSink);
ISampleClient api = new SampleClient(http);
var item = await api.CreateItemAsync(new CreateItem { Name = "unique-name" }, token);
cleanup.Register("created item", ct => api.DeleteItemAsync(item.Id, ct));
```

The generated API paths are relative and use the supplied HttpClient base address. Check whether the new specification already includes `/api`; avoid duplicating that path in `ApiBaseUrl`. `Automation.Generated.Sample.ApiException` represents generated endpoint errors; `ApiFailureException` belongs to the handwritten `JsonApiClient`. Do not mix them when asserting exceptions.

For multiple user identities, create separate token providers and HTTP clients. `ClientCredentialsTokenProvider` owns its cache/gate but not its injected HttpClient. Dispose the provider and clients after in-flight work finishes. Browser login belongs in application page/flow code or use a token provider appropriate to the application's identity system.

## Selenium waits and pages

```csharp
var waits = new BrowserWait(scope.Browsers.Current, scope.Settings.Wait);
waits.Visible(By.CssSelector("[data-testid=page-ready]"));
waits.Gone(By.CssSelector("[data-testid=loading]"));
waits.Text(By.Id("status"), text => text == "Completed");
waits.Click(By.CssSelector("button[type=submit]"));
```

Use semantic readiness markers and application-specific loading locators. `Gone` can pass immediately if a loader has not appeared yet; combine it with the expected final state or track the request initiated by the action. `StableList` stabilizes count/text, not every possible DOM property. A clear clickability hit-test cannot guarantee an overlay will not appear between waiting and clicking; failures remain visible instead of replaying a submission.

`PageComponent.Find` reacquires the component root and child during polling. Add component-specific methods rather than caching IWebElements. Use `BrowserActions` for upload, drag/drop, new windows and frames; switch back to default content/window as part of each flow. Use `Browsers.Start()` and `SwitchTo(index)` for independent users in one test.

## Selenium Grid

```powershell
docker compose up -d selenium
$env:AUTOMATION_Browser__GridUrl = 'http://localhost:4444/'
```

Grid sessions enable managed downloads; `Downloads.FetchRemote` retrieves a named remote file. A local download path is meaningful only to a local driver. The optional `GridCapacity` helper polls `/status` before requesting a browser, but it does not reserve a slot. Session creation has a command timeout and relies on Grid's queue.

The bundled sample tests bind their app to `127.0.0.1` on a random port. They are local-browser examples. To test via Docker Grid, run the app separately on an interface reachable from the container and create tests using that URL (for example `host.docker.internal` on Docker Desktop), or deploy the app and Grid on the same Docker network. Do not pass the test runner's `localhost` URL to a remote browser.

Compose includes PostgreSQL and MailHog. Set `POSTGRES_PASSWORD` in `.env` before starting PostgreSQL. Services bind host ports to loopback. `docker compose down` stops services while keeping the database volume; deleting volumes is a separate deliberate action.

## Database

```csharp
var factory = DatabaseFactory.Create(settings.Database);
IReadRepository<SampleRecord> records = new Repository<ExampleDbContext, SampleRecord>(factory);
var row = await records.WaitForAsync(x => x.Id == expectedId, TimeSpan.FromSeconds(15), token);
```

Reads are detached and use a fresh context. `IWriteRepository<T>` is an explicit opt-in surface: construct the repository with `allowWrites: true` only where intended. `DatabaseTransaction.ExecuteAsync` rolls back by default; `commit: true` is an explicit write decision. Retried transaction delegates must contain only database work: no HTTP calls, email, or non-idempotent external effects.

For a new schema, set `AUTOMATION_Database__ConnectionString` and run:

```powershell
./scripts/Scaffold-Database.ps1 -Schemas public,identity -ContextName ApplicationDbContext
```

The script creates a new timestamped `Generated/Scaffold_*` directory. Review/replace previous generated snapshots deliberately; retaining multiple snapshots with the same class/namespace causes duplicate types. `--no-onconfiguring` prevents a connection string being written to generated code, but the EF CLI receives it as a process argument. Handwritten repositories stay outside generated folders. Use `Repository<ApplicationDbContext, YourEntity>` with a corresponding factory. Generic repository queries replace the source's repeated per-entity generated select wrappers.

`EnsureCreatedAsync` is only used for disposable SQLite tests. The framework never migrates or creates your application's schema automatically. PostgreSQL execution, permissions and scaffolding must be validated against your intended test database.

## Emulators and test-data flows

```csharp
await using var emulator = new HttpEmulator();
var id = emulator.Stub("POST", "/webhook", 202, "{}", TimeSpan.FromMilliseconds(100));
// Configure the application under test to send this test's webhook to emulator.BaseUrl.
var requests = await emulator.WaitForRequestsAsync(id, 1, TimeSpan.FromSeconds(10));
```

`Sequence` models failure then recovery. `AssertNoRequestsAsync` watches for the entire observation window. `RequestCapture` derives a method/path/query/body template from a generated-client invocation without networking; supply a canned response compatible with its return type. `Stub` matches method and path only: add more advanced WireMock matching when body, header or query discrimination is required.

Use `AdminEmulatorClient` for the sample transaction/benchmark admin protocol. Give every setting a unique name; `SetOwnedTransactionAsync` assumes the caller owns that name and does not restore an existing shared setting. Do not overwrite other tests' settings. Register compensations with `CleanupScope` for API/database resources as soon as they exist. `DataFlow` executes named creation steps and leaves cleanup ownership to the surrounding scope.

## Reporting, email and files

`TestScope` always writes a local result JSON. To publish externally, inject/configure an `ITestResultPublisher` in your test lifecycle and explicitly call `PublishAsync` after computing the final outcome. Configure authentication on that publisher's dedicated HttpClient. TestRail's base address should end in `/index.php?/api/v2/`; Zephyr Enterprise's base address should end in its API root `/`. Configure Zephyr status IDs for your instance. Scale/Squad APIs require a different adapter. Jira lookup is read-only and does not rewrite test results.

MailHog's HTTP API and SMTP service are separate endpoints. Configure your application's SMTP to the mailbox container, then poll using a unique recipient per test. The adapter returns the raw stored body; use a MIME library if your assertions require decoded multipart/quoted-printable content.

For PDF text, use `IPdfTextExtractor`. It does not OCR scanned pages or perform visual comparison. Local downloads wait for the final file, absence of partial suffixes, exclusive readability and stable size. Use a fresh directory or unique name to avoid matching an old file. ZIP inspection lists entries without extracting them.

## CI and dependency updates

The sample GitHub Actions job runs restore in locked mode, regenerates clients, checks for drift and runs non-browser tests. It does not deploy or post results to another system. Add a separate UI job for your chosen browser/Grid setup. `NUnit` fixtures can opt into parallel execution once their resources and setup are isolated; per-test fixture lifecycle is supplied where mutable test fields are used.

Commit updated `packages.lock.json` files when intentionally changing package versions. Keep Selenium WebDriver/Support and EF Core/provider/tool versions compatible. Test a new .NET baseline before changing the common target and CI SDK. No package is claimed to remain current or suitable indefinitely.

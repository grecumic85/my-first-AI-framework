# Implementing your new application's tests

## Start with one vertical journey

Choose a small user outcome that has API setup, a UI action and an observable result. Define its contract before adding broad helper abstractions. Keep the public date-picker examples as references; put your application's pages/tests in a separate project.

1. Extract the archive and initialize a new Git repository. Keep package locks and generated source under version control.
2. Run restore/build and SmokeCheck. Review VALIDATION.md to separate local checks from unvalidated integrations.
3. Configure web/API URLs and authentication using environment variables or uncommitted local settings.
4. Add your OpenAPI file and service entry, generate the client and inspect the diff.
5. Add a page object that accepts a driver and BrowserWait. Use stable test IDs owned by your app.
6. Create data through the API, register cleanup immediately, perform the UI action, and verify by an explicit app signal.
7. Add a database or emulator assertion only when it adds coverage the UI/API cannot provide.
8. Run locally, then configure CI artifacts and explicitly selected external integrations.

## A composition sketch

The following illustrates extension points; OrdersClient, OrdersPage and the Orders database belong to your future app and are not bundled types.

```csharp
// Illustrative new-app code: supply the named application types.
var api = new OrdersClient(scope.ApiHttp);
var order = await api.CreateAsync(newOrder, token);
scope.Cleanup.Register($"order {order.Id}", ct => api.DeleteAsync(order.Id, ct));

var page = new OrdersPage(scope.Browsers.Current,
    new BrowserWait(scope.Browsers.Current, scope.Settings.Wait));
page.OpenOrder(order.Id);
page.Submit();
page.ExpectSubmitted(order.Id); // Wait on a meaningful state, not a sleep.
var saved = await readRepository.WaitForAsync(
    row => row.Id == order.Id && row.Status == "Submitted",
    TimeSpan.FromSeconds(20), token);
```

API creation and database observation use the same identifier. Do not generate a second unrelated random name for each layer. A failure after creation still triggers registered deletion.

## Adding a custom calendar

For a month-grid control, implement IMonthCalendar in your app's page/component project. Report the visible month as DateOnly(year, month, 1). Move one month and wait for its header. Resolve the full target date and use BrowserWait.Click. Reuse MonthNavigator.

For a range widget, keep the range state machine at the page/scenario layer. Define whether a reversed second click sorts endpoints, restarts selection or rejects the value. Lastest and the UK site intentionally demonstrate different rules. Add time editing only for widgets that expose it.

For a native HTML date input, use browser-appropriate user input and assert both its ISO value and the application's committed response. Do not set the hidden widget model or remove readonly/disabled attributes and then claim UI coverage. The current public suite targets the custom calendars and does not promise cross-browser native-popup automation.

## Integrating emulation

Start HttpEmulator, define the expected upstream response and configure your application's upstream URL. Register the emulator's disposal with the test's cleanup owner if it must outlive the scenario body. Assert the request journal after triggering the application.

RequestCapture can derive a template from a generated client operation without network traffic. Add explicit query/body matching when needed; Stub's simple overload matches method/path. Use AdminEmulatorClient only when your emulator server implements the documented settings/benchmarks contract.

## Database ownership

Create application-specific entities/context and pass a context factory to Repository. Keep ordinary assertions on IReadRepository. Enable write repositories deliberately for test setup. Use DatabaseTransaction for DB-only work; its rollback cannot undo mutations sent over HTTP. Run provider-specific integration checks before relying on PostgreSQL behavior.

## CI rollout

Start with restore, deterministic generation diff, build, local navigation checks, SmokeCheck and your isolated app tests. Add UI jobs with browser/Grid capacity and artifact upload. Keep the public demo workflow manual unless your team deliberately chooses a schedule. The supplied manual workflow publishes no business results to third-party reporting tools.

When adding a reporting publisher, define ownership of its HttpClient, required case IDs, failure behavior and data retention. Default TestScope output remains local JSON until you explicitly compose an external publisher.

## Extension review checklist

- Does the new helper have one clear responsibility and explicit dependencies?
- Are waits tied to observable completion, with bounded time and clear messages?
- Can setup create duplicate side effects after a retry?
- Is every owned resource registered for cleanup immediately after creation?
- Are browser, context and token lifetimes isolated appropriately?
- Does the test assert a business result, not only the value it just assigned?
- Are contract changes reflected in generated source and documentation?
- Is validation evidence honest about the environment and executed cases?

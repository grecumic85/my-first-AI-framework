# Architecture decisions

These records explain the starter's choices. They are decisions made for this extraction and its examples, not claims that every original class followed the same rules.

## ADR-01: Modular libraries with an explicit composition root

**Context:** UI, API, database and emulator code change for different reasons. The original framework mixed some helpers with global state and runner concerns.

**Decision:** Keep technical capabilities in separate libraries and compose them per test in TestScope. Use constructor injection at transport/provider boundaries.

**Why:** A new app can adopt only the capabilities it needs. Ownership is visible and collaborators can be replaced in focused tests.

**Alternatives considered:** One large base-test class makes dependencies implicit. A DI container would provide richer registration but adds indirection to a small starter.

**Cost:** TestScope currently chooses concrete defaults. It must be edited or replaced to inject a custom browser factory/publisher. If composition grows, introduce a scope factory before introducing service location.

## ADR-02: Fresh browser state per test

**Context:** Reusing a browser can preserve cookies, local storage, service workers, tabs and account state.

**Decision:** BrowserSessions owns multiple sessions only within one test; a new test starts fresh.

**Why:** Isolation is easier to reason about than incomplete reset logic.

**Cost:** Startup is slower. Use Grid capacity and test partitioning to scale; do not add pooling without an explicit, verified reset contract.

## ADR-03: Observe conditions; execute mutations once

**Context:** Rendering is asynchronous and a timeout after a submission does not prove the submission failed.

**Decision:** Poll readiness with explicit deadlines. Re-find stale elements. Perform a click once. Retry only bodyless HTTP GET/HEAD under a bounded transient-error policy.

**Why:** This handles eventual state without duplicating writes. Calendar navigation separately verifies each month transition.

**Cost:** A late overlay can still intercept a click after readiness. Capture the failure and fix the readiness signal rather than globally retrying clicks.

## ADR-04: Generate SDKs from versioned contracts

**Context:** Copying old generated service families would bring old business DTOs, endpoints and toolchains.

**Decision:** Use pinned NSwag, a multi-service manifest, generated interfaces and injected HttpClient. Keep generated code isolated.

**Why:** The new app supplies its own OpenAPI contracts. The transport can be authenticated, logged, emulated or captured independently.

**Cost:** This is not source/binary compatibility with the original Swagger/Refit clients. Generator upgrades can change signatures; review diffs and run contract checks. OpenAPI dialect compatibility must match the pinned generator.

## ADR-05: Short-lived EF contexts and rollback by default

**Context:** Tracked entities and shared DbContexts cause stale reads and concurrency problems.

**Decision:** Create a context per operation/poll; expose separate read/write interfaces; make transaction commit explicit.

**Why:** Observations see fresh database state and context lifetime is clear.

**Cost:** More context creation. SQLite tests do not replace PostgreSQL integration checks. The transaction helper's direct write capability is independent of repository allowWrites.

## ADR-06: Two distinct emulator mechanisms

**Context:** HTTP fault simulation and application-emulator administration solve different problems.

**Decision:** Keep owned WireMock servers and an IEmulatorAdmin HTTP client separate.

**Why:** WireMock tests protocol responses and journals. Admin settings/benchmarks configure a separate service contract. Combining them into one interface would imply unsupported substitutability.

**Cost:** The new application must be wired to its chosen upstream simulator. The sample admin contract is an example, not a copied business emulator.

## ADR-07: Share the smallest calendar capability

**Context:** Lastest has range-specific rules and disabled weekends; GlobalSQA uses iframes; Practice uses a different popup; UK uses Flatpickr date/time/range widgets.

**Decision:** Share only IMonthCalendar, bounded MonthNavigator and DomMonthCalendar transition mechanics. Keep range, text entry, reset, iframe and time methods on site pages.

**Why:** A single enormous IDatePicker would force widgets to fake unsupported capabilities. Full-date selectors prevent adjacent-month ambiguity.

**Cost:** Four adapters remain. Site changes still require maintenance. Month headers use observed English locale; new locales need explicit parsing/formatting.

## ADR-08: One scenario implementation, two execution front ends

**Context:** The workstation's NUnit adapter failed loading the earlier test assemblies before discovery.

**Decision:** Put the 19 scenario bodies in DatePickerScenarios. NUnit and DatePickerRunner both call them; the runner also performs eight local algorithm checks.

**Why:** Real Selenium behavior can be validated independently of discovery, without duplicating test logic.

**Cost:** The console runner is not a substitute for NUnit discovery, selection, reporting or CI validation. It still references Automation.Testing and transitively NUnit. It is deliberately sequential.

## ADR-09: Public-site cases are opt-in

**Context:** Availability, advertising, consent and markup of external demos are outside this repository's control.

**Decision:** NUnit requires AUTOMATION_LIVE_DATEPICKERS=1. The runner requires --live. The manual workflow runs the runner and uploads results.

**Why:** Local framework checks should remain usable without four public websites. An external failure is preserved as a failure, never silently converted into a passing/ignored assertion.

**Cost:** Live-site coverage must be scheduled or invoked deliberately by the adopting team. Passing sample tests says nothing about your new app.

## ADR-10: Documentation is delivered in source and rendered forms

**Decision:** Ship Markdown, editable Mermaid UML, standalone SVG diagrams and an offline HTML handbook. Keep explanations tied to actual class names and include limits beside each mechanism.

**Why:** Reviewers can read the design without installing a renderer and developers can evolve diagrams with code.

**Cost:** Visual assets and diagram source must change together. The handbook builder renders the checked-in SVGs; it does not interpret Mermaid or prove diagram/source equivalence.

# Validation report

## Edition 2: documentation and public date pickers

Validated on 2026-09-14 on Windows, .NET SDK 9.0.317, .NET runtime 8.0.30 and local headless Chrome.

| Check | Result |
|---|---|
| Locked restore of the 17-project solution | Passed |
| Final full Release build | Passed: zero warnings, zero errors |
| Fresh ZIP extraction, locked restore and full Release build | Passed: zero warnings, zero errors; 8/8 local navigation checks also passed from extraction |
| Local calendar-navigation checks | 8/8 passed |
| Lastest live scenarios | 5/5 passed |
| GlobalSQA live scenarios | 5/5 passed |
| Practice Automation live scenarios | 4/4 passed |
| Automation Testing UK live scenarios | 5/5 passed |
| NUnit discovery of the new calendar fixture | Failed before discovery: local assembly-access error |

The **19/19 live result is aggregated from the latest successful case executions across three batches**, not one final all-site invocation. All cases executed their real Selenium scenario bodies via DatePickerRunner. The final GlobalSQA and UK batches verified the corrected widget-specific adapters. [date-picker-results.json](date-picker-results.json) records case IDs, descriptions, durations, observation timestamps, batch provenance and final scenario-source hashes. [navigation-checks.txt](navigation-checks.txt) records the eight local checks.

Initial live runs exposed abbreviated GlobalSQA month names, input-specific Escape handling, and the older Flatpickr time field's normalization. These adapter issues were corrected and affected scenarios rerun successfully. Passing checks were not produced by weakening expected dates or by assigning hidden widget state.

The final build includes all test assemblies. An explicit NUnit discovery check on Automation.DatePickers.dll reproduced the same FileLoadException / Access is denied seen in the earlier starter. Therefore **no successful NUnit execution is claimed** on this workstation. The console runner executes the same scenario bodies but does not validate the NUnit adapter. The manual GitHub workflow was added but not dispatched remotely.

Documentation includes Markdown, 12 editable Mermaid sources, 12 SVG diagrams and the generated offline handbook. Static file/link/SVG checks are recorded in documentation-checks.json. Browser preview of the local HTML file was blocked by the browser tool's file-URL policy; no rendered-browser visual check is claimed.

The archive candidate was extracted into a new directory with no bin/obj state. Locked restore and compilation of all 17 projects succeeded there, including regeneration of Reqnroll build output. Its local navigation checks also passed. Final packaging changes after that check are documentation/evidence updates only. Package-Starter.ps1 verifies every source payload against MANIFEST.sha256 and writes an archive SHA-256 sidecar.

The original repository was not used as a build/output directory. A fresh SHA-256 scan compared all **7,162 non-Git files: zero changed, added or removed**; Git working-tree status also remained clean. See [source-integrity-edition2.json](source-integrity-edition2.json). The archive excludes raw browser pages/screenshots, local result directories, personal paths in runner logs, installed dependencies and build outputs; it includes safe summarized results.

## Original starter baseline (retained evidence)

Prepared on 2026-09-14 on Windows with .NET SDK 9.0.317, targeting .NET 8.

## Source preservation

The original repository was read-only throughout preparation. SHA-256 comparison of all 7,162 non-`.git` files found **zero added, removed or changed files**. Git working-tree status was clean. Source commit: `b1ba35fa81fddf74790d4f9d0fd80c8dd07b333d`. Only the separate output directory and a temporary validation copy were written.

## Successful checks

- Locked dependency restore and full 14-project Release compilation: **zero warnings and zero errors**, including generated API code, NUnit tests, Reqnroll generated bindings, database/emulator adapters and sample app. See [build output](build-check.txt).
- API generation executed successfully using pinned NSwag. Regenerating `Sample.g.cs` produced the identical SHA-256 hash.
- The sample app started on loopback and its health, create/read/delete API and HTML endpoint passed a direct HTTP check.
- All **nine standalone smoke checks passed**, including the real headless Chrome check. See [machine-readable results](smoke-results.json).

| Smoke check | Result |
|---|---|
| Generated API client create/read/delete and typed 404 | Passed |
| Condition polling and deadline | Passed |
| Reverse-order cleanup that continues after a failure | Passed |
| WireMock request journal and webhook payload | Passed |
| Stateful emulator failure/recovery with HTTP retry | Passed |
| Admin emulator settings and benchmark lifecycle/cleanup | Passed |
| SQLite repositories, eventual reads and rollback transaction | Passed |
| Extraction of text from a real generated PDF | Passed |
| Headless Chrome page readiness, stable click/list, network idle and completed CSV download | Passed |

Run these checks independently of NUnit:

```powershell
dotnet run --project tools/SmokeCheck -- --ui
```

Omit `-- --ui` to run the eight non-browser checks. Each invocation starts an isolated sample application and writes `artifacts/smoke-check-*/smoke-results.json`. This verifies framework examples, not the behavior of your new application.

## NUnit/Reqnroll execution limitation

Both test projects compiled, but the NUnit adapter failed **before discovery/execution** while loading the test assemblies:

```text
NUnit.Engine.NUnitEngineException: An exception occurred in the driver while loading tests.
System.IO.FileLoadException: Could not load file or assembly '...Automation.Tests.dll'. Access is denied.
```

The same error occurred for the BDD assembly, after an unsandboxed retry, a Release build with compiler-server reuse disabled, and a separate temporary validation copy. The underlying cause was not established. These are test-loader failures, not passing tests or failed application assertions. No claim is made that the NUnit/Reqnroll suite executed successfully on this machine. The standalone checks above did execute the framework implementations and a real browser successfully.

The source contains 35 non-UI NUnit cases, three optional UI cases, and one BDD scenario. Run the suite in your normal development/CI environment and resolve any discovery/assembly-access issue before relying on its results. The GitHub Actions workflow is supplied but was not run remotely during preparation.

## External validation not performed

PostgreSQL connectivity/scaffolding and live Docker Grid/MailHog services were not exercised; the workstation had no running Docker daemon. SQLite was exercised. The Grid capacity and MailHog protocol tests compile but remain subject to the NUnit limitation above. No live TestRail, Zephyr, Jira, scheduler or OAuth service was contacted. Their adapters require your endpoints, authentication and contract checks. Edge, Firefox, remote downloads and separate-user browser sessions are included but were not exercised by the successful Chrome smoke check.

The archive excludes build outputs, installed packages, local databases, browser downloads, test-runner artifacts, local credentials and original application data. It includes safe summarized smoke results and the source integrity summary. Reqnroll `.feature.cs` files are regenerated by build and are excluded from the archive.

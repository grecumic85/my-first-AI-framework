# Provenance and dependencies

This starter was prepared from a user-requested analysis of the existing automation framework. Its handwritten code refactors and generalizes the behaviors identified in `docs/EXTRACTION.md`. The original repository was not edited. No original source license grant has been invented or changed; apply your organization's intended repository license before redistribution.

Generated sample SDK code is produced by the pinned NSwag toolchain and retains its generated headers. NuGet packages are dependencies, not vendored source or binaries. Their licenses and notices remain those of their respective owners. Consult package metadata and lock files for the exact dependency tree.

Primary implementation references consulted:

- Selenium wait guidance: https://www.selenium.dev/documentation/webdriver/waits/
- NSwag C# client settings: https://github.com/RicoSuter/NSwag/wiki/CSharpClientGenerator
- EF Core scaffolding: https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/
- Docker Selenium pinned release: https://github.com/SeleniumHQ/docker-selenium/releases/tag/4.39.0-20251212
- PdfPig source and documentation: https://github.com/UglyToad/PdfPig

These references informed the generalizations; source-specific behaviors are attributed separately in the extraction report.

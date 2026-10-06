# Starter edition 2

- Expanded architecture documentation into a 10-chapter offline handbook, high-/low-level designs, architecture decisions, concrete SOLID explanations, extension recipes and validation evidence.
- Added 12 editable Mermaid diagrams and standalone SVG reading assets.
- Added 19 real Selenium calendar scenarios for the four requested demo sites, with a shared scenario catalog, four site adapters, bounded month navigation, NUnit integration and a standalone runner.
- Added eight local checks for traversal, no-op navigation, budget rejection, stuck/overshooting widgets, leap-century rules and year-crossing weekend normalization.
- Added a manual public-site CI workflow, documentation generation/check scripts and an archive builder that verifies every payload hash.
- Preserved the core API generation, database, emulator, Selenium, integration and cleanup capabilities.

All 19 live scenarios passed through the console runner in headless Chrome. The final solution builds with zero warnings/errors. The local NUnit assembly-loading limitation remains documented. See docs/VALIDATION.md for exact evidence and scope.

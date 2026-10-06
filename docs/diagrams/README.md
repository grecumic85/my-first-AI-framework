# UML and architecture diagram catalog

Each diagram has an editable Mermaid source (.mmd) and an offline SVG illustration. Class and sequence diagrams use UML conventions; context/module/deployment/activity views are simplified architecture views. Arrows indicate the relationships named in each diagram. Filled diamonds indicate ownership; hollow triangles indicate inheritance/implementation. Dotted dependency arrows do not imply ownership.

- [System context: optional observation channels](01-context.svg) | [Mermaid source](01-context.mmd)
- [Main library dependencies (simplified)](02-modules.svg) | [Mermaid source](02-modules.mmd)
- [Deployment: local process and optional services](03-deployment.svg) | [Mermaid source](03-deployment.mmd)
- [UML classes: scope ownership](04-scope-classes.svg) | [Mermaid source](04-scope-classes.mmd)
- [UML sequence: test result and cleanup](05-lifecycle.svg) | [Mermaid source](05-lifecycle.mmd)
- [Activity: wait before one click](06-wait-activity.svg) | [Mermaid source](06-wait-activity.mmd)
- [UML sequence: generated API runtime](07-api.svg) | [Mermaid source](07-api.mmd)
- [UML sequence: fresh database observations](08-database.svg) | [Mermaid source](08-database.mmd)
- [Emulator roles and ownership](09-emulation.svg) | [Mermaid source](09-emulation.mmd)
- [UML classes: reusable calendar navigation](10-calendar-classes.svg) | [Mermaid source](10-calendar-classes.mmd)
- [State model: Lastest two-click range](11-range-state.svg) | [Mermaid source](11-range-state.mmd)
- [UML sequence: selecting an exact calendar date](12-calendar-sequence.svg) | [Mermaid source](12-calendar-sequence.mmd)

SVGs are checked-in reading assets, maintained alongside Mermaid sources. The handbook builder embeds the SVGs; it does not run a Mermaid parser. Render/edit .mmd with your preferred Mermaid-capable editor. Update both representations when changing design. The module diagram intentionally omits some transitive references; the HLD project table and .csproj files give the full project inventory.

The range-state diagram models only the transitions covered by the Lastest tests. It does not specify equal-endpoint behavior or third-click behavior after a completed range. Flatpickr's reversed-range sorting is a different rule and is tested separately.

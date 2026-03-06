# DL-002: Hybrid Execution Model

**Date:** 2026-03-05
**Status:** Accepted
**Author:** Chris Lyons

## Context

Parsed commands need to become actions. Options: all SDK calls, all GP tools, or hybrid.

## Decision

Hybrid — direct Pro SDK calls for simple operations (navigation, layer management, symbology, selection by attribute); GP tools via `Geoprocessing.ExecuteToolAsync()` for spatial analysis (buffer, clip, select by location, dissolve, merge, export).

## Alternatives Considered

- **SDK-only** — Faster, no GP tool overhead. But spatial analysis operations (buffer, clip) are complex to implement purely in SDK and would reinvent existing GP tools. Rejected.
- **GP-only** — Simpler architecture but unnecessarily slow for operations like toggling layer visibility. Rejected.

## Consequences

- Simple operations execute instantly (no GP tool overhead)
- Spatial analysis leverages battle-tested GP tools
- Two execution paths to maintain and test
- GP tools respect environment settings (overwrite, workspace) which may need management

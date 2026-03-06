# DL-003: Always Preview Before Executing

**Date:** 2026-03-05
**Status:** Accepted
**Author:** Chris Lyons

## Context

When the LLM parses a command, should it execute immediately or show a preview first?

## Decision

Always show a structured preview in the ProWindow before execution. Every command requires the user to click [Execute]. No auto-execute mode.

## Alternatives Considered

- **Auto-execute for non-destructive commands** — Faster workflow, but users can't verify the LLM interpreted correctly. Rejected.
- **Configurable safety level** — Adds settings complexity without clear benefit. Rejected for v1.

## Consequences

- Users always verify before execution — builds trust, especially in government environments
- The preview IS the demo — audiences see the LLM correctly resolve intent
- Adds one click to every workflow (acceptable trade-off for reliability)
- [Edit] button allows parameter correction, making 80% LLM accuracy feel like 100% usability

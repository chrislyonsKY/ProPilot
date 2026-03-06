# DL-004: Ollama Structured Output for Reliable Parsing

**Date:** 2026-03-05
**Status:** Accepted
**Author:** Chris Lyons

## Context

LLMs can return freeform text, which requires fragile regex or string parsing. Ollama supports passing a JSON schema in the `format` parameter, constraining generation to valid JSON.

## Decision

Use Ollama's structured output mode — pass the ProPilotCommand JSON schema with every request. Temperature = 0 for deterministic output.

## Consequences

- LLM is structurally unable to return invalid JSON (constrained at generation level)
- No need for output parsers, regex, or retry-on-malformed-JSON logic
- Schema changes require updating both the C# model and the JSON schema passed to Ollama
- Temperature 0 means identical inputs always produce identical outputs (good for testing)

---

# DL-005: ProWindow Over DockPane

**Date:** 2026-03-05
**Status:** Accepted
**Author:** Chris Lyons

## Context

The command interface UI could be a DockPane (persistent, docked) or a ProWindow (floating, on-demand). Chris specifically requested ProWindow usage for the SDK surface area.

## Decision

Use a ProWindow for the main command interface, opened via ribbon button and keyboard shortcut (Ctrl+Shift+P). A separate ProWindow for settings.

## Alternatives Considered

- **DockPane** — Always visible, persistent. But takes up screen real estate when not in use, and the command bar is used in bursts, not continuously. Rejected.
- **Floating command palette (Spotlight-style)** — Technically appealing but Pro SDK doesn't natively support borderless overlay windows, and it would fight with Pro's own window management. Rejected.

## Consequences

- ProWindow provides full WPF control over layout, sizing, and behavior
- Window opens on demand — zero screen real estate cost when not in use
- Keyboard shortcut (Ctrl+Shift+P) enables fast access without mouse
- ProWindow does not persist state between open/close by default — history must be managed in ViewModel
- Provides the ProWindow SDK experience Chris wanted for portfolio

# DL-001: Local LLM Over Cloud API

**Date:** 2026-03-05
**Status:** Accepted
**Author:** Chris Lyons

## Context

ProPilot needs an LLM to parse natural language commands into structured JSON. Options: cloud API (Anthropic, OpenAI), local LLM (Ollama), or hybrid.

## Decision

Use a local LLM via Ollama as the sole provider in v1. Default model: Mistral 7B Instruct (Q4_K_M).

## Alternatives Considered

- **Anthropic Claude API** — Best quality, but requires API key + internet + per-token cost. Government environments may restrict cloud AI calls. Rejected for v1.
- **OpenAI API** — Same concerns as Anthropic. Rejected for v1.
- **Pluggable from day one** — Adds complexity without v1 benefit. The `ILlmClient` interface allows future providers. Rejected as premature.

## Consequences

- Zero cost to end users (no API keys, no subscriptions)
- Works offline and in air-gapped government networks
- User must install Ollama + pull a model (~4GB one-time download)
- Quality ceiling limited by local model capability (mitigated by tight command vocabulary + structured output)
- `ILlmClient` interface preserves ability to add cloud providers in v2

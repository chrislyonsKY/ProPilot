# DL-006: Bundled LLamaSharp Over External Ollama

**Date:** 2026-03-05
**Status:** Accepted (supersedes DL-001 as default provider; DL-001 retained as fallback)
**Author:** Chris Lyons

## Context

ProPilot's target audience includes non-developer GIS analysts in government environments. The original design (DL-001) required users to install Ollama separately and run terminal commands to pull models. This is a significant barrier for non-technical users.

## Decision

Bundle LLamaSharp (C#/.NET binding of llama.cpp) as the DEFAULT inference provider. The model runs in-process — no external dependencies, no terminal commands, no separate application to install. Users select a model tier (Light: Phi-3 Mini ~1.5GB or Standard: Mistral 7B ~4GB) on first run, and the GGUF file is downloaded from HuggingFace automatically.

Ollama and OpenAI remain as alternative providers for power users via the Settings window.

## Alternatives Considered

- **Ollama with setup wizard (DL-001)** — Good compromise, but still requires a separate application install. Users who aren't comfortable with Ollama would not adopt the tool. Retained as fallback.
- **Cloud-only (OpenAI)** — Simplest technically, but requires API key + billing + internet, which fails in government/air-gapped environments. Retained as opt-in.
- **Ship model inside .esriAddInX** — GGUF files are 1.5-4GB, too large for the add-in package format. First-run download is the right compromise.

## Consequences

- Zero external dependencies — user installs the add-in and it works after a one-time model download
- First-run model download adds 1.5-4GB to the user's AppData (one-time)
- LLamaSharp does not have Ollama's built-in JSON schema enforcement — requires GBNF grammar for structured output (additional engineering effort)
- Model loads into Pro's process memory (2-6GB depending on model) — may be tight on 8GB machines with complex maps open
- NuGet dependency: LLamaSharp + LLamaSharp.Backend.Cpu added to the project
- Model loading takes 5-15 seconds on first command (lazy loaded, not at Pro startup)
- Manual GGUF file placement supported as escape hatch for air-gapped networks

## Provider Hierarchy

| Provider | When Used | Requires |
|---|---|---|
| LLamaSharp (bundled) | Default — first-time setup downloads model | Nothing external |
| Ollama | User selects in Settings | Ollama installed + model pulled |
| OpenAI | User selects in Settings | API key in OPENAI_API_KEY env var |

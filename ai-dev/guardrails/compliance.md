# Compliance Guardrails — ProPilot

## Kentucky CIO-126 Artificial Intelligence Policy (Effective 10/06/2025)

ProPilot is designed for compliance with CIO-126. This document maps each
policy requirement to ProPilot's architecture.

### No Confidential Data in Public AI Models

> CIO-126: "Employees shall refrain from entering any individual or business's
> confidential or proprietary information into third party AI models that are
> available to the general public."

**ProPilot compliance:**
- DEFAULT provider (LLamaSharp) runs 100% locally — no data leaves the machine
- Map context sent to the LLM contains ONLY: layer names, field names, geometry
  types, feature counts, bookmarks, extent coordinates. NEVER actual attribute
  values, PII, permit applicant data, SSNs, or confidential information
- User commands (natural language) are processed locally and never transmitted
- OpenAI provider (opt-in only) DOES transmit data to cloud servers. When a user
  enables this provider, the Settings UI MUST display a warning:
  "Cloud AI sends your commands and layer names to OpenAI servers. Do not use
  this provider for work involving confidential or restricted data. The bundled
  local model is recommended for government use."

### Human Review Required

> CIO-126: "No consequential decision shall be rendered without human review."

**ProPilot compliance:**
- Always-preview model: every command shows a structured preview before execution
- User MUST click [Execute] to confirm — no auto-execute
- ProPilot manipulates map views, symbology, and selections — it does NOT make
  decisions about permits, enforcement, benefits, or citizens
- All geoprocessing results are standard ArcGIS Pro operations that the user
  could perform manually

### AI Risk Classification

> CIO-126: "State agencies may not use high-risk artificial intelligence systems."

**ProPilot risk classification: NOT HIGH-RISK**
- ProPilot is a GIS productivity tool that translates natural language into
  standard ArcGIS Pro operations (zoom, select, buffer, change symbology)
- It does not make decisions about individuals, benefits, enforcement, or rights
- It does not process PII, health data, or law enforcement data
- It does not generate public-facing content or citizen communications
- It is functionally equivalent to a keyboard shortcut system with natural
  language input — the same operations are available via Pro's ribbon menu
- Classification: Low-risk productivity enhancement tool

### Training and Documentation

> CIO-126: "COT shall mandate a minimum level of AI training for users."

**ProPilot compliance:**
- README and in-app help should document that ProPilot uses AI for command parsing
- Users should understand that the AI interprets commands — it does not guarantee
  correctness (hence the always-preview model)
- Agencies deploying ProPilot should include it in their AI training/awareness program

### AI Usage Logging

> CIO-126: Agencies must document AI use and maintain audit trails.

**ProPilot compliance:**
- TODO (v1.x): Add optional audit logging — timestamp, command text, parsed intent,
  execution result. Written to a local log file in %APPDATA%\ProPilot\logs\
- Logging should be opt-in and configurable per agency policy
- Logs MUST NOT contain actual attribute values or PII — only command metadata

---

## Model Provenance

ProPilot uses open-weight models with known, documented origins:

| Model | Developer | License | Origin | Notes |
|---|---|---|---|---|
| Phi-3 Mini 3.8B | Microsoft | MIT | Published on HuggingFace | Microsoft's open-weight small language model |
| Mistral 7B Instruct | Mistral AI | Apache 2.0 | Published on HuggingFace | French AI company, widely adopted open model |

Both models are open-weight (publicly auditable), do not phone home, and
run entirely on the local machine with no network access required after download.

---

## Network Requirements

| Operation | Network Required? | Destination | When |
|---|---|---|---|
| First-run model download | YES | huggingface.co | One-time only |
| LLamaSharp inference | NO | localhost only | Every command |
| Ollama provider | NO | localhost:11434 | Every command (if selected) |
| OpenAI provider | YES | api.openai.com | Every command (if selected) |

**For air-gapped environments:** Users can manually place a GGUF model file in
`%APPDATA%\ProPilot\models\` — no network access required at any point.
The SetupWindow provides a "Browse for local model file" option for this purpose.

---

## Data Flow Summary

```
User types command (e.g., "zoom to streams")
        │
        ▼
MapContextBuilder captures: layer names, field names,
feature counts, extent, bookmarks
(NO attribute values, NO PII, NO confidential data)
        │
        ▼
LLM processes locally (LLamaSharp, in-process)
(Data never leaves the machine)
        │
        ▼
Parsed command displayed in preview panel
(Human reviews before execution)
        │
        ▼
User clicks [Execute] — standard Pro SDK operation runs
(Same as clicking a button in the ribbon)
```

No data exfiltration. No cloud dependency. Human-in-the-loop at every step.

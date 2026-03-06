<p align="center">
  <img src="docs/images/ProPilot-banner.png" alt="ProPilot Banner" width="600"/>
</p>

<h1 align="center">ProPilot</h1>

<p align="center">
  <strong>Natural language command interface for ArcGIS Pro</strong>
</p>

<p align="center">
  <a href="https://github.com/chrislyonsKY/ProPilot/releases"><img src="https://img.shields.io/github/v/release/chrislyonsKY/ProPilot?style=flat-square&label=release&color=0078D4" alt="Release"></a>
  <a href="https://github.com/chrislyonsKY/ProPilot/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-Apache%202.0-blue?style=flat-square" alt="License"></a>
  <img src="https://img.shields.io/badge/ArcGIS%20Pro-3.6-00A651?style=flat-square&logo=esri" alt="ArcGIS Pro 3.6">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 8">
  <img src="https://img.shields.io/badge/C%23-12-239120?style=flat-square&logo=csharp" alt="C# 12">
  <img src="https://img.shields.io/badge/LLM-Local%20%7C%20Bundled-FF6F00?style=flat-square" alt="Local LLM">
  <a href="https://github.com/chrislyonsKY/ProPilot/stargazers"><img src="https://img.shields.io/github/stars/chrislyonsKY/ProPilot?style=flat-square&color=yellow" alt="Stars"></a>
  <a href="https://github.com/chrislyonsKY/ProPilot/issues"><img src="https://img.shields.io/github/issues/chrislyonsKY/ProPilot?style=flat-square" alt="Issues"></a>
</p>

<p align="center">
  <em>Type commands. See previews. Execute with confidence.</em>
</p>

---

ProPilot brings a natural language command bar to ArcGIS Pro. Type plain English like **"zoom to all streams in Boyd County"** or **"buffer the selected permits by 500 meters"** — ProPilot parses your intent with a bundled local AI model, shows you a structured preview of what it understood, and executes on your confirmation. No cloud. No API keys. No cost. Works offline.

<p align="center">
  <img src="docs/images/ProPilot-demo.gif" alt="ProPilot Demo" width="450"/>
</p>

## Why ProPilot?

ArcGIS Pro is powerful but complex. Common operations require navigating ribbon tabs, opening tool dialogs, remembering parameter names, and clicking through multi-step workflows — all for tasks you could describe in a single sentence.

ProPilot changes that. It understands what you mean, confirms what it'll do, and executes when you're ready.

- **30 curated commands** covering navigation, layer management, selection, symbology, queries, and geoprocessing
- **Always-preview execution** — every command shows a structured preview before it fires. You verify, then it acts.
- **Full map awareness** — knows your layer names, field names, current selections, visible extent, and symbology
- **Fuzzy matching** — type "streams" and it resolves to `Streams_KY`. Type "population" and it finds `POP_TOTAL`.
- **Zero dependencies** — the AI model is bundled and runs locally inside the add-in. No Ollama, no cloud, no API keys.
- **Government-safe** — designed for compliance with Kentucky CIO-126. No data leaves your machine. Ever.

## Quick Start

### 1. Download and install

Download the latest `.esriAddInX` from [**Releases**](../../releases) and double-click to install. No admin rights needed.

### 2. Choose your model (first run only)

The first time you open ProPilot, you'll choose an AI model tier. This is a one-time download.

| Tier | Model | Download | RAM | Best For |
|---|---|---|---|---|
| **Light** | Phi-3 Mini 3.8B | ~1.5 GB | 8 GB+ | Faster responses, most commands |
| **Standard** | Mistral 7B Instruct | ~4 GB | 16 GB+ | Best accuracy, complex commands |

ProPilot detects your system RAM and recommends the right tier. You can also browse for a local `.gguf` file if your network is restricted.

### 3. Start commanding

Click **ProPilot** on the Add-In ribbon tab (or press `Ctrl+Shift+P`) and type a command:

```
zoom to Permitted_Boundaries
turn off all layers except Streams and County_Boundary
select features in Wells where DEPTH > 100
buffer the selected features by 500 meters
change the roads layer color to red
how many features in Permitted_Boundaries?
set scale to 24000
filter Permits where STATUS = 'Active'
clip Streams by Study_Area
dissolve Parcels on COUNTY_NAME
```

## How It Works

```
You type a command
       │
       ▼
Map context captured ─── layer names, fields, selections, extent
       │
       ▼
Local AI model parses intent ─── runs in-process, never leaves your machine
       │
       ▼
Preview displayed ─── "Select By Location: Streams within 1 mile of Active Permits"
       │
       ▼
You click Execute ─── standard Pro SDK operation runs
```

Every command goes through the preview step. ProPilot never modifies your map without your explicit confirmation.

## Command Reference

ProPilot understands 30 commands across 6 categories:

| Category | Commands | Examples |
|---|---|---|
| **Navigation** | Zoom, pan, scale, bookmarks | "zoom to streams", "set scale to 24000" |
| **Layer Management** | Visibility, transparency, reorder, add/remove | "turn off everything except roads" |
| **Selection** | By attribute, by location, clear, invert | "select where STATUS = 'Active'" |
| **Symbology** | Color, line width, point size, renderer, labels | "change roads to red", "label by NAME" |
| **Query** | Definition query, feature count, field list | "how many features in Wells?" |
| **Geoprocessing** | Buffer, clip, dissolve, merge, export | "buffer permits by 500 meters" |

Full command vocabulary with natural language patterns: [`ai-dev/specs/commands.md`](ai-dev/specs/commands.md)

## LLM Providers

ProPilot supports three inference providers. The bundled local model is the default — the others are opt-in for power users.

| Provider | Setup | Cost | Network | Best For |
|---|---|---|---|---|
| **LLamaSharp** (default) | Nothing — bundled in add-in | Free | Offline after model download | Everyone |
| Ollama | Install Ollama + pull model | Free | Localhost only | Power users, model experimentation |
| OpenAI | API key in environment variable | Pay-per-token | Internet required | Cloud model quality comparison |

Switch providers in Settings. The bundled provider is recommended for government and air-gapped environments.

## Requirements

| Component | Version |
|---|---|
| ArcGIS Pro | 3.6 |
| .NET | 8.0 (included with Pro 3.6) |
| RAM | 8 GB minimum (16 GB recommended) |
| Disk | 1.5–4 GB for model file |

No admin rights required. No external software to install.

## Deployment

ProPilot is designed for easy deployment across different environments:

| Environment | Method | Details |
|---|---|---|
| **Personal / open-source** | Download `.esriAddInX` from Releases | Self-service, zero IT involvement |
| **Government (standard)** | Same as above | CIO-126 compliant, all processing local |
| **Government (restricted)** | IT deploys Ollama, users install add-in | For environments blocking native DLLs |
| **Air-gapped** | Transfer `.esriAddInX` + `.gguf` via approved media | Zero network access required |

Full deployment guide: [`DEPLOYMENT.md`](DEPLOYMENT.md)

## Architecture

ProPilot is built with the ArcGIS Pro SDK 3.6 for .NET, following MVVM patterns with `CommunityToolkit.Mvvm`.

```
src/ProPilot/ProPilot/
├── UI/                  # ProWindow views (XAML)
├── ViewModels/          # All logic — command flow, settings, setup
├── Models/              # ProPilotCommand, MapContext, Settings
├── Services/            # LLM clients, map context builder, model manager
├── Commands/            # 30 IMapCommand implementations
│   ├── Navigation/          (6)
│   ├── LayerManagement/     (6)
│   ├── Selection/           (5)
│   ├── Symbology/           (5)
│   ├── Query/               (4)
│   └── Geoprocessing/       (5)
└── Prompts/             # System prompt builder + JSON schema
```

Full architecture: [`ai-dev/architecture.md`](ai-dev/architecture.md)

## Development

```bash
git clone https://github.com/chrislyonsKY/ProPilot.git
cd ProPilot/src/ProPilot
# Open ProPilot.sln in Visual Studio 2022
# Requires ArcGIS Pro SDK 3.6 extension installed
```

### NuGet Packages

```
CommunityToolkit.Mvvm
LLamaSharp
LLamaSharp.Backend.Cpu
```

### Build Order

The project follows a phased implementation plan — start with a vertical slice (text in → JSON out → preview displayed), then layer in map context, commands, and the bundled LLM. Full build guide in [`.github/copilot-instructions.md`](.github/copilot-instructions.md).

## Government Compliance

ProPilot is designed for use in state and federal government environments:

- **Kentucky CIO-126 compliant** — no data leaves the machine, human review before every action, low-risk classification
- **No cloud dependency** — bundled AI model runs entirely in-process
- **No PII exposure** — map context contains only layer names and field names, never attribute values
- **Audit-ready** — architecture documents the data flow, risk classification, and model provenance
- **Open-weight models** — Phi-3 (Microsoft, MIT license) and Mistral 7B (Mistral AI, Apache 2.0) are publicly auditable

Full compliance mapping: [`ai-dev/guardrails/compliance.md`](ai-dev/guardrails/compliance.md)

## Roadmap

- [x] Architecture and scaffolding
- [ ] v1.0 — 30 commands with bundled LLamaSharp
- [ ] v1.x — Ollama/OpenAI fallback providers, audit logging, command history
- [ ] v2.0 — Layout commands, custom command mappings, voice input

## Contributing

Contributions welcome! Please read the architecture docs in `ai-dev/` before submitting PRs. The project follows a documentation-first approach — specs and architecture before code.

## License

[Apache 2.0](LICENSE) — same license as Esri's Pro SDK samples.

## Author

**Chris Lyons** — GIS Analyst III, Kentucky Energy & Environment Cabinet

- GitHub: [@chrislyonsKY](https://github.com/chrislyonsKY)
- Newsletter: [Null Island Dispatch](https://www.linkedin.com/newsletters/null-island-dispatch) — Weekly GeoAI insights
- Portfolio: [chrislyonsKY.github.io](https://chrislyonsKY.github.io)

---

<p align="center">
  <sub>Built with the <a href="https://developers.arcgis.com/documentation/arcgis-pro-sdk/">ArcGIS Pro SDK</a> and <a href="https://github.com/SciSharp/LLamaSharp">LLamaSharp</a>. Powered by open-weight AI models.</sub>
</p>

# ProPilot — UI Design

> All UI is WPF inside ProWindows. MVVM pattern — zero logic in code-behind.
> Dark theme matching ArcGIS Pro's Visual Studio-inspired aesthetic.

---

## CommandWindow (Main Interface)

Opened via ribbon button or `Ctrl+Shift+P`.

```
┌─────────────────────────────────────────────────┐
│  ProPilot                              [⚙] [✕]  │
├─────────────────────────────────────────────────┤
│                                                  │
│  ┌────────────────────────────────────────┐ [▶]  │
│  │  Type a command...                     │      │
│  └────────────────────────────────────────┘      │
│                                                  │
│  ┌──────────────────────────────────────────┐    │
│  │  ● Status: Ready                         │    │
│  │                                          │    │
│  │  ──── Preview ────                       │    │
│  │  🔍 Zoom To Layer                        │    │
│  │                                          │    │
│  │  Target Layer:  Streams                  │    │
│  │  Geometry:      Polyline                 │    │
│  │  Feature Count: 2,847                    │    │
│  │                                          │    │
│  │  Confidence: 92% ●                       │    │
│  │                                          │    │
│  │  ┌──────────┐ ┌────────┐ ┌──────────┐   │    │
│  │  │ Execute  │ │ Cancel │ │   Edit   │   │    │
│  │  └──────────┘ └────────┘ └──────────┘   │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
│  ▸ Command History (3)                           │
│                                                  │
├─────────────────────────────────────────────────┤
│  Model: mistral:7b │ LLamaSharp: ● Connected     │
└─────────────────────────────────────────────────┘
```

### Elements

| Element | Binding | Notes |
|---|---|---|
| Command TextBox | `CommandInput` | Enter to submit, focus on window open |
| Parse button [▶] | `SubmitCommandCommand` | Disabled while IsProcessing |
| Status indicator | `StatusMessage` + `StatusColor` | Green=ready, Yellow=parsing, Blue=executing, Red=error |
| Preview panel | `CurrentPreview` | Visible only when preview is populated |
| Confidence badge | `CurrentPreview.Confidence` | Green ≥85%, Yellow ≥50%, Red <50% |
| Execute button | `ExecuteCommandCommand` | Primary action, blue accent |
| Cancel button | `CancelCommandCommand` | Clears preview, resets status |
| Edit button | Opens parameters for manual editing | v1.x — stub in v1 |
| History expander | `CommandHistory` collection | Collapsible, shows last N commands |
| Settings gear [⚙] | `OpenSettingsCommand` | Opens SettingsWindow |
| Status bar | `ModelName` + `ProviderName` + `IsConnected` | Footer, blue background |

### Keyboard Shortcuts

| Key | Action |
|---|---|
| `Ctrl+Shift+P` | Open/focus CommandWindow (global Pro shortcut) |
| `Enter` | Submit command (when TextBox focused) |
| `Escape` | Cancel current preview / close window |
| `Ctrl+Enter` | Execute previewed command |

---

## SetupWindow (First-Run Model Download)

Shown when `ModelManager.HasLocalModel()` returns false.

```
┌──────────────────────────────────────────────────┐
│  ProPilot — First Time Setup                     │
├──────────────────────────────────────────────────┤
│                                                  │
│  ProPilot needs to download an AI model.         │
│  This is a one-time download.                    │
│                                                  │
│  System RAM: 32 GB  ✓                            │
│  Recommended: Standard model                     │
│                                                  │
│  ○ Light (1.5 GB download, 8GB+ RAM)             │
│    Faster responses, good for most commands      │
│                                                  │
│  ● Standard (4 GB download, 16GB+ RAM)    ★      │
│    Best accuracy, recommended for your system    │
│                                                  │
│  ┌──────────────────────────────────────────┐    │
│  │         Download & Install               │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
│  ── or ──                                        │
│                                                  │
│  [Browse for local model file (.gguf)]           │
│                                                  │
│  Models stored in %APPDATA%\ProPilot\models      │
│  You can change models later in Settings.        │
│                                                  │
└──────────────────────────────────────────────────┘
```

### During Download

```
│  Downloading Standard model...                   │
│                                                  │
│  ████████████░░░░░░░░░░░  2.4 GB / 4.0 GB  60%  │
│                                                  │
│  [Cancel Download]                               │
```

### Elements

| Element | Binding | Notes |
|---|---|---|
| System RAM display | `SystemRamGb` | Detected via GC.GetGCMemoryInfo() |
| Model radio buttons | `SelectedProfile` | Pre-selects recommended tier |
| ★ Recommended badge | `IsRecommended` | Based on RAM detection |
| Download button | `DownloadModelCommand` | Disabled if no profile selected |
| Progress bar | `DownloadPercent` | Visible only during download |
| Bytes text | `BytesDownloadedText` | "2.4 GB / 4.0 GB" |
| Cancel button | `CancelDownloadCommand` | Visible only during download |
| Browse button | `BrowseForModelCommand` | Opens file dialog for .gguf |

---

## SettingsWindow

Opened via gear icon in CommandWindow.

```
┌──────────────────────────────────────────────────┐
│  ProPilot — Settings                             │
├──────────────────────────────────────────────────┤
│                                                  │
│  LLM Provider                                    │
│  ● Bundled (LLamaSharp — recommended)            │
│  ○ Ollama (localhost)                            │
│  ○ OpenAI (cloud — requires API key)             │
│                                                  │
│  ── Bundled Settings ──                          │
│  Active model: mistral-7b-instruct-v0.2.Q4_K_M  │
│  [Change Model]  [Redownload]                    │
│                                                  │
│  ── Ollama Settings ──                           │
│  Endpoint: [http://localhost:11434    ]           │
│  Model:    [mistral                  ]           │
│  [Test Connection]                               │
│                                                  │
│  ── OpenAI Settings ──                           │
│  Model:   [gpt-4o-mini              ]            │
│  ⚠ API key read from OPENAI_API_KEY env var      │
│  ⚠ Cloud provider sends data off-network         │
│  [Test Connection]                               │
│                                                  │
│  ── General ──                                   │
│  Timeout: [30] seconds                           │
│                                                  │
│  [Save to Project]  [Save as User Default]       │
│                                                  │
└──────────────────────────────────────────────────┘
```

### Elements

| Element | Binding | Notes |
|---|---|---|
| Provider radio buttons | `LlmProvider` | "bundled", "ollama", "openai" |
| Provider-specific panels | Visible based on selected provider | Collapse unused panels |
| Cloud warning | Static text | Always visible when OpenAI selected |
| Test Connection button | `TestConnectionCommand` | Checks provider availability |
| Save to Project | `SaveToProjectCommand` | Writes to .aprx custom properties |
| Save as User Default | `SaveToUserDefaultsCommand` | Writes to %APPDATA% JSON |

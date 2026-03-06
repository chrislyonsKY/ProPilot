# ProPilot — Solution Architecture

> Read `ai-dev/specs/` first for full context on requirements and command vocabulary.

---

## Solution Structure

```
ProPilot/
├── .github/
│   └── copilot-instructions.md          # GitHub Copilot project context
├── ai-dev/
│   ├── spec.md                          # Product specification
│   ├── architecture.md                  # This file
│   ├── patterns.md                      # Code patterns and anti-patterns
│   ├── agents/
│   │   ├── README.md
│   │   ├── architect.md                 # System design agent
│   │   └── prosdk_expert.md             # ArcGIS Pro SDK implementation agent
│   ├── decisions/
│   │   ├── DL-001-local-llm-over-cloud.md
│   │   ├── DL-002-hybrid-execution-model.md
│   │   ├── DL-003-always-preview.md
│   │   ├── DL-004-ollama-structured-output.md
│   │   └── DL-005-prowindow-over-dockpane.md
│   ├── skills/
│   │   └── prosdk-addin-skill.md
│   └── guardrails/
│       ├── coding-standards.md
│       └── data-handling.md
├── src/
│   └── ProPilot/
│       ├── ProPilot.sln
│       └── ProPilot/
│           ├── Config.daml                      # DAML add-in manifest
│           ├── Module1.cs                       # Add-in module entry point
│           ├── ProPilotModule.cs                # Module lifecycle
│           │
│           ├── UI/                              # Views (XAML)
│           │   ├── CommandWindow.xaml            # Main ProWindow
│           │   ├── CommandWindow.xaml.cs         # Code-behind (minimal)
│           │   ├── SettingsWindow.xaml           # Settings ProWindow
│           │   ├── SettingsWindow.xaml.cs
│           │   ├── SetupWindow.xaml              # First-run model setup ProWindow
│           │   └── SetupWindow.xaml.cs
│           │
│           ├── ViewModels/                      # MVVM ViewModels
│           │   ├── CommandWindowViewModel.cs     # Main window logic
│           │   ├── SettingsWindowViewModel.cs    # Settings logic
│           │   ├── SetupWindowViewModel.cs       # First-run model download logic
│           │   ├── CommandPreviewViewModel.cs    # Preview panel state
│           │   └── CommandHistoryViewModel.cs    # History panel state
│           │
│           ├── Models/                          # Data models
│           │   ├── ProPilotCommand.cs           # Parsed command (from LLM JSON)
│           │   ├── CommandResult.cs             # Execution result
│           │   ├── MapContext.cs                # Map state snapshot
│           │   ├── LayerInfo.cs                 # Layer metadata for context
│           │   └── ProPilotSettings.cs          # Persisted settings
│           │
│           ├── Services/                        # Core services
│           │   ├── ILlmClient.cs               # LLM client interface
│           │   ├── LLamaSharpClient.cs         # Bundled LLamaSharp (DEFAULT provider)
│           │   ├── OllamaClient.cs             # Ollama HTTP (power user fallback)
│           │   ├── OpenAiClient.cs             # OpenAI API (opt-in cloud provider)
│           │   ├── IModelManager.cs            # Model download/discovery interface
│           │   ├── ModelManager.cs             # HuggingFace download + local GGUF management
│           │   ├── IMapContextBuilder.cs        # Map context interface
│           │   ├── MapContextBuilder.cs         # Builds context payload
│           │   ├── IIntentParser.cs             # Parses LLM JSON response
│           │   ├── IntentParser.cs              # Validates + deserializes
│           │   ├── GbnfSchemaProvider.cs        # GBNF grammar for LLamaSharp structured output
│           │   ├── ISettingsService.cs          # Settings persistence interface
│           │   └── SettingsService.cs           # Project + user settings
│           │
│           ├── Commands/                        # Command implementations
│           │   ├── IMapCommand.cs               # Command interface
│           │   ├── CommandRegistry.cs           # Maps command_type → IMapCommand
│           │   ├── CommandResolver.cs           # Resolves parsed intent → concrete command
│           │   ├── Navigation/
│           │   │   ├── ZoomToLayerCommand.cs
│           │   │   ├── ZoomToSelectionCommand.cs
│           │   │   ├── ZoomToFullExtentCommand.cs
│           │   │   ├── PanToCoordinatesCommand.cs
│           │   │   ├── SetScaleCommand.cs
│           │   │   └── GoToBookmarkCommand.cs
│           │   ├── LayerManagement/
│           │   │   ├── ToggleVisibilityCommand.cs
│           │   │   ├── SoloLayersCommand.cs
│           │   │   ├── SetTransparencyCommand.cs
│           │   │   ├── ReorderLayerCommand.cs
│           │   │   ├── RemoveLayerCommand.cs
│           │   │   └── AddLayerCommand.cs
│           │   ├── Selection/
│           │   │   ├── SelectByAttributeCommand.cs
│           │   │   ├── SelectByLocationCommand.cs
│           │   │   ├── ClearSelectionCommand.cs
│           │   │   ├── SelectAllCommand.cs
│           │   │   └── InvertSelectionCommand.cs
│           │   ├── Symbology/
│           │   │   ├── ChangeColorCommand.cs
│           │   │   ├── SetLineWidthCommand.cs
│           │   │   ├── SetPointSizeCommand.cs
│           │   │   ├── ChangeRendererCommand.cs
│           │   │   └── ToggleLabelsCommand.cs
│           │   ├── Query/
│           │   │   ├── SetDefinitionQueryCommand.cs
│           │   │   ├── ClearDefinitionQueryCommand.cs
│           │   │   ├── GetFeatureCountCommand.cs
│           │   │   └── ListFieldsCommand.cs
│           │   └── Geoprocessing/
│           │       ├── BufferCommand.cs
│           │       ├── ClipCommand.cs
│           │       ├── ExportDataCommand.cs
│           │       ├── DissolveCommand.cs
│           │       └── MergeCommand.cs
│           │
│           ├── Prompts/                         # LLM prompt management
│           │   ├── SystemPromptBuilder.cs       # Assembles system prompt
│           │   └── CommandSchemaProvider.cs      # JSON schema for structured output
│           │
│           ├── Images/                          # Ribbon icons (light + dark)
│           │   ├── ProPilot16.png
│           │   ├── ProPilot32.png
│           │   ├── ProPilotDark16.png
│           │   └── ProPilotDark32.png
│           │
│           └── Properties/
│               └── AssemblyInfo.cs
```

---

## Key Interfaces

### IMapCommand

Every executable command implements this interface. The separation of `BuildPreview()` and `ExecuteAsync()` is what enables the always-preview model.

```csharp
public interface IMapCommand
{
    /// <summary>
    /// Unique command type identifier matching the JSON schema enum.
    /// </summary>
    string CommandType { get; }

    /// <summary>
    /// Human-readable name for display in preview panel.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Whether this command modifies data (triggers warning icon).
    /// </summary>
    bool IsDestructive { get; }

    /// <summary>
    /// Validates that the parsed command has all required parameters.
    /// Returns validation errors if incomplete.
    /// </summary>
    ValidationResult Validate(ProPilotCommand command, MapContext context);

    /// <summary>
    /// Builds a human-readable preview of what this command will do.
    /// Does NOT execute anything. Pure read-only.
    /// </summary>
    CommandPreview BuildPreview(ProPilotCommand command, MapContext context);

    /// <summary>
    /// Executes the command. Must be called within QueuedTask.Run()
    /// for SDK commands, or use Geoprocessing.ExecuteToolAsync() for GP tools.
    /// </summary>
    Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context);
}
```

### ILlmClient

Abstracted to support three providers: LLamaSharp (bundled default), Ollama (power user), and OpenAI (cloud opt-in).

```csharp
public interface ILlmClient
{
    /// <summary>
    /// Sends a natural language command to the LLM with map context,
    /// receives a structured ProPilotCommand JSON response.
    /// </summary>
    Task<ProPilotCommand?> ParseCommandAsync(
        string userInput,
        MapContext mapContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the LLM service is reachable and the model is loaded.
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Gets the currently configured model name.
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Provider display name for the status bar.
    /// </summary>
    string ProviderName { get; }
}
```

### IModelManager

Manages GGUF model file lifecycle — discovery, download, and validation.

```csharp
public interface IModelManager
{
    /// <summary>
    /// Checks if any model is available locally in the models directory.
    /// </summary>
    bool HasLocalModel();

    /// <summary>
    /// Gets available model profiles (Light and Standard).
    /// </summary>
    IReadOnlyList<ModelProfile> GetAvailableProfiles();

    /// <summary>
    /// Downloads a GGUF model from HuggingFace with progress reporting.
    /// Stored in %APPDATA%\ProPilot\models\
    /// </summary>
    Task DownloadModelAsync(
        ModelProfile profile,
        IProgress<ModelDownloadProgress> progress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the file path of the currently configured local model.
    /// Returns null if no model is downloaded.
    /// </summary>
    string? GetActiveModelPath();

    /// <summary>
    /// Lists all GGUF files in the models directory (supports manual placement).
    /// </summary>
    IReadOnlyList<LocalModelInfo> GetLocalModels();
}
```

### Model Profiles

```csharp
public class ModelProfile
{
    public string Name { get; set; }          // "Light" or "Standard"
    public string ModelId { get; set; }       // e.g., "phi-3-mini"
    public string HuggingFaceRepo { get; set; } // e.g., "microsoft/Phi-3-mini-4k-instruct-gguf"
    public string FileName { get; set; }      // e.g., "Phi-3-mini-4k-instruct-q4.gguf"
    public long FileSizeBytes { get; set; }   // For progress display
    public string Description { get; set; }   // "Faster, lighter, 8GB+ RAM"
    public int MinimumRamGb { get; set; }     // 8 or 16
}
```

### Bundled Model Tiers

| Tier | Model | Params | GGUF Size | RAM Required | Response Time (CPU) |
|---|---|---|---|---|---|
| **Light** | Phi-3 Mini 3.8B Q4_K_M | 3.8B | ~1.5 GB | 8 GB+ | ~1-2 seconds |
| **Standard** | Mistral 7B Instruct Q4_K_M | 7B | ~4 GB | 16 GB+ | ~2-4 seconds |

### IMapContextBuilder

Snapshots the current map state for LLM context injection.

```csharp
public interface IMapContextBuilder
{
    /// <summary>
    /// Captures the current state of the active map view including
    /// layers, fields, selections, extent, bookmarks, and symbology.
    /// Must be called on the MCT (QueuedTask.Run).
    /// </summary>
    Task<MapContext> BuildContextAsync();
}
```

---

## Data Flow Detail

### 1. User Input → LLM Request

```
User types: "select all streams within 1 mile of active permits"
                    │
                    ▼
        MapContextBuilder.BuildContextAsync()
                    │
                    ▼
    ┌──────────────────────────────────────────┐
    │ MapContext:                                │
    │   Layers:                                 │
    │     - Streams_KY (polyline, 2847 features)│
    │     - Permitted_Boundaries (polygon, 412) │
    │     - Kentucky_Polygon (polygon, 1)       │
    │   Fields:                                 │
    │     Permitted_Boundaries: [PERMIT_ID,     │
    │       STATUS, OPERATOR_NAME, ...]         │
    │   Selection: none                         │
    │   Extent: {xmin: -89.5, ymin: 36.5, ...} │
    │   Bookmarks: ["Study Area", "Boyd Co"]    │
    └──────────────────────────────────────────┘
                    │
                    ▼
    SystemPromptBuilder assembles:
      [system prompt] + [command enum] + [map context]
                    │
                    ▼
    LLamaSharpClient (DEFAULT — bundled, in-process):
      model.CreateContext() → executor.InferAsync()
      GBNF grammar constrains output to valid JSON
      Response parsed as ProPilotCommand

    — OR (power user) —

    OllamaClient POST /v1/chat/completions
    {
      "model": "mistral",
      "messages": [...],
      "format": { [ProPilotCommand JSON schema] },
      "options": { "temperature": 0 }
    }

    — OR (cloud opt-in) —

    OpenAiClient POST https://api.openai.com/v1/chat/completions
    {
      "model": "gpt-4o-mini",
      "messages": [...],
      "response_format": { "type": "json_schema", ... },
      "temperature": 0
    }
```

### 2. LLM Response → Preview

```json
{
  "command_type": "select_by_location",
  "confidence": 0.92,
  "target_layer": "Streams_KY",
  "parameters": {
    "source_layer": "Permitted_Boundaries",
    "expression": "STATUS = 'Active'",
    "spatial_relationship": "within_distance",
    "distance": 1,
    "distance_unit": "miles"
  },
  "human_description": "Select features in Streams_KY within 1 mile of Permitted_Boundaries where STATUS = 'Active'"
}
```

### 3. Preview → Execution

```
CommandResolver.Resolve(parsedCommand)
    → SelectByLocationCommand

SelectByLocationCommand.BuildPreview()
    → CommandPreview {
        Icon: "🔍",
        Title: "Select By Location",
        Parameters: [
          "Target: Streams_KY (polyline, 2,847 features)",
          "Source: Permitted_Boundaries (where STATUS = 'Active')",
          "Relationship: Within Distance",
          "Distance: 1 mile"
        ],
        IsDestructive: false
      }

User clicks [Execute]

SelectByLocationCommand.ExecuteAsync()
    → Geoprocessing.ExecuteToolAsync("SelectLayerByLocation_management", params)
    → CommandResult { Success: true, Message: "347 features selected" }
```

---

## Threading Model

ArcGIS Pro SDK requires all map/layer/CIM operations to run on the Main CIM Thread (MCT) via `QueuedTask.Run()`. The ProPilot architecture handles this as follows:

| Operation | Thread | Mechanism |
|---|---|---|
| UI interaction (typing, clicking) | UI Thread | WPF standard |
| LLamaSharp inference (bundled) | Background thread | `Task.Run()` wrapping `executor.InferAsync()` |
| HTTP call to Ollama/OpenAI | Background thread | `HttpClient.SendAsync()` with `await` |
| MapContextBuilder (reading map state) | MCT | `QueuedTask.Run()` |
| SDK command execution (zoom, visibility, symbology) | MCT | `QueuedTask.Run()` |
| GP tool execution | Background (Pro manages) | `Geoprocessing.ExecuteToolAsync()` |
| JSON deserialization | Background thread | `System.Text.Json` |
| Model download (first run) | Background thread | `HttpClient.GetStreamAsync()` with progress |
| Model loading into memory | Background thread | `LLamaWeights.LoadFromFile()` — lazy on first command |

The CommandWindowViewModel orchestrates this:

```csharp
// Pseudocode for the submit flow
async Task OnSubmitCommand(string userInput)
{
    Status = "Parsing...";

    // 1. Build context on MCT
    var context = await QueuedTask.Run(() => _contextBuilder.BuildContextAsync());

    // 2. Call LLM on background thread (HttpClient is async)
    var parsed = await _llmClient.ParseCommandAsync(userInput, context);

    // 3. Resolve command + build preview (CPU-bound, background is fine)
    var command = _registry.Resolve(parsed);
    var preview = command.BuildPreview(parsed, context);

    // 4. Display preview (UI thread via property binding)
    CurrentPreview = preview;
    Status = "Ready to execute";

    // 5. User clicks Execute → run on MCT or via GP
    // (triggered by Execute button click)
}
```

---

## Configuration Architecture

### Settings Hierarchy

```
User types command
        │
        ▼
  Load settings:
    1. Check project custom properties → ProPilot.Settings
    2. Fallback to %APPDATA%\ProPilot\settings.json
    3. Fallback to hardcoded defaults
```

### ProPilotSettings Model

```csharp
public class ProPilotSettings
{
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";
    public string ModelName { get; set; } = "mistral";
    public int TimeoutSeconds { get; set; } = 30;
    public bool SaveHistoryAcrossSessions { get; set; } = false;
}
```

### Persistence Strategy

- **Project-level:** Serialized to project custom properties via `Project.Current.SetCustomProperty()`
- **User-level:** Serialized to `%APPDATA%\ProPilot\settings.json`
- **Settings UI** shows which level is active and allows saving to either

---

## Error Handling Strategy

| Error Condition | User Experience | Technical Response |
|---|---|---|
| Ollama not running | Status bar: "● Disconnected" + helpful message | `HttpRequestException` caught, retry with backoff |
| Model not found | "Model 'mistral' not found. Run: `ollama pull mistral`" | 404 from Ollama API |
| LLM timeout (>30s) | "Command timed out. Try a simpler command or check Ollama." | `TaskCanceledException` via CancellationToken |
| Invalid JSON response | "Could not parse response. Retrying..." (auto-retry once) | JSON deserialization failure, retry with same input |
| Unknown command | "I didn't understand that. Try: 'zoom to [layer]' or 'select where...'" | `command_type: "unknown"` in response |
| Low confidence (<0.5) | Preview shows warning: "Low confidence — please verify" | Yellow warning icon in preview |
| Layer not found | "Layer 'rivers' not found. Available: Streams_KY, NHD_Flowlines" | Validation failure in IMapCommand.Validate() |
| GP tool failure | "Buffer failed: [GP error message]" | Exception from `Geoprocessing.ExecuteToolAsync()` |
| No active map | "Open a map first, then try your command." | `MapView.Active == null` check |

---

## Testing Strategy

### Unit Tests (MSTest / xUnit)

- IntentParser: verify JSON deserialization for all 30 command types
- CommandResolver: verify command_type → IMapCommand mapping
- SystemPromptBuilder: verify context serialization format
- MapContext serialization/deserialization round-trip
- Settings persistence read/write/fallback

### Integration Tests

- OllamaClient: verify HTTP request/response with mock server
- End-to-end: natural language → parsed command → correct IMapCommand type (requires Ollama running)

### Manual Test Matrix

- Each of the 30 commands tested with 3-5 natural language variations
- Fuzzy matching: test with partial layer names, misspellings, abbreviations
- Edge cases: empty map, no layers, no selection, disconnected Ollama
- Performance: measure parse latency per command on target hardware

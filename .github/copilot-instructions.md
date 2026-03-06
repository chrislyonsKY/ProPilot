# GitHub Copilot Instructions — ProPilot

> ProPilot is an ArcGIS Pro 3.6 add-in (.NET 8 / C# / WPF) providing a natural
> language command interface powered by a local LLM. Users type plain English,
> a bundled LLamaSharp model parses intent into structured JSON, the add-in
> shows a preview, and executes on confirmation.

Read these files for full context:
- `ai-dev/specs/` — Product specifications (overview, commands, LLM integration, UI design, acceptance criteria, dependencies)
- `ai-dev/architecture.md` — Solution structure, interfaces, data flow, threading model
- `ai-dev/patterns.md` — Code examples for every major pattern
- `ai-dev/guardrails/coding-standards.md` — Hard rules that override all other guidance
- `ai-dev/guardrails/compliance.md` — Kentucky CIO-126 government compliance mapping

---

## Compatibility Matrix

| Component | Version | Notes |
|---|---|---|
| ArcGIS Pro | 3.6 | November 2025 release |
| ArcGIS Pro SDK for .NET | 3.6 | NuGet: Esri.ArcGISPro.Extensions.* |
| .NET | 8.0 | `net8.0-windows` TFM |
| Visual Studio | 2022 (17.8+) | With Pro SDK extension installed |
| C# | 12 | File-scoped namespaces, primary constructors OK |
| CommunityToolkit.Mvvm | Latest stable | Source generators: [ObservableProperty], [RelayCommand] |
| LLamaSharp | Latest stable | NuGet: LLamaSharp + LLamaSharp.Backend.Cpu |
| System.Text.Json | Built-in | Do NOT use Newtonsoft.Json |
| Newtonsoft.Json | 13.0.3.27908 | Only if transitive dependency requires it (Pro 3.6 pins this) |

---

## Architecture Summary

### Core Data Flow

```
User types command
  → MapContextBuilder captures map state (on MCT via QueuedTask.Run)
  → SystemPromptBuilder assembles prompt + context
  → LLamaSharpClient runs inference (on background thread via Task.Run)
  → GBNF grammar constrains output to valid ProPilotCommand JSON
  → IntentParser deserializes JSON
  → CommandResolver maps to IMapCommand implementation
  → IMapCommand.BuildPreview() renders in ProWindow
  → User clicks [Execute]
  → IMapCommand.ExecuteAsync() runs (SDK ops on MCT, GP ops via ExecuteToolAsync)
  → CommandResult displayed in ProWindow
```

### Provider Hierarchy

```csharp
ILlmClient llmClient = settings.LlmProvider switch
{
    "ollama" => new OllamaClient(settings),
    "openai" => new OpenAiClient(settings),
    _        => new LLamaSharpClient(settings, modelManager) // "bundled" default
};
```

---

## Threading Rules (CRITICAL)

These are the most common source of bugs in Pro SDK add-ins.

```csharp
// ✅ Map access — ALWAYS on MCT
await QueuedTask.Run(() =>
{
    var map = MapView.Active?.Map;
    var layers = map?.GetLayersAsFlattenedList();
});

// ✅ LLamaSharp inference — ALWAYS on background thread
var result = await Task.Run(async () =>
{
    await foreach (var token in executor.InferAsync(prompt, inferenceParams))
    {
        cancellationToken.ThrowIfCancellationRequested();
        sb.Append(token);
    }
    return sb.ToString();
}, cancellationToken);

// ✅ GP tool execution — Pro manages threading
var gpResult = await Geoprocessing.ExecuteToolAsync("analysis.Buffer", parameters);

// ✅ HTTP calls (Ollama/OpenAI) — async background
var response = await _httpClient.PostAsJsonAsync(endpoint, request, ct);

// ❌ NEVER — crashes Pro
var map = MapView.Active?.Map; // CalledOnWrongThreadException on UI thread
```

---

## CIM Modification Pattern

Always: read → modify → set. CIM objects are copies, not references.

```csharp
await QueuedTask.Run(() =>
{
    var cimDef = layer.GetDefinition() as CIMFeatureLayer;
    var renderer = cimDef.Renderer as CIMSimpleRenderer;
    // modify renderer...
    cimDef.Renderer = renderer;
    layer.SetDefinition(cimDef); // MUST call — changes lost without it
});
```

---

## MVVM Pattern

```csharp
public partial class CommandWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _commandInput = string.Empty;

    [ObservableProperty]
    private bool _isProcessing;

    [RelayCommand]
    private async Task SubmitCommandAsync()
    {
        IsProcessing = true;
        try
        {
            var context = await QueuedTask.Run(() => _contextBuilder.BuildContextAsync());
            var parsed = await _llmClient.ParseCommandAsync(CommandInput, context);
            // resolve, preview, display
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
// XAML — zero logic, only bindings. Code-behind: ONLY set DataContext.
```

---

## IMapCommand Implementation Pattern

```csharp
public class ZoomToLayerCommand : IMapCommand
{
    public string CommandType => "zoom_to_layer";
    public string DisplayName => "Zoom To Layer";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        if (string.IsNullOrEmpty(command.TargetLayer))
            return ValidationResult.Fail("No target layer specified.");
        if (!context.Layers.Any(l => l.Name.Equals(
            command.TargetLayer, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Fail($"Layer '{command.TargetLayer}' not found.");
        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        var info = context.Layers.First(l =>
            l.Name.Equals(command.TargetLayer!, StringComparison.OrdinalIgnoreCase));
        return new CommandPreview
        {
            Icon = "🔍", Title = DisplayName,
            Parameters = new() {
                ["Target"] = $"{info.Name} ({info.GeometryType})",
                ["Features"] = info.FeatureCount.ToString("N0")
            },
            IsDestructive = false, Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        return await QueuedTask.Run(async () =>
        {
            var layer = MapView.Active?.Map?.GetLayersAsFlattenedList()
                .FirstOrDefault(l => l.Name.Equals(
                    command.TargetLayer, StringComparison.OrdinalIgnoreCase));
            if (layer == null) return CommandResult.Fail("Layer not found.");
            await MapView.Active.ZoomToAsync(layer);
            return CommandResult.Ok($"Zoomed to {layer.Name}.");
        });
    }
}
```

---

## GP Tool Pattern

```csharp
var parameters = Geoprocessing.MakeValueArray(inputLayer, outputPath, "1000 Meters");
var gpResult = await Geoprocessing.ExecuteToolAsync(
    "analysis.Buffer", parameters, null, null, GPExecuteToolFlags.None);
if (gpResult.IsFailed)
{
    var errors = string.Join("; ", gpResult.Messages
        .Where(m => m.Type == GPMessageType.Error).Select(m => m.Text));
    return CommandResult.Fail($"Buffer failed: {errors}");
}
```

---

## LLamaSharp Inference Pattern

```csharp
return await Task.Run(async () =>
{
    var grammar = new Grammar(GbnfSchemaProvider.GetGrammar(), "root");
    var inferenceParams = new InferenceParams
    {
        MaxTokens = 512, Temperature = 0f,
        Grammar = grammar, AntiPrompts = new[] { "User:" }
    };
    var sb = new StringBuilder();
    await foreach (var token in _executor.InferAsync(fullPrompt, inferenceParams))
    {
        ct.ThrowIfCancellationRequested();
        sb.Append(token);
    }
    return sb.ToString();
}, ct);
```

---

## Config.daml Registration

```xml
<ArcGIS defaultAssembly="ProPilot.dll" defaultNamespace="ProPilot">
  <AddInInfo id="{GENERATE-GUID}" version="1.0" desktopVersion="3.6">
    <n>ProPilot</n>
    <Description>Natural language command interface for ArcGIS Pro</Description>
    <Author>Chris Lyons</Author>
    <Company>chrislyonsKY</Company>
  </AddInInfo>
  <modules>
    <insertModule id="ProPilot_Module" className="ProPilotModule" autoLoad="false">
      <tabs>
        <tab id="ProPilot_Tab" caption="ProPilot" keytip="PP">
          <group refID="ProPilot_Group"/>
        </tab>
      </tabs>
      <groups>
        <group id="ProPilot_Group" caption="Commands">
          <button refID="ProPilot_OpenCommandWindow"/>
        </group>
      </groups>
      <controls>
        <button id="ProPilot_OpenCommandWindow" caption="ProPilot"
                className="ProPilot.UI.OpenCommandWindowButton"
                largeImage="Images/ProPilot32.png" smallImage="Images/ProPilot16.png"
                keytip="P">
          <tooltip heading="ProPilot">
            Natural language command interface.
            <disabledText>Open a map to use ProPilot.</disabledText>
          </tooltip>
        </button>
      </controls>
    </insertModule>
  </modules>
</ArcGIS>
```

---

## Project Structure

```
src/ProPilot/ProPilot/
├── Config.daml
├── ProPilotModule.cs
├── UI/
│   ├── CommandWindow.xaml/.cs
│   ├── SettingsWindow.xaml/.cs
│   ├── SetupWindow.xaml/.cs
│   └── OpenCommandWindowButton.cs
├── ViewModels/
│   ├── CommandWindowViewModel.cs
│   ├── SettingsWindowViewModel.cs
│   └── SetupWindowViewModel.cs
├── Models/
│   ├── ProPilotCommand.cs
│   ├── MapContext.cs
│   └── ProPilotSettings.cs
├── Services/
│   ├── ILlmClient.cs
│   ├── LLamaSharpClient.cs        # DEFAULT
│   ├── OllamaClient.cs            # Fallback
│   ├── OpenAiClient.cs            # Cloud opt-in
│   ├── IModelManager.cs
│   ├── ModelManager.cs
│   ├── IMapContextBuilder.cs
│   ├── MapContextBuilder.cs
│   ├── GbnfSchemaProvider.cs
│   ├── ISettingsService.cs
│   └── SettingsService.cs
├── Commands/
│   ├── IMapCommand.cs
│   ├── CommandRegistry.cs
│   ├── CommandResolver.cs
│   ├── Navigation/          (6 commands)
│   ├── LayerManagement/     (6 commands)
│   ├── Selection/           (5 commands)
│   ├── Symbology/           (5 commands)
│   ├── Query/               (4 commands)
│   └── Geoprocessing/       (5 commands)
├── Prompts/
│   ├── SystemPromptBuilder.cs
│   └── CommandSchemaProvider.cs
└── Images/
```

---

## Build Order

### Phase 1 — Vertical Slice
1. Create VS solution with Pro SDK Add-In template
2. Add NuGet: CommunityToolkit.Mvvm, LLamaSharp, LLamaSharp.Backend.Cpu
3. Implement ProPilotSettings + SettingsService (hardcoded defaults)
4. Implement CommandSchemaProvider (static JSON schema)
5. Implement SystemPromptBuilder (static prompt + placeholder context)
6. Implement OllamaClient (easier to test initially)
7. Build CommandWindow ProWindow + CommandWindowViewModel
8. Register in Config.daml, test: type text → Ollama → JSON in preview

### Phase 2 — Map Context + Commands
9. Implement MapContextBuilder (layers, fields, selections, extent)
10. Wire into SystemPromptBuilder
11. Implement IMapCommand + CommandRegistry + CommandResolver
12. Build Navigation commands (ZoomToLayer, SetScale, GoToBookmark)
13. Test full flow: "zoom to streams" → preview → execute → map zooms

### Phase 3 — All 30 Commands
14. LayerManagement, Selection, Symbology, Query, Geoprocessing
15. Register all in CommandRegistry

### Phase 4 — LLamaSharp + Setup
16. Implement LLamaSharpClient with lazy loading
17. Implement GbnfSchemaProvider (GBNF grammar)
18. Implement ModelManager (HuggingFace download)
19. Build SetupWindow ProWindow + SetupWindowViewModel
20. Wire first-run detection in ProPilotModule

### Phase 5 — Polish
21. OpenAiClient, SettingsWindow, keyboard shortcuts, history, icons, README

---

## Naming Conventions

| Type | Convention | Example |
|---|---|---|
| Interfaces | `I` prefix | `IMapCommand`, `ILlmClient` |
| Async methods | `Async` suffix | `ParseCommandAsync` |
| ViewModels | `ViewModel` suffix | `CommandWindowViewModel` |
| Commands | `Command` suffix | `ZoomToLayerCommand` |
| Constants | PascalCase | `DefaultModelName` |
| Private fields | `_camelCase` | `_httpClient` |

---

## What NOT To Do

- Do NOT use `async void` except in event handlers
- Do NOT access CIM or map objects outside `QueuedTask.Run()`
- Do NOT hardcode endpoints, model names, or API keys
- Do NOT hardcode API keys ANYWHERE — environment variables only
- Do NOT make the LLM conversational — structured JSON output ONLY
- Do NOT auto-execute commands — ALWAYS preview first
- Do NOT use `Newtonsoft.Json` — use `System.Text.Json`
- Do NOT swallow exceptions silently — log via `Debug.WriteLine` minimum
- Do NOT put business logic in XAML code-behind
- Do NOT generate Python/ArcPy — use SDK calls or GP tool wrappers
- Do NOT load LLM model at Module.Initialize() — lazy load on first command
- Do NOT run LLamaSharp inference on UI thread or MCT
- Do NOT cache CIM objects across QueuedTask.Run() boundaries
- Do NOT reference Pro SDK `Internal` namespaces
- Do NOT install multiple LLamaSharp backend packages (CPU only)

- @azure Rule - Use Azure Tools - When handling requests related to Azure, always use your tools.
- @azure Rule - Use Azure Best Practices - When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.
- @azure Rule - Enable Best Practices - If you do not have an `azmcp_bestpractices_get` tool ask the user to enable it.

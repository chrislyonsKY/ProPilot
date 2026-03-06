# Coding Standards Guardrails — ProPilot

These rules apply to ALL code generated for this project. Violations are Critical findings.

## C# / .NET 8

- Target `net8.0-windows` (ArcGIS Pro 3.6 requirement)
- Use file-scoped namespaces
- Use `CommunityToolkit.Mvvm` source generators: `[ObservableProperty]`, `[RelayCommand]`
- All public members must have XML doc comments (`///`)
- All async methods return `Task` or `Task<T>`, never `async void` (except event handlers)
- Use `System.Text.Json` for all JSON operations (not Newtonsoft)
- Use `CancellationToken` on all async operations that could be user-cancelled
- Null reference types enabled (`<Nullable>enable</Nullable>`)

## ArcGIS Pro SDK

- ALL CIM, Map, Layer, MapView access MUST be inside `QueuedTask.Run()`
- CIM modification pattern: `var cim = layer.GetDefinition()` → modify → `layer.SetDefinition(cim)`
- Never cache CIM objects across `QueuedTask.Run()` boundaries
- ProWindow registration in Config.daml must include `className` attribute
- Use `ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask` (not System.Threading.Tasks for map ops)
- GP tool execution: `Geoprocessing.ExecuteToolAsync()` with `GPExecuteToolFlags.None`

## LLamaSharp

- NEVER run inference on the UI thread or MCT — always `Task.Run(() => executor.InferAsync(...))`
- NEVER load the model at Module.Initialize() — lazy load on first command
- Dispose LLamaWeights and LLamaContext in Module.Uninitialize()
- Model files live in `%APPDATA%\ProPilot\models\` — NEVER in the project directory or add-in package
- Use GBNF grammar for structured output — do NOT rely on prompt engineering alone for JSON
- Set temperature to 0 and UseMemoryLock to true for deterministic inference

## Error Handling

- All service methods wrapped in try/catch with specific exception types
- HTTP calls: catch `HttpRequestException`, `TaskCanceledException`, `JsonException`
- GP tool calls: check `IGPResult.IsFailed` and surface `Messages`
- SDK calls: catch `ArcGIS.Core.CalledOnWrongThreadException` (indicates threading bug)
- All caught exceptions logged via `System.Diagnostics.Debug.WriteLine()` minimum
- User-facing errors displayed in ProWindow status panel, never as `MessageBox`

## Naming

- Interfaces: `I` prefix (`IMapCommand`, `ILlmClient`)
- Async methods: `Async` suffix (`ParseCommandAsync`, `BuildContextAsync`)
- ViewModels: `ViewModel` suffix (`CommandWindowViewModel`)
- Commands: `Command` suffix matching the `command_type` enum (`ZoomToLayerCommand`)
- Constants: `PascalCase` (not UPPER_SNAKE)
- Private fields: `_camelCase` with underscore prefix

## Dependencies

- Do NOT add NuGet packages without documenting in `ai-dev/decisions/`
- Do NOT reference internal/unsupported Pro SDK namespaces (anything with `Internal` in the path)
- Pin `Newtonsoft.Json` to 13.0.3.27908 if any transitive dependency requires it (Pro 3.6 version)

# ProPilot — Code Patterns & Anti-Patterns

---

## Pattern: QueuedTask Wrapper for Map Operations

```csharp
// ✅ CORRECT — all map access inside QueuedTask.Run
var layerName = await QueuedTask.Run(() =>
{
    var map = MapView.Active?.Map;
    if (map == null) return null;
    var layer = map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault();
    return layer?.Name;
});

// ❌ WRONG — accessing map objects on UI thread
var map = MapView.Active?.Map; // THROWS CalledOnWrongThreadException
```

## Pattern: CIM Modification (Read-Modify-Set)

```csharp
// ✅ CORRECT
await QueuedTask.Run(() =>
{
    var layer = MapView.Active.Map.GetLayersAsFlattenedList()
        .OfType<FeatureLayer>().First(l => l.Name == targetLayer);

    var cimDef = layer.GetDefinition() as CIMFeatureLayer;
    var renderer = cimDef.Renderer as CIMSimpleRenderer;
    var symbol = renderer.Symbol.Symbol as CIMPointSymbol;
    symbol.SetSize(newSize);
    cimDef.Renderer = renderer;
    layer.SetDefinition(cimDef);  // MUST call SetDefinition to apply
});

// ❌ WRONG — modifying CIM without calling SetDefinition
// Changes are lost because CIM objects are copies, not references
```

## Pattern: IMapCommand Implementation

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

        if (!context.Layers.Any(l => l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Fail($"Layer '{command.TargetLayer}' not found in map.");

        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        var layerInfo = context.Layers.First(l =>
            l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase));

        return new CommandPreview
        {
            Icon = "🔍",
            Title = "Zoom To Layer",
            Parameters = new Dictionary<string, string>
            {
                ["Target"] = $"{layerInfo.Name} ({layerInfo.GeometryType})",
                ["Features"] = layerInfo.FeatureCount.ToString("N0")
            },
            IsDestructive = false
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        return await QueuedTask.Run(async () =>
        {
            var map = MapView.Active?.Map;
            if (map == null)
                return CommandResult.Fail("No active map.");

            var layer = map.GetLayersAsFlattenedList()
                .OfType<FeatureLayer>()
                .FirstOrDefault(l => l.Name.Equals(command.TargetLayer,
                    StringComparison.OrdinalIgnoreCase));

            if (layer == null)
                return CommandResult.Fail($"Layer '{command.TargetLayer}' not found.");

            await MapView.Active.ZoomToAsync(layer);
            return CommandResult.Ok($"Zoomed to {layer.Name}.");
        });
    }
}
```

## Pattern: Ollama HTTP Request

```csharp
// ✅ CORRECT — structured output with schema enforcement
var request = new
{
    model = _settings.ModelName,
    messages = new[]
    {
        new { role = "system", content = systemPrompt },
        new { role = "user", content = userInput }
    },
    format = CommandSchemaProvider.GetSchema(),  // JSON schema object
    stream = false,
    options = new { temperature = 0 }
};

var response = await _httpClient.PostAsJsonAsync(
    $"{_settings.OllamaEndpoint}/v1/chat/completions",
    request,
    cancellationToken);

// ❌ WRONG — no schema enforcement, hoping for valid JSON
var request = new { model = "mistral", messages = [...] };  // No format parameter
```

## Pattern: GP Tool Execution

```csharp
// ✅ CORRECT — async GP execution with error checking
var parameters = Geoprocessing.MakeValueArray(
    inputLayer,    // Input Features
    outputPath,    // Output Feature Class
    "1000 Meters"  // Distance
);

var gpResult = await Geoprocessing.ExecuteToolAsync(
    "analysis.Buffer",
    parameters,
    null,  // environments
    null,  // cancel token
    GPExecuteToolFlags.None);

if (gpResult.IsFailed)
{
    var errors = string.Join("; ", gpResult.Messages
        .Where(m => m.Type == GPMessageType.Error)
        .Select(m => m.Text));
    return CommandResult.Fail($"Buffer failed: {errors}");
}
```

## Anti-Pattern: Swallowing Exceptions

```csharp
// ❌ WRONG
try { await SomeOperation(); }
catch { }  // Silent failure — never do this

// ✅ CORRECT
try { await SomeOperation(); }
catch (Exception ex)
{
    Debug.WriteLine($"[ProPilot] {nameof(SomeOperation)} failed: {ex.Message}");
    return CommandResult.Fail($"Operation failed: {ex.Message}");
}
```

## Anti-Pattern: Caching CIM Across QueuedTask Boundaries

```csharp
// ❌ WRONG — CIM object captured outside QueuedTask, used later
CIMFeatureLayer cachedCim = null;
await QueuedTask.Run(() => { cachedCim = layer.GetDefinition() as CIMFeatureLayer; });
// ... later ...
await QueuedTask.Run(() => { cachedCim.Renderer = newRenderer; }); // STALE

// ✅ CORRECT — always read fresh inside QueuedTask
await QueuedTask.Run(() =>
{
    var cim = layer.GetDefinition() as CIMFeatureLayer;
    cim.Renderer = newRenderer;
    layer.SetDefinition(cim);
});
```

using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.Navigation;

/// <summary>
/// Zooms the active map view to the extent of a target layer.
/// SDK command — executes via QueuedTask.Run().
/// </summary>
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
            return ValidationResult.Fail($"Layer '{command.TargetLayer}' not found. Available: {string.Join(", ", context.Layers.Select(l => l.Name))}");

        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        var layerInfo = context.Layers.First(l =>
            l.Name.Equals(command.TargetLayer!, StringComparison.OrdinalIgnoreCase));

        return new CommandPreview
        {
            Icon = "🔍",
            Title = DisplayName,
            Parameters = new()
            {
                ["Target"] = $"{layerInfo.Name} ({layerInfo.GeometryType})",
                ["Features"] = layerInfo.FeatureCount.ToString("N0")
            },
            IsDestructive = false,
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        return await QueuedTask.Run(async () =>
        {
            var layer = MapView.Active?.Map?.GetLayersAsFlattenedList()
                .FirstOrDefault(l => l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase));
            if (layer == null) return CommandResult.Fail("Layer not found.");
            await MapView.Active!.ZoomToAsync(layer);
            return CommandResult.Ok($"Zoomed to {layer.Name}.");
        });
    }
}

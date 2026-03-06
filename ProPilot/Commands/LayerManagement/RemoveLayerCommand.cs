using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.LayerManagement;

public class RemoveLayerCommand : IMapCommand
{
    public string CommandType => "remove_layer";
    public string DisplayName => "Remove Layer";
    public bool IsDestructive => true;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        if (string.IsNullOrEmpty(command.TargetLayer))
            return ValidationResult.Fail("No target layer specified.");
        if (!context.Layers.Any(l => l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Fail($"Layer '{command.TargetLayer}' not found.");
        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        var info = context.Layers.First(l =>
            l.Name.Equals(command.TargetLayer!, StringComparison.OrdinalIgnoreCase));
        return new CommandPreview
        {
            Icon = "🗑",
            Title = DisplayName,
            Parameters = new()
            {
                ["Layer"] = $"{info.Name} ({info.GeometryType})",
                ["Features"] = info.FeatureCount.ToString("N0")
            },
            IsDestructive = true,
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        return await QueuedTask.Run(() =>
        {
            var map = MapView.Active?.Map;
            if (map == null) return CommandResult.Fail("No active map.");

            var layer = map.GetLayersAsFlattenedList()
                .FirstOrDefault(l => l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase));
            if (layer == null) return CommandResult.Fail("Layer not found.");

            var name = layer.Name;
            map.RemoveLayer(layer);
            return CommandResult.Ok($"Removed layer '{name}' from the map.");
        });
    }
}

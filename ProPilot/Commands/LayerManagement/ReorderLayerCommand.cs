using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.LayerManagement;

public class ReorderLayerCommand : IMapCommand
{
    public string CommandType => "reorder_layer";
    public string DisplayName => "Reorder Layer";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        if (string.IsNullOrEmpty(command.TargetLayer))
            return ValidationResult.Fail("No target layer specified.");
        if (!context.Layers.Any(l => l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Fail($"Layer '{command.TargetLayer}' not found.");
        var pos = command.Parameters?.Position;
        if (string.IsNullOrEmpty(pos) || (!pos.Equals("top", StringComparison.OrdinalIgnoreCase)
            && !pos.Equals("bottom", StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Fail("Position must be 'top' or 'bottom'.");
        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        return new CommandPreview
        {
            Icon = "?",
            Title = DisplayName,
            Parameters = new()
            {
                ["Layer"] = command.TargetLayer!,
                ["Position"] = command.Parameters!.Position!
            },
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        var pos = command.Parameters!.Position!;
        return await QueuedTask.Run(() =>
        {
            var map = MapView.Active?.Map;
            if (map == null) return CommandResult.Fail("No active map.");

            var layer = map.GetLayersAsFlattenedList()
                .FirstOrDefault(l => l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase));
            if (layer == null) return CommandResult.Fail("Layer not found.");

            var index = pos.Equals("top", StringComparison.OrdinalIgnoreCase) ? 0 : -1;
            map.MoveLayer(layer, index);
            return CommandResult.Ok($"Moved {layer.Name} to {pos}.");
        });
    }
}

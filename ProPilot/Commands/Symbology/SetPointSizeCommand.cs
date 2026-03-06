using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.Symbology;

public class SetPointSizeCommand : IMapCommand
{
    public string CommandType => "set_point_size";
    public string DisplayName => "Set Point Size";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        if (string.IsNullOrEmpty(command.TargetLayer))
            return ValidationResult.Fail("No target layer specified.");
        if (!context.Layers.Any(l => l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Fail($"Layer '{command.TargetLayer}' not found.");
        if (command.Parameters?.Value == null || command.Parameters.Value <= 0)
            return ValidationResult.Fail("No valid point size specified (must be > 0).");
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
                ["Size"] = $"{command.Parameters!.Value!.Value:F1} pt"
            },
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        var size = command.Parameters!.Value!.Value;
        return await QueuedTask.Run(() =>
        {
            var layer = MapView.Active?.Map?.GetLayersAsFlattenedList()
                .OfType<FeatureLayer>()
                .FirstOrDefault(l => l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase));
            if (layer == null) return CommandResult.Fail("Layer not found.");

            var cimDef = layer.GetDefinition() as CIMFeatureLayer;
            if (cimDef?.Renderer is CIMSimpleRenderer renderer
                && renderer.Symbol?.Symbol is CIMPointSymbol pointSymbol)
            {
                foreach (var marker in pointSymbol.SymbolLayers.OfType<CIMVectorMarker>())
                    marker.Size = size;
                foreach (var marker in pointSymbol.SymbolLayers.OfType<CIMCharacterMarker>())
                    marker.Size = size;
                layer.SetDefinition(cimDef);
                return CommandResult.Ok($"Set {layer.Name} point size to {size:F1} pt.");
            }

            return CommandResult.Fail("Layer does not have a simple point renderer.");
        });
    }
}

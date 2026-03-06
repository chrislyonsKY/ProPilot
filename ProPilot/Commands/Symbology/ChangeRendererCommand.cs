using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.Symbology;

public class ChangeRendererCommand : IMapCommand
{
    public string CommandType => "change_renderer";
    public string DisplayName => "Change Renderer";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        if (string.IsNullOrEmpty(command.TargetLayer))
            return ValidationResult.Fail("No target layer specified.");
        if (!context.Layers.Any(l =>
            l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Fail($"Layer '{command.TargetLayer}' not found.");
        if (string.IsNullOrEmpty(command.Parameters?.RendererType))
            return ValidationResult.Fail("No renderer type specified.");
        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        return new CommandPreview
        {
            Icon = "??", Title = DisplayName,
            Parameters = new()
            {
                ["Layer"] = command.TargetLayer!,
                ["Renderer"] = command.Parameters!.RendererType!,
                ["Field"] = command.Parameters.FieldName ?? "(default)"
            },
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        return await QueuedTask.Run(() =>
        {
            var layer = MapView.Active?.Map?.GetLayersAsFlattenedList()
                .OfType<FeatureLayer>()
                .FirstOrDefault(l =>
                    l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase));
            if (layer == null) return CommandResult.Fail("Layer not found.");

            var cimDef = layer.GetDefinition() as CIMFeatureLayer;
            if (cimDef == null) return CommandResult.Fail("Cannot read layer definition.");

            var type = command.Parameters!.RendererType!;
            if (type.Equals("simple", StringComparison.OrdinalIgnoreCase))
            {
                var existingSymRef = (cimDef.Renderer as CIMSimpleRenderer)?.Symbol;
                cimDef.Renderer = new CIMSimpleRenderer
                {
                    Symbol = existingSymRef ?? SymbolFactory.Instance
                        .ConstructPointSymbol(CIMColor.CreateRGBColor(0, 0, 255))
                        .MakeSymbolReference()
                };
                layer.SetDefinition(cimDef);
                return CommandResult.Ok($"Changed {layer.Name} to simple renderer.");
            }

            return CommandResult.Fail(
                $"Renderer type '{type}' requires manual configuration in the Pro UI.");
        });
    }
}

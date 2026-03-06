using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.Symbology;

public class ChangeColorCommand : IMapCommand
{
    public string CommandType => "change_color";
    public string DisplayName => "Change Color";
    public bool IsDestructive => false;

    private static readonly Dictionary<string, CIMColor> NamedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["red"] = CIMColor.CreateRGBColor(255, 0, 0),
        ["green"] = CIMColor.CreateRGBColor(0, 128, 0),
        ["blue"] = CIMColor.CreateRGBColor(0, 0, 255),
        ["yellow"] = CIMColor.CreateRGBColor(255, 255, 0),
        ["orange"] = CIMColor.CreateRGBColor(255, 165, 0),
        ["purple"] = CIMColor.CreateRGBColor(128, 0, 128),
        ["black"] = CIMColor.CreateRGBColor(0, 0, 0),
        ["white"] = CIMColor.CreateRGBColor(255, 255, 255),
        ["gray"] = CIMColor.CreateRGBColor(128, 128, 128),
        ["brown"] = CIMColor.CreateRGBColor(139, 69, 19),
        ["cyan"] = CIMColor.CreateRGBColor(0, 255, 255),
        ["magenta"] = CIMColor.CreateRGBColor(255, 0, 255),
        ["pink"] = CIMColor.CreateRGBColor(255, 192, 203),
    };

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        if (string.IsNullOrEmpty(command.TargetLayer))
            return ValidationResult.Fail("No target layer specified.");
        if (!context.Layers.Any(l => l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Fail($"Layer '{command.TargetLayer}' not found.");
        if (string.IsNullOrEmpty(command.Parameters?.Color))
            return ValidationResult.Fail("No color specified.");
        if (!NamedColors.ContainsKey(command.Parameters.Color))
            return ValidationResult.Fail($"Unknown color '{command.Parameters.Color}'. Available: {string.Join(", ", NamedColors.Keys)}");
        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        return new CommandPreview
        {
            Icon = "??",
            Title = DisplayName,
            Parameters = new()
            {
                ["Layer"] = command.TargetLayer!,
                ["Color"] = command.Parameters!.Color!
            },
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        var colorName = command.Parameters!.Color!;
        if (!NamedColors.TryGetValue(colorName, out var cimColor))
            return CommandResult.Fail($"Unknown color '{colorName}'.");

        return await QueuedTask.Run(() =>
        {
            var layer = MapView.Active?.Map?.GetLayersAsFlattenedList()
                .OfType<FeatureLayer>()
                .FirstOrDefault(l => l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase));
            if (layer == null) return CommandResult.Fail("Layer not found.");

            // CIM read-modify-set
            var cimDef = layer.GetDefinition() as CIMFeatureLayer;
            if (cimDef?.Renderer is CIMSimpleRenderer simpleRenderer)
            {
                SetSymbolColor(simpleRenderer.Symbol?.Symbol, cimColor);
                layer.SetDefinition(cimDef);
                return CommandResult.Ok($"Changed {layer.Name} color to {colorName}.");
            }

            return CommandResult.Fail("Layer does not use a simple renderer. Change renderer first.");
        });
    }

    private static void SetSymbolColor(CIMSymbol? symbol, CIMColor color)
    {
        if (symbol is CIMPointSymbol pointSymbol)
        {
            foreach (var sl in pointSymbol.SymbolLayers.OfType<CIMVectorMarker>())
                foreach (var mg in sl.MarkerGraphics ?? [])
                    if (mg.Symbol is CIMPolygonSymbol polyFill)
                        foreach (var layer in polyFill.SymbolLayers.OfType<CIMSolidFill>())
                            layer.Color = color;
        }
        else if (symbol is CIMLineSymbol lineSymbol)
        {
            foreach (var sl in lineSymbol.SymbolLayers.OfType<CIMSolidStroke>())
                sl.Color = color;
        }
        else if (symbol is CIMPolygonSymbol polygonSymbol)
        {
            foreach (var sl in polygonSymbol.SymbolLayers.OfType<CIMSolidFill>())
                sl.Color = color;
        }
    }
}

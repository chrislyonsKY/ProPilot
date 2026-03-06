using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Mapping;

namespace ProPilot.Commands.Selection;

public class SelectByLocationCommand : IMapCommand
{
    public string CommandType => "select_by_location";
    public string DisplayName => "Select By Location";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        if (string.IsNullOrEmpty(command.TargetLayer))
            return ValidationResult.Fail("No target layer specified.");
        if (!context.Layers.Any(l => l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Fail($"Layer '{command.TargetLayer}' not found.");
        var source = command.Parameters?.SourceLayer;
        if (string.IsNullOrEmpty(source))
            return ValidationResult.Fail("No source layer specified for spatial relationship.");
        if (!context.Layers.Any(l => l.Name.Equals(source, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Fail($"Source layer '{source}' not found.");
        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        var relationship = command.Parameters?.SpatialRelationship ?? "INTERSECT";
        return new CommandPreview
        {
            Icon = "??",
            Title = DisplayName,
            Parameters = new()
            {
                ["Target"] = command.TargetLayer!,
                ["Source"] = command.Parameters!.SourceLayer!,
                ["Relationship"] = relationship
            },
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        var relationship = command.Parameters?.SpatialRelationship ?? "INTERSECT";
        var parameters = ArcGIS.Desktop.Core.Geoprocessing.Geoprocessing.MakeValueArray(
            command.TargetLayer,
            relationship,
            command.Parameters!.SourceLayer);

        var gpResult = await ArcGIS.Desktop.Core.Geoprocessing.Geoprocessing.ExecuteToolAsync(
            "management.SelectLayerByLocation", parameters, null, null,
            GPExecuteToolFlags.None);

        if (gpResult.IsFailed)
        {
            var errors = string.Join("; ", gpResult.Messages
                .Where(m => m.Type == GPMessageType.Error).Select(m => m.Text));
            return CommandResult.Fail($"Select by location failed: {errors}");
        }

        return CommandResult.Ok($"Selected features in {command.TargetLayer} that {relationship} {command.Parameters.SourceLayer}.");
    }
}

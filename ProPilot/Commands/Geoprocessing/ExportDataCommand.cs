using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Core.Geoprocessing;

namespace ProPilot.Commands.Geoprocessing;

public class ExportDataCommand : IMapCommand
{
    public string CommandType => "export_data";
    public string DisplayName => "Export Data";
    public bool IsDestructive => false;

    public ValidationResult Validate(ProPilotCommand command, MapContext context)
    {
        if (string.IsNullOrEmpty(command.TargetLayer))
            return ValidationResult.Fail("No target layer specified.");
        if (!context.Layers.Any(l => l.Name.Equals(command.TargetLayer, StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Fail($"Layer '{command.TargetLayer}' not found.");
        if (string.IsNullOrEmpty(command.Parameters?.OutputPath))
            return ValidationResult.Fail("No output path specified.");
        return ValidationResult.Success();
    }

    public CommandPreview BuildPreview(ProPilotCommand command, MapContext context)
    {
        var info = context.Layers.First(l =>
            l.Name.Equals(command.TargetLayer!, StringComparison.OrdinalIgnoreCase));
        return new CommandPreview
        {
            Icon = "💾", Title = DisplayName,
            Parameters = new()
            {
                ["Input"] = $"{info.Name} ({info.FeatureCount:N0} features)",
                ["Output"] = command.Parameters!.OutputPath!
            },
            Confidence = command.Confidence
        };
    }

    public async Task<CommandResult> ExecuteAsync(ProPilotCommand command, MapContext context)
    {
        var gp = ArcGIS.Desktop.Core.Geoprocessing.Geoprocessing.MakeValueArray(
            command.TargetLayer, command.Parameters!.OutputPath);
        var result = await ArcGIS.Desktop.Core.Geoprocessing.Geoprocessing.ExecuteToolAsync(
            "conversion.ExportFeatures", gp, null, null, GPExecuteToolFlags.None);
        if (result.IsFailed)
        {
            var err = string.Join("; ", result.Messages
                .Where(m => m.Type == GPMessageType.Error).Select(m => m.Text));
            return CommandResult.Fail($"Export failed: {err}");
        }
        return CommandResult.Ok(
            $"Exported {command.TargetLayer} to {command.Parameters.OutputPath}.");
    }
}
